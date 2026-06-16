using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FlightModel
{
    public static class VfxPrefabResolver
    {
        public const string RcsPuffAssetPath = "Assets/Prefabs/VFX/PF_RcsPuff.prefab";

        public static GameObject ResolveRcsPuff(GameObject assigned)
        {
            if (IsUsablePrefab(assigned))
            {
                return assigned;
            }

#if UNITY_EDITOR
            GameObject loaded = AssetDatabase.LoadAssetAtPath<GameObject>(RcsPuffAssetPath);
            if (loaded != null)
            {
                return loaded;
            }
#endif

            Debug.LogWarning("VfxPrefabResolver: PF_RcsPuff is missing; RCS VFX disabled.");
            return null;
        }

        public static bool IsUsablePrefab(UnityEngine.Object asset)
        {
            if (ReferenceEquals(asset, null))
            {
                return false;
            }

            try
            {
                return asset != null;
            }
            catch (MissingReferenceException)
            {
                return false;
            }
        }
    }
}
