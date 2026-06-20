using NUnit.Framework;

namespace FlightModel.Tests
{
    public class RcsThrusterMatcherTests
    {
        [Test]
        public void PositivePitch_UsesFrontDownAndBackUpNozzles()
        {
            ShipInputCommand command = new() { pitch = 1f };

            AssertActive(command, "rcs_frontdownleft", "rcs_frontdownright", "rcs_backupleft", "rcs_backupright");
            AssertInactive(command, "rcs_frontupleft", "rcs_frontupright", "rcs_backdownleft", "rcs_backdownright");
        }

        [Test]
        public void PositivePitch_DoesNotLightEveryTopAndBottomNozzle()
        {
            ShipInputCommand command = new() { pitch = 1f };

            string[] verticalNozzles =
            {
                "rcs_frontdownleft",
                "rcs_frontdownright",
                "rcs_backdownleft",
                "rcs_backdownright",
                "rcs_frontupleft",
                "rcs_frontupright",
                "rcs_backupleft",
                "rcs_backupright"
            };

            int activeCount = 0;
            for (int i = 0; i < verticalNozzles.Length; i++)
            {
                if (RcsThrusterMatcher.GetEmissionStrength(verticalNozzles[i], command) > 0f)
                {
                    activeCount++;
                }
            }

            Assert.AreEqual(4, activeCount);
        }

        [Test]
        public void NegativePitch_UsesFrontUpAndBackDownNozzles()
        {
            ShipInputCommand command = new() { pitch = -1f };

            AssertActive(command, "rcs_frontupleft", "rcs_frontupright", "rcs_backdownleft", "rcs_backdownright");
            AssertInactive(command, "rcs_frontdownleft", "rcs_frontdownright", "rcs_backupleft", "rcs_backupright");
        }

        [Test]
        public void PositiveRoll_UsesLeftDownAndRightUpNozzles()
        {
            ShipInputCommand command = new() { roll = 1f };

            AssertActive(command, "rcs_frontdownleft", "rcs_backdownleft", "rcs_frontupright", "rcs_backupright");
            AssertInactive(command, "rcs_frontupleft", "rcs_backupleft", "rcs_frontdownright", "rcs_backdownright");
        }

        [Test]
        public void NegativeRoll_UsesLeftUpAndRightDownNozzles()
        {
            ShipInputCommand command = new() { roll = -1f };

            AssertActive(command, "rcs_frontupleft", "rcs_backupleft", "rcs_frontdownright", "rcs_backdownright");
            AssertInactive(command, "rcs_frontdownleft", "rcs_backdownleft", "rcs_frontupright", "rcs_backupright");
        }

        [Test]
        public void PositiveYaw_UsesFrontLeftAndBackRightNozzles()
        {
            ShipInputCommand command = new() { yaw = 1f };

            Assert.Greater(RcsThrusterMatcher.GetEmissionStrength("rcs_frontoutleft", command), 0f);
            Assert.Greater(RcsThrusterMatcher.GetEmissionStrength("rcs_backoutright", command), 0f);
            Assert.AreEqual(0f, RcsThrusterMatcher.GetEmissionStrength("rcs_frontoutright", command));
            Assert.AreEqual(0f, RcsThrusterMatcher.GetEmissionStrength("rcs_backoutleft", command));
        }

        [Test]
        public void PositiveVerticalThrust_UsesDownNozzles()
        {
            ShipInputCommand command = new() { thrustUp = 1f };

            AssertActive(command, "rcs_frontdownleft", "rcs_frontdownright", "rcs_backdownleft", "rcs_backdownright");
            AssertInactive(command, "rcs_frontupleft", "rcs_frontupright", "rcs_backupleft", "rcs_backupright");
        }

        [Test]
        public void NegativeVerticalThrust_UsesUpNozzles()
        {
            ShipInputCommand command = new() { thrustUp = -1f };

            AssertActive(command, "rcs_frontupleft", "rcs_frontupright", "rcs_backupleft", "rcs_backupright");
            AssertInactive(command, "rcs_frontdownleft", "rcs_frontdownright", "rcs_backdownleft", "rcs_backdownright");
        }

        [Test]
        public void BoostForwardOutput_LightsCommittedForwardAndBoostNozzles()
        {
            ShipThrusterOutput output = new()
            {
                mainEngineForward = 1f,
                maneuverForward = 0f,
                boostActive = true
            };

            ShipInputCommand command = output.ToMatcherCommand();

            Assert.AreEqual(1f, command.thrustForward);
            AssertActive(command, "rcs_backaftleft", "rcs_backaftright", "rcs_frontforwardleft", "rcs_frontforwardright");
        }

        static void AssertActive(ShipInputCommand command, params string[] nodes)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                Assert.Greater(RcsThrusterMatcher.GetEmissionStrength(nodes[i], command), 0f, nodes[i]);
            }
        }

        static void AssertInactive(ShipInputCommand command, params string[] nodes)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                Assert.AreEqual(0f, RcsThrusterMatcher.GetEmissionStrength(nodes[i], command), nodes[i]);
            }
        }
    }
}
