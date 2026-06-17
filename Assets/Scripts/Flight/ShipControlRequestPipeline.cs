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
            float responsiveness = tuning.attitudeAssistResponsiveness;

            switch (state.assistMode)
            {
                case FlightAssistMode.AttitudeAssist:
                    request.angularPitch = BuildAngularCounterRequest(pilotRequest.angularPitch, state.angularVelocityRadians.x, responsiveness);
                    request.angularYaw = BuildAngularCounterRequest(pilotRequest.angularYaw, state.angularVelocityRadians.y, responsiveness);
                    request.angularRoll = BuildAngularCounterRequest(pilotRequest.angularRoll, state.angularVelocityRadians.z, responsiveness);
                    break;

                case FlightAssistMode.CoupledAssist:
                    request.linearRight = BuildLinearCounterRequest(pilotRequest.linearRight, localVelocity.x, tuning.coupledAssistResponsiveness);
                    request.linearUp = BuildLinearCounterRequest(pilotRequest.linearUp, localVelocity.y, tuning.coupledAssistResponsiveness);
                    break;

                case FlightAssistMode.FrameLockAssist:
                    request.linearForward = BuildLinearCounterRequest(pilotRequest.linearForward, localVelocity.z, tuning.frameLockAssistResponsiveness);
                    request.linearRight = BuildLinearCounterRequest(pilotRequest.linearRight, localVelocity.x, tuning.frameLockAssistResponsiveness);
                    request.linearUp = BuildLinearCounterRequest(pilotRequest.linearUp, localVelocity.y, tuning.frameLockAssistResponsiveness);
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
            float responsiveness = tuning.brakeResponsiveness;

            return new ShipControlRequest
            {
                linearForward = BuildLinearCounterRequest(0f, localVelocity.z, responsiveness),
                linearRight = BuildLinearCounterRequest(0f, localVelocity.x, responsiveness),
                linearUp = BuildLinearCounterRequest(0f, localVelocity.y, responsiveness),
                angularPitch = BuildAngularCounterRequest(0f, state.angularVelocityRadians.x, responsiveness * 2f),
                angularYaw = BuildAngularCounterRequest(0f, state.angularVelocityRadians.y, responsiveness * 2f),
                angularRoll = BuildAngularCounterRequest(0f, state.angularVelocityRadians.z, responsiveness * 2f),
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

        static float BuildLinearCounterRequest(float pilotAxis, float localVelocity, float responsiveness)
        {
            if (!IsBelowThreshold(pilotAxis) || Mathf.Abs(localVelocity) < 1e-4f)
            {
                return 0f;
            }

            return Mathf.Clamp(-Mathf.Sign(localVelocity) * responsiveness * 0.25f, -1f, 1f);
        }

        static float BuildAngularCounterRequest(float pilotAxis, float angularVelocity, float responsiveness)
        {
            if (!IsBelowThreshold(pilotAxis) || Mathf.Abs(angularVelocity) < 1e-4f)
            {
                return 0f;
            }

            return Mathf.Clamp(-Mathf.Sign(angularVelocity) * responsiveness * 0.25f, -1f, 1f);
        }

        static bool IsBelowThreshold(float value) => Mathf.Abs(value) < NoInputThreshold;
    }
}
