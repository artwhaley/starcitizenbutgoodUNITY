using System.Collections.Generic;
using UnityEngine;

namespace FlightModel
{
    public static class ShipHierarchyUtility
    {
        public static Transform FindChildRecursive(Transform root, params string[] names)
        {
            if (root == null)
            {
                return null;
            }

            foreach (string name in names)
            {
                Transform[] all = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in all)
                {
                    if (child.name == name)
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        public static void DisableMeshColliders(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }

        public static void CollectByNamePrefix(Transform root, string prefix, List<Transform> output)
        {
            if (root == null)
            {
                return;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith(prefix))
                {
                    output.Add(child);
                }
            }
        }

        public static void CollectByNameTokens(Transform root, string token, List<Transform> output)
        {
            if (root == null)
            {
                return;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Contains(token))
                {
                    output.Add(child);
                }
            }
        }
    }
}
