using UnityEngine;

namespace FlightModel
{
    public class RcsThrusterVfx : MonoBehaviour
    {
        static readonly Color NormalRcsColor = new(0.55f, 0.78f, 1f, 0.9f);
        static readonly Color BoostRcsColor = new(1f, 0.86f, 0.25f, 0.95f);

        [SerializeField] ShipVisualReferences visuals;
        [SerializeField] ShipInputReader input;
        [SerializeField] ShipFlightController flight;
        [SerializeField] GameObject puffPrefab;
        [SerializeField] float emitInterval = 0.025f;
        [SerializeField] float inputThreshold = 0.16f;
        [SerializeField] float visualDeadzone = 0.12f;
        [SerializeField] int puffCountPerEmit = 4;
        [SerializeField] float minSpeedMultiplier = 0.35f;
        [SerializeField] float maxSpeedMultiplier = 1.85f;
        [SerializeField] float minBurstMultiplier = 0.12f;
        [SerializeField] float maxBurstMultiplier = 1.9f;

        readonly System.Collections.Generic.Dictionary<Transform, ParticleSystem> nodePuffs = new();
        float timer;
        bool puffsBuilt;

        public void Configure(ShipVisualReferences shipVisuals, ShipInputReader shipInput, GameObject puff)
            => Configure(shipVisuals, shipInput, null, puff);

        public void Configure(ShipVisualReferences shipVisuals, ShipInputReader shipInput, ShipFlightController shipFlight, GameObject puff)
        {
            visuals = shipVisuals;
            input = shipInput;
            flight = shipFlight;
            puffPrefab = VfxPrefabResolver.IsUsablePrefab(puff) ? puff : null;
            puffsBuilt = false;
            nodePuffs.Clear();
            TryEnable();
            BuildNodePuffs();
        }

        void Awake()
        {
            TryEnable();
            BuildNodePuffs();
        }

        void TryEnable()
        {
            if (visuals == null || visuals.RcsNodes == null || visuals.RcsNodes.Count == 0 || puffPrefab == null)
            {
                enabled = false;
                if (visuals != null && (visuals.RcsNodes == null || visuals.RcsNodes.Count == 0))
                {
                    Debug.LogWarning("RcsThrusterVfx: no RCS nodes wired; disabling.", this);
                }

                return;
            }

            enabled = true;
        }

        void BuildNodePuffs()
        {
            if (puffsBuilt || !enabled || puffPrefab == null || visuals == null || visuals.RcsNodes == null)
            {
                return;
            }

            foreach (Transform node in visuals.RcsNodes)
            {
                if (node == null || nodePuffs.ContainsKey(node))
                {
                    continue;
                }

                Transform existing = node.Find("RcsPuff");
                ParticleSystem particleSystem;
                if (existing != null)
                {
                    particleSystem = existing.GetComponent<ParticleSystem>();
                    AlignPuffTransform(existing, node);
                }
                else
                {
                    GameObject instance = Instantiate(puffPrefab, node);
                    instance.name = "RcsPuff";
                    particleSystem = instance.GetComponent<ParticleSystem>();
                    AlignPuffTransform(instance.transform, node);
                }

                if (particleSystem == null)
                {
                    continue;
                }

                ParticleVfxUtility.ConfigureGasPlume(particleSystem, GasPlumeProfile.Rcs);
                ParticleVfxUtility.ApplyProfileMaterial(particleSystem, GasPlumeProfile.Rcs);
                nodePuffs[node] = particleSystem;
            }

            puffsBuilt = nodePuffs.Count > 0;
        }

        void AlignPuffTransform(Transform puffTransform, Transform node)
        {
            puffTransform.localPosition = Vector3.zero;
            puffTransform.localRotation = BlenderImportedAxes.GetPlumeLocalRotation(
                node,
                visuals.MarkerActionAxisLocal);
            puffTransform.localScale = Vector3.one;
        }

        void Update()
        {
            if (!enabled || !puffsBuilt || visuals == null)
            {
                return;
            }

            ShipInputCommand command = ResolveAppliedCommand();
            float activity = Mathf.Max(
                Mathf.Abs(command.thrustForward),
                Mathf.Abs(command.thrustRight),
                Mathf.Abs(command.thrustUp),
                Mathf.Abs(command.pitch),
                Mathf.Abs(command.yaw),
                Mathf.Abs(command.roll));

            if (activity < inputThreshold)
            {
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            timer = emitInterval;
            Color rcsColor = command.boost ? BoostRcsColor : NormalRcsColor;

            foreach (Transform node in visuals.RcsNodes)
            {
                if (node == null || !nodePuffs.TryGetValue(node, out ParticleSystem particleSystem))
                {
                    continue;
                }

                float strength = RcsThrusterMatcher.GetEmissionStrength(node.name, command);
                if (strength <= visualDeadzone)
                {
                    continue;
                }

                strength = Mathf.InverseLerp(visualDeadzone, 1f, strength);

                var main = particleSystem.main;
                main.startSpeedMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, strength);
                main.startColor = rcsColor;

                float burstMultiplier = Mathf.Lerp(minBurstMultiplier, maxBurstMultiplier, strength);
                int puffCount = Mathf.Max(1, Mathf.CeilToInt(puffCountPerEmit * burstMultiplier));
                if (command.brake)
                {
                    puffCount = Mathf.CeilToInt(puffCount * 1.5f);
                }

                particleSystem.Emit(puffCount);
            }
        }

        ShipInputCommand ResolveAppliedCommand()
        {
            if (flight != null)
            {
                return flight.LastThrusterOutput.ToMatcherCommand();
            }

            return input != null ? input.LastCommand : default;
        }
    }
}
