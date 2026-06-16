using System.Collections.Generic;
using UnityEngine;

namespace FlightModel
{
    public class ShipVisualReferences : MonoBehaviour
    {
        [SerializeField] Transform cog;
        [SerializeField] Transform fpvCameraNode;
        [SerializeField] Transform gunNode1;
        [SerializeField] Transform gunNode2;
        [SerializeField] Transform[] rcsNodes;
        [SerializeField] Transform[] engineNodes;
        [SerializeField] Vector3 markerActionAxisLocal = Vector3.up;

        public Transform Cog => cog;
        public Transform FpvCameraNode => fpvCameraNode;
        public Transform GunNode1 => gunNode1;
        public Transform GunNode2 => gunNode2;
        public IReadOnlyList<Transform> RcsNodes => rcsNodes;
        public IReadOnlyList<Transform> EngineNodes => engineNodes;
        public Vector3 MarkerActionAxisLocal => markerActionAxisLocal;

        public Vector3 GetWorldActionDirection(Transform marker)
            => BlenderImportedAxes.GetWorldActionDirection(marker, markerActionAxisLocal);

        public Quaternion GetActionRotation(Transform marker)
            => BlenderImportedAxes.GetActionRotation(marker, markerActionAxisLocal);

        void Awake() => TryAutoWire();

        public bool TryAutoWire()
        {
            Transform root = transform;

            cog ??= root.name is "uwing" or "uwing2" or "COG" ? root : ShipHierarchyUtility.FindChildRecursive(root, "uwing", "uwing2", "COG");
            fpvCameraNode ??= ShipHierarchyUtility.FindChildRecursive(root, "node_camera_fpv");
            gunNode1 ??= ShipHierarchyUtility.FindChildRecursive(root, "node_gun1");
            gunNode2 ??= ShipHierarchyUtility.FindChildRecursive(root, "node_gun2");

            if (rcsNodes == null || rcsNodes.Length == 0)
            {
                var foundRcs = new List<Transform>();
                ShipHierarchyUtility.CollectByNamePrefix(root, "rcs_", foundRcs);
                if (foundRcs.Count > 0)
                {
                    rcsNodes = foundRcs.ToArray();
                }
            }

            if (engineNodes == null || engineNodes.Length == 0)
            {
                var foundEngines = new List<Transform>();
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    string name = child.name.ToLowerInvariant();
                    if (name == "engine" || name.StartsWith("engine."))
                    {
                        foundEngines.Add(child);
                    }
                }

                if (foundEngines.Count > 0)
                {
                    engineNodes = foundEngines.ToArray();
                }
            }

            return fpvCameraNode != null && gunNode1 != null && gunNode2 != null;
        }

        void OnValidate()
        {
            if (cog == null)
            {
                Debug.LogWarning($"{name}: missing COG reference.", this);
            }

            if (fpvCameraNode == null)
            {
                Debug.LogWarning($"{name}: missing FPV camera node.", this);
            }

            if (gunNode1 == null)
            {
                Debug.LogWarning($"{name}: missing gun node 1.", this);
            }

            if (gunNode2 == null)
            {
                Debug.LogWarning($"{name}: missing gun node 2.", this);
            }
        }
    }
}
