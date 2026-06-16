using System.Collections;
using UnityEngine;

namespace FlightModel
{
    public class WeaponTracerVfx : MonoBehaviour
    {
        [SerializeField] int poolSize = 40;
        [SerializeField] float lineWidth = 0.12f;
        [SerializeField] float defaultDuration = 0.1f;
        [SerializeField] Color hitColor = Color.green;
        [SerializeField] Color missColor = new(1f, 0.35f, 0.1f);

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
            }

            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"WeaponTracer_{i}", typeof(LineRenderer));
                go.transform.SetParent(transform, false);
                var line = go.GetComponent<LineRenderer>();
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.startWidth = lineWidth;
                line.endWidth = lineWidth * 0.35f;
                line.numCapVertices = 2;
                if (material != null)
                {
                    line.sharedMaterial = material;
                }

                line.enabled = false;
                pool[i] = line;
            }
        }

        public void DrawTracer(Vector3 start, Vector3 end, bool hit, float duration = -1f)
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
            line.endColor = color;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.enabled = true;
            active[index] = StartCoroutine(HideAfter(line, duration > 0f ? duration : defaultDuration));
        }

        static IEnumerator HideAfter(LineRenderer line, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (line != null)
            {
                line.enabled = false;
            }
        }
    }
}
