using UnityEngine;

namespace FlightModel.Docking
{
    /// <summary>
    /// Owns the local pilot's docking-mode state and docking-camera availability.
    /// Docking mode permits capture. Docking camera is only one possible view
    /// while docking mode is enabled. Docking-relative controls apply only while
    /// that camera is active.
    ///
    /// The controller does NOT change flight tuning assets, does NOT add an autopilot
    /// path, and does NOT implement capture/snap/undock itself.
    /// </summary>
    public class DockingModeController : MonoBehaviour
    {
        [SerializeField] ShipDockingNode dockingNode;
        [SerializeField] DockingTargetProvider targetProvider;
        [SerializeField] ShipCameraController cameraController;
        [SerializeField] DockingHud dockingHud;

        public ShipDockingNode DockingNode
        {
            get => dockingNode;
            set
            {
                dockingNode = value;
                if (targetProvider != null && value != null)
                {
                    targetProvider.SetSearchOrigin(value.NodeTransform);
                }
            }
        }

        public ShipCameraController CameraController
        {
            get => cameraController;
            set => cameraController = value;
        }

        public DockingHud Hud
        {
            get => dockingHud;
            set => dockingHud = value;
        }

        public DockingTargetProvider TargetProvider
        {
            get => targetProvider;
            set
            {
                targetProvider = value;
                if (value != null && dockingNode != null)
                {
                    value.SetSearchOrigin(dockingNode.NodeTransform);
                }
            }
        }

        public bool IsDockingModeEnabled { get; private set; }
        public bool IsDockingCameraActive =>
            cameraController != null && cameraController.IsDockingActive;
        public bool ShouldUseDockingControls =>
            IsDockingModeEnabled && IsDockingCameraActive;

        public ShipInputCommand TransformInput(in ShipInputCommand pilotInput, in ShipState shipState)
        {
            if (!ShouldUseDockingControls)
            {
                return pilotInput;
            }
            return DockingInputTransformer.Transform(pilotInput, shipState, dockingNode);
        }

        public void ToggleDockingMode(ShipDockingNode node)
            => SetDockingModeEnabled(!IsDockingModeEnabled, node);

        public void SetDockingModeEnabled(bool enabled, ShipDockingNode node = null)
        {
            if (node != null)
            {
                DockingNode = node;
            }

            bool changed = IsDockingModeEnabled != enabled;
            IsDockingModeEnabled = enabled;
            if (dockingNode != null)
            {
                dockingNode.SetDeployed(enabled);
            }

            if (!enabled)
            {
                if (cameraController != null)
                {
                    cameraController.ExitDockingView();
                }

                if (dockingHud != null)
                {
                    dockingHud.SetActive(false);
                }
            }
            else
            {
                if (targetProvider != null && dockingNode != null)
                {
                    targetProvider.SetSearchOrigin(dockingNode.NodeTransform);
                }

                SyncHudVisibility();
            }

            if (changed)
            {
                Debug.Log(
                    enabled
                        ? "DockingModeController: docking mode enabled."
                        : "DockingModeController: docking mode disabled.",
                    this);
            }
        }

        public void ToggleCameraView()
        {
            if (targetProvider != null && dockingNode != null)
            {
                targetProvider.SetSearchOrigin(dockingNode.NodeTransform);
            }

            cameraController?.ToggleView(IsDockingModeEnabled, dockingNode);
            SyncHudVisibility();
        }

        public void SyncHudVisibility()
        {
            if (dockingHud != null)
            {
                dockingHud.SetActive(IsDockingModeEnabled && IsDockingCameraActive);
            }
        }

        public void EnterDockingMode(ShipDockingNode node)
        {
            SetDockingModeEnabled(true, node);
            if (cameraController != null && dockingNode != null)
            {
                cameraController.EnterDockingMode(dockingNode);
            }

            SyncHudVisibility();
        }

        public void ExitDockingMode()
            => SetDockingModeEnabled(false);
    }
}
