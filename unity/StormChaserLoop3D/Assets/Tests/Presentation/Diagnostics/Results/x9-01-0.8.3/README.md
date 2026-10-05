# X9-01 presentation observations - 0.8.3, seed554

Frozen build WASM.unityweb SHA256:
`7A88352EAE007B857DAEE2D068C0D705F07FE359DE983DCFFB85E6B04F8B88BF`.
Chrome154, Radeon890M hardware ANGLE/D3D11; canvas1280x800, PiP320x240.
No concurrent Chrome probe runs. Existing build served from
`builds/perf-snapshot/codex-0.8.3-20261005`; no Unity/build invocation.

Driving: title20s then one180s W/weave/jump capture; 11165 active gameplay
frames, 2233 GPU-sampled frames. Query seed554&physProbe=1.
Physics probe intentionally holds the session timer; this is an extended diagnostic
run, not acceptance of the normal timer. Stress: stationary80s seed554&windVisualStress=max,
4808 active frames, 961 GPU-sampled frames.
Both captures have zero disjoint queries, browser errors and console error lines.
No gate verdict is emitted by these diagnostics.

| Draw component | Driving GPU mean / p95 ms | Max-pool GPU mean / p95 ms | GL submission mean ms, driving / pool |
|---|---:|---:|---:|
| PiP scene (excluding separately classified VFX/lens) | 0.220 / 1.132 | 0.276 / 1.358 | 0.195 / 0.319 |
| Camcorder lens blit | 0.036 / 0.117 | 0.040 / 0.179 | 0.052 / 0.104 |
| Rain mesh | 0.017 / 0.020 | 0.034 / 0.034 | 0.001 / 0.001 |
| Overcast deck | 0.035 / 0.053 | 0.041 / 0.070 | 0.001 / 0.002 |
| Wind cards | 0.000 / 0.001 | 0.080 / 0.358 | 0.002 / 0.082 |

Corrected max-pool funnel-only72-index mesh: GPU mean/p95
0.000482/0.000760ms; mean/max visible draws
0.182/2. This averages zeros when out of view;
it is not an EF5 filling the screen measurement. The initial driving funnel bucket
was too broad and included36-index siren geometry; its original numbers remain
flagged in drive-summary.json and are NOT presented as funnel-only cost.
Other dust/debris and color-varying cards remain partly unclassified.
Rain and deck each max2 draws (main+PiP). Pool logs consistently active60/60;
wind visible draws mean/max41.30/56.
Occupied cards can be culled;60 occupied does not mean120 visible draws.

Audio controls aggregate: driving mean/p950.025/0.100ms;
stress mean0.016ms. Sirens/radio are active gameplay features,
but browser AudioParams carry no reliable Unity-source name: separate siren and
radio CPU/DSP timings are unavailable. AudioOutputDevice::FireRenderCallback total
3485.065ms/11165 frames
= 0.312ms/rendered-frame equivalent,
on the audio thread; not main-thread cost. Nested events must not be summed.
Maximum AudioBufferSourceNode voices18; this is not a count of Unity AudioSource components.

Instrumented frame intervals: driving mean/p95/max
16.79/17.00/66.80ms,
3 intervals over50ms. Title mean/p95/max
17.76/17.00/1150.30ms,
2 over50ms, including first-use/instrumented work. These include GL wrappers,
query setup, shader/uniform introspection and screenshots. Cursor's uninstrumented
CU-01 report must supply the M1 whole-frame evidence; no exclusions or pass verdict
are inferred from this capture. Physics result as logged (not reinterpreted):

```text
[PHYS-RESULT] {"verdict":"PASS","steps":9001,"physMaxMs":2.70,"physMaxAtS":6,"physAvgMs":0.223,"stepsOverBudget":0,"impacts":34,"peakAwakeBodies":3}
```

No component-specific share was specified in the request. No cuts proposed from
these small measured GPU costs; full Unity component CPU updates, rain mesh uploads,
per-camera culling, UI work and pixel-overdraw remain outside this attribution.
PiP brightness fix is newer than this snapshot: added post-processing needs a fresh
candidate and its own PiP cost comparison. M1 moves the old3ms CPU breakdown to
post-M1 unless the uninstrumented frame gate fails; this report does not close it.
Raw deltas and console proof saved in drive-frames.json and stress-frames.json.
Full shaders/audio traces remain in %TEMP%/stormchaser-x901-083-drive and -stress.
