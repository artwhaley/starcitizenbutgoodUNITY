using UnityEngine;

namespace FlightModel
{
    public class DockingZone : MonoBehaviour
    {
        [SerializeField] float requiredSpeedMax = 5f;
        [SerializeField] float requiredAlignDot = 0.95f;

        void OnTriggerStay(Collider other)
        {
            ShipFlightController flight = other.GetComponentInParent<ShipFlightController>();
            if (flight == null)
            {
                flight = other.GetComponent<ShipFlightController>();
            }

            if (flight == null)
            {
                return;
            }

            float speed = flight.State.linearVelocity.magnitude;
            Vector3 shipForward = flight.State.rotation * Vector3.forward;
            float align = Vector3.Dot(shipForward, transform.forward);
            if (speed <= requiredSpeedMax && align >= requiredAlignDot)
            {
                Debug.Log("DOCKING READY");
            }
        }
    }
}
