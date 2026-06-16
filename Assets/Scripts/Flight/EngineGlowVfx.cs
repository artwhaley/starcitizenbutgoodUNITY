using UnityEngine;

namespace FlightModel
{
    public class EngineGlowVfx : MonoBehaviour
    {
        [SerializeField] ShipVisualReferences visuals;
        [SerializeField] ShipInputReader input;
        [SerializeField] GameObject plumePrefab;
        [SerializeField] float inputThreshold = 0.05f;
        [SerializeField] float plumeEmissionRate = 48f;
        [SerializeField] float boostEmissionMultiplier = 2.2f;
        [SerializeField] Color plumeColor = new(0.6f, 0.88f, 1f, 0.9f);

        ParticleSystem[] plumes;

        public void Configure(ShipVisualReferences shipVisuals, ShipInputReader shipInput, GameObject plume = null)
        {
            visuals = shipVisuals;
            input = shipInput;
            if (plume != null)
            {
                plumePrefab = plume;
            }

            BuildPlumes();
        }

        void Awake() => BuildPlumes();

        void BuildPlumes()
        {
            if (visuals == null || visuals.EngineNodes == null || visuals.EngineNodes.Count == 0)
            {
                enabled = false;
                return;
            }

            plumes = new ParticleSystem[visuals.EngineNodes.Count];

            for (int i = 0; i < visuals.EngineNodes.Count; i++)
            {
                Transform node = visuals.EngineNodes[i];
                if (node == null)
                {
                    continue;
                }

                Transform existing = node.Find("EnginePlume");
                ParticleSystem particleSystem;
                if (existing != null)
                {
                    particleSystem = existing.GetComponent<ParticleSystem>();
                    AlignPlumeTransform(existing, node);
                }
                else if (plumePrefab != null)
                {
                    GameObject instance = Instantiate(plumePrefab, node);
                    instance.name = "EnginePlume";
                    particleSystem = instance.GetComponent<ParticleSystem>();
                    AlignPlumeTransform(instance.transform, node);
                }
                else
                {
                    GameObject plumeObject = new("EnginePlume", typeof(ParticleSystem));
                    plumeObject.transform.SetParent(node, false);
                    AlignPlumeTransform(plumeObject.transform, node);
                    particleSystem = plumeObject.GetComponent<ParticleSystem>();
                }

                if (particleSystem != null)
                {
                    ParticleVfxUtility.ConfigureGasPlume(particleSystem, GasPlumeProfile.Engine);
                    ParticleVfxUtility.ApplyProfileMaterial(particleSystem, GasPlumeProfile.Engine);
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    plumes[i] = particleSystem;
                }
            }

            enabled = true;
        }

        void AlignPlumeTransform(Transform plumeTransform, Transform engineNode)
        {
            Transform outwardFrom = visuals.Cog != null ? visuals.Cog : visuals.transform;
            plumeTransform.localPosition = Vector3.zero;
            plumeTransform.localRotation = BlenderImportedAxes.GetPlumeLocalRotation(
                engineNode,
                visuals.MarkerActionAxisLocal,
                outwardFrom);
            plumeTransform.localScale = Vector3.one;
        }

        void Update()
        {
            if (input == null || plumes == null || visuals == null)
            {
                return;
            }

            ShipInputCommand command = input.LastCommand;
            float forward = Mathf.Max(0f, command.thrustForward);
            bool enginesActive = forward > inputThreshold;
            float emissionScale = forward;

            if (command.boost && forward > inputThreshold)
            {
                emissionScale *= boostEmissionMultiplier;
            }

            float targetRate = enginesActive ? plumeEmissionRate * emissionScale : 0f;

            for (int i = 0; i < plumes.Length; i++)
            {
                ParticleSystem plume = plumes[i];
                if (plume == null)
                {
                    continue;
                }

                var emission = plume.emission;
                emission.rateOverTime = targetRate;

                var main = plume.main;
                float heat = command.boost ? 1f : Mathf.Clamp01(forward);
                Color hotColor = Color.Lerp(plumeColor, new Color(1f, 0.72f, 0.28f, 1f), heat * 0.35f);
                main.startColor = hotColor;

                var sizeOverLifetime = plume.sizeOverLifetime;
                sizeOverLifetime.sizeMultiplier = Mathf.Lerp(0.75f, command.boost ? 1.55f : 1.2f, Mathf.Clamp01(forward));

                if (enginesActive && !plume.isPlaying)
                {
                    plume.Play();
                }
                else if (!enginesActive && plume.isPlaying)
                {
                    plume.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }
    }
}
