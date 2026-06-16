using UnityEngine;

namespace FlightModel
{
    public class ShipCameraController : MonoBehaviour
    {
        [SerializeField] Transform cogTransform;
        [SerializeField] Transform cockpitCameraMount;
        [SerializeField] Transform shipFpvNode;
        [SerializeField] Camera cockpitCamera;
        [SerializeField] Transform externalCameraRig;
        [SerializeField] Transform externalCameraPivot;
        [SerializeField] Camera externalCamera;

        [Header("Defaults (meters / degrees)")]
        [SerializeField] float cockpitFov = 95f;
        [SerializeField] float externalFov = 75f;
        [SerializeField] float externalDistance = 10.1f;
        [SerializeField] float externalDefaultPan = 0f;
        [SerializeField] float externalDefaultTilt = 10f;
        [SerializeField] float externalMinDistance = 4f;
        [SerializeField] float externalMaxDistance = 40f;
        [SerializeField] float externalMinTilt = -30f;
        [SerializeField] float externalMaxTilt = 60f;
        [SerializeField] float panSpeed = 90f;
        [SerializeField] float tiltSpeed = 60f;
        [SerializeField] float zoomSpeed = 2f;

        bool externalActive;
        float panDegrees;
        float tiltDegrees;

        public bool IsExternalActive => externalActive;
        public float ExternalPanDegrees => panDegrees;
        public float ExternalTiltDegrees => tiltDegrees;
        public float ExternalDistance => externalDistance;
        public float ActiveFov => externalActive ? externalFov : cockpitFov;

        void Awake() => ResetExternalCamera();

        void LateUpdate()
        {
            if (cogTransform == null)
            {
                return;
            }

            if (shipFpvNode != null && cockpitCameraMount != null)
            {
                Quaternion lookRotation = cogTransform != null ? cogTransform.rotation : shipFpvNode.rotation;
                cockpitCameraMount.SetPositionAndRotation(shipFpvNode.position, lookRotation);
            }

            if (externalCameraRig != null)
            {
                externalCameraRig.SetPositionAndRotation(cogTransform.position, cogTransform.rotation);
            }

            if (externalCameraPivot != null)
            {
                externalCameraPivot.localRotation = Quaternion.Euler(tiltDegrees, panDegrees, 0f);
            }

            if (externalCamera != null)
            {
                externalCamera.transform.localPosition = new Vector3(0f, 0f, -externalDistance);
            }

            if (cockpitCamera != null)
            {
                cockpitCamera.fieldOfView = cockpitFov;
            }

            if (externalCamera != null)
            {
                externalCamera.fieldOfView = externalFov;
            }
        }

        public void ToggleView()
        {
            externalActive = !externalActive;
            UpdateActiveCamera();
        }

        public void ApplyExternalPanTilt(float panInput, float tiltInput, float deltaTime)
        {
            if (!externalActive)
            {
                return;
            }

            panDegrees += panInput * panSpeed * deltaTime;
            tiltDegrees = Mathf.Clamp(tiltDegrees + tiltInput * tiltSpeed * deltaTime, externalMinTilt, externalMaxTilt);
        }

        public void ApplyZoomDelta(float scrollDelta)
        {
            if (!externalActive || Mathf.Approximately(scrollDelta, 0f))
            {
                return;
            }

            externalDistance = Mathf.Clamp(externalDistance - scrollDelta * zoomSpeed, externalMinDistance, externalMaxDistance);
        }

        public Camera GetActiveCamera() => externalActive && externalCamera != null ? externalCamera : cockpitCamera;

        public void SetFpvNode(Transform fpvNode) => shipFpvNode = fpvNode;

        public void ResetExternalCamera()
        {
            panDegrees = externalDefaultPan;
            tiltDegrees = externalDefaultTilt;
            externalDistance = 10.1f;
            UpdateActiveCamera();
        }

        void UpdateActiveCamera()
        {
            if (cockpitCamera != null)
            {
                cockpitCamera.enabled = !externalActive;
            }

            if (externalCamera != null)
            {
                externalCamera.enabled = externalActive;
            }
        }
    }
}
