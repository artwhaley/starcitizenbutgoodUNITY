using UnityEngine;
using UnityEngine.UI;

namespace FlightModel
{
    public struct FlightHudViewModel
    {
        public string viewMode;
        public float externalPanDegrees;
        public float externalTiltDegrees;
        public float externalDistance;
        public float cockpitFov;
    }

    public class FlightDebugHud : MonoBehaviour
    {
        [SerializeField] Text leftText;
        [SerializeField] Text rightText;
        [SerializeField] Text reticleText;

        void Awake()
        {
            if (TryGetComponent(out GraphicRaycaster raycaster))
            {
                raycaster.enabled = false;
            }

            EnsureReticle();
        }

        void EnsureReticle()
        {
            if (reticleText != null)
            {
                reticleText.raycastTarget = false;
                return;
            }

            var go = new GameObject("Reticle", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(24f, 24f);

            reticleText = go.GetComponent<Text>();
            reticleText.text = "+";
            reticleText.alignment = TextAnchor.MiddleCenter;
            reticleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            reticleText.fontSize = 20;
            reticleText.color = new Color(1f, 1f, 1f, 0.8f);
            reticleText.raycastTarget = false;
        }

        public void SetTelemetry(in ShipState state, in ShipInputCommand input, in FlightHudViewModel viewModel)
        {
            if (leftText == null || rightText == null)
            {
                return;
            }

            float speed = state.linearVelocity.magnitude;
            Vector3 requestedLinear = state.appliedOutput.requestedLocalLinear;
            Vector3 appliedLinear = state.appliedOutput.appliedLocalLinear;
            Vector3 requestedAngular = state.appliedOutput.requestedLocalAngular;
            Vector3 appliedAngular = state.appliedOutput.appliedLocalAngular;

            leftText.text =
                "--- FLIGHT ---\n" +
                $"VIEW (V): {viewModel.viewMode}\n" +
                (viewModel.viewMode == "EXTERNAL"
                    ? $"EXT CAM Pan/Tilt/Dist: {viewModel.externalPanDegrees:0} / {viewModel.externalTiltDegrees:0} / {viewModel.externalDistance:0}\n"
                    : string.Empty) +
                $"ASSIST (F): {state.assistMode}\n" +
                $"BOOST: {(state.boostActive ? "ON" : "OFF")}\n" +
                $"FINE (G): {(state.fineControlActive ? "ON" : "OFF")}\n" +
                $"SPEED: {speed:0.0} m/s\n" +
                $"MASS: {state.currentMassKg:0} kg\n" +
                $"FUEL: {state.remainingFuelKg:0.0} kg\n" +
                $"HYPR: {state.remainingHypergolicKg:0.0} kg\n" +
                $"CAP BLOCK: {(state.appliedOutput.linearSpeedCapped ? "LINEAR" : "none")}\n" +
                $"RES BLOCK: {(state.appliedOutput.mainEngineFuelBlocked ? "FUEL " : string.Empty)}{(state.appliedOutput.hypergolicBlocked ? "HYPR" : string.Empty)}\n" +
                $"BRAKE: {(input.brake ? "ON" : "OFF")}";

            rightText.text =
                "--- THRUST ---\n" +
                $"REQ LIN: {requestedLinear.x:+0.00;-0.00;+0.00} {requestedLinear.y:+0.00;-0.00;+0.00} {requestedLinear.z:+0.00;-0.00;+0.00}\n" +
                $"APL LIN: {appliedLinear.x:+0.0;-0.0;+0.0} {appliedLinear.y:+0.0;-0.0;+0.0} {appliedLinear.z:+0.0;-0.0;+0.0}\n" +
                $"REQ ANG: {requestedAngular.x:+0.00;-0.00;+0.00} {requestedAngular.y:+0.00;-0.00;+0.00} {requestedAngular.z:+0.00;-0.00;+0.00}\n" +
                $"APL ANG: {appliedAngular.x:+0.0;-0.0;+0.0} {appliedAngular.y:+0.0;-0.0;+0.0} {appliedAngular.z:+0.0;-0.0;+0.0}\n" +
                $"MAIN/MNV: {state.appliedOutput.thrusters.mainEngineForward:0.00} / {state.appliedOutput.thrusters.maneuverForward:0.00}\n" +
                $"ANG P/Y/R: {state.angularVelocityRadians.x:+0.000;-0.000;+0.000} {state.angularVelocityRadians.y:+0.000;-0.000;+0.000} {state.angularVelocityRadians.z:+0.000;-0.000;+0.000}\n" +
                $"FIRE: {(input.firePrimary ? "ON" : "OFF")}\n" +
                $"FOV: {viewModel.cockpitFov:0}";
        }
    }
}
