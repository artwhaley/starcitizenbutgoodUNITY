using UnityEngine;
using FlightModel.Authority;
using FlightModel.World;

namespace FlightModel
{
    public class LocalGameAuthority : MonoBehaviour, IGameAuthority
    {
        public const float SimulationDeltaSeconds = 1f / 60f;

        public WeaponDefinition weaponDefinition;
        public ShipWeaponHardpoints hardpoints;
        public Transform playerShipRoot;
        public ProjectileViewPool viewPool;
        [SerializeField] LayerMask hitMask = ~0;
        public ShipFlightController controlledFlight;
        public FlightModel.Docking.DockingCaptureController dockingCapture;

        ProjectileWorld projectileWorld;
        int nextProjectileId = 1;
        float fireAccumulator;
        float nextTelemetryTime;
        int spawnCountThisSecond;
        int processFireCallsThisSecond;
        string lastSpawnFailureReason;
        float tickAccumulator;

        const int ClientId = 1;

        public uint ServerTick { get; private set; }

        LocalEntityRegistry registry;
        WorldEntity controlledShip;
        static bool loggedWorldEntityWarning;

        Authority.WeaponFireRequest pendingFireRequest;
        bool hasPendingFire;
        bool pendingFireHeld;

        ClientInputCommand latestInputCommand;
        bool hasInputCommand;

        public ProjectileWorld ProjectileWorld => projectileWorld;

        /// <summary>
        /// The entity ID of the locally controlled ship, or 1 if not yet registered.
        /// </summary>
        public int ControlledEntityId => controlledShip != null && controlledShip.Id.IsValid
            ? controlledShip.Id.Value
            : 1;

        /// <summary>
        /// The ship state last produced by simulation, or default if no sim has run yet.
        /// </summary>
        public ShipState LatestShipState => controlledFlight != null
            ? controlledFlight.State
            : default;

        void Awake()
        {
            projectileWorld = new ProjectileWorld();
            EnsureEntityRegistry();
            EnsureControlledShipEntity();
            WireControlledFlight();
        }

        void WireControlledFlight()
        {
            if (controlledFlight == null)
            {
                controlledFlight = GetComponent<ShipFlightController>();
            }

            if (controlledFlight == null)
            {
                controlledFlight = GetComponentInChildren<ShipFlightController>(true);
            }

            if (controlledFlight == null)
            {
                Debug.LogError(
                    "LocalGameAuthority: no ShipFlightController found. " +
                    "Flight simulation will not run.", this);
            }
        }

        void EnsureEntityRegistry()
        {
            LocalEntityRegistry[] existing = FindObjectsByType<LocalEntityRegistry>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            registry = existing.Length > 0 ? existing[0] : null;

            if (registry == null)
            {
                var go = new GameObject("LocalEntityRegistry");
                DontDestroyOnLoad(go);
                registry = go.AddComponent<LocalEntityRegistry>();
            }
        }

        void EnsureControlledShipEntity()
        {
            if (playerShipRoot == null)
            {
                return;
            }

            if (controlledShip != null
                && controlledShip.Id.IsValid
                && (controlledShip.transform == playerShipRoot
                    || controlledShip.transform.IsChildOf(playerShipRoot)
                    || playerShipRoot.IsChildOf(controlledShip.transform)))
            {
                return;
            }

            // Prefer the root or its parent chain for an existing WorldEntity
            Transform search = playerShipRoot;
            while (search != null)
            {
                WorldEntity entity = search.GetComponent<WorldEntity>();
                if (entity != null)
                {
                    controlledShip = registry.Register(entity, EntityKind.Ship);
                    return;
                }

                search = search.parent;
            }

            // Temporary bridge: add WorldEntity at runtime
            controlledShip = playerShipRoot.gameObject.AddComponent<WorldEntity>();
            registry.Register(controlledShip, EntityKind.Ship);

            if (!loggedWorldEntityWarning)
            {
                Debug.LogWarning(
                    $"LocalGameAuthority: added WorldEntity to '{playerShipRoot.name}' at runtime. " +
                    "TODO: add WorldEntity component to PF_PlayerShip prefab.", playerShipRoot);
                loggedWorldEntityWarning = true;
            }
        }

        public void RefreshControlledShipEntity(Transform root = null)
        {
            if (root != null)
            {
                playerShipRoot = root;
            }

            EnsureEntityRegistry();
            EnsureControlledShipEntity();
        }

        public void SubmitInput(in ClientInputCommand command)
        {
            latestInputCommand = command;
            hasInputCommand = true;
        }

        public void SubmitWeaponFire(in Authority.WeaponFireRequest request)
        {
            pendingFireRequest = request;
            pendingFireHeld = request.fireHeld;
            hasPendingFire = true;
        }

        public void Tick(float deltaTime)
        {
            tickAccumulator += deltaTime;
            tickAccumulator = Mathf.Min(tickAccumulator, SimulationDeltaSeconds * 3f);
            bool anySimulationRan = false;

            while (tickAccumulator >= SimulationDeltaSeconds)
            {
                tickAccumulator -= SimulationDeltaSeconds;

                // Simulate controlled ship from latest submitted input.
                bool skipControlledSimulate = dockingCapture != null && dockingCapture.IsFlightGated;
                if (controlledFlight != null && !skipControlledSimulate)
                {
                    ShipInputCommand shipInput = hasInputCommand
                        ? latestInputCommand.shipInput
                        : default;
                    controlledFlight.Simulate(SimulationDeltaSeconds, shipInput);
                }

                // Docking capture override (magnetic attraction, snap, docked
                // pose follow, recapture lockout). Runs after simulation so the
                // controller can read the fresh ShipState and overwrite it.
                if (dockingCapture != null)
                {
                    dockingCapture.TickOverride(SimulationDeltaSeconds);
                }

                if (hasPendingFire)
                {
                    ProcessFire(SimulationDeltaSeconds);
                }

                LayerMask activeHitMask = weaponDefinition != null ? weaponDefinition.hitMask : hitMask;
                projectileWorld.TickProjectiles(SimulationDeltaSeconds, activeHitMask, playerShipRoot);

                ServerTick++;
                anySimulationRan = true;
            }

            if (anySimulationRan)
            {
                viewPool?.SyncViews(projectileWorld);
            }

            if (Time.unscaledTime >= nextTelemetryTime && (processFireCallsThisSecond > 0 || spawnCountThisSecond > 0))
            {
                Debug.Log($"LocalGameAuthority telemetry: ProcessFire called {processFireCallsThisSecond}x, spawns={spawnCountThisSecond}, active={projectileWorld.ActiveCount}, acc={fireAccumulator:0.000}, fireHeld={pendingFireHeld}, hpfCount={hardpoints?.HardpointCount ?? 0}, failReason={lastSpawnFailureReason ?? "none"}");
                processFireCallsThisSecond = 0;
                spawnCountThisSecond = 0;
                lastSpawnFailureReason = null;
                nextTelemetryTime = Time.unscaledTime + 1f;
            }
        }

        void ProcessFire(float deltaTime)
        {
            if (!pendingFireHeld)
            {
                fireAccumulator = 0f;
                hasPendingFire = false;
                return;
            }

            float fireRate = weaponDefinition != null ? weaponDefinition.fireRatePerSecond : 20f;
            float interval = 1f / Mathf.Max(0.1f, fireRate);
            if (fireAccumulator < 0.01f)
            {
                fireAccumulator = interval;
            }

            fireAccumulator += deltaTime;
            int shots = 0;
            while (fireAccumulator >= interval && shots < 5)
            {
                if (!TrySpawnProjectile())
                {
                    break;
                }
                fireAccumulator -= interval;
                shots++;
            }

            processFireCallsThisSecond++;

            // Keep pendingFireHeld latched between ticks until client sends fireHeld=false
        }

        bool TrySpawnProjectile()
        {
            if (hardpoints == null)
            {
                lastSpawnFailureReason = "hardpoints null";
                return false;
            }

            if (hardpoints.HardpointCount == 0)
            {
                lastSpawnFailureReason = "HardpointCount zero";
                return false;
            }

            if (playerShipRoot == null)
            {
                lastSpawnFailureReason = "playerShipRoot null";
                return false;
            }

            WeaponHardpoint hardpoint = hardpoints.GetNextMuzzle();
            if (!hardpoint.IsValid)
            {
                lastSpawnFailureReason = "no valid hardpoint in array";
                return false;
            }

            int projectileId = nextProjectileId++;
            Vector3 worldPosition = hardpoint.WorldPosition;
            Vector3 worldForward = hardpoint.WorldForward;

            Debug.DrawRay(worldPosition, worldForward * 5f, Color.cyan, 1f);

            if (projectileId == 1)
            {
                Debug.Log($"LocalGameAuthority: first projectile from {hardpoint.node.name} pos={worldPosition} fwd={worldForward} (world)");
            }
            float speed = weaponDefinition != null ? weaponDefinition.projectileSpeedMetersPerSecond : 650f;
            float lifetime = weaponDefinition != null ? weaponDefinition.maxLifetimeSeconds : 1.25f;
            float maxRange = weaponDefinition != null ? weaponDefinition.maxRangeMeters : 800f;
            float damage = weaponDefinition != null ? weaponDefinition.damage : 1f;
            Vector3 velocity = worldForward * speed;

            int ownerEntityId = controlledShip != null && controlledShip.Id.IsValid
                ? controlledShip.Id.Value
                : 1;

            projectileWorld.SpawnProjectile(
                projectileId,
                ownerEntityId,
                worldPosition,
                velocity,
                lifetime,
                maxRange,
                damage);

            spawnCountThisSecond++;
            return true;
        }
    }
}
