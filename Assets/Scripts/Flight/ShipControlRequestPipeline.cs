using System.Collections.Generic;
using UnityEngine;

namespace FlightModel
{
    public static class ShipControlRequestPipeline
    {
        const float NoInputThreshold = 0.05f;
        const float LinearVelocityDeadbandMps = 0.05f;
        const float AngularVelocityDeadbandRad = 0.01f;
        const float BrakeLinearVelocityDeadbandMps = 0.02f;
        const float BrakeAngularVelocityDeadbandRad = 0.005f;
        const float DampingGentleScale = 0.8f;

        public static ShipControlRequest BuildAssistRequest(
            in ShipState state,
            in ShipTuning tuning,
            in ShipControlRequest pilotRequest,
            float deltaSeconds)
        {
            if (state.assistMode == FlightAssistMode.AssistOff || pilotRequest.brake || tuning == null)
            {
                return default;
            }

            var request = new ShipControlRequest();
            Vector3 localVelocity = Quaternion.Inverse(state.rotation) * state.linearVelocity;

            switch (state.assistMode)
            {
                case FlightAssistMode.AttitudeAssist:
                    ApplyAttitudeAssist(ref request, state, tuning, pilotRequest, deltaSeconds);
                    break;

                case FlightAssistMode.CoupledAssist:
                    ApplyAttitudeAssist(ref request, state, tuning, pilotRequest, deltaSeconds);
                    request.linearRight = BuildLinearAssistRequest(
                        pilotRequest.linearRight,
                        localVelocity.x,
                        tuning.rightAccel,
                        tuning.leftAccel,
                        tuning.coupledAssistResponsiveness,
                        deltaSeconds);
                    request.linearUp = BuildLinearAssistRequest(
                        pilotRequest.linearUp,
                        localVelocity.y,
                        tuning.upAccel,
                        tuning.downAccel,
                        tuning.coupledAssistResponsiveness,
                        deltaSeconds);
                    break;

                case FlightAssistMode.FrameLockAssist:
                    ApplyAttitudeAssist(ref request, state, tuning, pilotRequest, deltaSeconds);
                    request.linearForward = BuildLinearAssistRequest(
                        pilotRequest.linearForward,
                        localVelocity.z,
                        tuning.mainEngineForwardAccel,
                        tuning.reverseAccel,
                        tuning.frameLockAssistResponsiveness,
                        deltaSeconds);
                    request.linearRight = BuildLinearAssistRequest(
                        pilotRequest.linearRight,
                        localVelocity.x,
                        tuning.rightAccel,
                        tuning.leftAccel,
                        tuning.frameLockAssistResponsiveness,
                        deltaSeconds);
                    request.linearUp = BuildLinearAssistRequest(
                        pilotRequest.linearUp,
                        localVelocity.y,
                        tuning.upAccel,
                        tuning.downAccel,
                        tuning.frameLockAssistResponsiveness,
                        deltaSeconds);
                    break;
            }

            return request;
        }

        public static ShipControlRequest BuildBrakeRequest(
            in ShipState state,
            in ShipTuning tuning,
            in ShipControlRequest pilotRequest,
            float deltaSeconds)
        {
            if (!pilotRequest.brake || tuning == null)
            {
                return default;
            }

            Vector3 localVelocity = Quaternion.Inverse(state.rotation) * state.linearVelocity;

            return new ShipControlRequest
            {
                linearForward = BuildBrakeAxisRequest(
                    localVelocity.z,
                    tuning.mainEngineForwardAccel,
                    tuning.reverseAccel,
                    tuning.brakeResponsiveness,
                    BrakeLinearVelocityDeadbandMps,
                    deltaSeconds),
                linearRight = BuildBrakeAxisRequest(
                    localVelocity.x,
                    tuning.rightAccel,
                    tuning.leftAccel,
                    tuning.brakeResponsiveness,
                    BrakeLinearVelocityDeadbandMps,
                    deltaSeconds),
                linearUp = BuildBrakeAxisRequest(
                    localVelocity.y,
                    tuning.upAccel,
                    tuning.downAccel,
                    tuning.brakeResponsiveness,
                    BrakeLinearVelocityDeadbandMps,
                    deltaSeconds),
                angularPitch = BuildBrakeAxisRequest(
                    state.angularVelocityRadians.x,
                    tuning.pitchPositiveAccel,
                    tuning.pitchNegativeAccel,
                    tuning.brakeResponsiveness,
                    BrakeAngularVelocityDeadbandRad,
                    deltaSeconds),
                angularYaw = BuildBrakeAxisRequest(
                    state.angularVelocityRadians.y,
                    tuning.yawPositiveAccel,
                    tuning.yawNegativeAccel,
                    tuning.brakeResponsiveness,
                    BrakeAngularVelocityDeadbandRad,
                    deltaSeconds),
                angularRoll = BuildBrakeAxisRequest(
                    state.angularVelocityRadians.z,
                    tuning.rollPositiveAccel,
                    tuning.rollNegativeAccel,
                    tuning.brakeResponsiveness,
                    BrakeAngularVelocityDeadbandRad,
                    deltaSeconds),
                brake = true
            };
        }

        public static ShipControlRequest MergeAll(
            in ShipControlRequest pilotRequest,
            in ShipControlRequest assistRequest,
            in ShipControlRequest brakeRequest,
            IReadOnlyList<IShipControlRequestSource> externalSources,
            in ShipState state,
            in ShipTuning tuning)
        {
            ShipControlRequest externalRequest = default;
            if (externalSources != null)
            {
                for (int i = 0; i < externalSources.Count; i++)
                {
                    IShipControlRequestSource source = externalSources[i];
                    if (source != null
                        && source.TryBuildRequest(state, tuning, pilotRequest, out ShipControlRequest next))
                    {
                        externalRequest = ShipControlRequest.Merge(externalRequest, next);
                    }
                }
            }

            return ShipControlRequest.Merge(pilotRequest, assistRequest, brakeRequest, externalRequest);
        }

        static void ApplyAttitudeAssist(
            ref ShipControlRequest request,
            in ShipState state,
            in ShipTuning tuning,
            in ShipControlRequest pilotRequest,
            float deltaSeconds)
        {
            request.angularPitch = BuildAngularAssistRequest(
                pilotRequest.angularPitch,
                state.angularVelocityRadians.x,
                tuning.pitchPositiveAccel,
                tuning.pitchNegativeAccel,
                tuning.attitudeAssistResponsiveness,
                deltaSeconds);
            request.angularYaw = BuildAngularAssistRequest(
                pilotRequest.angularYaw,
                state.angularVelocityRadians.y,
                tuning.yawPositiveAccel,
                tuning.yawNegativeAccel,
                tuning.attitudeAssistResponsiveness,
                deltaSeconds);
            request.angularRoll = BuildAngularAssistRequest(
                pilotRequest.angularRoll,
                state.angularVelocityRadians.z,
                tuning.rollPositiveAccel,
                tuning.rollNegativeAccel,
                tuning.attitudeAssistResponsiveness,
                deltaSeconds);
        }

        static float BuildLinearAssistRequest(
            float pilotAxis,
            float localVelocity,
            float positiveAccel,
            float negativeAccel,
            float responsiveness,
            float deltaSeconds)
        {
            if (!IsBelowThreshold(pilotAxis)
                || Mathf.Abs(localVelocity) <= LinearVelocityDeadbandMps
                || responsiveness <= 0f)
            {
                return 0f;
            }

            return BuildVelocityDampingRequest(
                localVelocity,
                positiveAccel,
                negativeAccel,
                responsiveness,
                LinearVelocityDeadbandMps,
                deltaSeconds);
        }

        static float BuildAngularAssistRequest(
            float pilotAxis,
            float angularVelocity,
            float positiveAccel,
            float negativeAccel,
            float responsiveness,
            float deltaSeconds)
        {
            if (!IsBelowThreshold(pilotAxis)
                || Mathf.Abs(angularVelocity) <= AngularVelocityDeadbandRad
                || responsiveness <= 0f)
            {
                return 0f;
            }

            return BuildVelocityDampingRequest(
                angularVelocity,
                positiveAccel,
                negativeAccel,
                responsiveness,
                AngularVelocityDeadbandRad,
                deltaSeconds);
        }

        static float BuildBrakeAxisRequest(
            float velocityComponent,
            float positiveAccel,
            float negativeAccel,
            float responsiveness,
            float deadband,
            float deltaSeconds)
            => BuildVelocityDampingRequest(
                velocityComponent,
                positiveAccel,
                negativeAccel,
                responsiveness,
                deadband,
                deltaSeconds);

        static float BuildVelocityDampingRequest(
            float velocity,
            float positiveAccel,
            float negativeAccel,
            float responsiveness,
            float deadband,
            float deltaSeconds)
        {
            float speed = Mathf.Abs(velocity);
            if (speed <= deadband)
            {
                return 0f;
            }

            float desiredAccelMagnitude = speed * responsiveness * DampingGentleScale;
            if (deltaSeconds > 1e-5f)
            {
                desiredAccelMagnitude = Mathf.Min(desiredAccelMagnitude, speed / deltaSeconds);
            }

            float desiredAccel = -Mathf.Sign(velocity) * desiredAccelMagnitude;
            float opposingAuthority = velocity > 0f ? negativeAccel : positiveAccel;
            float authority = Mathf.Max(opposingAuthority, 1e-4f);
            return Mathf.Clamp(desiredAccel / authority, -1f, 1f);
        }

        static bool IsBelowThreshold(float value) => Mathf.Abs(value) < NoInputThreshold;
    }
}
