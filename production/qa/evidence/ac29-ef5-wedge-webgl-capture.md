# AC-29 EF5 wedge — WebGL development capture (2026-10-02)

Storm Director AC-29 / X7-06. Claude captured; Codex analyses.

**Setup:** `builds/webgl-dev` (Development, 0.6.1 + Codex `StormVisualSpike` + Claude `StormCaptureHarness`),
served from `127.0.0.1`, Playwright Chromium 154, 1280×720 viewport (960×600 canvas, DPR 1), real GPU:
ANGLE AMD Radeon 890M (D3D11). Truck parked, no input, tornado held Mature 35 m ahead, spawner and
session timer stopped. Frame time = requestAnimationFrame deltas over 60 s. Draws = WebGL2 `draw*`
calls per frame (main + PiP + post + UI), counted by wrapping the prototype.

| Run | URL params | Frames | Median | p95 | p99 | Max | > 33.3 ms | > 50 ms | Draws/frame |
|---|---|---|---|---|---|---|---|---|---|
| EF5 wedge | `stormVisualSpike=wedge&stormHarness=hold` | 3,452 | 16.7 ms | **17.2 ms** | 33.5 ms | 50.4 ms | 90 | 2 | **436** |
| Baseline, EF0 card tornado | `stormHarness=hold` | 3,455 | 16.7 ms | 17.1 ms | 33.5 ms | 50.4 ms | 90 | 2 | 464 |
| Title screen (no tornado, no PiP) | `stormHarness=hold` | 174 (3 s) | — | — | — | — | — | — | 315 |

**Readings (observations, not a gate verdict):**
- p95 17.2 ms against the 33.3 ms budget. Both runs are vsync-locked at 60 Hz, so this shows headroom
  but not how much: GPU time per frame is below 16.7 ms on this machine.
- The wedge and baseline hitch profiles are identical (90 frames > 33.3 ms, 2 > 50 ms in each), so those
  hitches are not caused by the wedge.
- The wedge run issues **28 fewer** draws than the EF0 card tornado (436 vs 464), consistent with Codex's
  batched ring + two mesh batches replacing individual dust/debris cards. Title-screen 315 is not a
  like-for-like empty sky (no PiP camera), so "extra draws over empty sky" is not measured cleanly.
- Particle count not measured independently (Codex's structural count: 1,200 debris + 500 gusts).
- **Not measured:** wedge pixel overdraw (≤ 4 layers) — needs a GPU capture (RenderDoc / Unity
  Rendering Debugger overdraw view), not a browser timer. Not measured on Steam Deck.
- Framing: at 35 m the wedge crown is cut off by the top of the frame (see screenshot).

**Evidence:** `ac29-ef5-wedge-webgl-dev.png`, `ac29-baseline-ef0-webgl-dev.png`.
