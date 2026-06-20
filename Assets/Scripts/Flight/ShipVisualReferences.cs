using System.Collections.Generic;
using UnityEngine;

namespace FlightModel
{
    public class ShipVisualReferences : MonoBehaviour
    {
        [SerializeField] Transform cog;
        [SerializeField] Transform fpvCameraNode;
        [SerializeField] Transform[] gunNodes;
        [SerializeField] Transform[] rcsNodes;
        [SerializeField] Transform[] engineNodes;
        [SerializeField] Vector3 markerActionAxisLocal = Vector3.up;

        public Transform Cog => cog;
        public Transform FpvCameraNode => fpvCameraNode;

        /// <summary>All gun node transforms, auto-discovered by name prefix.</summary>
        public IReadOnlyList<Transform> GunNodes => gunNodes;

        /// <summary>Backward-compat: first gun node, or null.</summary>
        public Transform GunNode1 => gunNodes != null && gunNodes.Length > 0 ? gunNodes[0] : null;

        /// <summary>Backward-compat: second gun node, or null.</summary>
        public Transform GunNode2 => gunNodes != null && gunNodes.Length > 1 ? gunNodes[1] : null;

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

            if (gunNodes == null || gunNodes.Length == 0)
            {
                var foundGuns = new List<Transform>();
                ShipHierarchyUtility.CollectByNamePrefix(root, "node_gun", foundGuns);
                if (foundGuns.Count > 0)
                {
                    gunNodes = foundGuns.ToArray();
                }
            }

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

            return fpvCameraNode != null && gunNodes != null && gunNodes.Length > 0;
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

            if (gunNodes == null || gunNodes.Length == 0)
            {
                Debug.LogWarning($"{name}: no gun nodes wired.", this);
            }
        }
    }
}
