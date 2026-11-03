# Hunter IA - Parcial 2 (Unity)

Extends Parcial 1 (Boids + Hunter FSM) with wolves, graph navigation, A* and FoV/LoS perception. Map: Variante A (ring).
The GitHub project https://github.com/LOLTRAX7/Parcial-2-IA is READ-ONLY context. Never copy it verbatim; reimplement.

## Hard rules (professor constraints)

- NO NavMesh or any automatic navigation system. A* must be implemented inside this project.
- A* runs on our own graph (nodes + connections). No predefined routes replacing A*.
- NO `visited` collection/flag in A*. Use the standard closed/came-from structures only if strictly needed.
- NO `HeuristicMethod` selector. A single fixed heuristic (Euclidean/Manhattan). Exception only for huge maps.
- Big map -> more than one graph. Ours is not big: one shared graph, wolves exclude safe-zone nodes.
- Do NOT use waypoints-as-grid or a grid. Graph nodes only. Wolf patrol waypoints are graph nodes.
- Move with A* ONLY when the target is not in direct line of sight. If visible, use steering (Seek/Arrive/Pursuit).
- Last known position: wolves MUST reach it via A*, never direct movement.
- Wolves must NOT hold a global reference to the Hunter. They only know what perception/alert gives them.
- Hunter perception queries are throttled (interval timer), never every frame.
- Optimize: no per-frame allocations in A*/perception, use a priority queue (heap), cache paths, recompute only on target change.
- Hunter MUST use a FSM; transitions are decided inside states, not by an external controller.
- State priority: Dead > Flee > Exit > Gather > Attack > Search.
- Keep Parcial 1 systems working (Boids flocking, Hunter attack/gather logic).

## Conventions

- Code, comments, identifiers, UI in English. Conventional commits, no AI attribution.
- Gizmos required: Boid radii, Hunter radii (Vision/Melee/Range), wolf FoV, A* path, last known position.
