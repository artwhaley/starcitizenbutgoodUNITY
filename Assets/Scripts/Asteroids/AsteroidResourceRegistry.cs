using System.Collections.Generic;
using UnityEngine;

namespace FlightModel.Asteroids
{
    public class AsteroidResourceRegistry : MonoBehaviour
    {
        public const int MinResourceUnits = 25;
        public const int MaxResourceUnits = 150;

        static AsteroidResourceRegistry instance;

        readonly Dictionary<AsteroidDescriptorId, AsteroidResourceState> statesByDescriptorId = new();

        public static AsteroidResourceRegistry Instance => instance;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning(
                    $"AsteroidResourceRegistry: duplicate instance on '{name}'. " +
                    $"Keeping existing instance on '{instance.name}'.", this);
                Destroy(this);
                return;
            }

            instance = this;
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public static int ComputeTotalResourceUnits(int resourceSeed)
        {
            uint seed = unchecked((uint)resourceSeed);
            int span = MaxResourceUnits - MinResourceUnits + 1;
            return MinResourceUnits + (int)(seed % (uint)span);
        }

        public AsteroidResourceState GetOrCreate(in AsteroidDescriptor descriptor)
        {
            if (!descriptor.id.IsValid)
            {
                return default;
            }

            if (statesByDescriptorId.TryGetValue(descriptor.id, out AsteroidResourceState existing))
            {
                return existing;
            }

            var created = new AsteroidResourceState
            {
                descriptorId = descriptor.id,
                resourceSeed = descriptor.resourceSeed,
                totalResourceUnits = ComputeTotalResourceUnits(descriptor.resourceSeed),
                remainingResourceUnits = ComputeTotalResourceUnits(descriptor.resourceSeed),
                depleted = false
            };

            statesByDescriptorId[descriptor.id] = created;
            return created;
        }

        public bool TryGet(AsteroidDescriptorId descriptorId, out AsteroidResourceState state)
        {
            return statesByDescriptorId.TryGetValue(descriptorId, out state);
        }

        public bool ApplyMiningHit(AsteroidDescriptorId descriptorId, float amount, out AsteroidResourceState state)
        {
            state = default;
            if (!descriptorId.IsValid)
            {
                return false;
            }

            if (!statesByDescriptorId.TryGetValue(descriptorId, out state))
            {
                return false;
            }

            if (state.depleted || amount <= 0f)
            {
                return false;
            }

            int depletion = Mathf.Max(1, Mathf.RoundToInt(amount));
            state.remainingResourceUnits = Mathf.Max(0, state.remainingResourceUnits - depletion);
            state.depleted = state.remainingResourceUnits <= 0;
            statesByDescriptorId[descriptorId] = state;
            return true;
        }

        public void ClearForTests()
        {
            statesByDescriptorId.Clear();
        }
    }
}
