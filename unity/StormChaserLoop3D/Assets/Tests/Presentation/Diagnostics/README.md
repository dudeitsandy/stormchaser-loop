# Presentation verification

Follow-up verdict and evidence: [capture-review-2026-10-02.md](capture-review-2026-10-02.md). AC-29 frame-time and draw-count subchecks pass on the reference capture; X7-06 remains open for Unity CPU attribution, and wedge pixel overdraw is still unmeasured. The far-field material renders a visible 800 m daylight silhouette in the development build. Boost visuals/audio now read `BoostActive` (dormant until S7-04), and engine audio reads `EngineLoad`; earlier acceleration-proxy/interface-wait notes below describe the initial delivery.

## Sprint 7 vehicle feedback and storm probes (2026-10-02)

`VehicleCardVfx` self-installs through `SceneInstaller.EveryScene`. Its fixed default pool has 24 smoke/dust cards and 12 spark cards. Sliding rear-wheel smoke requires ground contact, speed and slip; spawn raycasts follow terrain height. Landing and toss emit bounded dust bursts. Declared `VehicleImpact` and `StyleEvent` listeners supply sparks and DRIFT/AIR/NEAR MISS pops once gameplay raises them. Disable/run-end clears cards and UI; disable unsubscribes all callbacks. `ProceduralAudio` adds one skid loop and one vehicle one-shot source, with slip-dependent squeal, landing thump, toss whoosh, severity-dependent metal crunch, and smoothed speed/acceleration-dependent engine revs. Boost awaits an authoritative gameplay query, and engine load is currently an acceleration proxy. Listening/ArtTest/retry checks remain necessary.

Opt-in probes are included in gameplay code, with no scene/prefab edits:

- WebGL: add `?stormVisualSpike=wedge` or `?stormVisualSpike=far` to the game URL.
- Desktop/editor launch: `-stormVisualSpike=wedge` or `-stormVisualSpike=far`.
- Wedge applies a **visual-only** Mature EF5 preview to the first existing illustrated tornado. Width 24 m, height 20 m, ground width 20.4 m; three orbiting sub-vortices; debris ring centered at 14.4 m with height 18 m; dust ring spans 12–35 m. It replaces sparse dust/debris cards with one ring mesh and two dynamic billboard mesh batches containing 1,200 debris and 500 gusts. Seven renderers imply at most 14 draws for main + PiP before other scene/presentation work. These are structural counts, not measured draw calls or timing. The controller continues moving/expiring normally: discard capture intervals after it expires or before the preview activates. This does not model EF5 physics/wind, nor the proposed single-atlas production material. Overlapping billboards can exceed four layers; pixel overdraw must be captured, not inferred from renderer count.
- Far requires Claude's `Resources/Presentation/FarFieldStormMaterial`, whose fog-exempt contract is in AGENTS.md. It builds an octagonal wall-cloud base, three anvil cards and one wedge silhouette at a fixed horizontal 800 m bearing. The main camera far clip is temporarily extended and restored on disable. Missing material disables the probe with a prerequisite warning. This probe is for shader/depth/readability validation, not director lifecycle behavior. The PiP clip is unchanged.

For AC-29 capture **60 uninterrupted active seconds** with the mature wedge visible up close. Compare against the same empty-sky camera path on the reference machine; report p95 complete frame latency, extra draws, live particles and a pixel overdraw capture. The seven-renderer/1,700-particle implementation satisfies only structural capacity expectations; neither p95 ≤33.3 ms nor ≤4-layer overdraw is certified. If the existing controller lifecycle prevents the 60-second window, a Claude-owned controlled ArtTest harness is required.

Unity CPU markers now cover `Presentation.Pip.Update`, `Presentation.Lens.Blit`, `Presentation.Lens.UI`, `Presentation.Tornado.Update/Camera`, `Presentation.Wind.Update/Camera`, `Presentation.VehicleVfx.Update/Camera`, and `Presentation.Audio.Update`. Capture in a development build and include native camera rendering/culling and UI cost; markers alone do not account for all presentation work. Browser instrumentation still does not measure Unity component CPU work. Existing browser color-based VFX classification predates the dark EF5 and new vehicle effects: treat unknown cards separately rather than labeling them as tornado/wind automatically.

From the repository root, run the pure presentation tests without opening Unity:

```powershell
powershell -NoProfile -File unity/StormChaserLoop3D/Assets/Tests/Presentation/Diagnostics/verify-presentation.ps1
```

This compiles current gameplay source using Unity's bundled compiler and existing Bee references, then runs the pure NUnit cases using Unity's bundled .NET runtime. It does not replace Unity EditMode or visual validation.

Measure an existing `builds/webgl` build with Node 24 and installed Google Chrome:

```powershell
node unity/StormChaserLoop3D/Assets/Tests/Presentation/Diagnostics/webgl-presentation-probe.mjs . "$env:TEMP/stormchaser-webgl-profile" 30
```

The tool serves the build unchanged, injects browser diagnostics, and opens an isolated hidden headless Chrome profile. It writes screenshot, raw timing/trace data, and summary JSON to the requested output directory. It drives and turns the truck; results/title frames are excluded using the camcorder pass. It closes its own browser afterward.

GPU queries sample every fifth rendered frame. Framebuffer dimensions identify PiP; shader uniforms identify the lens and card material. Linear material colors separate tornado and wind draws, including cards rendered by PiP. PiP's bucket therefore excludes card draws, which belong to their VFX bucket. Unknown cards remain explicitly unclassified. GL submission times measure native API calls and exclude diagnostic bookkeeping. Audio submission measures AudioParam controls; Chrome audio callback traces execute on another thread. Nested audio events must not be summed. GPU, API, and audio timings can overlap and their additive work subtotal is not frame latency.

`webgl-baseline-2026-10-01.json` records a 30-second local 0.5 baseline: Chrome 154 / Radeon 890M, 960×600 canvas, 320×240 PiP, 1,735 active frames, 347 GPU samples, zero disjoint queries. Build WASM SHA256: `1F24BA0BF5B3EB216A9661C83E97CC3F3B2B719617396023379C020FA63EF290`.

The measured GPU/API/audio-control subtotal is 0.510 ms mean and 1.435 ms p95. Unity component CPU work is not measured by this probe. Wind coverage is light (maximum four visible draws; pool capacity sixty). The build predates the single-surface funnel change. These numbers do not certify the full 3 ms budget or a worst-case target-device budget. A fresh development spike build must supply per-component Unity CPU measurements, sustained heavy wind, and captures verifying the new funnel in both cameras. Audio-output callback work is 238.282 ms total, or 0.137 ms per rendered-frame equivalent, separately on the audio thread.

If the complete budget exceeds 3 ms, test PiP at 30 Hz / 160×120 first, then a wind pool of 36 and emission of 18 cards/second. The new single-surface funnel already reduces default tornado renderers from 33 to 21.

## Rebuilt funnel verification

The 16:37 rebuilt gameplay build has WASM SHA256 `307F2CF6DACFC053118D50A2893C407E6E7898806301943F458A181F4EF79A93`. `funnel-main-pip-2026-10-01.png` shows a connected tapered silhouette in both cameras. The default funnel now has one surface rather than stacked ribbons.

`webgl-funnel-2026-10-01.json` records a 40-second driving run: 2,325 active frames, 465 GPU samples, zero browser errors/disjoint queries. Measured GPU/API/audio-controls subtotal: 0.424 ms mean / 1.037 ms p95. `webgl-wind-2026-10-01.json` records a stationary-input run in which the moving tornado reaches the truck: strong wind bonuses, damage, up to six audio sources, 35 visible wind draws and 63 tornado draws across cameras. Active-frame subtotal: 0.684 ms mean / 1.575 ms p95. Results-screen frames are excluded. Screenshots every five seconds add overhead in this second run; raw audio traces span both gameplay and results and are not normalized to only active frames.

To reproduce stationary captures, set `$env:PRESENTATION_PROBE_STATIONARY = '1'` before the Node command. Unset it for driving measurements. The probe saves screenshots at five-second intervals as well as the final frame. The forced maximum-pool and full Unity component CPU measurements remain outstanding; no 3 ms certification is implied.
