using UnityEngine;

namespace FlightModel
{
    public class ShipPilotSeat : MonoBehaviour
    {
        [SerializeField] ShipVehicle vehicle;
        [SerializeField] Transform seatTransform;

        public ShipVehicle Vehicle => vehicle;
        public Transform SeatTransform => seatTransform != null ? seatTransform : transform;
        public bool HasLocalPilot { get; private set; }

        void Awake()
        {
            if (vehicle == null)
            {
                vehicle = GetComponentInParent<ShipVehicle>();
            }
        }

        public bool TryEnterLocalPilot()
        {
            if (vehicle == null)
            {
                Debug.LogError(
                    "ShipPilotSeat: no ShipVehicle found in parent hierarchy. " +
                    "The seat must be authored under the ship vehicle hierarchy.",
                    this);
                return false;
            }

            if (HasLocalPilot)
            {
                return true;
            }

            HasLocalPilot = true;
            vehicle.SetLocalPilotSeat(this);
            return true;
        }

        public void ExitLocalPilot()
        {
            if (!HasLocalPilot)
            {
                return;
            }

            HasLocalPilot = false;
            if (vehicle != null)
            {
                vehicle.ClearLocalPilotSeat(this);
            }
        }
    }
}
