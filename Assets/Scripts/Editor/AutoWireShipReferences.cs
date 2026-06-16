using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlightModel.Editor
{
    public static class AutoWireShipReferences
    {
        [MenuItem("FlightModel/Auto-Wire Ship References")]
        static void WireSelected()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("Select a ship root with ShipVisualReferences.");
                return;
            }

            ShipVisualReferences refsComponent = Selection.activeGameObject.GetComponent<ShipVisualReferences>();
            if (refsComponent == null)
            {
                Debug.LogWarning("Selected object has no ShipVisualReferences.");
                return;
            }

            SerializedObject so = new SerializedObject(refsComponent);
            so.FindProperty("cog").objectReferenceValue = FindChild(Selection.activeTransform, "COG", "uwing");
            so.FindProperty("fpvCameraNode").objectReferenceValue = FindChild(Selection.activeTransform, "node_camera_fpv");
            so.FindProperty("gunNode1").objectReferenceValue = FindChild(Selection.activeTransform, "node_gun1");
            so.FindProperty("gunNode2").objectReferenceValue = FindChild(Selection.activeTransform, "node_gun2");

            List<Transform> rcs = new();
            CollectRcs(Selection.activeTransform, rcs);
            SerializedProperty rcsProp = so.FindProperty("rcsNodes");
            rcsProp.arraySize = rcs.Count;
            for (int i = 0; i < rcs.Count; i++)
            {
                rcsProp.GetArrayElementAtIndex(i).objectReferenceValue = rcs[i];
            }

            List<Transform> engines = new();
            CollectEngines(Selection.activeTransform, engines);
            SerializedProperty engineProp = so.FindProperty("engineNodes");
            engineProp.arraySize = engines.Count;
            for (int i = 0; i < engines.Count; i++)
            {
                engineProp.GetArrayElementAtIndex(i).objectReferenceValue = engines[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(refsComponent);
            Debug.Log("Auto-wired ship references.");
        }

        static Transform FindChild(Transform root, params string[] names)
        {
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

        static void CollectRcs(Transform root, List<Transform> output)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith("rcs_"))
                {
                    output.Add(child);
                }
            }
        }

        static void CollectEngines(Transform root, List<Transform> output)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                string name = child.name.ToLowerInvariant();
                if (name == "engine" || name.StartsWith("engine."))
                {
                    output.Add(child);
                }
            }
        }
    }
}
