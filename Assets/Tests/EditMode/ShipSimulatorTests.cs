using NUnit.Framework;
using UnityEngine;

namespace FlightModel.Tests
{
    public class ShipSimulatorTests
    {
        ShipTuning CreateTuning()
        {
            ShipTuning t = ScriptableObject.CreateInstance<ShipTuning>();
            t.dryMassKg = 1000f;
            t.fuelCapacityKg = 500f;
            t.hypergolicCapacityKg = 200f;
            t.maxLinearSpeedMps = 300f;
            t.boostMaxLinearSpeedMps = 600f;
            t.fineControlMaxLinearSpeedMps = 50f;
            t.maxPitchSpeedRad = 3f;
            t.maxYawSpeedRad = 3f;
            t.maxRollSpeedRad = 4f;
            t.mainEngineForwardAccel = 80f;
            t.maneuverForwardAccel = 20f;
            t.reverseAccel = 40f;
            t.rightAccel = 30f;
            t.leftAccel = 30f;
            t.upAccel = 30f;
            t.downAccel = 30f;
            t.pitchPositiveAccel = 25f;
            t.pitchNegativeAccel = 25f;
            t.yawPositiveAccel = 25f;
            t.yawNegativeAccel = 25f;
            t.rollPositiveAccel = 35f;
            t.rollNegativeAccel = 35f;
            t.boostAccelMultiplier = 2f;
            t.boostAngularSpeedMultiplier = 2f;
            t.fineControlLinearAccelMultiplier = 0.5f;
            t.fineControlAngularAccelMultiplier = 0.5f;
            t.brakeResponsiveness = 1f;
            t.attitudeAssistResponsiveness = 0.5f;
            t.coupledAssistResponsiveness = 1.5f;
            t.frameLockAssistResponsiveness = 1.5f;
            t.fuelBurnRatePerNewtonSecond = 1e-6f;
            t.hypergolicBurnRatePerNewtonSecond = 2e-6f;
            return t;
        }

        ShipState CreateState(ShipTuning tuning, FlightAssistMode assist = FlightAssistMode.AssistOff)
        {
            return new ShipState
            {
                position = Vector3.zero,
                rotation = Quaternion.identity,
                linearVelocity = Vector3.zero,
                angularVelocityRadians = Vector3.zero,
                assistMode = assist,
                frameId = World.ReferenceFrameId.LocalZone,
                boostActive = false,
                fineControlActive = false,
                currentMassKg = tuning.dryMassKg + tuning.fuelCapacityKg + tuning.hypergolicCapacityKg,
                remainingFuelKg = tuning.fuelCapacityKg,
                remainingHypergolicKg = tuning.hypergolicCapacityKg,
                appliedOutput = default
            };
        }

        [Test]
        public void ForwardThrust_IncreasesForwardSpeed()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning);

            ShipInputCommand input = new ShipInputCommand
            {
                thrustForward = 1f
            };

            ShipSimulator.Step(ref state, tuning, null, 0.02f, input, out _);

            Assert.Greater(state.linearVelocity.z, 0f,
                "Forward thrust should increase forward speed.");
        }

        [Test]
        public void NoInput_PreservesVelocity_AssistOff()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning);
            state.linearVelocity = new Vector3(10f, 5f, 50f);

            ShipInputCommand input = default;
            ShipSimulator.Step(ref state, tuning, null, 0.02f, input, out _);

            Assert.AreEqual(10f, state.linearVelocity.x, 0.1f);
            Assert.AreEqual(5f, state.linearVelocity.y, 0.1f);
            Assert.AreEqual(50f, state.linearVelocity.z, 0.1f);
        }

        [Test]
        public void Brake_ReducesExistingVelocity()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning);
            state.linearVelocity = new Vector3(30f, 0f, 100f);

            ShipInputCommand input = new ShipInputCommand { brake = true };
            ShipSimulator.Step(ref state, tuning, null, 0.1f, input, out _);

            float speedAfter = state.linearVelocity.magnitude;
            float speedBefore = new Vector3(30f, 0f, 100f).magnitude;
            Assert.Less(speedAfter, speedBefore,
                "Brake should reduce velocity magnitude.");
        }

        [Test]
        public void BrakeRequest_DeadbandsTinyVelocity()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning);
            state.linearVelocity = new Vector3(0.01f, -0.01f, 0.019f);
            state.angularVelocityRadians = new Vector3(0.004f, -0.004f, 0.004f);

            ShipControlRequest pilot = ShipControlRequest.FromPilot(new ShipInputCommand { brake = true });
            ShipControlRequest brake = ShipControlRequestPipeline.BuildBrakeRequest(state, tuning, pilot, 0.1f);

            Assert.AreEqual(0f, brake.linearRight, 0.0001f);
            Assert.AreEqual(0f, brake.linearUp, 0.0001f);
            Assert.AreEqual(0f, brake.linearForward, 0.0001f);
            Assert.AreEqual(0f, brake.angularPitch, 0.0001f);
            Assert.AreEqual(0f, brake.angularYaw, 0.0001f);
            Assert.AreEqual(0f, brake.angularRoll, 0.0001f);
            Assert.IsTrue(brake.brake);
        }

        [Test]
        public void BrakeRequest_ScalesDownNearDeadbandVelocity()
        {
            ShipTuning tuning = CreateTuning();
            tuning.brakeResponsiveness = 4f;
            ShipState state = CreateState(tuning);
            state.linearVelocity = new Vector3(0.12f, 0f, 0f);

            ShipControlRequest pilot = ShipControlRequest.FromPilot(new ShipInputCommand { brake = true });
            ShipControlRequest brake = ShipControlRequestPipeline.BuildBrakeRequest(state, tuning, pilot, 0.1f);

            Assert.Less(brake.linearRight, 0f);
            Assert.Less(Mathf.Abs(brake.linearRight), 0.05f,
                "Near-deadband braking should apply a small correction, not full RCS output.");
        }

        [Test]
        public void CoupledAssist_DampsLateralVelocityAndDeadbandsSettledAxis()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning, FlightAssistMode.CoupledAssist);
            state.linearVelocity = new Vector3(0.12f, 0.02f, 0f);

            ShipControlRequest assist = ShipControlRequestPipeline.BuildAssistRequest(
                state, tuning, default, 0.1f);

            Assert.Less(assist.linearRight, 0f);
            Assert.Less(Mathf.Abs(assist.linearRight), 0.05f,
                "Coupled assist should scale damping near zero instead of firing full RCS.");
            Assert.AreEqual(0f, assist.linearUp, 0.0001f,
                "Settled lateral axes should remain inside the assist deadband.");
        }

        [Test]
        public void CoupledAssist_LocksTinyLateralVelocityToZero()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning, FlightAssistMode.CoupledAssist);
            state.linearVelocity = new Vector3(0.04f, -0.04f, 8f);

            ShipSimulator.Step(ref state, tuning, null, 0.02f, default, out _);

            Assert.AreEqual(0f, state.linearVelocity.x, 0.0001f);
            Assert.AreEqual(0f, state.linearVelocity.y, 0.0001f);
            Assert.AreEqual(8f, state.linearVelocity.z, 0.0001f,
                "Coupled assist should not lock forward velocity.");
        }

        [Test]
        public void FrameLockAssist_LocksTinyForwardVelocityToZero()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning, FlightAssistMode.FrameLockAssist);
            state.linearVelocity = new Vector3(0f, 0f, 0.04f);

            ShipSimulator.Step(ref state, tuning, null, 0.02f, default, out _);

            Assert.AreEqual(0f, state.linearVelocity.z, 0.0001f);
        }

        [Test]
        public void AttitudeAssist_LocksTinyAngularVelocityToZero()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning, FlightAssistMode.AttitudeAssist);
            state.angularVelocityRadians = new Vector3(0.008f, -0.008f, 0.008f);

            ShipSimulator.Step(ref state, tuning, null, 0.02f, default, out _);

            Assert.AreEqual(0f, state.angularVelocityRadians.x, 0.0001f);
            Assert.AreEqual(0f, state.angularVelocityRadians.y, 0.0001f);
            Assert.AreEqual(0f, state.angularVelocityRadians.z, 0.0001f);
        }

        [Test]
        public void ForwardThrust_BurnsFuel()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning);
            float fuelBefore = state.remainingFuelKg;

            ShipInputCommand input = new ShipInputCommand
            {
                thrustForward = 1f
            };

            ShipSimulator.Step(ref state, tuning, null, 0.1f, input, out _);

            Assert.Less(state.remainingFuelKg, fuelBefore,
                "Forward thrust should consume main engine fuel.");
        }

        [Test]
        public void StrafeThrust_BurnsHypergolic()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning);
            float hypergolicBefore = state.remainingHypergolicKg;

            ShipInputCommand input = new ShipInputCommand
            {
                thrustRight = 1f
            };

            ShipSimulator.Step(ref state, tuning, null, 0.1f, input, out _);

            Assert.Less(state.remainingHypergolicKg, hypergolicBefore,
                "Strafe thrust should consume hypergolic propellant.");
        }

        [Test]
        public void AngularThrust_BurnsHypergolic()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning);
            float hypergolicBefore = state.remainingHypergolicKg;

            ShipInputCommand input = new ShipInputCommand
            {
                pitch = 1f
            };

            ShipSimulator.Step(ref state, tuning, null, 0.1f, input, out _);

            Assert.Less(state.remainingHypergolicKg, hypergolicBefore,
                "Angular thrust should consume hypergolic propellant.");
        }

        [Test]
        public void CurrentMass_DecreasesAsPropellantBurns()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning);
            float massBefore = state.currentMassKg;

            ShipInputCommand input = new ShipInputCommand
            {
                thrustForward = 1f
            };

            ShipSimulator.Step(ref state, tuning, null, 0.1f, input, out _);

            Assert.Less(state.currentMassKg, massBefore,
                "Ship mass should decrease as propellant is consumed.");
        }

        [Test]
        public void CurrentMass_Equals_DryPlusRemainingPropellant()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning);

            ShipInputCommand input = new ShipInputCommand
            {
                thrustForward = 1f
            };

            ShipSimulator.Step(ref state, tuning, null, 0.1f, input, out _);

            float expected = tuning.dryMassKg + state.remainingFuelKg + state.remainingHypergolicKg;
            Assert.AreEqual(expected, state.currentMassKg, 0.01f,
                "currentMassKg should equal dry mass + remaining fuel + remaining hypergolic.");
        }

        [Test]
        public void NoFuel_BlocksMainEngineThrust()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning);
            state.remainingFuelKg = 0f;
            state.remainingHypergolicKg = 0f;
            state.currentMassKg = tuning.dryMassKg;

            ShipInputCommand input = new ShipInputCommand
            {
                thrustForward = 1f
            };

            ShipSimulator.Step(ref state, tuning, null, 0.1f, input, out _);

            // With no fuel and no hypergolic, forward thrust should be blocked
            Assert.IsTrue(state.appliedOutput.mainEngineFuelBlocked,
                "Main engine fuel should be marked blocked when fuel and hypergolic are empty.");
        }

        [Test]
        public void NoHypergolic_BlocksManeuverThrust()
        {
            ShipTuning tuning = CreateTuning();
            ShipState state = CreateState(tuning);
            state.remainingFuelKg = tuning.fuelCapacityKg;
            state.remainingHypergolicKg = 0f;
            state.currentMassKg = tuning.dryMassKg + tuning.fuelCapacityKg;

            ShipInputCommand input = new ShipInputCommand
            {
                thrustRight = 1f,
                pitch = 1f
            };

            ShipSimulator.Step(ref state, tuning, null, 0.1f, input, out _);

            // With no hypergolic, strafe and angular should be blocked
            Assert.AreEqual(0f, state.appliedOutput.appliedLocalLinear.x, 0.001f,
                "Lateral acceleration should be blocked when hypergolic is empty.");
            Assert.AreEqual(0f, state.appliedOutput.appliedLocalAngular.x, 0.001f,
                "Angular acceleration should be blocked when hypergolic is empty.");
        }
    }
}
