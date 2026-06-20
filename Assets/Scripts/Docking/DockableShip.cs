using UnityEngine;

namespace FlightModel.Docking
{
    /// <summary>
    /// Lightweight aggregator on a dockable ship that bundles the authored
    /// references the docking capture controller needs. This belongs on the
    /// ship prefab; startup code may fill references, but should not create the
    /// component to hide missing prefab setup.
    /// </summary>
    public class DockableShip : MonoBehaviour
    {
        [SerializeField] ShipDockingNode shipDockingNode;
        [SerializeField] ShipFlightController flight;
        [SerializeField] DockingTargetProvider targetProvider;
        [SerializeField] Transform shipRoot;

        public ShipDockingNode ShipDockingNode
        {
            get => shipDockingNode;
            set => shipDockingNode = value;
        }

        public ShipFlightController Flight
        {
            get => flight;
            set => flight = value;
        }

        public DockingTargetProvider TargetProvider
        {
            get => targetProvider;
            set => targetProvider = value;
        }

        /// <summary>
        /// The transform whose pose is driven by ShipState each LateUpdate.
        /// Defaults to the component's own transform.
        /// </summary>
        public Transform ShipRoot
        {
            get => shipRoot != null ? shipRoot : transform;
            set => shipRoot = value;
        }
    }
}
