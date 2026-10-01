# Sprint 6 — Look & Feel Gate (2026-10-02 → 2026-10-15)

## Goal
Answer the two questions that decide every asset we build next, with evidence instead of opinion:
1. **Does ADR-0003 (stylized world, retro lens) look right?** → visual test scene, side-by-side vs 0.4.
2. **What do the vehicle and the terrain need to be?** → designed together (they constrain each other
   and the art), so the Sprint 7+ build doesn't get done twice.

Nothing ships to players this sprint unless the test looks good enough to swap into 0.5.

## Lanes

| ID | Owner | Task | Acceptance |
|----|-------|------|------------|
| C1 | Claude | **Toon lit shader** (URP, Shader Graph custom lighting): 2–3 band diffuse, rim light, shadow tint, per-material base color. Works on WebGL + PC quality level. | Truck/props/ground use it in `Scenes/ArtTest.unity`; no WebGL GL errors |
| C2 | Claude | **Outline pass**: Render Graph renderer feature (depth/normal edge detect) on PC renderer; toggle per quality level | Clean outlines at 1080p, ≤1.5 ms on dev GPU |
| C3 | Claude | **ArtTest scene + world grade**: gradient sky, saturated grade, CRT/grain/chroma removed from world volume; kitbashed placeholder truck (cab, bed, wheels) | Side-by-side captures vs 0.4 in `production/marketing/art-test/` |
| C4 | Claude | **Vehicle Feel GDD** (`design/gdd/vehicle-feel.md`): Rocket League target — weight transfer, powerslide/handbrake, suspension, air control, impact model, wind as body force | 8-section GDD, reviewed by Andy |
| C5 | Claude | **ADR-0004 Destructible terrain** (design only): scale, chunking, what breaks, physics budget, WebGL/Deck constraints | ADR drafted, reviewed by Andy |
| X1 | Codex | **Camcorder lens** on the PiP viewfinder + photo presentation: scanlines, grain, chroma bleed, REC dot, date/time stamp, slight barrel — applied to the viewfinder RenderTexture only | World has no CRT; viewfinder does; costs ≤1 ms |
| X2 | Codex | **Card-based tornado visual**: layered scrolling/rotating cards (hand-drawn-style funnel bands), debris sprite cards orbiting, dust skirt at base; scales with `TornadoController.Intensity` / `ConeScale` | Replaces cone mesh visually in ArtTest; Claude wires to prefab |
| X3 | Codex | **Wind VFX**: 2D streak/dust cards spawned around the truck scaled by `PlayerVehicle.CurrentWind`; flying debris near funnels | Wind risk is *visible* (playtest note 2026-10-01) |

Ownership is unchanged (AGENTS.md): shaders, renderer assets, scenes, prefabs = Claude;
`Scripts/Presentation/**` = Codex. Codex visuals that must live on a prefab: Codex writes the
component, Claude adds it to the prefab.

## Open decision (Andy)
**3D model source** for production (not needed for the test — placeholders are fine):
asset pack (stylized), AI mesh generation (Meshy/Tripo), commission, or Andy in Blender.
Decide after the art test approves the direction.

## Gate
Andy reviews `production/marketing/art-test/` side-by-sides + plays ArtTest. Approve → Sprint 7
builds Vehicle Feel + terrain + art in that style. Adjust → one more iteration on C1–C3/X1–X3.
