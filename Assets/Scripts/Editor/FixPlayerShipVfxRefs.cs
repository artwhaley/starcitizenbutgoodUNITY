using UnityEditor;
using UnityEngine;

namespace FlightModel.Editor
{
    public static class FixPlayerShipVfxRefs
    {
        const string PlayerShipPath = "Assets/Prefabs/Ships/PF_PlayerShip.prefab";

        [MenuItem("FlightModel/Fix Player Ship VFX References")]
        static void FixReferences()
        {
            GameObject rcsPuff = AssetDatabase.LoadAssetAtPath<GameObject>(VfxPrefabResolver.RcsPuffAssetPath);
            if (rcsPuff == null)
            {
                Debug.LogError("PF_RcsPuff not found at expected path.");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerShipPath);
            if (prefabRoot == null)
            {
                Debug.LogError("PF_PlayerShip not found.");
                return;
            }

            PlayerShipController controller = prefabRoot.GetComponent<PlayerShipController>();
            if (controller == null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                Debug.LogError("PF_PlayerShip missing PlayerShipController.");
                return;
            }

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("rcsPuffPrefab").objectReferenceValue = rcsPuff;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerShipPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            AssetDatabase.SaveAssets();
            Debug.Log("Reassigned PF_PlayerShip.rcsPuffPrefab to PF_RcsPuff.");
        }
    }
}
