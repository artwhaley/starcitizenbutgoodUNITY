using UnityEngine;

namespace FlightModel.World
{
    /// <summary>
    /// Attach to the root GameObject of any entity in the game world
    /// (ships, stations, asteroids, characters, etc.).
    /// </summary>
    public class WorldEntity : MonoBehaviour
    {
        [SerializeField] EntityKind kind;
        [SerializeField] int serializedEntityId;

        EntityId id;

        public EntityId Id
        {
            get => id;
            private set => id = value;
        }

        public EntityKind Kind
        {
            get => kind;
            set => kind = value;
        }

        public Transform Root => transform;

        public int SerializedEntityId => serializedEntityId;

        /// <summary>
        /// Assign a runtime entity ID. If a non-zero ID was already serialized
        /// on the prefab/instance, it takes priority and is preserved.
        /// </summary>
        public void Assign(EntityId newId, EntityKind fallbackKind)
        {
            if (serializedEntityId > 0)
            {
                id = new EntityId(serializedEntityId);
            }
            else
            {
                id = newId;
            }

            if (kind == EntityKind.Unknown && fallbackKind != EntityKind.Unknown)
            {
                kind = fallbackKind;
            }
        }
    }
}
