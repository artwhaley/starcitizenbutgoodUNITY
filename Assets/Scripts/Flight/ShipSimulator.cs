using System.Collections.Generic;
using UnityEngine;

namespace FlightModel
{
    public static class ShipSimulator
    {
        const float NoInputThreshold = 0.05f;
        const float LinearVelocityLockMps = 0.05f;
        const float AngularVelocityLockRad = 0.01f;

        public static void Step(
            ref ShipState state,
            ShipTuning tuning,
            IReadOnlyList<IShipControlRequestSource> externalRequestSources,
            float deltaSeconds,
            in ShipInputCommand input,
            out ShipSimulationTelemetry telemetry)
        {
            telemetry = default;

            if (deltaSeconds <= 0f || tuning == null)
            {
                return;
            }

            if (state.currentMassKg <= 0f)
            {
                state.currentMassKg = tuning.dryMassKg + state.remainingFuelKg + state.remainingHypergolicKg;
            }

            ShipControlRequest pilotRequest = ShipControlRequest.FromPilot(input);
            ShipControlRequest assistRequest = ShipControlRequestPipeline.BuildAssistRequest(state, tuning, pilotRequest, deltaSeconds);
            ShipControlRequest brakeRequest = ShipControlRequestPipeline.BuildBrakeRequest(state, tuning, pilotRequest, deltaSeconds);

            ShipControlRequest mergedRequest = ShipControlRequestPipeline.MergeAll(
                pilotRequest, assistRequest, brakeRequest, externalRequestSources, state, tuning);

            state.boostActive = mergedRequest.boost;
            state.fineControlActive = mergedRequest.fineControl;

            float massRatio = tuning.dryMassKg / Mathf.Max(1f, state.currentMassKg);
            float maxLinearSpeed = GetActiveMaxLinearSpeed(mergedRequest, tuning);
            float linearAccelScale = GetActiveLinearAccelScale(mergedRequest, tuning);
            float angularSpeedScale = GetActiveAngularSpeedScale(mergedRequest, tuning);
            float angularAccelScale = GetActiveAngularAccelScale(mergedRequest, tuning);

            Vector3 requestedLocalLinear = new(
                mergedRequest.linearRight,
                mergedRequest.linearUp,
                mergedRequest.linearForward);
            Vector3 requestedLocalAngular = new(
                mergedRequest.angularPitch,
                mergedRequest.angularYaw,
                mergedRequest.angularRoll);

            Vector3 localLinearAccel = new(
                ResolveSignedLinearAccel(requestedLocalLinear.x, tuning.rightAccel, tuning.leftAccel, linearAccelScale) * massRatio,
                ResolveSignedLinearAccel(requestedLocalLinear.y, tuning.upAccel, tuning.downAccel, linearAccelScale) * massRatio,
                ResolveSignedForwardAccel(requestedLocalLinear.z, mergedRequest, tuning) * massRatio);

            Vector3 localAngularAccel = new(
                ResolveSignedAngularAccel(requestedLocalAngular.x, tuning.pitchPositiveAccel, tuning.pitchNegativeAccel, angularAccelScale) * massRatio,
                ResolveSignedAngularAccel(requestedLocalAngular.y, tuning.yawPositiveAccel, tuning.yawNegativeAccel, angularAccelScale) * massRatio,
                ResolveSignedAngularAccel(requestedLocalAngular.z, tuning.rollPositiveAccel, tuning.rollNegativeAccel, angularAccelScale) * massRatio);

            bool mainEngineFuelBlocked = !ShipPropellantAccounting.HasMainEngineFuel(state)
                && !mergedRequest.fineControl
                && mergedRequest.linearForward > 0f;
            bool hypergolicBlocked = !ShipPropellantAccounting.HasHypergolic(state);

            if (mainEngineFuelBlocked && localLinearAccel.z > 0f)
            {
                localLinearAccel.z = 0f;
            }

            if (hypergolicBlocked)
            {
                localLinearAccel.x = 0f;
                localLinearAccel.y = 0f;
                if (mergedRequest.fineControl || mergedRequest.linearForward <= 0f)
                {
                    localLinearAccel.z = 0f;
                }

                localAngularAccel = Vector3.zero;
            }

            Vector3 velocityBefore = state.linearVelocity;
            state.linearVelocity += state.rotation * localLinearAccel * deltaSeconds;
            Vector3 velocityBeforeCap = state.linearVelocity;
            state.linearVelocity = ClampWorldLinearSpeed(state.linearVelocity, maxLinearSpeed);
            bool linearSpeedCapped = state.linearVelocity.sqrMagnitude + 0.01f < velocityBeforeCap.sqrMagnitude;

            if (linearSpeedCapped)
            {
                Vector3 actualWorldAccel = (state.linearVelocity - velocityBefore) / deltaSeconds;
                localLinearAccel = Quaternion.Inverse(state.rotation) * actualWorldAccel;
            }

            state.angularVelocityRadians += localAngularAccel * deltaSeconds;
            state.angularVelocityRadians = ClampAngularVelocity(state.angularVelocityRadians, angularSpeedScale, tuning);
            ApplyAssistVelocityLocks(ref state, pilotRequest);

            state.position += state.linearVelocity * deltaSeconds;
            state.rotation = IntegrateRotation(state.rotation, state.angularVelocityRadians, deltaSeconds);

            ShipPropellantAccounting.BurnResult burn = ShipPropellantAccounting.ComputeBurn(
                tuning, mergedRequest, localLinearAccel, localAngularAccel, state.currentMassKg, deltaSeconds);
            ShipPropellantAccounting.ApplyBurn(ref state, burn);
            state.currentMassKg = tuning.dryMassKg + state.remainingFuelKg + state.remainingHypergolicKg;

            ShipThrusterOutput thrusterOutput = BuildThrusterOutput(
                tuning, mergedRequest, localLinearAccel, localAngularAccel, linearAccelScale, angularAccelScale, massRatio);
            ShipInputCommand appliedThrusterCommand = thrusterOutput.ToMatcherCommand();

            state.appliedOutput.requestedLocalLinear = requestedLocalLinear;
            state.appliedOutput.requestedLocalAngular = requestedLocalAngular;
            state.appliedOutput.appliedLocalLinear = localLinearAccel;
            state.appliedOutput.appliedLocalAngular = localAngularAccel;
            state.appliedOutput.thrusters = thrusterOutput;
            state.appliedOutput.mainEngineFuelBlocked = mainEngineFuelBlocked;
            state.appliedOutput.hypergolicBlocked = hypergolicBlocked;
            state.appliedOutput.linearSpeedCapped = linearSpeedCapped;

            telemetry = new ShipSimulationTelemetry
            {
                pilotRequest = pilotRequest,
                assistRequest = assistRequest,
                brakeRequest = brakeRequest,
                mergedControlRequest = mergedRequest,
                thrusterOutput = thrusterOutput,
                appliedThrusterCommand = appliedThrusterCommand
            };
        }

        static ShipThrusterOutput BuildThrusterOutput(
            ShipTuning tuning,
            in ShipControlRequest request,
            Vector3 localLinearAccel,
            Vector3 localAngularAccel,
            float linearAccelScale,
            float angularAccelScale,
            float massRatio)
        {
            float NormalizeLinear(float accel, float authority)
                => authority <= 1e-5f ? 0f : Mathf.Clamp01(Mathf.Abs(accel) / (authority * massRatio * Mathf.Max(linearAccelScale, 1e-4f)));

            float NormalizeAngular(float accel, float positiveAuthority, float negativeAuthority)
            {
                float authority = accel >= 0f ? positiveAuthority : negativeAuthority;
                return authority <= 1e-5f
                    ? 0f
                    : Mathf.Clamp01(Mathf.Abs(accel) / (authority * massRatio * Mathf.Max(angularAccelScale, 1e-4f)));
            }

            float forwardAuthority = request.fineControl
                ? tuning.maneuverForwardAccel
                : request.boost ? tuning.mainEngineForwardAccel * tuning.boostAccelMultiplier : tuning.mainEngineForwardAccel;

            float mainForward = 0f;
            float maneuverForward = 0f;
            if (request.fineControl || request.linearForward <= 0f)
            {
                float strength = NormalizeLinear(localLinearAccel.z, request.linearForward <= 0f ? tuning.reverseAccel : tuning.maneuverForwardAccel);
                maneuverForward = Mathf.Sign(localLinearAccel.z) * strength;
            }
            else if (localLinearAccel.z > 0f)
            {
                mainForward = NormalizeLinear(localLinearAccel.z, forwardAuthority);
            }

            return new ShipThrusterOutput
            {
                mainEngineForward = mainForward,
                maneuverForward = maneuverForward,
                maneuverRight = Mathf.Sign(localLinearAccel.x) * NormalizeLinear(localLinearAccel.x, localLinearAccel.x >= 0f ? tuning.rightAccel : tuning.leftAccel),
                maneuverUp = Mathf.Sign(localLinearAccel.y) * NormalizeLinear(localLinearAccel.y, localLinearAccel.y >= 0f ? tuning.upAccel : tuning.downAccel),
                rcsPitch = Mathf.Sign(localAngularAccel.x) * NormalizeAngular(localAngularAccel.x, tuning.pitchPositiveAccel, tuning.pitchNegativeAccel),
                rcsYaw = Mathf.Sign(localAngularAccel.y) * NormalizeAngular(localAngularAccel.y, tuning.yawPositiveAccel, tuning.yawNegativeAccel),
                rcsRoll = Mathf.Sign(localAngularAccel.z) * NormalizeAngular(localAngularAccel.z, tuning.rollPositiveAccel, tuning.rollNegativeAccel),
                boostActive = request.boost,
                fineControlActive = request.fineControl,
                brakeActive = request.brake
            };
        }

        static float GetActiveMaxLinearSpeed(in ShipControlRequest request, ShipTuning tuning)
        {
            if (request.fineControl)
            {
                return tuning.fineControlMaxLinearSpeedMps;
            }

            return request.boost ? tuning.boostMaxLinearSpeedMps : tuning.maxLinearSpeedMps;
        }

        static float GetActiveLinearAccelScale(in ShipControlRequest request, ShipTuning tuning)
        {
            if (request.fineControl)
            {
                return tuning.fineControlLinearAccelMultiplier;
            }

            return 1f;
        }

        static float GetActiveAngularSpeedScale(in ShipControlRequest request, ShipTuning tuning)
        {
            return request.boost ? tuning.boostAngularSpeedMultiplier : 1f;
        }

        static float GetActiveAngularAccelScale(in ShipControlRequest request, ShipTuning tuning)
        {
            if (request.fineControl)
            {
                return tuning.fineControlAngularAccelMultiplier;
            }

            return 1f;
        }

        public static float ResolveSignedForwardAccel(float axisRequest, in ShipControlRequest request, ShipTuning tuning)
        {
            float scale = GetActiveLinearAccelScale(request, tuning);
            if (axisRequest >= 0f)
            {
                float forwardAccel = request.fineControl ? tuning.maneuverForwardAccel : tuning.mainEngineForwardAccel;
                if (request.boost && !request.fineControl)
                {
                    forwardAccel *= tuning.boostAccelMultiplier;
                }

                return axisRequest * forwardAccel * scale;
            }

            return axisRequest * tuning.reverseAccel * scale;
        }

        static float ResolveSignedLinearAccel(float axisRequest, float positiveAccel, float negativeAccel, float scale)
        {
            if (axisRequest >= 0f)
            {
                return axisRequest * positiveAccel * scale;
            }

            return axisRequest * negativeAccel * scale;
        }

        static float ResolveSignedAngularAccel(float axisRequest, float positiveAccel, float negativeAccel, float scale)
        {
            if (axisRequest >= 0f)
            {
                return axisRequest * positiveAccel * scale;
            }

            return axisRequest * negativeAccel * scale;
        }

        static Vector3 ClampWorldLinearSpeed(Vector3 worldVelocity, float maxSpeed)
        {
            float speed = worldVelocity.magnitude;
            if (speed <= maxSpeed || speed <= 1e-6f)
            {
                return worldVelocity;
            }

            return worldVelocity * (maxSpeed / speed);
        }

        static Vector3 ClampAngularVelocity(Vector3 angularVelocity, float speedScale, ShipTuning tuning)
        {
            angularVelocity.x = Mathf.Clamp(
                angularVelocity.x,
                -tuning.maxPitchSpeedRad * speedScale,
                tuning.maxPitchSpeedRad * speedScale);
            angularVelocity.y = Mathf.Clamp(
                angularVelocity.y,
                -tuning.maxYawSpeedRad * speedScale,
                tuning.maxYawSpeedRad * speedScale);
            angularVelocity.z = Mathf.Clamp(
                angularVelocity.z,
                -tuning.maxRollSpeedRad * speedScale,
                tuning.maxRollSpeedRad * speedScale);
            return angularVelocity;
        }

        static void ApplyAssistVelocityLocks(ref ShipState state, in ShipControlRequest pilotRequest)
        {
            bool braking = pilotRequest.brake;
            bool lockAngular = braking || state.assistMode != FlightAssistMode.AssistOff;
            bool lockLinearRight = braking
                || state.assistMode == FlightAssistMode.CoupledAssist
                || state.assistMode == FlightAssistMode.FrameLockAssist;
            bool lockLinearUp = lockLinearRight;
            bool lockLinearForward = braking || state.assistMode == FlightAssistMode.FrameLockAssist;

            if (lockLinearRight || lockLinearUp || lockLinearForward)
            {
                Vector3 localVelocity = Quaternion.Inverse(state.rotation) * state.linearVelocity;
                if (lockLinearRight && IsBelowInputThreshold(pilotRequest.linearRight))
                {
                    localVelocity.x = LockSmallValue(localVelocity.x, LinearVelocityLockMps);
                }
                if (lockLinearUp && IsBelowInputThreshold(pilotRequest.linearUp))
                {
                    localVelocity.y = LockSmallValue(localVelocity.y, LinearVelocityLockMps);
                }
                if (lockLinearForward && IsBelowInputThreshold(pilotRequest.linearForward))
                {
                    localVelocity.z = LockSmallValue(localVelocity.z, LinearVelocityLockMps);
                }
                state.linearVelocity = state.rotation * localVelocity;
            }

            if (lockAngular)
            {
                if (IsBelowInputThreshold(pilotRequest.angularPitch))
                {
                    state.angularVelocityRadians.x = LockSmallValue(
                        state.angularVelocityRadians.x,
                        AngularVelocityLockRad);
                }
                if (IsBelowInputThreshold(pilotRequest.angularYaw))
                {
                    state.angularVelocityRadians.y = LockSmallValue(
                        state.angularVelocityRadians.y,
                        AngularVelocityLockRad);
                }
                if (IsBelowInputThreshold(pilotRequest.angularRoll))
                {
                    state.angularVelocityRadians.z = LockSmallValue(
                        state.angularVelocityRadians.z,
                        AngularVelocityLockRad);
                }
            }
        }

        static float LockSmallValue(float value, float threshold)
            => Mathf.Abs(value) <= threshold ? 0f : value;

        static bool IsBelowInputThreshold(float value)
            => Mathf.Abs(value) < NoInputThreshold;

        public static Quaternion IntegrateRotation(Quaternion rotation, Vector3 angularVelocityRadians, float deltaSeconds)
        {
            Vector3 omegaWorld = rotation * angularVelocityRadians;
            float angularSpeed = omegaWorld.magnitude;
            if (angularSpeed <= 1e-6f)
            {
                return Normalize(rotation);
            }

            Quaternion delta = Quaternion.AngleAxis(angularSpeed * Mathf.Rad2Deg * deltaSeconds, omegaWorld / angularSpeed);
            return Normalize(delta * rotation);
        }

        static Quaternion Normalize(Quaternion q)
        {
            float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return mag > 1e-6f
                ? new Quaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag)
                : Quaternion.identity;
        }
    }
}
