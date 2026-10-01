# S7-01 Streaming Spike — G1 Gate Results (ADR-0004 Validation #1)

Pass criteria: no frame > 50 ms, no GC spike > 5 ms, physics step ≤ 4 ms, over a 3-min run at 29 m/s
across 2 km of streamed tiles with 60-piece debris drops every 20 s.

## Run 1 — headless Chromium (software rendering), 2026-10-01
`{"verdict":"FAIL","frames":10538,"medianMs":17.00,"p99Ms":34.00,"maxMs":81.00,"framesOver50":5,"gcFrames":6,"gcSpikeMs":16.00,"physMaxMs":9.30,"physAvgMs":0.331,"sliceMaxMs":2.80,"colliderBakeMaxMs":3.30,"tilesBuilt":262,"initialLoadMs":51}`

Findings:
- Streaming itself healthy: slices ≤ 2.8 ms, collider cook ≤ 3.3 ms, initial 5×5 ring 51 ms.
- Physics 9.3 ms occurred once (first debris contact), never repeated in 8 later drops; avg 0.33 ms.
- GC: instrumentation allocated every frame (overlay string) + per-summary list copy; streamer had
  small per-tile allocs (closures, System.Random, string rename). Software rendering inflates frames.
- 25 harmless "MeshCollider has no vertices" errors from component add order.

Fixes before Run 2 (commit after 03c0c75): allocation-free streamer (comparer, struct RNG, no rename),
overlay at 4 Hz + reused sort buffer, collider added before MeshFilter, first debris drop moved into
the 5 s warm-up (models PhysX warm-up during the loading screen — an explicit assumption).

## Run 2 — headless Chromium (software rendering), 2026-10-01
`{"verdict":"FAIL","frames":10441,"medianMs":17.00,"p99Ms":34.00,"maxMs":51.00,"framesOver50":2,"gcFrames":0,"gcSpikeMs":0.00,"physMaxMs":1.50,"physAvgMs":0.300,"sliceMaxMs":2.90,"colliderBakeMaxMs":3.80,"tilesBuilt":271,"initialLoadMs":40}`

- **GC eliminated** (0 collections over 3 min, 271 tiles) and **physics ≤ 1.5 ms** with 60-piece debris drops.
- Only failure: 2 frames at 51 ms (1 ms over) — 3× the software renderer's ~17 ms frame quantum;
  CPU-side streaming work never exceeded 3.8 ms in a frame. Headless is not authoritative for
  frame time; Run 3 (real GPU) decides. Screenshot: `s7-01-spike-run2.png`.

## Run 3 — Andy, real Chrome (real GPU), 2026-10-01
`{"verdict":"FAIL","frames":7524,"medianMs":17.00,"p99Ms":33.00,"maxMs":27944.00,"framesOver50":2,"gcFrames":0,"gcSpikeMs":0.00,"physMaxMs":1.20,"physAvgMs":0.251,"sliceMaxMs":2.00,"colliderBakeMaxMs":2.10,"tilesBuilt":200,"initialLoadMs":19}`

- maxMs 27,944 = the tab was hidden/minimized (Chrome stops rAF for background tabs) — not game time.
- An earlier, separate real-Chrome attempt showed `phys≤7.30` at t=102 (overlay snapshot); not
  reproduced in this run (1.2 ms) or in headless run 2 (1.5 ms).

## Gate decision — G1: **PASS with condition** (Andy, 2026-10-01)
**Keep the 2 km world.** Streaming is clearly viable in WebGL: 0 GC across all post-fix runs, ≤ 2–3 ms
streaming work per frame, 19–40 ms initial load, steady 60 fps median.
**Condition:** the one-off 7.3 ms physics step (> 4 ms budget, < 16.6 ms frame) is carried into
**S7-06** as an explicit physics-budget check under real debris + vehicle load; if it recurs there,
add a PhysX warm-up at load and/or reduce the WebGL debris cap.
**Metric follow-up:** SpikeMetrics should ignore hidden-tab time and record when maxima occur
(do before the next perf gate).
