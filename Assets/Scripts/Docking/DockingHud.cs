using UnityEngine;
using UnityEngine.UI;

namespace FlightModel.Docking
{
    /// <summary>
    /// Manual docking HUD. Visible only while the pilot has docking camera/control
    /// mode active. Shows alignment circle, lateral offset needles, velocity bug,
    /// numeric readouts, and ship/target port state readouts drawn on a runtime
    /// screen-space overlay canvas. Drawing primitives uses procedurally generated
    /// circle/ring sprites to avoid depending on prefab assets.
    /// </summary>
    public class DockingHud : MonoBehaviour
    {
        static readonly Color ReadoutBlue = new(0.1f, 0.72f, 1f, 0.98f);
        static readonly Color ReadoutOutline = new(0f, 0f, 0.08f, 0.95f);

        [Header("Layout")]
        [SerializeField] Vector2 referenceSize = new Vector2(640f, 480f);
        [SerializeField] float bugPixelsPerMeterPerSecond = 40f;
        [SerializeField] float axisCircleRadius = 70f;
        [SerializeField] float angularErrorPixelScale = 6f;

        Canvas canvas;
        RectTransform rootRect;

        Image centerMark;
        Image targetMark;
        Image alignmentCircle;
        Image alignmentDot;
        Image lateralNeedleH;
        Image lateralNeedleV;
        Image velocityBug;

        Text portStateText;
        Text alignmentText;
        Text distanceOffsetText;
        Text closureText;
        Text statusText;
        Text rollText;

        public bool IsVisible { get; private set; }

        void Awake()
        {
            if (!TryGetComponent(out canvas))
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.enabled = false;

            if (!TryGetComponent(out CanvasScaler _))
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = referenceSize;
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (!TryGetComponent(out GraphicRaycaster raycaster))
            {
                raycaster = gameObject.AddComponent<GraphicRaycaster>();
            }
            raycaster.enabled = false;

            rootRect = GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = gameObject.AddComponent<RectTransform>();
            }
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            BuildVisuals();
        }

        public void SetActive(bool active)
        {
            IsVisible = active;
            if (canvas != null)
            {
                canvas.enabled = active;
            }
            else
            {
                gameObject.SetActive(active);
            }
        }

        void BuildVisuals()
        {
            centerMark = CreateFilledCircle("CenterMark", 4f, new Color(1f, 1f, 1f, 0.9f));
            centerMark.rectTransform.anchoredPosition = Vector2.zero;

            targetMark = CreateFilledCircle("TargetMark", 3f, new Color(0.4f, 0.95f, 0.4f, 0.7f));
            targetMark.rectTransform.anchoredPosition = Vector2.zero;

            alignmentCircle = CreateRing("AlignmentCircle", axisCircleRadius, new Color(0.3f, 0.9f, 0.7f, 0.6f));
            alignmentCircle.rectTransform.anchoredPosition = Vector2.zero;

            alignmentDot = CreateFilledCircle("AlignmentDot", 7f, new Color(1f, 0.85f, 0.3f, 0.95f));
            alignmentDot.rectTransform.anchoredPosition = Vector2.zero;

            lateralNeedleH = CreateBar("LateralNeedleH", 6f, 28f, new Color(1f, 0.95f, 0.5f, 0.95f));
            lateralNeedleH.rectTransform.anchoredPosition = Vector2.zero;

            lateralNeedleV = CreateBar("LateralNeedleV", 28f, 6f, new Color(1f, 0.95f, 0.5f, 0.95f));
            lateralNeedleV.rectTransform.anchoredPosition = Vector2.zero;

            velocityBug = CreateFilledCircle("VelocityBug", 5f, new Color(0.55f, 0.85f, 1f, 0.95f));
            velocityBug.rectTransform.anchoredPosition = Vector2.zero;

            portStateText = CreateText("PortState", TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -8f));
            portStateText.text = "MODE: --";
            portStateText.fontSize = 18;
            portStateText.color = ReadoutBlue;

            alignmentText = CreateText("AlignmentReadout", TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f));
            alignmentText.text = "ALIGN";
            alignmentText.fontSize = 16;
            alignmentText.color = ReadoutBlue;

            distanceOffsetText = CreateText("DistanceOffset", TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -(axisCircleRadius + 30f)));
            distanceOffsetText.text = "";
            distanceOffsetText.fontSize = 14;
            distanceOffsetText.color = ReadoutBlue;

            closureText = CreateText("ClosureReadout", TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -(axisCircleRadius + 52f)));
            closureText.text = "";
            closureText.fontSize = 14;
            closureText.color = ReadoutBlue;

            rollText = CreateText("RollReadout", TextAnchor.LowerRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 8f));
            rollText.text = "ROLL 0.0°";
            rollText.fontSize = 14;
            rollText.color = ReadoutBlue;

            statusText = CreateText("StatusReadout", TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f));
            statusText.text = "DOCKING MODE";
            statusText.fontSize = 16;
            statusText.color = ReadoutBlue;
        }

        Image CreateFilledCircle(string name, float radius, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = GetOrCreateCircleSprite();
            img.color = color;
            img.raycastTarget = false;
            RectTransform r = img.rectTransform;
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(radius * 2f, radius * 2f);
            return img;
        }

        Image CreateRing(string name, float radius, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = GetOrCreateRingSprite();
            img.color = color;
            img.raycastTarget = false;
            RectTransform r = img.rectTransform;
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(radius * 2f + 4f, radius * 2f + 4f);
            return img;
        }

        Image CreateBar(string name, float width, float height, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = GetOrCreateCircleSprite();
            img.color = color;
            img.raycastTarget = false;
            RectTransform r = img.rectTransform;
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(width, height);
            return img;
        }

        Text CreateText(string name, TextAnchor anchor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            var text = go.GetComponent<Text>();
            text.alignment = anchor;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            text.text = "";
            text.color = ReadoutBlue;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = ReadoutOutline;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var r = text.rectTransform;
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(620f, 28f);
            r.anchoredPosition = anchoredPosition;
            return text;
        }

        static Sprite circleSpriteCache;
        static Sprite ringSpriteCache;

        static Sprite GetOrCreateCircleSprite()
        {
            if (circleSpriteCache != null)
            {
                return circleSpriteCache;
            }

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var clear = new Color(0f, 0f, 0f, 0f);
            var fill = new Color(1f, 1f, 1f, 1f);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius + 0.5f;
                    float dy = y - radius + 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y, d <= radius ? fill : clear);
                }
            }
            tex.Apply();
            circleSpriteCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            circleSpriteCache.name = "DockingHudCircle";
            return circleSpriteCache;
        }

        static Sprite GetOrCreateRingSprite()
        {
            if (ringSpriteCache != null)
            {
                return ringSpriteCache;
            }

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var clear = new Color(0f, 0f, 0f, 0f);
            var fill = new Color(1f, 1f, 1f, 1f);
            float outer = size * 0.5f;
            float inner = outer - 4f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - outer + 0.5f;
                    float dy = y - outer + 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y, d <= outer && d >= inner ? fill : clear);
                }
            }
            tex.Apply();
            ringSpriteCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            ringSpriteCache.name = "DockingHudRing";
            return ringSpriteCache;
        }

        public void UpdateTelemetry(in DockingTelemetry telemetry)
        {
            if (!IsVisible)
            {
                return;
            }

            SetPortText(telemetry.shipPortDeployed, telemetry.hasTarget,
                telemetry.targetPortAvailable, telemetry.magneticCaptureActive,
                telemetry.docked, telemetry.recaptureLockout);

            if (!telemetry.hasTarget)
            {
                distanceOffsetText.text = telemetry.targetPortAvailable
                    ? "NO TARGET IN RANGE"
                    : "PORT UNAVAILABLE";
                closureText.text = "";
                alignmentText.text = "--";
                rollText.text = "ROLL --";
                alignmentDot.rectTransform.anchoredPosition = Vector2.zero;
                lateralNeedleH.rectTransform.anchoredPosition = Vector2.zero;
                lateralNeedleV.rectTransform.anchoredPosition = Vector2.zero;
                velocityBug.rectTransform.anchoredPosition = Vector2.zero;
                return;
            }

            float hMax = axisCircleRadius * 0.95f;
            float vMax = axisCircleRadius * 0.95f;

            Vector2 lateralOffset = telemetry.lateralNeedleNormalized;
            lateralNeedleH.rectTransform.anchoredPosition = new Vector2(
                Mathf.Clamp(lateralOffset.x * hMax, -hMax, hMax),
                0f);
            lateralNeedleV.rectTransform.anchoredPosition = new Vector2(
                0f,
                Mathf.Clamp(lateralOffset.y * vMax, -vMax, vMax));

            Vector2 angularErrorDeg = new Vector2(
                telemetry.angularAxisError.x * Mathf.Rad2Deg,
                telemetry.angularAxisError.y * Mathf.Rad2Deg);
            float angMax = axisCircleRadius * 0.85f;
            alignmentDot.rectTransform.anchoredPosition = new Vector2(
                Mathf.Clamp(angularErrorDeg.x * angularErrorPixelScale, -angMax, angMax),
                Mathf.Clamp(angularErrorDeg.y * angularErrorPixelScale, -angMax, angMax));

            Vector2 lateralVelocity = telemetry.lateralVelocityMetersPerSecond;
            velocityBug.rectTransform.anchoredPosition = new Vector2(
                Mathf.Clamp(lateralVelocity.x * bugPixelsPerMeterPerSecond, -hMax, hMax),
                Mathf.Clamp(lateralVelocity.y * bugPixelsPerMeterPerSecond, -vMax, vMax));

            distanceOffsetText.text =
                $"DIST {telemetry.distanceMeters:0.0}m  CDST {telemetry.closureDistanceMeters:+0.00;-0.00;+0.00}m  X {telemetry.lateralAxisOffsetMeters.x:+0.00;-0.00;+0.00}m/{telemetry.lateralGuidanceDegrees.x:+0.0;-0.0;+0.0}deg  Y {telemetry.lateralAxisOffsetMeters.y:+0.00;-0.00;+0.00}m/{telemetry.lateralGuidanceDegrees.y:+0.0;-0.0;+0.0}deg";
            closureText.text =
                $"CLOSURE {telemetry.closureVelocityMetersPerSecond:+0.00;-0.00;+0.00} m/s";
            alignmentText.text =
                $"YAW {angularErrorDeg.x:+0.0;-0.0;+0.0}°  PITCH {angularErrorDeg.y:+0.0;-0.0;+0.0}°";
            rollText.text = $"ROLL {telemetry.rollOffsetDegrees:+0.0;-0.0;+0.0}°";
        }

        void SetPortText(bool deployed, bool hasTarget, bool targetAvailable,
            bool magnet, bool docked, bool lockout)
        {
            if (!hasTarget)
            {
                portStateText.text = "MODE: NO TARGET";
                portStateText.color = ReadoutBlue;
            }
            else if (docked)
            {
                portStateText.text = "MODE: DOCKED";
                portStateText.color = ReadoutBlue;
            }
            else if (magnet)
            {
                portStateText.text = "MODE: MAGNETIC";
                portStateText.color = ReadoutBlue;
            }
            else if (lockout)
            {
                portStateText.text = "MODE: LOCKOUT";
                portStateText.color = ReadoutBlue;
            }
            else if (!targetAvailable)
            {
                portStateText.text = "MODE: TARGET OFFLINE";
                portStateText.color = ReadoutBlue;
            }
            else if (deployed)
            {
                portStateText.text = "MODE: ENABLED";
                portStateText.color = ReadoutBlue;
            }
            else
            {
                portStateText.text = "MODE: DISABLED";
                portStateText.color = ReadoutBlue;
            }

            if (!docked && !lockout)
            {
                statusText.text = "DOCKING MODE";
                statusText.color = ReadoutBlue;
            }
            else if (lockout)
            {
                statusText.text = "RECAPTURE LOCKOUT";
                statusText.color = ReadoutBlue;
            }
            else
            {
                statusText.text = "DOCKED";
                statusText.color = ReadoutBlue;
            }
        }
    }
}
