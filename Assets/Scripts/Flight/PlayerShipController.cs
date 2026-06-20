using UnityEngine;

namespace FlightModel
{
    /// <summary>
    /// Compatibility shell for older scene/prefab references. Ship behavior now
    /// lives on ShipVehicle plus LocalPlayerVehicleController so the ship can be
    /// a vehicle, not the player character.
    /// </summary>
    public class PlayerShipController : MonoBehaviour
    {
        [SerializeField] ShipVehicle vehicle;

        public ShipVehicle Vehicle => vehicle;

        void Awake()
        {
            if (vehicle == null)
            {
                vehicle = GetComponent<ShipVehicle>();
            }

            if (vehicle == null)
            {
                Debug.LogError(
                    "PlayerShipController: ShipVehicle missing from prefab. " +
                    "Add ShipVehicle explicitly; PlayerShipController no longer creates or owns ship behavior.",
                    this);
            }
        }
    }
}
