# AGENTS.md — Shared rules for Codex + Claude in this repo

Two coding agents (Codex and Claude Code) are working in this working tree at the same time.
Read this before touching anything. Current plan: `production/sprints/sprint-05-ship.md`.

## Project
- Unity project: `unity/StormChaserLoop3D/` — Unity **6000.6.0f1**, URP, C#, new Input System,
  UI Toolkit for runtime UI, Cinemachine 6.6 (`Unity.Cinemachine` namespace).
- The repo root (`src/`, `index.html`, Vite) is the old Phaser 2D game. Do not modify it.
- Game code lives in `Assets/Scripts/` and compiles into the `StormChaser` asmdef.
  Tests: `Assets/Tests/` (`StormChaser.Tests` asmdef, EditMode NUnit).

## Lane ownership (hard rule)
Only edit files in your lane. If you need something from the other lane, write it under
"Requests" below instead of editing their file.

**Codex owns:**
- `Assets/Scripts/Presentation/**` (photo feedback, PiP viewfinder, off-screen indicator, audio)
- `Assets/Scripts/Environment/**` (props, scatter)
- `Assets/Tests/Presentation/**`, `Assets/Tests/Environment/**`

**Claude owns:** everything else under `Assets/` — notably `Scripts/Core`, `Scripts/Session`,
`Scripts/Vehicle`, `Scripts/Tornado`, `Scripts/Disaster`, `Scripts/Photo`, `Scripts/Scoring`,
`Scripts/UI/HudController.cs`, `Scripts/Camera`, `Assets/Editor`, all `.unity` scenes, prefabs,
`ProjectSettings/`, `Packages/`.

## Never
- Edit `.unity`, `.prefab`, `ProjectSettings/*`, or `Packages/*` from Codex. Scene YAML merges
  are destructive. Codex features install themselves at runtime (pattern below).
- `git add -A` / `git add .` / `git commit -a`. Stage only your own paths.
- Commit unless the user asked you to.
- Run Unity while another Unity process is open on this project (project lock). Check first:
  `tasklist | findstr /i Unity.exe`. If one is running, wait.

## Integration contract
`Assets/Scripts/Core/GameEvents.cs` is the only coupling between lanes. Gameplay raises events;
presentation listens. Presentation must never call `GameEvents.Raise*`.

- `GameEvents.RunStarted` — run begins
- `GameEvents.PhotoTaken(PhotoResult)` — score, aim, distance, quality, `ShotTier`, subject, position
- `GameEvents.PhotoMissed` — shutter pressed with nothing in range
- `GameEvents.PlayerDamaged(int current, int max)`
- `GameEvents.RunEnded(RunSummary)`

Other read-only things presentation can query:
- `DisasterEntity.Active` — live disasters (no `FindObjectsByType` per frame)
- `PlayerVehicle` (find once in `Start`) — `CurrentSpeed`, `MaxSpeed`, `InputEnabled`
- `VehicleHealth` — `CurrentHealth`, `MaxHealth`, `IsInvulnerable`
- `TornadoController` (a `DisasterEntity`) — `EFRating`, `Intensity` (0–1 lifecycle), `ConeScale`, `DamageRadius`
- `RunManager.Current` — `Title` / `Running` / `Ending` / `Results`. During results `Time.timeScale == 0`; use unscaled time for UI animation.
- `Camera.main` — the gameplay camera (Cinemachine brain)

## Self-installing presentation pattern (Codex)
Each Codex feature is a MonoBehaviour that builds its own GameObjects/UI in code and installs
itself after scene load, so no scene edits are needed:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
private static void Install()
{
    if (Object.FindAnyObjectByType<PlayerVehicle>() == null) return; // only in gameplay scenes
    new GameObject(nameof(PhotoFeedback)).AddComponent<PhotoFeedback>();
}
```

- Runtime UI: add a `UIDocument` to an *inactive* GameObject, assign the shared panel
  `doc.panelSettings = Resources.Load<PanelSettings>("UI/PanelSettings");` and `doc.sortingOrder`,
  then activate it and build elements in code under `doc.rootVisualElement`
  (see `Scripts/Core/RunScreens.cs` for the pattern). Sorting so layers stack correctly:
  HUD = 0, Codex overlays = 10–50, Claude's title/results screens = 100.
  Lane-owned assets (if any) go in `Assets/Resources/Presentation/`.
- Audio: synthesize `AudioClip`s in code (`AudioClip.Create`) — no asset imports needed.
- Values that designers would tune go in `[SerializeField]` fields with sane defaults.

## Coding conventions
PascalCase types/methods/public members, `_camelCase` private fields, doc comments on public
APIs, cache references in `Awake`/`Start` (never `Find*` in `Update`), new Input System only,
URP Render Graph only (no Compatibility Mode).

## Verifying (headless)
```
"C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe" -batchmode -projectPath <abs path to unity/StormChaserLoop3D> -runTests -testPlatform EditMode -testResults <out.xml> -logFile <out.log>
```
Grep the log for `error CS`. Compile errors in one lane block both, so fix yours fast.

## Status board
Update your own line when you start/finish a task.

| Task | Owner | Status |
|------|-------|--------|
| C1 Run flow (title/results/retry/best) | Claude | code done, verifying |
| C2 Vehicle damage + wreck | Claude | code done, verifying |
| C3 Tornado behavior + EF roster | Claude | code done, verifying |
| C4 HUD + scene wiring + bounds | Claude | code done, verifying |
| C5 Build script + itch push | Claude | script written; butler not installed |
| X1 Photo feedback (flash/popup/tier) | Codex | implementing |
| X2 PiP viewfinder | Codex | implementing |
| X3 Off-screen indicator | Codex | implementing |
| X4 Procedural audio | Codex | implementing |
| X5 Environment props | Codex | implementing |

## Requests
(Agent → other agent. Append, don't edit the other's entries.)
