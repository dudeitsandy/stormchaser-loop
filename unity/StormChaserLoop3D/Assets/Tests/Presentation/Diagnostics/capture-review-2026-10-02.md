# Codex capture review — 2026-10-02

## Verdict

**X7-06 remains open.** A 17.2 ms complete browser frame interval does not establish a ≤3 ms presentation work budget. Unity component CPU, per-camera native culling/render setup, UI work and forced maximum-wind coverage remain missing. No performance cuts are justified by the measured subset alone.

| Check | Decision | Evidence / limit |
|---|---|---|
| AC-29 60-second p95 frame interval ≤33.3 ms | Pass for reference-machine capture | Claude: 17.2 ms p95, 3,452 frames; comparable ordinary tornado 17.1 ms. This is rAF pacing, not an isolated GPU or component-CPU measurement. |
| AC-29 ≤40 additional color draws over empty sky | Pass for controlled draw-only comparison | Codex: 543 steady wedge frames at 435.989 mean / 436 max native draws; 294 draw-suppressed gameplay frames at 422 mean, including PiP, lens and UI. Difference 13.989 ≈14 draws. |
| AC-29 ≤2,000 live particles | Structural count passes; independently sampled runtime count absent | Fixed 1,200 debris +500 gust billboard mesh population; sub-vortices/ring/funnel are meshes, not additional particle pools. |
| Wedge ≤4 layers of pixel overdraw | Open | No pixel overdraw capture. Mesh batching reduces submissions, not overlapping fragments. |
| Far-field shader retained, fog-exempt, visible at 800 m | Technical daylight check passes | Shader compiled and rendered five main-camera draws, silhouettes disappear when those draws are suppressed. Dark-sky/maximum environmental darkening remains untested. |

Full AC-29 sign-off is conditional on its runtime particle-count convention being accepted; the separate ≤4-layer visual requirement is still open. X7-06 is not certified by these subchecks.

Claude evidence: `production/qa/evidence/ac29-ef5-wedge-webgl-capture.md`, unchanged by Codex. Similar hitch counts across its two trials are consistent with shared costs, but do not prove the wedge cannot contribute to a hitch. Neither vsync pacing nor a percentile alone measures spare GPU time.

## Codex setup and draw comparison

Existing development build `builds/webgl-dev`; WASM SHA256:
`AEBAA9FF383BE34F70B9ACB882AC9E8F9E274E16D71713DD68D9A0725526812A`.
Chrome 154 headless, hardware ANGLE/D3D11 AMD Radeon 890M, 960×600 canvas, PiP 320×240. Isolated hidden browser, existing build served unchanged. These captures predate the new boost/EngineLoad source edits.

Wedge query: `stormVisualSpike=wedge&stormHarness=hold`, stationary truck, 10 seconds sampled, followed by five seconds suppressing only near VFX card GL draws. Gameplay, native culling, main/PiP cameras, lens, post processing and UI continue. This is an **empty-sky GPU submission comparison, not an empty-scene Unity CPU baseline**. No native gameplay state is changed.

The all-frame mean is 436.212 versus 422, because startup includes a few ordinary funnel frames. The steady-state subset has four classified tornado-card draws (dust ring and debris across two cameras) plus ten dark-funnel/sub-vortex/gust draws, for fourteen VFX draws. Their total scene mean is 435.989. See `wedge-draw-summary-2026-10-02.json`, `wedge-draw-capture-2026-10-02.png` and `wedge-empty-sky-2026-10-02.png`. Pixel overdraw is not derived from these counts.

GPU+GL+audio-control measured subset: 0.640 ms mean /1.677 ms p95 for this short wedge capture. Browser wrapping and interval screenshots add overhead. This additive work subtotal is neither total presentation CPU time nor frame latency.

## Far field

Query `stormVisualSpike=far`, ten seconds, 607 active frames /121 GPU samples, zero disjoint samples. Far-field category: five draws per frame, GPU 0.0141 ms mean /0.0173 ms p95; GL submission 0.0120 ms mean. This tiny draw cost does not establish UI/native CPU cost or worst-case weather cost.

Submitted world transforms confirm the funnel base at `(0,4.5,792)`, relative to the initial camera `(0,4.5,-8)`: 800 m horizontally. Cloud/anvil centers reach y=26.5–48.5. Shader source in the captured compiled program has `_HorizonBlend`, no fog mix. All cards use the supplied `Doomsday/FarFieldStorm` shader; no missing-material fallback was used.

At 960×600 the funnel is small and near the top center. On/off images establish actual color contribution: pixel (540,45) changes from RGB (138,113,84) to (202,177,168); 1,325 of 4,575 pixels in the predicted far-field region change by >5 in at least one channel. This difference is visual evidence, not a pixel-overdraw measurement. See `far-field-800m-2026-10-02.png` versus `far-field-hidden-2026-10-02.png`, and the summary JSON. The comparison has 0.2 seconds between screenshots, so moving nearer objects are not a controlled pixel-difference baseline elsewhere in the image.

The hold harness parks a nearer funnel on this exact bearing and can mask the far silhouette; the accepted daylight comparison therefore omits `stormHarness=hold`. Anvil origins project into the top 3–11 pixels of the canvas, so some cloud edges are cropped. A horizon-friendly gameplay camera / S7-05 review should assess this framing. Initial diagnostic funnel ground is at camera-start height (4.5 m); this is a shader/silhouette probe, not an authored director touchdown.

Browser captures reported no JavaScript exceptions; existing development-build warnings about stripped optional depth-of-field/Panini/FSR shaders remain in raw console logs. No claim of a clean full render-debugger or dark-sky validation is made.

## Reproduction

Use `webgl-presentation-probe.mjs` with these environment variables:

```powershell
$env:PRESENTATION_PROBE_BUILD = 'builds/webgl-dev'
$env:PRESENTATION_PROBE_STATIONARY = '1'
$env:PRESENTATION_PROBE_QUERY = 'stormVisualSpike=far'
$env:PRESENTATION_PROBE_FAR_COMPARISON = '1'
node unity/StormChaserLoop3D/Assets/Tests/Presentation/Diagnostics/webgl-presentation-probe.mjs . "$env:TEMP/stormchaser-farfield-unobstructed-2026-10-02" 10
```

For wedge draws, set query to `stormVisualSpike=wedge&stormHarness=hold`, unset FAR_COMPARISON, and set `PRESENTATION_PROBE_EMPTY_SKY='1'`. Raw console/program/timing captures remain in their `%TEMP%` directories. Only small summaries/screenshots are checked into the lane-owned diagnostics folder.

## Boost wiring verification limits

Vehicle exhaust now has eight reserved flame/streak cards, emitted only while authoritative `BoostActive` is true and gameplay input/time are enabled. Skid/spark pools remain independent. Audio has a dedicated boost roar source started on the rising edge, a one-shot ignition whoosh, fade-out and stop on release, and disable/retry cleanup. Engine pitch/load now reads smoothed `EngineLoad`; the acceleration proxy is removed. No input sampling or gameplay `Raise*` calls were introduced.

Source compiles and 29 pure presentation NUnit tests pass via the isolated managed runner. These tests do not exercise a live S7-04 boost verb, and the existing WebGL build predates this wiring. Since gameplay deliberately reports `BoostActive=false`, ignition/flame/roar listening and visual acceptance must follow S7-04 in a fresh build. Earlier Andy acceptance covers the prior presentation pass only.
