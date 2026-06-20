using UnityEngine;

namespace FlightModel.Docking
{
    /// <summary>
    /// Marker on or under the player ship's hierarchy identifying where the docking
    /// port attachment is. The action axis is treated as the node's outward-facing
    /// direction; default uses the project's Blender import axis convention.
    /// Ship port is deployable; <see cref="IsDockingActive"/> is true only when
    /// the node is fully deployed.
    /// </summary>
    public class ShipDockingNode : MonoBehaviour
    {
        [SerializeField] Transform node;
        [SerializeField] Vector3 actionAxisLocal = BlenderImportedAxes.DefaultActionAxisLocal;
        [SerializeField] bool startsDeployed = false;

        DockingPortDeploymentState deploymentState;

        public Transform NodeTransform => node != null ? node : transform;
        public Vector3 ActionAxisLocal => actionAxisLocal;
        public DockingPortDeploymentState DeploymentState => deploymentState;

        public bool IsDeployed =>
            deploymentState == DockingPortDeploymentState.Deployed
            || deploymentState == DockingPortDeploymentState.Deploying;

        public bool IsDockingActive =>
            deploymentState == DockingPortDeploymentState.Deployed;

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
        /// Build a world rotation whose camera +Z aligns with the authored docking
        /// action axis while preserving the node's authored roll. This must not
        /// choose an arbitrary world-up reference; doing so can snap the camera as
        /// the ship rotates across axis thresholds.
        /// </summary>
        public Quaternion GetCameraAlignedRotation()
            => DockingFrameUtility.FromTransform(NodeTransform, actionAxisLocal).rotation;

        void Awake()
        {
            deploymentState = startsDeployed
                ? DockingPortDeploymentState.Deployed
                : DockingPortDeploymentState.Retracted;
        }

        public void SetDeployed(bool deployed)
        {
            deploymentState = deployed
                ? DockingPortDeploymentState.Deployed
                : DockingPortDeploymentState.Retracted;
        }

        public void ToggleDeployed()
        {
            SetDeployed(!IsDeployed);
        }

        public static bool IsPotentialNodeName(string transformName)
        {
            if (string.IsNullOrEmpty(transformName))
            {
                return false;
            }

            return transformName == "node_docking"
                || transformName.StartsWith("node_docking_");
        }
    }
}
