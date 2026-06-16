using System;

namespace FlightModel
{
    [Serializable]
    public class HardwareAxisBinding
    {
        public string devicePath;
        public string displayName;
        public int axisIndex;
        public bool enabled;
        public AxisCalibration calibration = new();
    }
}
