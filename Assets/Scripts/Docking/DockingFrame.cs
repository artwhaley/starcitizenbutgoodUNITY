using UnityEngine;

namespace FlightModel.Docking
{
    public readonly struct DockingFrame
    {
        public readonly Vector3 position;
        public readonly Vector3 right;
        public readonly Vector3 up;
        public readonly Vector3 forward;
        public readonly Quaternion rotation;

        public DockingFrame(Vector3 position, Vector3 right, Vector3 up, Vector3 forward)
        {
            this.position = position;
            this.forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            this.up = ProjectOntoPlane(up, this.forward);
            if (this.up.sqrMagnitude < 1e-6f)
            {
                this.up = Vector3.up;
            }
            this.up.Normalize();
            this.right = Vector3.Cross(this.up, this.forward).normalized;
            this.up = Vector3.Cross(this.forward, this.right).normalized;
            rotation = Quaternion.LookRotation(this.forward, this.up);
        }

        public Vector3 WorldToFrameVector(Vector3 vector) => new(
            Vector3.Dot(vector, right),
            Vector3.Dot(vector, up),
            Vector3.Dot(vector, forward));

        public static Vector3 ProjectOntoPlane(Vector3 vector, Vector3 normal)
            => vector - normal * Vector3.Dot(vector, normal);
    }

    public static class DockingFrameUtility
    {
        public static DockingFrame FromTransform(Transform node, Vector3 actionAxisLocal)
        {
            if (node == null)
            {
                return new DockingFrame(Vector3.zero, Vector3.right, Vector3.up, Vector3.forward);
            }

            return FromPose(node.position, node.rotation, actionAxisLocal);
        }

        public static DockingFrame FromPose(Vector3 position, Quaternion rotation, Vector3 actionAxisLocal)
        {
            Vector3 localForward = actionAxisLocal.sqrMagnitude > 1e-6f
                ? actionAxisLocal.normalized
                : BlenderImportedAxes.DefaultActionAxisLocal;

            Vector3 localUp = ChooseSecondaryLocalAxis(localForward);
            Vector3 worldForward = rotation * localForward;
            Vector3 worldUp = rotation * localUp;
            return new DockingFrame(position, Vector3.zero, worldUp, worldForward);
        }

        static Vector3 ChooseSecondaryLocalAxis(Vector3 localForward)
        {
            if (Mathf.Abs(Vector3.Dot(localForward, Vector3.forward)) < 0.95f)
            {
                return Vector3.forward;
            }

            if (Mathf.Abs(Vector3.Dot(localForward, Vector3.up)) < 0.95f)
            {
                return Vector3.up;
            }

            return Vector3.right;
        }
    }
}
