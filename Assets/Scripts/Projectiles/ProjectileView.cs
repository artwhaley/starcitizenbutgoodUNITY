using UnityEngine;

namespace FlightModel
{
    public class ProjectileView : MonoBehaviour
    {
        LineRenderer lineRenderer;
        Vector3 startPosition;
        Vector3 endPosition;
        Color baseColor;

        public int ProjectileId { get; private set; }

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.positionCount = 2;
                lineRenderer.useWorldSpace = true;
                lineRenderer.alignment = LineAlignment.View;
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;
                lineRenderer.startWidth = 0.18f;
                lineRenderer.endWidth = 0.12f;
                lineRenderer.numCapVertices = 2;

                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                if (shader != null)
                {
                    Material mat = new Material(shader);
                    if (shader.name.Contains("Universal"))
                    {
                        mat.SetColor("_BaseColor", Color.white);
                        mat.SetFloat("_Surface", 0f);
                    }
                    else
                    {
                        mat.SetColor("_Color", Color.white);
                    }

                    lineRenderer.sharedMaterial = mat;
                }
                else
                {
                    Debug.LogWarning("ProjectileView: no compatible unlit shader found in URP project. LineRenderer will use default material.");
                }
            }
        }

        public void Initialize(int projectileId, Vector3 position, Color color)
        {
            ProjectileId = projectileId;
            startPosition = position;
            endPosition = position;
            baseColor = color;
            transform.position = position;
        }

        public void UpdateState(Vector3 currentPosition, float bulletLength = 1.5f)
        {
            if (currentPosition == endPosition)
            {
                return;
            }

            Vector3 direction = (currentPosition - endPosition).normalized;
            if (direction.sqrMagnitude < 1e-6f)
            {
                return;
            }

            Vector3 tailPosition = currentPosition - direction * bulletLength;
            lineRenderer.SetPosition(0, tailPosition);
            lineRenderer.SetPosition(1, currentPosition);
            endPosition = currentPosition;

            float alpha = 1f;
            Color color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            lineRenderer.startColor = color;
            lineRenderer.endColor = new Color(color.r, color.g, color.b, alpha * 0.65f);
        }

        public void SetImpactVisibility(bool visible)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = visible;
            }
        }

        public void FadeOut(float duration)
        {
            if (lineRenderer == null)
            {
                return;
            }

            StartCoroutine(FadeOutRoutine(duration));
        }

        System.Collections.IEnumerator FadeOutRoutine(float duration)
        {
            float startAlpha = lineRenderer.startColor.a;
            float t = 0f;
            while (t < duration && lineRenderer != null)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, t / duration);
                Color c = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                lineRenderer.startColor = c;
                lineRenderer.endColor = new Color(c.r, c.g, c.b, alpha * 0.5f);
                yield return null;
            }

            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
    }
}
