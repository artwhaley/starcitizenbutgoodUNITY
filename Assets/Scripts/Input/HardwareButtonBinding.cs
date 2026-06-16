using System;

namespace FlightModel
{
    [Serializable]
    public class HardwareButtonBinding
    {
        public string devicePath;
        public string displayName;
        public int buttonIndex;
        public bool enabled;
    }
}
