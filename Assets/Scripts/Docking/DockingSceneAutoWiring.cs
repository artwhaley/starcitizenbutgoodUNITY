using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlightModel.Docking
{
    /// <summary>
    /// Runtime scene-wiring helper. After scene load, this class walks the
    /// scene root named 'station' and attaches <see cref="StationDockingPort"/>
    /// components to children whose transform name matches the documented
    /// docking-port marker conventions.
    ///
    /// Important contract: this helper NEVER creates a docking marker GameObject
    /// out of thin air. Docking ports are authored into the station model by
    /// the asset team; if the station root has no such child, the helper logs
    /// an explicit warning and stops. Pasting a fake marker here breaks the
    /// binding between the docking pose and the real station FBX the moment
    /// the asset team attaches an actual port.
    /// </summary>
    public static class DockingSceneAutoWiring
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoWireScene()
        {
            GameObject stationRoot = FindStationRoot();
            if (stationRoot == null)
            {
                return;
            }

            EnsureStationPortChildren(stationRoot);
        }

        static GameObject FindStationRoot()
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root != null && root.name == "station")
                {
                    return root;
                }
            }
            return null;
        }

        static void EnsureStationPortChildren(GameObject stationRoot)
        {
            List<Transform> portMarkers = new List<Transform>();
            CollectPortMarkers(stationRoot.transform, portMarkers);

            if (portMarkers.Count == 0)
            {
                Debug.LogError(
                    $"DockingSceneAutoWiring: scene has a '{stationRoot.name}' scene root but no authored " +
                    $"docking port child was found. Add a child whose name is one of: " +
                    $"{string.Join(", ", GetAcceptedMarkerNames())}. " +
                    $"The station model must own this marker; nothing is created at runtime. " +
                    $"No StationDockingPort will be available in this scene.",
                    stationRoot);
                return;
            }

            for (int i = 0; i < portMarkers.Count; i++)
            {
                Transform marker = portMarkers[i];
                StationDockingPort port = marker.GetComponent<StationDockingPort>();
                if (port == null)
                {
                    port = marker.gameObject.AddComponent<StationDockingPort>();
                }

                Transform attach = StationDockingPort.FindShipAttachChild(marker, marker);
                port.ConfigureReferences(marker, attach, DockingPortClass.SmallShip);
            }
        }

        static IEnumerable<string> GetAcceptedMarkerNames()
        {
            yield return "node_docking_port";
            yield return "node_docking";
            yield return "StationDockingPort";
            yield return "station_docking_port";
            // Anything starting with "node_docking_" is also accepted.
            // See StationDockingPort.IsPotentialPortName for the full list.
        }

        static void CollectPortMarkers(Transform root, List<Transform> portMarkers)
        {
            if (root == null)
            {
                return;
            }

            // Walk descendants breadth-first so direct authored port children
            // are discovered before deeply nested authored port markers.
            Queue<Transform> queue = new Queue<Transform>();
            for (int i = 0; i < root.childCount; i++)
            {
                queue.Enqueue(root.GetChild(i));
            }

            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();
                if (current == null)
                {
                    continue;
                }

                if (StationDockingPort.IsPotentialPortName(current.name)
                    && !portMarkers.Contains(current))
                {
                    portMarkers.Add(current);
                }

                // Keep searching deeper in case the station has nested ports.
                for (int i = 0; i < current.childCount; i++)
                {
                    queue.Enqueue(current.GetChild(i));
                }
            }
        }
    }
}
