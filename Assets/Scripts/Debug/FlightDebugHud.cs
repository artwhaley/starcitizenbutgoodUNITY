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
        [SerializeField] Text fuelText;
        [SerializeField] Text thrustDebugText;
        [SerializeField] Text reticleText;
        [SerializeField] Text targetLeadText;
        [SerializeField] Text progradeText;
        [SerializeField] Text retrogradeText;

        bool debugOverlayVisible;

        public void ToggleDebugOverlay() => SetDebugOverlayVisible(!debugOverlayVisible);

        public void SetDebugOverlayVisible(bool visible)
        {
            // Always drive the underlying GameObject (if wired) AND update
            // the bool unconditionally — cheap, and closes a hole where the
            // bool could flip to true while the panel stayed hidden if the
            // prefab field got assigned after the first toggle.
            if (thrustDebugText != null)
            {
                thrustDebugText.gameObject.SetActive(visible);
            }

            debugOverlayVisible = visible;
        }

        void Awake()
        {
            if (TryGetComponent(out GraphicRaycaster raycaster))
            {
                raycaster.enabled = false;
            }

            EnsureReticle();
            targetLeadText = EnsureMarker(targetLeadText, "TargetLeadPip", "o", 22, new Color(1f, 0.86f, 0.25f, 0.95f));
            progradeText = EnsureMarker(progradeText, "ProgradeMarker", "PRO", 14, new Color(0.35f, 1f, 0.65f, 0.9f));
            retrogradeText = EnsureMarker(retrogradeText, "RetrogradeMarker", "RET", 14, new Color(1f, 0.45f, 0.35f, 0.9f));
            DisableDuplicateMarkers("TargetLeadPip", targetLeadText);
            DisableDuplicateMarkers("ProgradeMarker", progradeText);
            DisableDuplicateMarkers("RetrogradeMarker", retrogradeText);
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

        Text EnsureMarker(Text marker, string name, string label, int fontSize, Color color)
        {
            if (marker == null)
            {
                Transform existing = transform.Find(name);
                if (existing != null)
                {
                    marker = existing.GetComponent<Text>();
                }
            }

            if (marker != null)
            {
                ConfigureMarker(marker, label, fontSize, color);
                marker.raycastTarget = false;
                marker.gameObject.SetActive(false);
                return marker;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(56f, 28f);

            Text text = go.GetComponent<Text>();
            ConfigureMarker(text, label, fontSize, color);
            text.gameObject.SetActive(false);
            return text;
        }

        void DisableDuplicateMarkers(string markerName, Text keep)
        {
            Text[] markers = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                Text marker = markers[i];
                if (marker != null && marker != keep && marker.name == markerName)
                {
                    marker.gameObject.SetActive(false);
                }
            }
        }

        static void ConfigureMarker(Text text, string label, int fontSize, Color color)
        {
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.raycastTarget = false;
        }

        public void SetTelemetry(in ShipState state, in ShipInputCommand input, in FlightHudViewModel viewModel)
        {
            if (leftText == null || rightText == null || fuelText == null)
            {
                return;
            }

            float speed = state.linearVelocity.magnitude;

            // Left sub-column (anchored ~25% from screen center on the left):
            // Assist Mode / Speed / Brake.
            leftText.text =
                $"ASSIST (F): {state.assistMode}\n" +
                $"SPEED: {speed:0.0} m/s\n" +
                $"BRAKE: {(input.brake ? "ON" : "OFF")}";

            // Right sub-column (anchored ~25% from screen center on the right):
            // Boost / Fine Control.
            rightText.text =
                $"BOOST: {(state.boostActive ? "ON" : "OFF")}\n" +
                $"FINE (G): {(state.fineControlActive ? "ON" : "OFF")}";

            // Bottom-left block: propellant totals.
            fuelText.text =
                $"FUEL: {state.remainingFuelKg:0.0} kg\n" +
                $"HYPR: {state.remainingHypergolicKg:0.0} kg";

            // Developer debug overlay (top-center panel).
            // Skipped entirely while hidden to avoid per-frame string allocations.
            if (!debugOverlayVisible || thrustDebugText == null)
            {
                return;
            }

            Vector3 requestedLinear = state.appliedOutput.requestedLocalLinear;
            Vector3 appliedLinear = state.appliedOutput.appliedLocalLinear;
            Vector3 requestedAngular = state.appliedOutput.requestedLocalAngular;
            Vector3 appliedAngular = state.appliedOutput.appliedLocalAngular;

            thrustDebugText.text =
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

        public void SetWorldMarkers(Camera activeCamera, in ShipState state, PrimaryWeaponController weapon)
        {
            if (activeCamera == null)
            {
                HideMarker(targetLeadText);
                HideMarker(progradeText);
                HideMarker(retrogradeText);
                return;
            }

            UpdateVelocityMarker(activeCamera, progradeText, state.linearVelocity);
            UpdateVelocityMarker(activeCamera, retrogradeText, -state.linearVelocity);

            if (weapon != null && weapon.TryGetLeadPoint(activeCamera, out Vector3 leadPoint, out _))
            {
                SetWorldMarker(activeCamera, targetLeadText, leadPoint);
            }
            else
            {
                HideMarker(targetLeadText);
            }
        }

        void UpdateVelocityMarker(Camera activeCamera, Text marker, Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.25f)
            {
                HideMarker(marker);
                return;
            }

            Vector3 worldPoint = activeCamera.transform.position + direction.normalized * 1000f;
            SetWorldMarker(activeCamera, marker, worldPoint);
        }

        static void SetWorldMarker(Camera activeCamera, Text marker, Vector3 worldPoint)
        {
            if (marker == null)
            {
                return;
            }

            Vector3 screenPoint = activeCamera.WorldToScreenPoint(worldPoint);
            bool visible = screenPoint.z > 0f
                && screenPoint.x >= 0f
                && screenPoint.x <= Screen.width
                && screenPoint.y >= 0f
                && screenPoint.y <= Screen.height;

            marker.gameObject.SetActive(visible);
            if (visible)
            {
                marker.rectTransform.position = screenPoint;
            }
        }

        static void HideMarker(Text marker)
        {
            if (marker != null)
            {
                marker.gameObject.SetActive(false);
            }
        }
    }
}
