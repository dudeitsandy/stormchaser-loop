# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 6.3 LTS (6000.3.x)
- **Language**: C#
- **Rendering**: Universal Render Pipeline (URP) with Render Graph (mandatory in 6.3+)
- **Physics**: Unity Physics (Rigidbody / CharacterController)

## Naming Conventions

- **Classes**: PascalCase (e.g., `PlayerController`)
- **Public fields/properties**: PascalCase (e.g., `MoveSpeed`)
- **Private fields**: _camelCase (e.g., `_moveSpeed`)
- **Methods**: PascalCase (e.g., `TakeDamage()`)
- **Events/UnityEvents**: PascalCase (e.g., `OnTornadoPhotographed`)
- **Files**: PascalCase matching class (e.g., `PlayerController.cs`)
- **Prefabs**: PascalCase matching root component (e.g., `TornadoVFX.prefab`)
- **Constants**: PascalCase or UPPER_SNAKE_CASE

## Performance Budgets

- **Target Framerate**: 60 FPS (PC), 30 FPS (Steam Deck target)
- **Frame Budget**: 16.6ms (PC), 33.3ms (Steam Deck)
- **Draw Calls**: [TO BE CONFIGURED]
- **Memory Ceiling**: [TO BE CONFIGURED]

## Testing

- **Framework**: NUnit (Unity Test Framework — built-in)
- **Minimum Coverage**: [TO BE CONFIGURED]
- **Required Tests**: Scoring formulas, photo quality thresholds, tornado EF scale math, terrain speed modifiers

## Forbidden Patterns

- Do NOT use `FindObjectOfType` in Update loops — cache references in Awake/Start
- Do NOT use URP Compatibility Mode — removed in Unity 6.3; use Render Graph API
- Do NOT use Legacy Input Manager — use the new Input System package

## Allowed Libraries / Addons

- [None configured yet — add as dependencies are approved]

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- [ADR-0001](../../docs/architecture/adr-0001-phaser-to-unity-3d-pivot.md) — Phaser 2D → Unity 6.3 URP 3D pivot; PiP photography system; Steam target
