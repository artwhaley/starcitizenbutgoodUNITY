using UnityEngine;

namespace FlightModel.Asteroids
{
    public class MineableAsteroid : MonoBehaviour, IHitReceiver
    {
        [SerializeField] Color depletedTint = new(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] float hitFlashSeconds = 0.12f;
        [SerializeField] Color hitFlashColor = new(1f, 0.65f, 0.2f, 1f);

        AsteroidInstance boundInstance;
        Color baseColor = Color.white;
        float flashTimer;
        bool hasBaseColor;

        public void Bind(AsteroidInstance instance)
        {
            boundInstance = instance;
            AsteroidResourceRegistry.Instance?.GetOrCreate(instance.Descriptor);
            CacheBaseColor();
            RefreshVisual();
        }

        public void Unbind()
        {
            boundInstance = null;
            flashTimer = 0f;
        }

        public void ApplyHit(in HitEvent hit)
        {
            if (boundInstance == null || !boundInstance.IsPromoted)
            {
                return;
            }

            AsteroidResourceRegistry registry = AsteroidResourceRegistry.Instance;
            if (registry == null)
            {
                Debug.LogWarning("MineableAsteroid: no AsteroidResourceRegistry in scene.", this);
                return;
            }

            registry.GetOrCreate(boundInstance.Descriptor);
            float amount = hit.damage > 0f ? hit.damage : 1f;
            if (!registry.ApplyMiningHit(boundInstance.DescriptorId, amount, out AsteroidResourceState state))
            {
                return;
            }

            TriggerHitFlash();
            RefreshVisual(state);
            Debug.Log(
                $"ASTEROID HIT {boundInstance.DescriptorId} remaining={state.remainingResourceUnits}/{state.totalResourceUnits}");
        }

        void Update()
        {
            if (flashTimer <= 0f || boundInstance?.MeshRenderer == null)
            {
                return;
            }

            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                RefreshVisual();
            }
        }

        void TriggerHitFlash()
        {
            if (boundInstance?.MeshRenderer == null)
            {
                return;
            }

            boundInstance.MeshRenderer.material.color = hitFlashColor;
            flashTimer = hitFlashSeconds;
        }

        void RefreshVisual()
        {
            AsteroidResourceRegistry registry = AsteroidResourceRegistry.Instance;
            if (registry != null
                && boundInstance != null
                && registry.TryGet(boundInstance.DescriptorId, out AsteroidResourceState state))
            {
                RefreshVisual(state);
                return;
            }

            ApplyRendererColor(hasBaseColor ? baseColor : Color.white);
        }

        void RefreshVisual(in AsteroidResourceState state)
        {
            Color color = hasBaseColor ? baseColor : Color.white;
            if (state.depleted)
            {
                color = Color.Lerp(color, depletedTint, 0.85f);
            }

            ApplyRendererColor(color);
        }

        void CacheBaseColor()
        {
            if (boundInstance?.MeshRenderer != null)
            {
                baseColor = boundInstance.MeshRenderer.sharedMaterial != null
                    ? boundInstance.MeshRenderer.sharedMaterial.color
                    : Color.white;
                hasBaseColor = true;
            }
        }

        void ApplyRendererColor(Color color)
        {
            if (boundInstance?.MeshRenderer != null)
            {
                boundInstance.MeshRenderer.material.color = color;
            }
        }
    }
}
