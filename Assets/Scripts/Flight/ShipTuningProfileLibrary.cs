using UnityEngine;

namespace FlightModel
{
    [CreateAssetMenu(menuName = "Flight/Tuning Profile Library", fileName = "TuningProfileLibrary")]
    public class ShipTuningProfileLibrary : ScriptableObject
    {
        public ShipTuning[] profiles;
    }
}
