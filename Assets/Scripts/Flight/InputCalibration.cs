using UnityEngine;

namespace FlightModel
{
    [System.Serializable]
    public class AxisCalibration
    {
        public bool invert;
        public float deadzone = 0.05f;
        public float scale = 0.5f;
        public float curveExponent = 1f;
    }

    public static class InputCalibrationUtility
    {
        public static float Apply(float raw, AxisCalibration c)
        {
            if (c == null)
            {
                return Mathf.Clamp(raw, -1f, 1f);
            }

            float value = Mathf.Clamp(raw, -1f, 1f);
            if (c.invert)
            {
                value = -value;
            }

            if (Mathf.Abs(value) < c.deadzone)
            {
                return 0f;
            }

            float sign = Mathf.Sign(value);
            float normalized = (Mathf.Abs(value) - c.deadzone) / (1f - c.deadzone);
            float curved = Mathf.Pow(normalized, c.curveExponent);
            return sign * curved * c.scale;
        }
    }
}
