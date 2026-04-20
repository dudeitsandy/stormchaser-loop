# ADR-0002 — PiP Camera via URP RenderTexture

**Status:** Research / Stub (implementation Sprint 3+)
**Date:** 2026-04-19

## Context

The photography mechanic requires a Picture-in-Picture (PiP) camera window the player
aims while driving. The main camera shows the 1P driving view; the PiP camera shows
the tornado framing for scoring.

## Approach: Second Camera → RenderTexture → UI RawImage

URP supports multiple cameras rendering to RenderTextures. The PiP camera renders
its output to a RenderTexture asset, which is then displayed on a UI RawImage element
overlaid on the HUD.

### Setup Steps (for implementation sprint)

1. **Create RenderTexture asset** — `Assets/Create/Rendering/Render Texture`
   - Size: 320×240 (adjustable — keep aspect ratio matching PiP window)
   - Depth Buffer: 16-bit
   - Color Format: DefaultHDR (matches URP)

2. **Add PiP Camera GameObject** — second Camera in scene
   - Output Texture: assign the RenderTexture asset
   - In URP Camera component: set **Render Type** to `Overlay` or `Base`
   - Cull Mask: same as Main Camera (renders full scene)

3. **Camera Stack (URP)** — URP requires explicit camera stacking
   - On Main Camera's URP component: add PiP Camera to the **Camera Stack**
   - Render Order: PiP renders first, Main Camera composites on top
   - OR: keep PiP as a standalone Base camera writing to RenderTexture (simpler)

4. **UI RawImage** — Canvas → RawImage component
   - Assign RenderTexture to the Texture slot
   - Position/size the PiP window in the HUD (bottom-right corner per design)

5. **Aim control** — PiP Camera transform driven by right stick input (AimCamera action)
   - Rotate around a pivot point (tornado position or fixed offset from truck)
   - L1 (AimHold) enables rotation input — released = camera snaps back or stays

## Performance Notes

- Second camera = second render pass — budget impact on Steam Deck (30fps target)
- Mitigation: reduce PiP RenderTexture resolution (160×120 acceptable for "photo" aesthetic)
- Mitigation: PiP camera uses lower quality settings (fewer shadow cascades, simpler culling)
- R02 in risk register tracks this — validate GPU cost in Sprint 3 before committing to approach

## Alternatives Considered

- **Single camera + render region**: URP does not natively support rendering a sub-region
  to a separate texture in one pass — requires custom Renderer Feature, higher complexity
- **Screenshot API**: `ScreenCapture.CaptureScreenshotAsTexture()` — captures full screen,
  not suitable for PiP framing
- **Cinemachine Virtual Camera**: good for aim smoothing but still needs a second Base camera
  and RenderTexture — complementary, not a replacement

## Decision

Proceed with **second Base Camera → RenderTexture → UI RawImage**. Simplest URP-native
approach. Validate performance cost in Sprint 3 before adding Cinemachine smoothing.

## References

- `docs/engine-reference/unity/modules/rendering.md` — URP camera data patterns
- `docs/engine-reference/unity/plugins/cinemachine.md` — aim smoothing option
- Risk R02: `production/risk-register/risks.md`
