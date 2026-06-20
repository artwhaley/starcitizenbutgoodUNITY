using System.Collections.Generic;
using FlightModel.World;
using UnityEngine;

namespace FlightModel
{
    public class ShipFlightController : MonoBehaviour
    {
        [SerializeField] ShipCollisionProxy collisionProxy;

        ShipState state;
        ShipTuning tuning;
        ShipInputCommand lastAppliedThrusterCommand;
        readonly List<IShipControlRequestSource> externalRequestSources = new();

        ShipControlRequest lastPilotRequest;
        ShipControlRequest lastAssistRequest;
        ShipControlRequest lastBrakeRequest;
        ShipControlRequest lastMergedControlRequest;
        ShipThrusterOutput lastThrusterOutput;
        ShipCollisionHit lastCollisionHit;
        bool lastStepHadCollision;

        public ShipState State => state;
        public ShipInputCommand LastAppliedThrusterCommand => lastAppliedThrusterCommand;
        public ShipThrusterOutput LastThrusterOutput => lastThrusterOutput;
        public ShipControlRequest LastPilotRequest => lastPilotRequest;
        public ShipControlRequest LastAssistRequest => lastAssistRequest;
        public ShipControlRequest LastBrakeRequest => lastBrakeRequest;
        public ShipControlRequest LastMergedControlRequest => lastMergedControlRequest;
        public ShipCollisionHit LastCollisionHit => lastCollisionHit;
        public bool LastStepHadCollision => lastStepHadCollision;
        public ShipTuning Tuning
        {
            get => tuning;
            set => tuning = value;
        }

        void Awake()
        {
            if (collisionProxy == null)
            {
                collisionProxy = GetComponentInChildren<ShipCollisionProxy>(true);
            }
        }

        public void RegisterExternalRequestSource(IShipControlRequestSource source)
        {
            if (source == null || externalRequestSources.Contains(source))
            {
                return;
            }

            externalRequestSources.Add(source);
        }

        public void InitializeState(Vector3 position, Quaternion rotation)
        {
            state = new ShipState
            {
                position = position,
                rotation = rotation,
                linearVelocity = Vector3.zero,
                angularVelocityRadians = Vector3.zero,
                assistMode = FlightAssistMode.AssistOff,
                frameId = ReferenceFrameId.LocalZone,
                boostActive = false,
                fineControlActive = false,
                currentMassKg = tuning != null
                    ? tuning.dryMassKg + tuning.fuelCapacityKg + tuning.hypergolicCapacityKg
                    : 0f,
                remainingFuelKg = tuning != null ? tuning.fuelCapacityKg : 0f,
                remainingHypergolicKg = tuning != null ? tuning.hypergolicCapacityKg : 0f,
                appliedOutput = default
            };
        }

        public void SetAssistMode(FlightAssistMode mode)
        {
            state.assistMode = mode;
        }

        /// <summary>
        /// Replace the entire simulation state. Use for teleport-like overrides
        /// such as docking capture, docked-pose follow, and undock separation.
        /// </summary>
        public void OverwriteState(in ShipState nextState)
        {
            state = nextState;
        }

        public void Simulate(float deltaSeconds, in ShipInputCommand input)
        {
            lastStepHadCollision = false;
            lastCollisionHit = default;

            if (deltaSeconds <= 0f || tuning == null)
            {
                lastAppliedThrusterCommand = default;
                lastPilotRequest = default;
                lastAssistRequest = default;
                lastBrakeRequest = default;
                lastMergedControlRequest = default;
                return;
            }

            Vector3 previousPosition = state.position;
            Quaternion previousRotation = state.rotation;

            ShipSimulator.Step(
                ref state,
                tuning,
                externalRequestSources,
                deltaSeconds,
                input,
                out ShipSimulationTelemetry telemetry);

            if (collisionProxy != null && collisionProxy.IsReady)
            {
                lastStepHadCollision = ShipCollisionResolver.ResolveMovement(
                    ref state,
                    collisionProxy.BakedShapes,
                    collisionProxy.CollisionWorld,
                    collisionProxy.ObstacleMask,
                    previousPosition,
                    previousRotation,
                    collisionProxy.Restitution,
                    collisionProxy.TangentialDamping,
                    collisionProxy.SkinWidth,
                    collisionProxy.DepenetrationSkinWidth,
                    collisionProxy.MaxDepenetrationMetersPerStep,
                    collisionProxy.MaxDepenetrationIterations,
                    out lastCollisionHit,
                    out bool collisionDepenetrated,
                    out float collisionDepenetrationDistance);

                telemetry.collisionDepenetrated = collisionDepenetrated;
                telemetry.collisionDepenetrationDistance = collisionDepenetrationDistance;
            }

            telemetry.collisionResolved = lastStepHadCollision;
            telemetry.collisionHit = lastCollisionHit;

            lastPilotRequest = telemetry.pilotRequest;
            lastAssistRequest = telemetry.assistRequest;
            lastBrakeRequest = telemetry.brakeRequest;
            lastMergedControlRequest = telemetry.mergedControlRequest;
            lastThrusterOutput = telemetry.thrusterOutput;
            lastAppliedThrusterCommand = telemetry.appliedThrusterCommand;
        }
    }
}
