using System.Collections;
using UnityEngine;

namespace FlightModel
{
    public class WeaponTracerVfx : MonoBehaviour
    {
        [SerializeField] int poolSize = 40;
        [SerializeField] float lineWidth = 0.035f;
        [SerializeField] float boltLengthMeters = 7f;
        [SerializeField] float defaultDuration = 0.08f;
        [SerializeField] Color hitColor = new(1.8f, 2.4f, 1.2f, 1f);
        [SerializeField] Color missColor = new(3.2f, 1.4f, 0.35f, 1f);

        LineRenderer[] pool;
        Coroutine[] active;
        int nextIndex;

        void Awake()
        {
            pool = new LineRenderer[poolSize];
            active = new Coroutine[poolSize];
            Material material = null;
            Shader shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                material = new Material(shader);
                material.SetColor("_Color", Color.white);
            }

            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"WeaponTracer_{i}", typeof(LineRenderer));
                go.transform.SetParent(transform, false);
                var line = go.GetComponent<LineRenderer>();
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.alignment = LineAlignment.View;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.startWidth = lineWidth;
                line.endWidth = lineWidth * 0.6f;
                line.numCapVertices = 2;
                if (material != null)
                {
                    line.sharedMaterial = material;
                }

                line.enabled = false;
                pool[i] = line;
            }
        }

        public void DrawTracer(Vector3 start, Vector3 end, bool hit, float duration = -1f, float projectileSpeed = 650f)
        {
            if (pool == null || pool.Length == 0)
            {
                return;
            }

            int index = nextIndex;
            nextIndex = (nextIndex + 1) % pool.Length;

            if (active[index] != null)
            {
                StopCoroutine(active[index]);
            }

            LineRenderer line = pool[index];
            Color color = hit ? hitColor : missColor;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.15f);
            line.enabled = true;
            active[index] = StartCoroutine(AnimateBolt(
                line,
                start,
                end,
                Mathf.Max(1f, projectileSpeed),
                duration > 0f ? Mathf.Min(duration, defaultDuration) : defaultDuration));
        }

        IEnumerator AnimateBolt(LineRenderer line, Vector3 start, Vector3 end, float projectileSpeed, float fallbackDuration)
        {
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            if (distance < 0.01f)
            {
                line.enabled = false;
                yield break;
            }

            Vector3 direction = delta / distance;
            float travelSeconds = Mathf.Clamp(distance / projectileSpeed, 0.025f, fallbackDuration);
            float elapsed = 0f;

            while (elapsed < travelSeconds && line != null)
            {
                float headDistance = Mathf.Lerp(0f, distance, elapsed / travelSeconds);
                float tailDistance = Mathf.Max(0f, headDistance - boltLengthMeters);
                line.SetPosition(0, start + direction * tailDistance);
                line.SetPosition(1, start + direction * headDistance);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (line != null)
            {
                line.enabled = false;
            }
        }
    }
}
