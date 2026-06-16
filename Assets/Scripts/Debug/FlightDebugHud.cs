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
            string assist = state.assistMode.ToString();

            leftText.text =
                "--- FLIGHT ---\n" +
                $"VIEW (V): {viewModel.viewMode}\n" +
                (viewModel.viewMode == "EXTERNAL"
                    ? $"EXT CAM Pan/Tilt/Dist: {viewModel.externalPanDegrees:0} / {viewModel.externalTiltDegrees:0} / {viewModel.externalDistance:0}\n"
                    : string.Empty) +
                $"ASSIST (F): {assist}\n" +
                $"SPEED: {speed:0.0} m/s\n" +
                $"VEL: {state.linearVelocity.x:0} {state.linearVelocity.y:0} {state.linearVelocity.z:0}\n" +
                $"FRAME: {state.frameId}\n" +
                $"BRAKE: {(input.brake ? "ON" : "OFF")}";

            rightText.text =
                "--- INPUT ---\n" +
                $"THR F/R/U: {input.thrustForward:+0.00;-0.00;+0.00} {input.thrustRight:+0.00;-0.00;+0.00} {input.thrustUp:+0.00;-0.00;+0.00}\n" +
                $"P/Y/R: {input.pitch:+0.00;-0.00;+0.00} {input.yaw:+0.00;-0.00;+0.00} {input.roll:+0.00;-0.00;+0.00}\n" +
                $"ANG P/Y/R: {state.angularVelocityRadians.x:+0.000;-0.000;+0.000} {state.angularVelocityRadians.y:+0.000;-0.000;+0.000} {state.angularVelocityRadians.z:+0.000;-0.000;+0.000}\n" +
                $"BOOST: {(input.boost ? "ON" : "OFF")}\n" +
                $"FIRE: {(input.firePrimary ? "ON" : "OFF")}\n" +
                $"FOV: {viewModel.cockpitFov:0}";
        }
    }
}
