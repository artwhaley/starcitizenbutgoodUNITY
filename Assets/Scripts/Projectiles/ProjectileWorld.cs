using System.Collections.Generic;
using UnityEngine;

namespace FlightModel
{
    public class ProjectileWorld
    {
        readonly List<ProjectileState> activeProjectiles = new();
        readonly List<int> despawnList = new();
        readonly List<ProjectileState> spawnQueue = new();

        public int ActiveCount => activeProjectiles.Count;

        public void SpawnProjectile(
            int projectileId,
            int ownerEntityId,
            Vector3 position,
            Vector3 velocity,
            float maxLifetimeSeconds,
            float maxRangeMeters,
            float damage)
        {
            spawnQueue.Add(new ProjectileState(
                projectileId,
                ownerEntityId,
                position,
                velocity,
                maxLifetimeSeconds,
                maxRangeMeters,
                damage));
        }

        public void TickProjectiles(float deltaTime, LayerMask hitMask, Transform ignoreRoot)
        {
            // Flush spawn queue
            if (spawnQueue.Count > 0)
            {
                activeProjectiles.AddRange(spawnQueue);
                spawnQueue.Clear();
            }

            if (activeProjectiles.Count == 0)
            {
                return;
            }

            despawnList.Clear();

            for (int i = 0; i < activeProjectiles.Count; i++)
            {
                ProjectileState proj = activeProjectiles[i];
                if (!proj.alive)
                {
                    despawnList.Add(i);
                    continue;
                }

                proj.remainingLifetime -= deltaTime;

                Vector3 previousPosition = proj.position;
                Vector3 nextPosition = proj.position + proj.velocity * deltaTime;
                float segmentLength = (nextPosition - previousPosition).magnitude;
                float totalTravel = (nextPosition - proj.spawnPosition).magnitude;

                bool shouldDespawn = false;

                if (proj.remainingLifetime <= 0f || totalTravel >= proj.maxRangeMeters)
                {
                    shouldDespawn = true;
                }

                if (!shouldDespawn && segmentLength > 0.001f)
                {
                    Vector3 direction = (nextPosition - previousPosition).normalized;
                    if (Physics.Raycast(previousPosition, direction, out RaycastHit hit, segmentLength, hitMask, QueryTriggerInteraction.Ignore))
                    {
                        if (!IsIgnored(hit.collider.transform, ignoreRoot))
                        {
                            nextPosition = hit.point;
                            shouldDespawn = true;

                            if (hit.collider.TryGetComponent<IHitReceiver>(out IHitReceiver receiver))
                            {
                                receiver.ApplyHit(new HitEvent
                                {
                                    projectileId = proj.projectileId,
                                    ownerEntityId = proj.ownerEntityId,
                                    point = hit.point,
                                    normal = hit.normal,
                                    damage = proj.damage
                                });
                            }
                        }
                    }
                }

                proj.position = nextPosition;
                activeProjectiles[i] = proj;

                if (shouldDespawn)
                {
                    proj.alive = false;
                    activeProjectiles[i] = proj;
                    despawnList.Add(i);
                }
            }

            // Remove despawned projectiles (reverse order to maintain indices)
            for (int i = despawnList.Count - 1; i >= 0; i--)
            {
                activeProjectiles.RemoveAt(despawnList[i]);
            }
        }

        public void GetActiveProjectiles(List<ProjectileState> outList)
        {
            outList.Clear();
            outList.AddRange(activeProjectiles);
        }

        static bool IsIgnored(Transform hitTransform, Transform ignoreRoot)
        {
            return ignoreRoot != null && hitTransform.IsChildOf(ignoreRoot);
        }
    }
}
