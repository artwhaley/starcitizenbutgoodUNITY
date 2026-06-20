using UnityEngine;

namespace FlightModel
{
    /// <summary>
    /// Blender empties authored with local +Z as the look / jet / firing axis
    /// import as Unity transforms where that axis becomes local +Y (standard
    /// Z-up to Y-up FBX conversion with bakeAxisConversion off).
    /// Use TransformDirection(Vector3.up) to get the correct world-forward.
    /// </summary>
    [System.Serializable]
    public struct WeaponHardpoint
    {
        public Transform node;

        public bool IsValid => node != null;

        public Vector3 WorldPosition => node != null ? node.position : Vector3.zero;

        /// <summary>
        /// Blender +Z maps to Unity local +Y after FBX import.
        /// </summary>
        public Vector3 WorldForward => node != null
            ? node.TransformDirection(BlenderImportedAxes.DefaultActionAxisLocal)
            : Vector3.forward;
    }
}
