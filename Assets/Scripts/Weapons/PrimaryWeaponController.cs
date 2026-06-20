using UnityEngine;
using FlightModel.World;

namespace FlightModel
{
    public class PrimaryWeaponController : MonoBehaviour
    {
        [SerializeField] LocalGameAuthority authority;
        [SerializeField] WeaponDefinition weaponDefinition;
        [SerializeField] ShipCameraController cameraController;
        [SerializeField] float targetAcquireAngleDegrees = 18f;

        WorldEntity owningEntity;
        static bool loggedEntityWarning;

        public float ProjectileSpeedMetersPerSecond => weaponDefinition != null
            ? weaponDefinition.projectileSpeedMetersPerSecond
            : 650f;

        public float MaxRangeMeters => weaponDefinition != null
            ? weaponDefinition.maxRangeMeters
            : 800f;

        static bool loggedMissingAuthority;

        void Awake()
        {
            if (authority == null)
            {
                authority = GetComponentInParent<LocalGameAuthority>();
            }
        }

        public void Tick(bool fireHeld)
        {
            if (authority == null)
            {
                if (!loggedMissingAuthority)
                {
                    Debug.LogError(
                        "PrimaryWeaponController: no LocalGameAuthority found. " +
                        "Add LocalGameAuthority to PF_PlayerShip prefab. Weapons disabled.", this);
                    loggedMissingAuthority = true;
                }

                return;
            }

            if (authority.weaponDefinition == null && weaponDefinition != null)
            {
                authority.weaponDefinition = weaponDefinition;
            }

            if (owningEntity == null)
            {
                owningEntity = GetComponentInParent<WorldEntity>();
            }

            int shooterId = 1;
            if (owningEntity != null && owningEntity.Id.IsValid)
            {
                shooterId = owningEntity.Id.Value;
            }
            else if (!loggedEntityWarning)
            {
                Debug.LogWarning(
                    "PrimaryWeaponController: no WorldEntity found in parent hierarchy. " +
                    "shooterEntityId defaults to 1.", this);
                loggedEntityWarning = true;
            }

            uint fireTick = authority.ServerTick;

            authority.SubmitWeaponFire(new Authority.WeaponFireRequest
            {
                clientId = 1,
                shooterEntityId = shooterId,
                weaponSlot = 0,
                inputTick = fireTick,
                fireHeld = fireHeld
            });
        }

        public bool TryGetLeadPoint(Camera cam, out Vector3 leadPoint, out SimpleTarget target)
        {
            leadPoint = default;
            target = null;
            if (cam == null)
            {
                return false;
            }

            float maxRange = MaxRangeMeters;
            if (!SimpleTarget.TryFindBestTarget(
                    cam.transform.position,
                    cam.transform.forward,
                    maxRange,
                    targetAcquireAngleDegrees,
                    out target))
            {
                return false;
            }

            Vector3 shooterPosition = cam.transform.position;
            leadPoint = WeaponAimPredictor.CalculateLeadPoint(
                shooterPosition,
                target.AimPoint,
                target.Velocity,
                Mathf.Max(1f, ProjectileSpeedMetersPerSecond));
            return true;
        }
    }
}
