# Orchestrator Prompt

Use this prompt when handing the stack to an execution-focused agent.

---

You are working in `C:\Users\artwh\OneDrive\Documents\Unity_FlightModel`.

Execute the flight-model rework ticket stack in `Tickets/README.md`, in order, one ticket at a time.

## Objectives

- Replace the current simplified flight model with a ship-authoritative model built around dry-mass acceleration tuning, asymmetric axis authority, speed/angular caps, boost/fine-control modes, propellant use, and applied-output-driven VFX.
- Preserve the distinction between:
  - global hardware bindings
  - per-ship tuning
- Keep the U-wing playable throughout the migration.

## Required Working Rules

- Read the current code before editing each ticket.
- Use the existing project files and naming where practical.
- Do not reintroduce fake damping or direct velocity lerp as the basis of stabilization.
- All automatic stabilization must request counter-thrust through ship authority limits.
- Leave a clear input hook for future autodock/autopilot sources.
- Keep VFX logic driven by applied thruster output, not raw input alone.
- Keep ship-specific particle/prefab presentation abstractable from the shared solver.

## Execution Rules

For each ticket:

1. Read the ticket and the relevant current files.
2. Implement only that ticket’s scope.
3. Compile with:
   `dotnet build Assembly-CSharp.csproj --no-restore`
4. If the project still compiles, do a focused smoke test mentally against the ticket’s acceptance criteria and tighten obvious gaps.
5. Commit the ticket with a clear git message.
6. Then move to the next ticket.

## Guardrails

- Do not blend T07 tuning UI work into earlier solver tickets unless compile safety forces a tiny compatibility edit.
- Do not silently change player controls without updating `README.md`.
- Do not remove existing user changes outside the ticket scope.
- If a ticket exposes a missing prerequisite, make the smallest compatible adjustment and note it in the commit message or final report.

## Final Deliverable

When all tickets are complete, provide:

- a short implementation summary
- any important tradeoffs or follow-up recommendations
- confirmation of compile status
- the list of commits created

---
