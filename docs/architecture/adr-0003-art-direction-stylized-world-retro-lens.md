# ADR-0003 — Art Direction: Stylized World, Retro Lens

**Status:** Accepted (direction) — pending visual test before full production
**Date:** 2026-10-01
**Deciders:** Andy Styx

## Context

The 0.4 prototype ships with a full-screen retro stack (CRT scanlines, film grain, chromatic
aberration, teal-orange LUT) over a world built entirely from Unity primitives and code-generated
meshes (cube truck, cone tornado, sphere trees, flat plane). The result reads as "unfinished
tech demo" rather than "intentional retro."

Andy wants a modern stylized look in the spirit of Fortnite: cel shading, cartoony realism, and
2D elements living in 3D space. The existing identity (vision-1.0, ADR-0001, personal aesthetic)
is 90s lo-fi neo-nostalgia. A full swap would trade away that identity; keeping the current
full-screen filter keeps the "unfinished" read.

Key fact: there is no art to throw away. No gameplay system depends on visuals. This is a choice
of style for art that does not exist yet.

## Decision

Split the look by layer, using the photography mechanic as the seam:

1. **The world is modern stylized.** Cel/toon lighting (banded diffuse, rim light), outlines,
   saturated readable palette, gradient sky. 2D hand-drawn VFX cards in 3D space carry the energy:
   debris, dust puffs, wind streaks, speed lines, and a layered card-based tornado.
2. **The player's camera is the retro lens.** The PiP viewfinder, captured photos, and the
   results screen look like 90s camcorder / disposable-camera output: scanlines, date stamp, grain,
   REC dot, slight chroma bleed. The CRT treatment moves from a global post-process to a diegetic
   effect — it is how the character sees, not a filter on the game.

Tagline for the look: *the world is a cartoon, your footage is from 1996.*

## Consequences

**Positive**
- Keeps the neo-nostalgia identity and gives the photo mechanic a signature output.
- Stylized world supports Kinetic Chaos (readable motion, juicy impacts) far better than primitives
  under heavy grain.
- The itch cover (`production/marketing/itch/cover_630x500.png`) already matches: camcorder frame
  over a dramatic storm.
- Toon shading is cheap; fine for the Steam Deck 30 FPS and WebGL targets.

**Negative / costs**
- 3D models are the open-ended cost; AI agents are weak at authoring them. Sources to evaluate:
  stylized asset packs, AI mesh generation (Meshy/Tripo), commission, or Andy in Blender.
- Outlines need a custom render pass on the Render Graph API (mandatory in Unity 6.3+), more
  involved than pre-6 tutorials suggest.
- Global post-FX volume gets reworked: CRT/grain/chroma move to the viewfinder RenderTexture and
  photo/results presentation; world grade becomes saturated rather than teal-orange.

**Estimate:** shaders + VFX + UI ≈ 1–2 weeks at part-time pace (Claude + Codex). Models: depends
on source.

## Sequencing

Do this *with* the Vehicle Feel pass and large-scale destructible terrain (sprint-05-ship.md
backlog), not before them and not after. Those efforts rebuild world assets anyway — choose the
style first, build each asset once.

**Gate before production — visual test scene:**
1. One cel-shaded truck (placeholder model acceptable) with outlines.
2. One card-based tornado (layered scrolling cards + debris sprites).
3. Camcorder treatment on the viewfinder only; world post-FX without CRT.
4. Side-by-side capture vs. the 0.4 look; Andy approves or adjusts before asset production starts.

## Alternatives Considered

- **Full swap to modern stylized (drop retro entirely):** loses identity; rejected.
- **Keep full-screen retro, add real models:** fixes "unfinished" but keeps grain/scanlines over
  fast motion, which fights readability and Kinetic Chaos; rejected.
- **Pixelated/low-res 3D (PS1 style):** strong retro, but narrows the audience and clashes with the
  Fortnite-style energy Andy wants; rejected.

## References
- `design/vision/vision-1.0.md` — Visual Style (superseded in part by this ADR)
- `docs/architecture/adr-0001-phaser-to-unity-3d-pivot.md`
- `docs/architecture/adr-0002-pip-camera-rendertexture.md` — viewfinder becomes the retro lens
