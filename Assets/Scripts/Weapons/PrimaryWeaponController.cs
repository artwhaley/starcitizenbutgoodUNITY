using UnityEngine;

namespace FlightModel
{
    public class PrimaryWeaponController : MonoBehaviour
    {
        [SerializeField] Transform gunNode1;
        [SerializeField] Transform gunNode2;
        [SerializeField] ShipCameraController cameraController;
        [SerializeField] Transform ignoreRoot;
        [SerializeField] WeaponTracerVfx tracerVfx;
        [SerializeField] float fireRatePerSecond = 20f;
        [SerializeField] float projectileSpeedMetersPerSecond = 650f;
        [SerializeField] float maxRangeMeters = 800f;
        [SerializeField] float targetAcquireAngleDegrees = 18f;
        [SerializeField] float debugLineDuration = 0.75f;
        [SerializeField] LayerMask hitMask = ~0;

        float accumulator;
        bool wasFireHeld;
        int nextGunIndex;
        float lastMissLogTime;

        public float ProjectileSpeedMetersPerSecond => projectileSpeedMetersPerSecond;

        void Awake()
        {
            if (tracerVfx == null)
            {
                tracerVfx = GetComponent<WeaponTracerVfx>();
                if (tracerVfx == null)
                {
                    tracerVfx = gameObject.AddComponent<WeaponTracerVfx>();
                }
            }
        }

        public void SetGunNodes(Transform gun1, Transform gun2)
        {
            gunNode1 = gun1;
            gunNode2 = gun2;
        }

        public void Tick(bool fireHeld, float deltaTime)
        {
            if (!fireHeld)
            {
                accumulator = 0f;
                wasFireHeld = false;
                return;
            }

            float interval = 1f / Mathf.Max(0.1f, fireRatePerSecond);
            if (!wasFireHeld)
            {
                accumulator = interval;
            }

            accumulator += deltaTime;
            int shots = 0;
            while (accumulator >= interval && shots < 5)
            {
                FireOneShot();
                accumulator -= interval;
                shots++;
            }

            wasFireHeld = true;
        }

        void FireOneShot()
        {
            Camera cam = cameraController != null ? cameraController.GetActiveCamera() : Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector3 aimStart = cam.transform.position;
            Vector3 aimEnd = aimStart + cam.transform.forward * maxRangeMeters;
            if (Physics.Raycast(aimStart, cam.transform.forward, out RaycastHit aimHit, maxRangeMeters, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (!IsIgnored(aimHit.collider.transform))
                {
                    aimEnd = aimHit.point;
                }
            }

            Transform gun = GetNextGunNode();
            Vector3 shotStart = gun != null ? gun.position : aimStart;
            Vector3 direction = (aimEnd - shotStart).normalized;
            if (direction.sqrMagnitude < 1e-6f)
            {
                direction = cam.transform.forward;
            }

            Vector3 shotEnd = shotStart + direction * maxRangeMeters;
            bool hit = Physics.Raycast(shotStart, direction, out RaycastHit shotHit, maxRangeMeters, hitMask, QueryTriggerInteraction.Ignore);
            if (hit && !IsIgnored(shotHit.collider.transform))
            {
                shotEnd = shotHit.point;
                Debug.DrawLine(shotStart, shotEnd, Color.green, debugLineDuration);
                tracerVfx?.DrawTracer(shotStart, shotEnd, true, debugLineDuration, projectileSpeedMetersPerSecond);

                if (shotHit.collider.TryGetComponent<SimpleTarget>(out SimpleTarget target))
                {
                    target.RegisterHit();
                }
                else
                {
                    Debug.Log($"HIT: {shotHit.collider.name}");
                }
            }
            else
            {
                Debug.DrawLine(shotStart, shotEnd, Color.red, debugLineDuration);
                tracerVfx?.DrawTracer(shotStart, shotEnd, false, debugLineDuration, projectileSpeedMetersPerSecond);
                if (Time.time - lastMissLogTime > 0.5f)
                {
                    Debug.Log("MISS");
                    lastMissLogTime = Time.time;
                }
            }
        }

        Transform GetNextGunNode()
        {
            Transform[] guns = { gunNode1, gunNode2 };
            for (int attempt = 0; attempt < guns.Length; attempt++)
            {
                int index = (nextGunIndex + attempt) % guns.Length;
                if (guns[index] != null)
                {
                    nextGunIndex = (index + 1) % guns.Length;
                    return guns[index];
                }
            }

            return null;
        }

        bool IsIgnored(Transform hitTransform)
        {
            return ignoreRoot != null && hitTransform.IsChildOf(ignoreRoot);
        }

        public bool TryGetLeadPoint(Camera cam, out Vector3 leadPoint, out SimpleTarget target)
        {
            leadPoint = default;
            target = null;
            if (cam == null)
            {
                return false;
            }

            if (!SimpleTarget.TryFindBestTarget(
                    cam.transform.position,
                    cam.transform.forward,
                    maxRangeMeters,
                    targetAcquireAngleDegrees,
                    out target))
            {
                return false;
            }

            Vector3 shooterPosition = cam.transform.position;
            leadPoint = CalculateLeadPoint(
                shooterPosition,
                target.AimPoint,
                target.Velocity,
                Mathf.Max(1f, projectileSpeedMetersPerSecond));
            return true;
        }

        static Vector3 CalculateLeadPoint(
            Vector3 shooterPosition,
            Vector3 targetPosition,
            Vector3 targetVelocity,
            float projectileSpeed)
        {
            Vector3 toTarget = targetPosition - shooterPosition;
            float a = Vector3.Dot(targetVelocity, targetVelocity) - projectileSpeed * projectileSpeed;
            float b = 2f * Vector3.Dot(toTarget, targetVelocity);
            float c = Vector3.Dot(toTarget, toTarget);
            float time;

            if (Mathf.Abs(a) < 0.001f)
            {
                time = Mathf.Abs(b) > 0.001f ? Mathf.Max(0f, -c / b) : toTarget.magnitude / projectileSpeed;
            }
            else
            {
                float discriminant = b * b - 4f * a * c;
                if (discriminant < 0f)
                {
                    time = toTarget.magnitude / projectileSpeed;
                }
                else
                {
                    float sqrt = Mathf.Sqrt(discriminant);
                    float t1 = (-b - sqrt) / (2f * a);
                    float t2 = (-b + sqrt) / (2f * a);
                    time = t1 > 0f && t2 > 0f ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2);
                    if (time < 0f)
                    {
                        time = toTarget.magnitude / projectileSpeed;
                    }
                }
            }

            return targetPosition + targetVelocity * time;
        }
    }
}
