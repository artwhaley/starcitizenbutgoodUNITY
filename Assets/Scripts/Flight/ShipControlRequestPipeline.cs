using UnityEngine;

namespace FlightModel
{
    public static class ShipControlRequestPipeline
    {
        const float NoInputThreshold = 0.05f;

        public static ShipControlRequest BuildAssistRequest(
            in ShipState state,
            in ShipTuning tuning,
            in ShipControlRequest pilotRequest)
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
                    request.angularPitch = BuildAngularAssistRequest(
                        pilotRequest.angularPitch,
                        state.angularVelocityRadians.x,
                        tuning.maxPitchSpeedRad,
                        tuning.pitchPositiveAccel,
                        tuning.pitchNegativeAccel,
                        tuning.attitudeAssistResponsiveness);
                    request.angularYaw = BuildAngularAssistRequest(
                        pilotRequest.angularYaw,
                        state.angularVelocityRadians.y,
                        tuning.maxYawSpeedRad,
                        tuning.yawPositiveAccel,
                        tuning.yawNegativeAccel,
                        tuning.attitudeAssistResponsiveness);
                    request.angularRoll = BuildAngularAssistRequest(
                        pilotRequest.angularRoll,
                        state.angularVelocityRadians.z,
                        tuning.maxRollSpeedRad,
                        tuning.rollPositiveAccel,
                        tuning.rollNegativeAccel,
                        tuning.attitudeAssistResponsiveness);
                    break;

                case FlightAssistMode.CoupledAssist:
                    request.linearRight = BuildLinearAssistRequest(
                        pilotRequest.linearRight,
                        localVelocity.x,
                        tuning.maxLinearSpeedMps,
                        tuning.rightAccel,
                        tuning.leftAccel,
                        tuning.coupledAssistResponsiveness);
                    request.linearUp = BuildLinearAssistRequest(
                        pilotRequest.linearUp,
                        localVelocity.y,
                        tuning.maxLinearSpeedMps,
                        tuning.upAccel,
                        tuning.downAccel,
                        tuning.coupledAssistResponsiveness);
                    break;

                case FlightAssistMode.FrameLockAssist:
                    request.linearForward = BuildLinearAssistRequest(
                        pilotRequest.linearForward,
                        localVelocity.z,
                        tuning.maxLinearSpeedMps,
                        tuning.mainEngineForwardAccel,
                        tuning.reverseAccel,
                        tuning.frameLockAssistResponsiveness);
                    request.linearRight = BuildLinearAssistRequest(
                        pilotRequest.linearRight,
                        localVelocity.x,
                        tuning.maxLinearSpeedMps,
                        tuning.rightAccel,
                        tuning.leftAccel,
                        tuning.frameLockAssistResponsiveness);
                    request.linearUp = BuildLinearAssistRequest(
                        pilotRequest.linearUp,
                        localVelocity.y,
                        tuning.maxLinearSpeedMps,
                        tuning.upAccel,
                        tuning.downAccel,
                        tuning.frameLockAssistResponsiveness);
                    break;
            }

            return request;
        }

        public static ShipControlRequest BuildBrakeRequest(in ShipState state, in ShipTuning tuning, in ShipControlRequest pilotRequest)
        {
            if (!pilotRequest.brake || tuning == null)
            {
                return default;
            }

            Vector3 localVelocity = Quaternion.Inverse(state.rotation) * state.linearVelocity;

            return new ShipControlRequest
            {
                linearForward = BuildBrakeAxisRequest(localVelocity.z),
                linearRight = BuildBrakeAxisRequest(localVelocity.x),
                linearUp = BuildBrakeAxisRequest(localVelocity.y),
                angularPitch = BuildBrakeAxisRequest(state.angularVelocityRadians.x),
                angularYaw = BuildBrakeAxisRequest(state.angularVelocityRadians.y),
                angularRoll = BuildBrakeAxisRequest(state.angularVelocityRadians.z),
                brake = true
            };
        }

        public static ShipControlRequest MergeAll(
            in ShipControlRequest pilotRequest,
            in ShipControlRequest assistRequest,
            in ShipControlRequest brakeRequest,
            IShipControlRequestSource[] externalSources,
            in ShipState state,
            in ShipTuning tuning)
        {
            ShipControlRequest externalRequest = default;
            if (externalSources != null)
            {
                for (int i = 0; i < externalSources.Length; i++)
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

        static float BuildLinearAssistRequest(
            float pilotAxis,
            float localVelocity,
            float maxSpeed,
            float positiveAccel,
            float negativeAccel,
            float responsiveness)
        {
            if (!IsBelowThreshold(pilotAxis) || Mathf.Abs(localVelocity) < 1e-4f)
            {
                return 0f;
            }

            float opposingAccel = localVelocity > 0f ? negativeAccel : positiveAccel;
            float desiredAccel = -Mathf.Sign(localVelocity) * opposingAccel * responsiveness;
            float authority = Mathf.Max(opposingAccel, 1e-4f);
            return Mathf.Clamp(desiredAccel / authority, -1f, 1f);
        }

        static float BuildAngularAssistRequest(
            float pilotAxis,
            float angularVelocity,
            float maxSpeed,
            float positiveAccel,
            float negativeAccel,
            float responsiveness)
        {
            if (!IsBelowThreshold(pilotAxis) || Mathf.Abs(angularVelocity) < 1e-4f)
            {
                return 0f;
            }

            float opposingAccel = angularVelocity > 0f ? negativeAccel : positiveAccel;
            float desiredAccel = -Mathf.Sign(angularVelocity) * opposingAccel * responsiveness;
            float authority = Mathf.Max(opposingAccel, 1e-4f);
            return Mathf.Clamp(desiredAccel / authority, -1f, 1f);
        }

        static float BuildBrakeAxisRequest(float velocityComponent)
        {
            if (Mathf.Abs(velocityComponent) < 1e-4f)
            {
                return 0f;
            }

            return Mathf.Clamp(-Mathf.Sign(velocityComponent), -1f, 1f);
        }

        static bool IsBelowThreshold(float value) => Mathf.Abs(value) < NoInputThreshold;
    }
}
