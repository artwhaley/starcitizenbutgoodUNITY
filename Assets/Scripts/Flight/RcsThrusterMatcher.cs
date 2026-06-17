using UnityEngine;

namespace FlightModel
{
    public static class RcsThrusterMatcher
    {
        const float InputThreshold = 0.08f;

        static readonly string[] ThrustForward =
        {
            "rcs_backaftleft",
            "rcs_backaftright"
        };

        static readonly string[] ThrustBackward =
        {
            "rcs_frontforwardleft",
            "rcs_frontforwardright"
        };

        static readonly string[] ThrustRight =
        {
            "rcs_frontoutleft",
            "rcs_backoutleft"
        };

        static readonly string[] ThrustLeft =
        {
            "rcs_frontoutright",
            "rcs_backoutright"
        };

        static readonly string[] ThrustUp =
        {
            "rcs_frontdownleft",
            "rcs_frontdownright",
            "rcs_backdownleft",
            "rcs_backdownright"
        };

        static readonly string[] ThrustDown =
        {
            "rcs_frontupleft",
            "rcs_frontupright",
            "rcs_backupleft",
            "rcs_backupright"
        };

        static readonly string[] PitchPositive =
        {
            "rcs_frontdownleft",
            "rcs_frontdownright",
            "rcs_backupleft",
            "rcs_backupright"
        };

        static readonly string[] PitchNegative =
        {
            "rcs_frontupleft",
            "rcs_frontupright",
            "rcs_backdownleft",
            "rcs_backdownright"
        };

        static readonly string[] YawPositive =
        {
            "rcs_frontoutleft",
            "rcs_backoutright"
        };

        static readonly string[] YawNegative =
        {
            "rcs_frontoutright",
            "rcs_backoutleft"
        };

        static readonly string[] RollPositive =
        {
            "rcs_frontdownleft",
            "rcs_backdownleft",
            "rcs_frontupright",
            "rcs_backupright"
        };

        static readonly string[] RollNegative =
        {
            "rcs_frontupleft",
            "rcs_backupleft",
            "rcs_frontdownright",
            "rcs_backdownright"
        };

        public static bool ShouldEmit(string nodeName, in ShipInputCommand command)
            => GetEmissionStrength(nodeName, command) > 0f;

        public static float GetEmissionStrength(string nodeName, in ShipInputCommand command)
        {
            string canonical = Canonicalize(nodeName);
            if (string.IsNullOrEmpty(canonical))
            {
                return 0f;
            }

            float strength = 0f;
            AddStrength(ref strength, command.thrustForward, ThrustForward, canonical);
            AddStrength(ref strength, -command.thrustForward, ThrustBackward, canonical);
            AddStrength(ref strength, command.thrustRight, ThrustRight, canonical);
            AddStrength(ref strength, -command.thrustRight, ThrustLeft, canonical);
            AddStrength(ref strength, command.thrustUp, ThrustUp, canonical);
            AddStrength(ref strength, -command.thrustUp, ThrustDown, canonical);
            AddStrength(ref strength, command.pitch, PitchPositive, canonical);
            AddStrength(ref strength, -command.pitch, PitchNegative, canonical);
            AddStrength(ref strength, command.yaw, YawPositive, canonical);
            AddStrength(ref strength, -command.yaw, YawNegative, canonical);
            AddStrength(ref strength, command.roll, RollPositive, canonical);
            AddStrength(ref strength, -command.roll, RollNegative, canonical);

            if (command.boost && command.thrustForward > InputThreshold)
            {
                AddStrength(ref strength, command.thrustForward, ThrustBackward, canonical);
            }

            if (command.brake)
            {
                strength = Mathf.Max(strength, Random.value < 0.08f ? 0.6f : 0f);
            }

            return strength;
        }

        static void AddStrength(ref float strength, float axisValue, string[] names, string canonical)
        {
            if (axisValue <= InputThreshold || !Contains(names, canonical))
            {
                return;
            }

            float normalized = Mathf.InverseLerp(InputThreshold, 1f, Mathf.Clamp01(axisValue));
            strength = Mathf.Max(strength, normalized);
        }

        static bool Contains(string[] names, string canonical)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == canonical)
                {
                    return true;
                }
            }

            return false;
        }

        static string Canonicalize(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName))
            {
                return string.Empty;
            }

            string lower = nodeName.ToLowerInvariant();
            int suffixIndex = lower.IndexOf('.');
            if (suffixIndex >= 0)
            {
                lower = lower[..suffixIndex];
            }

            return lower.Trim();
        }
    }
}
