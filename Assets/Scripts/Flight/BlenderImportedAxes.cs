using UnityEngine;

namespace FlightModel
{
    /// <summary>
    /// Blender empties authored with local +Z as look / jet direction import as Unity transforms
    /// where that axis is typically local +Y (Z-up to Y-up FBX conversion with bakeAxisConversion off).
    /// Use <see cref="Quaternion.FromToRotation"/> so Unity systems that emit along +Z align with the marker.
    /// If you enable Bake Axis Conversion on the FBX, try Vector3.forward instead.
    /// </summary>
    public static class BlenderImportedAxes
    {
        public static readonly Vector3 DefaultActionAxisLocal = Vector3.up;

        public static Vector3 GetWorldActionDirection(Transform marker, Vector3 actionAxisLocal)
        {
            return marker != null ? marker.TransformDirection(actionAxisLocal) : Vector3.forward;
        }

        public static Quaternion GetActionRotation(Transform marker, Vector3 actionAxisLocal)
        {
            if (marker == null)
            {
                return Quaternion.identity;
            }

            Vector3 worldDirection = GetWorldActionDirection(marker, actionAxisLocal);
            if (worldDirection.sqrMagnitude < 1e-6f)
            {
                return marker.rotation;
            }

            return Quaternion.LookRotation(worldDirection);
        }

        public static Quaternion GetActionLocalOffset(Vector3 actionAxisLocal)
        {
            return Quaternion.FromToRotation(Vector3.forward, actionAxisLocal);
        }

        /// <summary>
        /// Aligns particle +Z to the marker jet axis authored in Blender.
        /// </summary>
        public static Quaternion GetPlumeLocalRotation(Transform marker, Vector3 actionAxisLocal, Transform outwardFrom = null)
        {
            if (marker == null)
            {
                return Quaternion.identity;
            }

            Vector3 jetLocal = actionAxisLocal.sqrMagnitude > 1e-6f ? actionAxisLocal.normalized : DefaultActionAxisLocal;

            return Quaternion.FromToRotation(Vector3.forward, jetLocal);
        }
    }
}
