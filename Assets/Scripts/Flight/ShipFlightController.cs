using System.Collections.Generic;
using UnityEngine;

namespace FlightModel
{
    public class ShipFlightController : MonoBehaviour
    {
        ShipState state;
        ShipTuning tuning;
        ShipInputCommand lastAppliedThrusterCommand;
        readonly List<IShipControlRequestSource> externalRequestSources = new();
        readonly ShipAutopilotRequestSource autopilotRequestSource = new();

        ShipControlRequest lastPilotRequest;
        ShipControlRequest lastAssistRequest;
        ShipControlRequest lastBrakeRequest;
        ShipControlRequest lastMergedControlRequest;
        ShipThrusterOutput lastThrusterOutput;

        public ShipState State => state;
        public ShipInputCommand LastAppliedThrusterCommand => lastAppliedThrusterCommand;
        public ShipThrusterOutput LastThrusterOutput => lastThrusterOutput;
        public ShipControlRequest LastPilotRequest => lastPilotRequest;
        public ShipControlRequest LastAssistRequest => lastAssistRequest;
        public ShipControlRequest LastBrakeRequest => lastBrakeRequest;
        public ShipControlRequest LastMergedControlRequest => lastMergedControlRequest;
        public ShipTuning Tuning
        {
            get => tuning;
            set => tuning = value;
        }

        void Awake()
        {
            RegisterExternalRequestSource(autopilotRequestSource);
        }

        public void RegisterExternalRequestSource(IShipControlRequestSource source)
        {
            if (source == null || externalRequestSources.Contains(source))
            {
                return;
            }

            externalRequestSources.Add(source);
        }

        public void InitializeState(Vector3 position, Quaternion rotation)
        {
            state = new ShipState
            {
                position = position,
                rotation = rotation,
                linearVelocity = Vector3.zero,
                angularVelocityRadians = Vector3.zero,
                assistMode = FlightAssistMode.AssistOff,
                frameId = "LocalTestFrame",
                boostActive = false,
                fineControlActive = false,
                currentMassKg = tuning != null ? tuning.dryMassKg : 0f,
                remainingFuelKg = tuning != null ? tuning.fuelCapacityKg : 0f,
                remainingHypergolicKg = tuning != null ? tuning.hypergolicCapacityKg : 0f,
                appliedOutput = default
            };
        }

        public void SetAssistMode(FlightAssistMode mode)
        {
            state.assistMode = mode;
        }

        public void Simulate(float deltaSeconds, in ShipInputCommand input)
        {
            if (deltaSeconds <= 0f || tuning == null)
            {
                lastAppliedThrusterCommand = default;
                lastPilotRequest = default;
                lastAssistRequest = default;
                lastBrakeRequest = default;
                lastMergedControlRequest = default;
                return;
            }

            if (state.currentMassKg <= 0f)
            {
                state.currentMassKg = tuning.dryMassKg;
            }

            lastPilotRequest = ShipControlRequest.FromPilot(input);
            lastAssistRequest = ShipControlRequestPipeline.BuildAssistRequest(state, tuning, lastPilotRequest);
            lastBrakeRequest = ShipControlRequestPipeline.BuildBrakeRequest(state, tuning, lastPilotRequest);
            lastMergedControlRequest = ShipControlRequestPipeline.MergeAll(
                lastPilotRequest,
                lastAssistRequest,
                lastBrakeRequest,
                externalRequestSources.ToArray(),
                state,
                tuning);

            state.boostActive = lastMergedControlRequest.boost;
            state.fineControlActive = lastMergedControlRequest.fineControl;

            float massRatio = tuning.dryMassKg / Mathf.Max(1f, state.currentMassKg);
            float maxLinearSpeed = GetActiveMaxLinearSpeed(lastMergedControlRequest);
            float linearAccelScale = GetActiveLinearAccelScale(lastMergedControlRequest);
            float angularSpeedScale = GetActiveAngularSpeedScale(lastMergedControlRequest);
            float angularAccelScale = GetActiveAngularAccelScale(lastMergedControlRequest);

            Vector3 requestedLocalLinear = new(
                lastMergedControlRequest.linearRight,
                lastMergedControlRequest.linearUp,
                lastMergedControlRequest.linearForward);
            Vector3 requestedLocalAngular = new(
                lastMergedControlRequest.angularPitch,
                lastMergedControlRequest.angularYaw,
                lastMergedControlRequest.angularRoll);

            Vector3 localLinearAccel = new(
                ResolveSignedLinearAccel(requestedLocalLinear.x, tuning.rightAccel, tuning.leftAccel, linearAccelScale) * massRatio,
                ResolveSignedLinearAccel(requestedLocalLinear.y, tuning.upAccel, tuning.downAccel, linearAccelScale) * massRatio,
                ResolveSignedForwardAccel(requestedLocalLinear.z, lastMergedControlRequest) * massRatio);

            Vector3 localAngularAccel = new(
                ResolveSignedAngularAccel(requestedLocalAngular.x, tuning.pitchPositiveAccel, tuning.pitchNegativeAccel, angularAccelScale) * massRatio,
                ResolveSignedAngularAccel(requestedLocalAngular.y, tuning.yawPositiveAccel, tuning.yawNegativeAccel, angularAccelScale) * massRatio,
                ResolveSignedAngularAccel(requestedLocalAngular.z, tuning.rollPositiveAccel, tuning.rollNegativeAccel, angularAccelScale) * massRatio);

            bool mainEngineFuelBlocked = !ShipPropellantAccounting.HasMainEngineFuel(state)
                && !lastMergedControlRequest.fineControl
                && lastMergedControlRequest.linearForward > 0f;
            bool hypergolicBlocked = !ShipPropellantAccounting.HasHypergolic(state);

            if (mainEngineFuelBlocked && localLinearAccel.z > 0f)
            {
                localLinearAccel.z = 0f;
            }

            if (hypergolicBlocked)
            {
                localLinearAccel.x = 0f;
                localLinearAccel.y = 0f;
                if (lastMergedControlRequest.fineControl || lastMergedControlRequest.linearForward <= 0f)
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
            state.angularVelocityRadians = ClampAngularVelocity(state.angularVelocityRadians, angularSpeedScale);

            state.position += state.linearVelocity * deltaSeconds;
            state.rotation = IntegrateRotation(state.rotation, state.angularVelocityRadians, deltaSeconds);

            ShipPropellantAccounting.BurnResult burn = ShipPropellantAccounting.ComputeBurn(
                tuning,
                lastMergedControlRequest,
                localLinearAccel,
                localAngularAccel,
                state.currentMassKg,
                deltaSeconds);
            ShipPropellantAccounting.ApplyBurn(ref state, burn);
            state.currentMassKg = tuning.dryMassKg;

            lastThrusterOutput = BuildThrusterOutput(
                tuning,
                lastMergedControlRequest,
                localLinearAccel,
                localAngularAccel,
                linearAccelScale,
                angularAccelScale,
                massRatio);

            lastAppliedThrusterCommand = lastThrusterOutput.ToMatcherCommand();

            state.appliedOutput.requestedLocalLinear = requestedLocalLinear;
            state.appliedOutput.requestedLocalAngular = requestedLocalAngular;
            state.appliedOutput.appliedLocalLinear = localLinearAccel;
            state.appliedOutput.appliedLocalAngular = localAngularAccel;
            state.appliedOutput.thrusters = lastThrusterOutput;
            state.appliedOutput.mainEngineFuelBlocked = mainEngineFuelBlocked;
            state.appliedOutput.hypergolicBlocked = hypergolicBlocked;
            state.appliedOutput.linearSpeedCapped = linearSpeedCapped;
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

        float GetActiveMaxLinearSpeed(in ShipControlRequest request)
        {
            if (request.fineControl)
            {
                return tuning.fineControlMaxLinearSpeedMps;
            }

            return request.boost ? tuning.boostMaxLinearSpeedMps : tuning.maxLinearSpeedMps;
        }

        float GetActiveLinearAccelScale(in ShipControlRequest request)
        {
            if (request.fineControl)
            {
                return tuning.fineControlLinearAccelMultiplier;
            }

            return 1f;
        }

        float GetActiveAngularSpeedScale(in ShipControlRequest request)
        {
            return request.boost ? tuning.boostAngularSpeedMultiplier : 1f;
        }

        float GetActiveAngularAccelScale(in ShipControlRequest request)
        {
            if (request.fineControl)
            {
                return tuning.fineControlAngularAccelMultiplier;
            }

            return 1f;
        }

        float ResolveSignedForwardAccel(float axisRequest, in ShipControlRequest request)
        {
            float scale = GetActiveLinearAccelScale(request);
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

        Vector3 ClampAngularVelocity(Vector3 angularVelocity, float speedScale)
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

        static Quaternion IntegrateRotation(Quaternion rotation, Vector3 angularVelocityRadians, float deltaSeconds)
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
