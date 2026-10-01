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

## Run 2 — headless Chromium
_pending_

## Run 3 — Andy, real Chrome (authoritative for frame/GC criteria)
_pending_
