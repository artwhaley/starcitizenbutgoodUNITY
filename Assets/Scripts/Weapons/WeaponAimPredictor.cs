using UnityEngine;

namespace FlightModel
{
    public static class WeaponAimPredictor
    {
        public static Vector3 CalculateLeadPoint(
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
