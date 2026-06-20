using UnityEngine;

namespace FlightModel.Docking
{
    /// <summary>
    /// Marker on a station scene object identifying where a ship can dock. The
    /// action axis is the port's outward-facing direction (away from the station).
    /// <see cref="ShipAttachTransform"/> is the world-space target where the ship's
    /// docking node is placed when docked; for phase 0.1 it equals the node transform,
    /// but future animated ports can move the attach transform during retract and
    /// extend sequences and the docked ship will follow.
    /// </summary>
    public class StationDockingPort : MonoBehaviour
    {
        [SerializeField] string portId;
        [SerializeField] DockingPortClass portClass = DockingPortClass.SmallShip;
        [SerializeField] Transform node;
        [SerializeField] Transform shipAttachTransform;
        [SerializeField] Vector3 actionAxisLocal = BlenderImportedAxes.DefaultActionAxisLocal;
        [SerializeField] bool startsAvailable = true;

        DockingPortDeploymentState availability;

        public string PortId => string.IsNullOrEmpty(portId) ? name : portId;
        public DockingPortClass PortClass => portClass;
        public Transform NodeTransform => node != null ? node : transform;
        public Vector3 ActionAxisLocal => actionAxisLocal;
        public Transform ShipAttachTransform =>
            shipAttachTransform != null ? shipAttachTransform : NodeTransform;

        public DockingPortDeploymentState DeploymentState => availability;
        public bool IsAvailable => availability == DockingPortDeploymentState.Deployed;
        public bool IsDockingActive => availability == DockingPortDeploymentState.Deployed;

        public Vector3 WorldPosition => NodeTransform.position;

        public Vector3 WorldForward
        {
            get
            {
                Vector3 dir = BlenderImportedAxes.GetWorldActionDirection(NodeTransform, actionAxisLocal);
                return dir.sqrMagnitude > 1e-6f ? dir.normalized : NodeTransform.forward;
            }
        }

        public Quaternion WorldRotation => NodeTransform.rotation;

        /// <summary>
        /// Assign only fields that have not already been set (e.g. from a hand-authored
        /// inspector or earlier configure call). Preserves explicit inspector values;
        /// the runtime auto-wirer uses this to fill in blanks without overwriting them.
        /// </summary>
        public void ConfigureReferences(
            Transform nodeTransform,
            Transform shipAttach,
            DockingPortClass portClassValue)
        {
            if (node == null) node = nodeTransform;
            if (shipAttachTransform == null)
            {
                shipAttachTransform = shipAttach != null ? shipAttach : nodeTransform;
            }
            if (portClass == DockingPortClass.Unknown)
            {
                portClass = portClassValue;
            }
        }

        void Awake()
        {
            availability = startsAvailable
                ? DockingPortDeploymentState.Deployed
                : DockingPortDeploymentState.Disabled;
        }

        public void SetAvailable(bool available)
        {
            availability = available
                ? DockingPortDeploymentState.Deployed
                : DockingPortDeploymentState.Disabled;
        }

        public static bool IsPotentialPortName(string transformName)
        {
            if (string.IsNullOrEmpty(transformName))
            {
                return false;
            }

            return transformName == "node_docking_port"
                || transformName == "node_docking"
                || transformName == "StationDockingPort"
                || transformName == "station_docking_port"
                || transformName.StartsWith("node_docking_");
        }

        public static Transform FindShipAttachChild(Transform marker, Transform fallback)
        {
            if (marker == null)
            {
                return fallback;
            }

            for (int i = 0; i < marker.childCount; i++)
            {
                Transform child = marker.GetChild(i);
                if (child != null && (child.name == "ship_attach"
                    || child.name == "node_ship_attach"))
                {
                    return child;
                }
            }

            return fallback;
        }
    }
}
