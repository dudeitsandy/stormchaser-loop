# Pickup — hero truck build brief (S7-07, G2)

Default archetype, ★★★ Speed / ★★★ Armor / ★★ Trick (`vision-1.0.md`). Built by a headless Blender
script; shaded in Unity with the existing toon materials and outline pass (ADR-0003).

## References in this folder
| File | Take from it |
|---|---|
| `2024_Toyota_Tacoma_TRD_Pro_*`, `..._Trailhunter_*` | Base body: midsize cab + bed, blocky grille, flared arches, skid plate, roof rack |
| `Mobile_mesonet_2017_TORUS_Project.jpg` | **Signature piece:** instrument mast boomed forward over the hood (anemometer + vane), rack over the bed |
| `Private_stormchaser_group_Vortex-1*.jpg` | Chaser kit: bull bar, roof light pods, amber rotating beacon, whip antennas, tornado-warning decal |
| Fennec (sketchfab, Rocket League; reference only, never imported) | Attitude: oversized wheels, pumped arches, short overhangs, low/wide stance, detail as separate dark-trim shapes |

## Rules
- **Proportions:** Rocket League-exaggerated Tacoma. Wheels ≈ 1/3 body height, arches stick out past the body,
  short overhangs. Must fit the physics layout: body 1.0 × 0.5 × 1.85 m, axles at z +0.62 / −0.6, track
  ±0.52, wheel radius 0.22 (`WheelLayout.Pickup`). Model at that scale.
- **Detail is shape, not texture.** Chunky separate pieces (bumper, grille block, flares, rack, mast); no
  bevels smaller than ~3 cm at this scale, no PBR, no textures (a decal sheet may come later).
- **Material slots** (mapped to toon materials in Unity): `Body` (red), `Trim` (cream), `Dark` (bumpers,
  grille, flares, rack, tyres' sidewall), `Glass`, `Light` (headlights, light bar, beacon emissive).
- **Wheels are separate objects** named `Wheel_FL/FR/RL/RR`, pivots at the hub, so the vehicle can spin them.
- **Budget:** ≈ 3–5k triangles total, 5 materials, WebGL-friendly.
- **Must read at chase-cam distance** (~9 m) as a storm-chaser pickup: mast + light bar + rack silhouette.
- Our own design: no real logos, no copying a specific real chaser vehicle.

## G2 gate
Side-by-side capture vs the current cube truck in ArtTest at gameplay distance; Andy picks. Cube truck stays
as fallback until then.
