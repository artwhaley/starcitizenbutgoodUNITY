# FlightModel Future MMO Architecture Roadmap — Unity Version — Level 0

## Purpose

This document describes the broad architecture needed to grow the current Unity `FlightModel` demonstrator into a true persistent online space MMO.

This is future work.

The current project should still focus on the flight-feel demonstrator first.

The goal here is to keep the long-term architecture visible so early Unity choices do not block a future MMO-scale version.

---

## Important Correction

The project has switched from Unreal to Unity because Unity is currently working much better for development velocity and engine learning.

This roadmap assumes:

```text
Unity client
Unity C# simulation core
Unity Dedicated Server build for first real-time zone servers
C# backend services where practical
PostgreSQL for durable economy/inventory truth
```

Unreal-specific assumptions from the previous roadmap should be discarded.

---

## Core Product Vision

A focused persistent online space game with:

- Newtonian 6DOF ship flight
- flight assist modes layered on physical simulation
- server-authoritative combat
- space-only play to avoid planets/biomes/atmospheric scope
- stations as economic/social hubs
- FTL travel as diegetic loading/zone transfer
- persistent ships, cargo, wallets, inventory, crafting, markets, corporations, and reputation
- eventual multi-zone/multi-shard deployment

The game should not start by promising a seamless galaxy-scale MMO.

It should grow from:

```text
one fun ship
-> one multiplayer combat zone
-> one persistent station/belt loop
-> multiple zones with FTL transfer
-> regional shards
-> larger MMO platform
```

---

## Strategic Principle

Do not build “the MMO” first.

Build a ladder of increasingly real prototypes:

```text
Flight feel
Networked flight
Persistent station loop
Multi-zone travel
Small public alpha
Shard fleet
MMO-scale live service
```

Each stage should be independently playable and testable.

---

## Current Demonstrator Exit Criteria

Before starting MMO architecture implementation, the Unity flight demonstrator should prove:

- Assist Off is interesting, not just chaotic
- Attitude Assist makes rotation manageable
- Coupled/Stabilized Assist makes the ship approachable
- joystick/HOTAS/HOSAS input works
- aiming and firing while drifting is fun enough to continue
- code path is input-command driven
- simulation state is separate from visual presentation
- there is a clear path to server-authoritative simulation

If these are not true, do not build the MMO yet.

---

## Long-Term Architecture Overview

The eventual system should look roughly like this:

```text
Unity Client
  -> Gateway / Auth
  -> Zone Assignment / Session Router
  -> Unity Dedicated Zone Server Fleet
  -> Persistence Services
  -> Economy / Inventory / Social / Market Services
  -> PostgreSQL / Redis / Object Storage / Logs
```

The Unity project should be organized so the flight simulation can run in three places:

```text
1. local client demonstrator
2. Unity dedicated/headless zone server
3. possible future standalone C# simulation server
```

---

## Unity-Specific Architecture Direction

### First Real-Time Server

The first real-time server should probably be a **Unity Dedicated Server build** of the same project.

Unity’s Dedicated Server target is intended for builds that run as servers instead of graphical clients. Use that first before considering a completely custom standalone simulation server.

Early shape:

```text
Unity Client Build
Unity Dedicated Server Build
Shared C# simulation assembly
C# backend service
PostgreSQL
```

---

## Major Runtime Pieces

### 1. Unity Client

The client handles:

- rendering
- input
- camera/HUD
- cockpit/presentation later
- prediction/interpolation
- audio/VFX
- local UI
- station menus
- client-side presentation of ships, weapons, markets, chat, and inventory

The client is not trusted for durable state.

The client may predict movement, but the server owns truth.

---

### 2. Unity Real-Time Zone Server

The zone server handles one active gameplay area.

Examples:

- station exterior
- asteroid belt
- combat pocket
- jump gate area
- mission zone
- local sector instance

Responsibilities:

- receive player input commands
- run authoritative ship simulation
- simulate projectiles/weapons
- validate hits
- manage local NPCs
- manage local resource nodes
- enforce docking/undocking state
- manage interest/relevancy
- send state snapshots to clients
- emit durable events to backend

The zone server should not own global economy truth.

It should call backend services for durable transactions.

---

### 3. Gateway / Session Router

The gateway/session router knows:

- who is logged in
- which character is active
- which zone server owns the player
- whether a transfer is in progress
- where to send the client next
- how to handle disconnect/reconnect

FTL and docking transitions use this layer.

Example:

```text
Player activates FTL
-> source zone snapshots transferable ship state
-> router allocates target zone
-> backend confirms travel/event state
-> client shows jump/warp animation
-> target zone spawns player
-> client transfers connection
```

The first version can be very simple.

Do not start with a complex distributed orchestrator.

---

### 4. Persistence Backend

This owns durable game truth:

- accounts
- characters
- ships
- ship fittings
- cargo
- inventory
- wallets
- crafting jobs
- market orders
- station storage
- corporations
- permissions
- reputation
- mission progress
- insurance/repair state
- audit logs

Early implementation should be a modular monolith.

Because the game is already C# in Unity, a C# backend such as ASP.NET Core is a practical first choice.

Do not start with microservices unless load or team structure requires it.

A clean monolith with clear internal modules is better than premature distributed complexity.

---

### 5. Databases and Storage

Likely early stack:

```text
PostgreSQL
Redis optional
object storage for logs/build artifacts/assets
append-only event/audit logs
```

PostgreSQL should own transactional economy/inventory truth.

Redis can help with ephemeral session/cache state, but should not be the only durable source of important economy data.

---

### 6. Chat / Social / Corporation Systems

Eventually separate or semi-separate services:

- local chat
- global/system chat
- party chat
- corporation chat
- friends
- blocks
- invites
- corporation membership
- roles/permissions
- shared storage
- corp wallet
- mail/notifications

These systems are not part of the flight demonstrator.

They become important before public alpha because social retention is central to MMO behavior.

---

### 7. Economy / Market / Crafting

These systems need special discipline.

They require:

- transactional item movement
- audit logs
- anti-duplication guarantees
- idempotent event handling
- market order locking
- wallet transaction history
- rollback/admin tools
- fraud/dupe detection
- economic telemetry

Every item movement should be explainable after the fact.

Example transaction:

```text
Buy item
-> lock buyer wallet
-> lock sell order
-> verify item quantity
-> transfer currency
-> transfer item
-> write audit event
-> commit
```

Economy bugs kill MMO credibility quickly.

---

## Unity Project Organization

The Unity project should not become a pile of MonoBehaviours that directly mutate transforms.

Use a layered structure:

```text
Assets/
  Scripts/
    Simulation/
      ShipInputCommand.cs
      ShipState.cs
      ShipTuning.cs
      FlightAssistMode.cs
      ShipFlightSimulator.cs
      ReferenceFrameId.cs
    Presentation/
      ShipView.cs
      ShipCameraController.cs
      FlightHudPresenter.cs
    Input/
      PilotInputReader.cs
      JoystickInputMapper.cs
      InputCalibration.cs
    Networking/
      later
    GameRules/
      later
```

Core rule:

```text
Simulation code does not know about cameras, UI, VFX, or Unity scene hierarchy.
Presentation code reads simulation state and applies it to Unity transforms.
```

---

## Real-Time Simulation Model

### Ship Simulation

Ship movement should stay input-command driven:

```text
ShipInputCommand
  -> ShipFlightSimulator.Step()
  -> ShipState
  -> rendered transform / network snapshot
```

The long-term server should be able to run:

```text
initial state + ordered input commands = resulting state
```

The client can predict locally, but the zone server decides truth.

---

### Flight Model

The underlying flight model should remain Newtonian:

- mass
- thrust
- torque
- inertia
- linear velocity
- angular velocity
- frame-relative motion

Flight assist modes are control systems layered on top:

- Assist Off
- Attitude Assist
- Coupled/Stabilized Assist
- Match Velocity
- Docking Assist
- Emergency Brake
- damaged/partial assist behavior

Assist should apply simulated corrective force/torque where practical.

---

### Reference Frames

The eventual physical model should support entities existing inside frames.

Basic idea:

```text
EntityState
  EntityId
  FrameId
  LocalPosition
  LocalRotation
  LocalVelocity
  LocalAngularVelocity
```

Frames:

```text
ZoneFrame
StationFrame
ShipInteriorFrame
EVAFrame
DockingFrame
```

This supports:

- walking inside a moving ship
- leaving a ship onto a station
- ship flying away after player disembarks
- EVA
- docking
- future large-space coordinate management
- FTL/zone transfer

Do not build this fully until after flight feel is proven, but avoid coding against it.

For now, the demo can use:

```text
LocalTestFrame
```

---

## Unity Physics Guidance

For the ship flight model, do not make Rigidbody the deep truth by default.

Recommended split:

```text
Custom C# simulation:
  - thrust
  - torque
  - velocity
  - angular velocity
  - assist modes
  - frame IDs
  - server-authoritative state

Unity physics:
  - collision queries
  - raycasts
  - trigger volumes
  - visual debris
  - local character controller later
```

Using Unity Rigidbody for early feel testing is acceptable if it speeds iteration, but the long-term authoritative shape should be explicit simulation state driven by input commands.

---

## Networking Model

### Authority

The server owns:

- position
- velocity
- rotation
- weapon validation
- damage
- ship destruction
- cargo transfer
- mining/salvage results
- docking state
- durable rewards

The client owns:

- input
- camera
- local prediction
- UI
- presentation effects

The client never tells the server:

```text
I hit this player for 500 damage.
I created this item.
I earned this money.
```

The client says:

```text
I fired weapon X at tick T with input state Y.
```

The server decides the result.

---

### Unity Networking Options

There are two likely Unity networking paths:

#### Option A — Netcode for GameObjects

Best for:

- faster learning
- object-oriented Unity workflow
- small multiplayer prototypes
- early 2–8 player tests
- easier mental model

Possible role:

```text
early LAN multiplayer
first dedicated server proof
station/persistence slice
small closed tests
```

#### Option B — Netcode for Entities

Best for:

- high-performance server-authoritative multiplayer
- client prediction
- interpolation
- lag compensation
- larger combat zones
- ECS/DOTS style simulation

Possible role:

```text
serious 32-player combat zone and beyond
```

Do not switch to Entities just because it sounds more scalable.

Use the simplest path that proves the next risk.

A practical roadmap is:

```text
single-player custom C# sim
-> fake client/server split in process
-> Netcode for GameObjects or low-level transport for first LAN test
-> evaluate Netcode for Entities when scaling pressure is real
```

---

### Replication and Interest Management

The server cannot send every object to every player at high frequency.

Interest rules should account for:

- distance
- sensor range
- line of sight if relevant
- party/corp visibility
- target locks
- weapon/projectile relevance
- station/local-zone relevance

Space helps because distance and sensors are natural relevance boundaries.

---

### Snapshots

The server sends snapshots such as:

```text
entity id
frame id
position
rotation
linear velocity
angular velocity
health/shield state
important animation/effect state
```

Snapshot frequency can vary:

- nearby combat entities: high rate
- distant ships: lower rate
- far sensor blips: very low rate
- station market data: not part of real-time replication

---

## Zone and Shard Model

### Zone

A zone is one authoritative real-time simulation area.

Examples:

```text
Belt_Alpha_01
StationExterior_OdinPrime
MissionPocket_93821
JumpGate_SolToVega
```

A zone can be static or dynamically spawned.

---

### Shard

A shard is a coherent copy/partition of the persistent universe or region.

Early game can use one shard.

Later options:

```text
Regional shards
Instanced overflow zones
Single economy with multiple combat instances
Separate test/live shards
```

A “true MMO” does not require every player to be in one process.

It requires persistence, shared world rules, social continuity, and believable zone transitions.

---

### FTL as Transfer

FTL is the architecture gift.

It can hide:

- loading
- server allocation
- asset streaming
- travel validation
- state handoff
- database update
- reconnect/connection migration

Treat FTL as a first-class zone-transfer system.

---

## Deployment Architecture

Early local development:

```text
Unity Editor
local Play Mode client
local server process or in-process fake server
local PostgreSQL later
local backend service later
```

First online test:

```text
Unity client build
one cloud VM
one Unity dedicated server build
one backend service
PostgreSQL
```

Later production-like:

```text
gateway services
backend service fleet
Unity zone server fleet
PostgreSQL primary/replica
Redis/cache
object storage
logging/metrics
CI/CD
```

Eventually, game-server orchestration can use:

```text
Agones on Kubernetes
custom VM-based allocator
managed game server hosting
hybrid approach
```

Do not commit too early.

Unity’s Multiplay/Game Server Hosting documentation remains useful conceptually, but direct support status and vendor continuity should be checked before depending on it for a new live service.

---

## Backend Service Boundaries

Start with a modular monolith:

```text
Auth Module
Character Module
Inventory Module
Wallet Module
Market Module
Crafting Module
Station Module
Corporation Module
Telemetry Module
Admin Module
```

Split into microservices only when there is pressure from:

- scale
- deployment independence
- team ownership
- security boundary
- performance isolation

The architecture should allow future splitting, but not require it on day one.

---

## Tooling Required for a Real MMO

A true MMO needs tools as much as gameplay code.

Future tools:

- admin dashboard
- player lookup
- inventory editor
- item transaction audit viewer
- economy dashboard
- market health dashboard
- zone status dashboard
- server allocation viewer
- ban/mute/moderation tools
- bug report capture
- crash reporting
- build deployment dashboard
- content data validators
- spawn table editor
- recipe/item editor
- station/economy editor

Do not underestimate tools.

Without tools, a live MMO becomes unmanageable.

---

## Observability

Before public tests, add:

- server logs
- structured event logs
- metrics
- crash reports
- latency tracking
- packet loss tracking
- zone CPU/tick time
- player count per zone
- database transaction timing
- economy event counts
- item/currency creation and destruction rates

The team must be able to answer:

```text
Why did this player lose cargo?
Where did this item come from?
Why did this zone lag?
Did a dupe happen?
Which server owned the player?
```

---

## Security and Abuse

Future systems need:

- server authority
- input validation
- rate limits
- anti-cheat strategy
- dupe prevention
- economy audit logs
- marketplace abuse detection
- chat moderation
- report/block/mute systems
- admin permissions
- secure service-to-service auth
- backups and restore drills

Do not trust the client.

Do not trust “rare bugs” in economy code.

---

## Content Roadmap

The game should grow content from a small number of strong loops.

### First Sector

- one station
- one asteroid belt
- one combat zone
- one mineable resource
- one salvage source
- one commodity
- one crafted ship component
- three ship hulls
- four weapons
- basic NPC drones

### First Economy Loop

```text
undock
-> mine/salvage/fight
-> return to station
-> sell/refine/craft
-> upgrade ship
-> undock again
```

### First Social Loop

```text
party up
-> run belt/combat loop together
-> share proceeds
-> compare fittings
-> plan next outing
```

### First World Expansion

- second station
- different resource prices
- FTL transfer
- transport/trade route
- danger along route
- localized market differences

---

## Development Phases

### Phase 0 — Unity Flight Demonstrator

Current project.

Proves:

- 6DOF flight feel
- assist modes
- joystick support
- aiming/firing basics
- clean input-command simulation shape

---

### Phase 1 — Fake Client/Server Split

Same Unity project, two conceptual worlds:

```text
ClientPresentationWorld
ServerSimulationWorld
```

Input crosses boundary.

Snapshots come back.

No network yet.

Purpose:

- force server-authoritative thinking
- prepare prediction/reconciliation
- avoid rewriting flight code later

---

### Phase 2 — LAN Dedicated Zone Server

First real Unity dedicated server build.

Target:

- 2 to 8 players
- one zone
- server-authoritative movement
- simple replication
- no persistence beyond session

Purpose:

- prove networked flight does not feel terrible

---

### Phase 3 — 32-Player Combat Zone

Target:

- 16 to 32 players
- basic projectiles or raycast weapons
- damage/death/respawn
- interest management
- bandwidth/tick profiling

Purpose:

- prove zone server shape

---

### Phase 4 — Persistence Slice

Add backend:

- account placeholder
- character
- ship selection
- fitting
- wallet
- cargo
- station inventory

Target loop:

```text
undock
-> collect resource
-> dock
-> sell/craft/upgrade
-> logout
-> return with state intact
```

---

### Phase 5 — Multi-Zone / FTL

Add second zone.

Implement:

- FTL request
- zone allocation
- source zone handoff
- target zone spawn
- client transfer
- durable travel event

Purpose:

- prove the MMO geography can work without literal seamlessness

---

### Phase 6 — Social + Economy Alpha

Add:

- party
- chat
- friends
- basic corporation/guild
- player market
- crafting queue
- audit logs
- admin tools

Purpose:

- turn combat prototype into persistent online game

---

### Phase 7 — Shard Fleet

Add:

- automated zone server allocation
- warm server pool
- monitoring
- deployment pipeline
- crash recovery
- reconnect recovery
- load testing

Purpose:

- move from “server” to “platform”

---

### Phase 8 — Public Alpha

Only after:

- flight is fun
- persistence is reliable
- zone transfer works
- economy transactions are audited
- moderation tools exist
- server can be monitored
- patching/deployment is repeatable

Public alpha is not a technology milestone.

It is an operations milestone.

---

## What Next-Generation LLMs Can Help With

Future stronger models can help with:

- ticket generation
- code scaffolding
- test generation
- refactors
- backend CRUD services
- admin tools
- data validators
- content tables
- economy simulations
- procedural mission templates
- NPC dialogue
- documentation
- build/deployment scripts
- log analysis
- balancing reports

But they should not be allowed to invent architecture unchecked.

Agent rule:

```text
An agent gets one ticket, one boundary, one acceptance test.
```

Do not give an agent the task:

```text
Build the MMO backend.
```

Give it tasks like:

```text
Implement wallet transaction table and atomic transfer function.
```

---

## Key Architecture Decisions to Preserve Now

Even during the Unity flight demonstrator, preserve these:

1. Input-command-driven movement
2. C# simulation core
3. Unity Transform as presentation, not deep truth
4. `FrameId` concept
5. flight assist as control layer over Newtonian physics
6. no client-trusted durable outcomes
7. generic class names
8. no speculative MMO code before flight is fun

---

## Biggest Risks

### Risk 1 — Scope Explosion

The project dies if it tries to build planets, interiors, economy, multiplayer, crafting, and server orchestration at the same time.

Mitigation:

```text
one playable vertical slice at a time
```

---

### Risk 2 — Bad Flight Feel

If flight is not fun, nothing else matters.

Mitigation:

```text
finish and tune the demonstrator before architecture expansion
```

---

### Risk 3 — Premature Microservices

Distributed systems will slow development if introduced too early.

Mitigation:

```text
modular monolith first
clear service boundaries later
```

---

### Risk 4 — Economy Bugs

Dupes and bad transactions can destroy trust.

Mitigation:

```text
transactional DB operations
audit logs
admin tools
automated tests
```

---

### Risk 5 — Network Feel

Server authority can feel bad without prediction/smoothing.

Mitigation:

```text
fake client/server split before real multiplayer
small LAN server test before public test
```

---

### Risk 6 — Operational Blindness

A live MMO cannot be managed without observability.

Mitigation:

```text
logs, metrics, dashboards, admin tools before public alpha
```

---

### Risk 7 — Unity Networking Path Churn

Unity has multiple networking paths, and picking the wrong one too early can waste time.

Mitigation:

```text
keep simulation independent from networking package
prove each networking step with the smallest viable test
```

---

## Recommended Near-Term Roadmap After Demonstrator

When the demonstrator feels promising, the next roadmap should be:

```text
A. clean C# flight simulation core
B. fake client/server split
C. LAN Unity dedicated server
D. 8-player combat test
E. 32-player combat test
F. station persistence slice
G. two-zone FTL transfer
H. first economy/crafting loop
I. small closed alpha
```

This is the path from “fun ship” to “believable MMO seed.”

---

## One-Sentence Architecture

A true version of this game is:

> A Unity-based, server-authoritative, zone-sharded space MMO where Unity dedicated zone servers run a clean C# Newtonian ship simulation and reference-frame model, while durable backend services own inventory, economy, crafting, social systems, and world persistence.

---

## Source Notes

These notes were written against current Unity documentation as of June 2026:

- Unity Dedicated Server builds are available as desktop platform subtargets optimized for server builds.
- Unity Netcode for GameObjects is Unity’s high-level GameObject-oriented networking library.
- Unity Netcode for Entities targets server-authoritative multiplayer with prediction, interpolation, and lag compensation for more performance-sensitive action games.
- Unity Multiplay/Game Server Hosting allocation docs remain useful conceptually, but direct support status should be checked before using it as the long-term production host.
