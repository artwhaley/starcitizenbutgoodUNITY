using UnityEngine;

namespace FlightModel
{
    public class SimpleTarget : MonoBehaviour
    {
        static readonly System.Collections.Generic.List<SimpleTarget> ActiveTargets = new();

        [SerializeField] int hitPoints = 3;
        [SerializeField] float hitFlashSeconds = 0.12f;
        [SerializeField] Renderer meshRenderer;
        [SerializeField] Color hitFlashColor = Color.red;
        [Header("Drone Orbit")]
        [SerializeField] bool orbitEnabled = true;
        [SerializeField] Vector3 orbitCenter = Vector3.zero;
        [SerializeField] float orbitRadius;
        [SerializeField] float orbitDegreesPerSecond = 5f;
        [SerializeField] bool faceTravelDirection = true;

        Color originalColor;
        int remainingHitPoints;
        float flashTimer;
        float orbitAngleDegrees;
        Vector3 velocity;

        public Vector3 Velocity => velocity;
        public Vector3 AimPoint => transform.position;

        void Awake()
        {
            remainingHitPoints = hitPoints;
            if (meshRenderer != null)
            {
                originalColor = meshRenderer.material.color;
            }

            Vector3 offset = transform.position - orbitCenter;
            Vector2 horizontalOffset = new(offset.x, offset.z);
            if (orbitRadius <= 0.1f)
            {
                orbitRadius = Mathf.Max(80f, horizontalOffset.magnitude);
            }

            orbitAngleDegrees = horizontalOffset.sqrMagnitude > 0.01f
                ? Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg
                : 0f;
        }

        void OnEnable()
        {
            if (!ActiveTargets.Contains(this))
            {
                ActiveTargets.Add(this);
            }
        }

        void OnDisable()
        {
            ActiveTargets.Remove(this);
        }

        void Update()
        {
            UpdateOrbit();
            UpdateHitFlash();
        }

        void UpdateOrbit()
        {
            if (!orbitEnabled)
            {
                velocity = Vector3.zero;
                return;
            }

            Vector3 previous = transform.position;
            orbitAngleDegrees += orbitDegreesPerSecond * Time.deltaTime;
            float radians = orbitAngleDegrees * Mathf.Deg2Rad;
            Vector3 next = new(
                orbitCenter.x + Mathf.Cos(radians) * orbitRadius,
                previous.y,
                orbitCenter.z + Mathf.Sin(radians) * orbitRadius);

            transform.position = next;
            velocity = Time.deltaTime > 0f ? (next - previous) / Time.deltaTime : Vector3.zero;

            if (faceTravelDirection && velocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            }
        }

        void UpdateHitFlash()
        {
            if (flashTimer <= 0f)
            {
                return;
            }

            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && meshRenderer != null)
            {
                meshRenderer.material.color = originalColor;
            }
        }

        public void RegisterHit()
        {
            remainingHitPoints--;
            Debug.Log("TARGET HIT");

            if (meshRenderer != null)
            {
                meshRenderer.material.color = hitFlashColor;
                flashTimer = hitFlashSeconds;
            }

            if (remainingHitPoints <= 0)
            {
                gameObject.SetActive(false);
            }
        }

        public static bool TryFindBestTarget(
            Vector3 origin,
            Vector3 forward,
            float maxRange,
            float maxAngleDegrees,
            out SimpleTarget target)
        {
            target = null;
            float bestScore = float.MaxValue;
            float maxRangeSqr = maxRange * maxRange;

            for (int i = ActiveTargets.Count - 1; i >= 0; i--)
            {
                SimpleTarget candidate = ActiveTargets[i];
                if (candidate == null || !candidate.isActiveAndEnabled)
                {
                    ActiveTargets.RemoveAt(i);
                    continue;
                }

                Vector3 toTarget = candidate.AimPoint - origin;
                float distanceSqr = toTarget.sqrMagnitude;
                if (distanceSqr > maxRangeSqr || distanceSqr < 0.01f)
                {
                    continue;
                }

                float angle = Vector3.Angle(forward, toTarget);
                if (angle > maxAngleDegrees)
                {
                    continue;
                }

                float score = angle * 1000f + distanceSqr * 0.001f;
                if (score < bestScore)
                {
                    bestScore = score;
                    target = candidate;
                }
            }

            return target != null;
        }
    }
}
