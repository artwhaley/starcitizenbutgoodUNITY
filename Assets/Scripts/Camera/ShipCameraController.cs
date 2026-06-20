using UnityEngine;
using FlightModel.Docking;

namespace FlightModel
{
    /// <summary>
    /// Drives the player's primary camera plus the external/docking alternates.
    /// In docking mode, the cockpit camera object is reused and repositioned to
    /// the ship's docking node pose so the pilot flies "from" the docking camera.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class ShipCameraController : MonoBehaviour
    {
        public enum CameraMode
        {
            Cockpit,
            External,
            Docking
        }

        [SerializeField] Transform cogTransform;
        [SerializeField] Transform cockpitCameraMount;
        [SerializeField] Transform shipFpvNode;
        [SerializeField] Camera cockpitCamera;
        [SerializeField] Transform externalCameraRig;
        [SerializeField] Transform externalCameraPivot;
        [SerializeField] Camera externalCamera;

        [Header("Defaults (meters / degrees)")]
        [SerializeField] float cockpitFov = 95f;
        [SerializeField] float dockingFov = 70f;
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

        CameraMode currentMode = CameraMode.Cockpit;
        ShipDockingNode dockingNode;

        float panDegrees;
        float tiltDegrees;

        public CameraMode CurrentMode => currentMode;
        public bool IsExternalActive => currentMode == CameraMode.External;
        public bool IsDockingActive => currentMode == CameraMode.Docking;
        public bool IsCockpitActive => currentMode == CameraMode.Cockpit;
        public ShipDockingNode ActiveDockingNode => dockingNode;
        public float ExternalPanDegrees => panDegrees;
        public float ExternalTiltDegrees => tiltDegrees;
        public float ExternalDistance => externalDistance;

        public float ActiveFov
        {
            get
            {
                switch (currentMode)
                {
                    case CameraMode.External: return externalFov;
                    case CameraMode.Docking: return dockingFov;
                    default: return cockpitFov;
                }
            }
        }

        void Awake() => ResetExternalCamera();

        void LateUpdate()
        {
            if (cogTransform == null)
            {
                return;
            }

            if (currentMode == CameraMode.Docking && dockingNode != null)
            {
                if (cockpitCameraMount != null)
                {
                    cockpitCameraMount.SetPositionAndRotation(
                        dockingNode.WorldPosition, dockingNode.GetCameraAlignedRotation());
                }
            }
            else if (shipFpvNode != null && cockpitCameraMount != null)
            {
                Quaternion lookRotation = cogTransform.rotation;
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

            float cockpitFovTarget = currentMode == CameraMode.Docking ? dockingFov : cockpitFov;
            if (cockpitCamera != null)
            {
                cockpitCamera.fieldOfView = cockpitFovTarget;
            }

            if (externalCamera != null)
            {
                externalCamera.fieldOfView = externalFov;
            }
        }

        public void EnterDockingMode(ShipDockingNode node)
        {
            if (node == null)
            {
                return;
            }

            dockingNode = node;
            currentMode = CameraMode.Docking;
            UpdateActiveCamera();
        }

        public void ExitDockingMode()
        {
            dockingNode = null;
            currentMode = CameraMode.Cockpit;
            UpdateActiveCamera();
        }

        public void ExitDockingView()
        {
            if (currentMode == CameraMode.Docking)
            {
                currentMode = CameraMode.Cockpit;
            }

            dockingNode = null;
            UpdateActiveCamera();
        }

        public void ToggleView(bool dockingCameraAllowed, ShipDockingNode node)
        {
            if (!dockingCameraAllowed)
            {
                if (currentMode == CameraMode.Docking)
                {
                    currentMode = CameraMode.Cockpit;
                    dockingNode = null;
                }
                else
                {
                    currentMode = currentMode == CameraMode.External
                        ? CameraMode.Cockpit
                        : CameraMode.External;
                }

                UpdateActiveCamera();
                return;
            }

            switch (currentMode)
            {
                case CameraMode.Cockpit:
                    currentMode = CameraMode.External;
                    break;
                case CameraMode.External:
                    if (node != null)
                    {
                        dockingNode = node;
                        currentMode = CameraMode.Docking;
                    }
                    else
                    {
                        Debug.LogWarning(
                            "ShipCameraController: docking camera requested, but no authored ShipDockingNode is available.",
                            this);
                        currentMode = CameraMode.Cockpit;
                    }
                    break;
                case CameraMode.Docking:
                    dockingNode = null;
                    currentMode = CameraMode.Cockpit;
                    break;
            }

            UpdateActiveCamera();
        }

        public void ApplyExternalPanTilt(float panInput, float tiltInput, float deltaTime)
        {
            if (currentMode != CameraMode.External)
            {
                return;
            }

            panDegrees += panInput * panSpeed * deltaTime;
            tiltDegrees = Mathf.Clamp(tiltDegrees + tiltInput * tiltSpeed * deltaTime, externalMinTilt, externalMaxTilt);
        }

        public void ApplyZoomDelta(float scrollDelta)
        {
            if (currentMode != CameraMode.External || Mathf.Approximately(scrollDelta, 0f))
            {
                return;
            }

            externalDistance = Mathf.Clamp(
                externalDistance - scrollDelta * zoomSpeed,
                externalMinDistance,
                externalMaxDistance);
        }

        public Camera GetActiveCamera()
        {
            if (currentMode == CameraMode.External && externalCamera != null)
            {
                return externalCamera;
            }
            return cockpitCamera;
        }

        public void SetFpvNode(Transform fpvNode) => shipFpvNode = fpvNode;

        public void SetDockingFov(float fov) => dockingFov = Mathf.Clamp(fov, 10f, 170f);

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
                cockpitCamera.enabled = currentMode != CameraMode.External;
            }

            if (externalCamera != null)
            {
                externalCamera.enabled = currentMode == CameraMode.External;
            }
        }
    }
}
