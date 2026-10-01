# Presentation verification

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
