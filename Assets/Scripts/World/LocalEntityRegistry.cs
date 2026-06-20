using System.Collections.Generic;
using UnityEngine;

namespace FlightModel.World
{
    /// <summary>
    /// Local runtime entity registry. Allocates monotonically increasing entity IDs
    /// and tracks WorldEntity components in the active scene.
    ///
    /// For the single-player prototype this lives on a scene GameObject.
    /// Future multiplayer: the server will have its own authoritative registry.
    /// </summary>
    public class LocalEntityRegistry : MonoBehaviour
    {
        static LocalEntityRegistry instance;

        readonly Dictionary<EntityId, WorldEntity> entitiesById = new();
        readonly List<WorldEntity> entityList = new();
        int nextId = 1;

        public static LocalEntityRegistry Instance => instance;

        public IReadOnlyCollection<WorldEntity> Entities => entityList;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning(
                    $"LocalEntityRegistry: duplicate instance on '{name}'. " +
                    $"Keeping existing instance on '{instance.name}'.", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
            DiscoverSceneEntities();
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Register a WorldEntity and allocate an ID if needed.
        /// Returns the same entity for chaining.
        /// </summary>
        public WorldEntity Register(WorldEntity entity, EntityKind fallbackKind)
        {
            if (entity == null)
            {
                return null;
            }

            // If already registered under a valid ID, no-op
            if (entity.Id.IsValid && entitiesById.ContainsKey(entity.Id))
            {
                return entity;
            }

            // Determine the target ID, honoring serialized or pre-assigned IDs
            EntityId targetId = EntityId.Invalid;

            if (entity.Id.IsValid)
            {
                // Entity already has an assigned ID (e.g., from manual Assign call
                // or serializedEntityId already resolved). Honor it unless it collides.
                if (entitiesById.ContainsKey(entity.Id))
                {
                    Debug.LogError(
                        $"LocalEntityRegistry: entity '{entity.name}' has pre-assigned ID {entity.Id.Value} " +
                        $"that collides with an already-registered entity. Allocating fresh ID.", entity);
                }
                else
                {
                    targetId = entity.Id;
                }
            }
            else if (entity.SerializedEntityId > 0)
            {
                EntityId serializedId = new(entity.SerializedEntityId);
                if (entitiesById.ContainsKey(serializedId))
                {
                    Debug.LogError(
                        $"LocalEntityRegistry: duplicate serialized entity ID {entity.SerializedEntityId} " +
                        $"on '{entity.name}'. Allocating fresh ID instead.", entity);
                }
                else
                {
                    targetId = serializedId;
                }
            }

            // If no valid target ID yet, allocate a fresh one
            if (!targetId.IsValid)
            {
                targetId = AllocateId();
            }

            entity.Assign(targetId, fallbackKind);
            entitiesById[entity.Id] = entity;
            entityList.Add(entity);
            return entity;
        }

        /// <summary>
        /// Remove an entity from the registry.
        /// </summary>
        public void Unregister(WorldEntity entity)
        {
            if (entity == null || !entity.Id.IsValid)
            {
                return;
            }

            entitiesById.Remove(entity.Id);
            entityList.Remove(entity);
        }

        /// <summary>
        /// Look up a registered entity by ID.
        /// </summary>
        public bool TryGet(EntityId id, out WorldEntity entity) =>
            entitiesById.TryGetValue(id, out entity);

        /// <summary>
        /// Allocate a fresh, unused entity ID.
        /// </summary>
        public EntityId AllocateId()
        {
            while (entitiesById.ContainsKey(new EntityId(nextId)))
            {
                nextId++;
            }

            return new EntityId(nextId++);
        }

        void DiscoverSceneEntities()
        {
            WorldEntity[] existing = FindObjectsByType<WorldEntity>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
            {
                Register(existing[i], existing[i].Kind);
            }
        }
    }
}
