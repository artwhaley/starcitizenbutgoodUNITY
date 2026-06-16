using UnityEngine;

namespace FlightModel
{
    public class ShipFlightController : MonoBehaviour
    {
        const float NoInputThreshold = 0.05f;

        ShipState state;
        ShipTuning tuning;
        ShipInputCommand lastAppliedThrusterCommand;

        public ShipState State => state;
        public ShipInputCommand LastAppliedThrusterCommand => lastAppliedThrusterCommand;
        public ShipTuning Tuning
        {
            get => tuning;
            set => tuning = value;
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
                frameId = "LocalTestFrame"
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
                return;
            }

            float thrustForward = Mathf.Clamp(input.thrustForward, -1f, 1f);
            float thrustRight = Mathf.Clamp(input.thrustRight, -1f, 1f);
            float thrustUp = Mathf.Clamp(input.thrustUp, -1f, 1f);
            float pitch = Mathf.Clamp(input.pitch, -1f, 1f);
            float yaw = Mathf.Clamp(input.yaw, -1f, 1f);
            float roll = Mathf.Clamp(input.roll, -1f, 1f);
            lastAppliedThrusterCommand = new ShipInputCommand
            {
                thrustForward = thrustForward,
                thrustRight = thrustRight,
                thrustUp = thrustUp,
                pitch = pitch,
                yaw = yaw,
                roll = roll,
                brake = input.brake
            };

            float boostScale = input.boost ? tuning.boostMultiplier : 1f;
            Vector3 localForce = new(
                thrustRight * tuning.maxThrustNewtons.x,
                thrustUp * tuning.maxThrustNewtons.y,
                thrustForward * tuning.maxThrustNewtons.z * boostScale);

            Vector3 worldForce = state.rotation * localForce;
            Vector3 acceleration = worldForce / Mathf.Max(1f, tuning.massKg);
            state.linearVelocity += acceleration * deltaSeconds;

            if (input.brake)
            {
                Vector3 beforeBrakeVelocity = state.linearVelocity;
                state.linearVelocity = ExponentialDamp(state.linearVelocity, tuning.brakeLinearDampingStrength, deltaSeconds);
                AddLinearAssistCommand(beforeBrakeVelocity, state.linearVelocity, deltaSeconds);
            }
            else
            {
                switch (state.assistMode)
                {
                    case FlightAssistMode.CoupledAssist:
                        ApplyCoupledLinearAssist(deltaSeconds, thrustRight, thrustUp);
                        break;
                    case FlightAssistMode.FrameLockAssist:
                        ApplyFrameLockLinearAssist(deltaSeconds, thrustForward, thrustRight, thrustUp);
                        break;
                }
            }

            state.position += state.linearVelocity * deltaSeconds;

            Vector3 localTorque = new(
                pitch * tuning.maxTorque.x,
                yaw * tuning.maxTorque.y,
                roll * tuning.maxTorque.z);

            Vector3 angularAcceleration = localTorque / Mathf.Max(1f, tuning.massKg);
            state.angularVelocityRadians += angularAcceleration * deltaSeconds;

            if (input.brake)
            {
                Vector3 beforeBrakeAngularVelocity = state.angularVelocityRadians;
                state.angularVelocityRadians = ExponentialDamp(
                    state.angularVelocityRadians,
                    tuning.brakeAngularDampingStrength,
                    deltaSeconds);
                AddAngularAssistCommand(beforeBrakeAngularVelocity, state.angularVelocityRadians, deltaSeconds);
            }
            else if (state.assistMode != FlightAssistMode.AssistOff)
            {
                ApplyAttitudeAssist(deltaSeconds, pitch, yaw, roll);
            }

            Vector3 omegaLocal = state.rotation * new Vector3(
                state.angularVelocityRadians.x,
                state.angularVelocityRadians.y,
                state.angularVelocityRadians.z);

            float angularSpeed = omegaLocal.magnitude;
            if (angularSpeed > 1e-6f)
            {
                Quaternion delta = Quaternion.AngleAxis(angularSpeed * Mathf.Rad2Deg * deltaSeconds, omegaLocal / angularSpeed);
                state.rotation = Normalize(delta * state.rotation);
            }
        }

        void ApplyAttitudeAssist(float deltaSeconds, float pitch, float yaw, float roll)
        {
            Vector3 before = state.angularVelocityRadians;

            if (IsBelowThreshold(roll))
            {
                state.angularVelocityRadians.z = Mathf.Lerp(state.angularVelocityRadians.z, 0f, tuning.angularDampingStrength * deltaSeconds);
            }

            if (IsBelowThreshold(pitch))
            {
                state.angularVelocityRadians.x = Mathf.Lerp(state.angularVelocityRadians.x, 0f, tuning.angularDampingStrength * deltaSeconds);
            }

            if (IsBelowThreshold(yaw))
            {
                state.angularVelocityRadians.y = Mathf.Lerp(state.angularVelocityRadians.y, 0f, tuning.angularDampingStrength * deltaSeconds);
            }

            AddAngularAssistCommand(before, state.angularVelocityRadians, deltaSeconds);
        }

        void ApplyCoupledLinearAssist(float deltaSeconds, float thrustRight, float thrustUp)
        {
            Vector3 localVelocity = Quaternion.Inverse(state.rotation) * state.linearVelocity;
            Vector3 before = localVelocity;

            if (IsBelowThreshold(thrustRight))
            {
                localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, tuning.coupledLateralDampingStrength * deltaSeconds);
            }

            if (IsBelowThreshold(thrustUp))
            {
                localVelocity.y = Mathf.Lerp(localVelocity.y, 0f, tuning.coupledLateralDampingStrength * deltaSeconds);
            }

            state.linearVelocity = state.rotation * localVelocity;
            AddLocalLinearAssistCommand(before, localVelocity, deltaSeconds);
        }

        void ApplyFrameLockLinearAssist(float deltaSeconds, float thrustForward, float thrustRight, float thrustUp)
        {
            Vector3 localVelocity = Quaternion.Inverse(state.rotation) * state.linearVelocity;
            Vector3 before = localVelocity;

            if (IsBelowThreshold(thrustForward))
            {
                localVelocity.z = Mathf.Lerp(localVelocity.z, 0f, tuning.frameLockLinearDampingStrength * deltaSeconds);
            }

            if (IsBelowThreshold(thrustRight))
            {
                localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, tuning.frameLockLinearDampingStrength * deltaSeconds);
            }

            if (IsBelowThreshold(thrustUp))
            {
                localVelocity.y = Mathf.Lerp(localVelocity.y, 0f, tuning.frameLockLinearDampingStrength * deltaSeconds);
            }

            state.linearVelocity = state.rotation * localVelocity;
            AddLocalLinearAssistCommand(before, localVelocity, deltaSeconds);
        }

        void AddLinearAssistCommand(Vector3 beforeWorldVelocity, Vector3 afterWorldVelocity, float deltaSeconds)
        {
            Vector3 beforeLocal = Quaternion.Inverse(state.rotation) * beforeWorldVelocity;
            Vector3 afterLocal = Quaternion.Inverse(state.rotation) * afterWorldVelocity;
            AddLocalLinearAssistCommand(beforeLocal, afterLocal, deltaSeconds);
        }

        void AddLocalLinearAssistCommand(Vector3 beforeLocalVelocity, Vector3 afterLocalVelocity, float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            Vector3 localAcceleration = (afterLocalVelocity - beforeLocalVelocity) / deltaSeconds;
            float mass = Mathf.Max(1f, tuning.massKg);
            AddAxis(ref lastAppliedThrusterCommand.thrustRight, localAcceleration.x * mass, tuning.maxThrustNewtons.x);
            AddAxis(ref lastAppliedThrusterCommand.thrustUp, localAcceleration.y * mass, tuning.maxThrustNewtons.y);
            AddAxis(ref lastAppliedThrusterCommand.thrustForward, localAcceleration.z * mass, tuning.maxThrustNewtons.z);
        }

        void AddAngularAssistCommand(Vector3 beforeAngularVelocity, Vector3 afterAngularVelocity, float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            Vector3 angularAcceleration = (afterAngularVelocity - beforeAngularVelocity) / deltaSeconds;
            float mass = Mathf.Max(1f, tuning.massKg);
            AddAxis(ref lastAppliedThrusterCommand.pitch, angularAcceleration.x * mass, tuning.maxTorque.x);
            AddAxis(ref lastAppliedThrusterCommand.yaw, angularAcceleration.y * mass, tuning.maxTorque.y);
            AddAxis(ref lastAppliedThrusterCommand.roll, angularAcceleration.z * mass, tuning.maxTorque.z);
        }

        static void AddAxis(ref float field, float appliedNewtonsOrTorque, float maxNewtonsOrTorque)
        {
            if (Mathf.Abs(maxNewtonsOrTorque) < 1e-5f)
            {
                return;
            }

            field = Mathf.Clamp(field + appliedNewtonsOrTorque / maxNewtonsOrTorque, -1f, 1f);
        }

        static bool IsBelowThreshold(float value) => Mathf.Abs(value) < NoInputThreshold;

        static Vector3 ExponentialDamp(Vector3 value, float strength, float deltaSeconds)
        {
            return strength <= 0f ? value : value * Mathf.Exp(-strength * deltaSeconds);
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
