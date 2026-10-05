# Run Goals story 001 — counted airtime on the compact map

**Date:** 2026-10-04 · **Build:** 0.7.7 source · **Probe:** `Tests/PlayMode/AirtimeProbeTests.cs` (Explicit,
`-testFilter AirtimeProbeTests`), VerificationScene, reads `VehicleModel._countedAirSeconds` / `_airTimer` each
physics step.

| Case | Counted air (above `MinAirtimeHeight` 1.8 m) | Total air | Peak body rise | Airtime style event |
|---|---|---|---|---|
| Standing jump | 0.00 s | 0.84 s | 1.57 m | none |
| Boosted jump at top speed (flat road) | 0.00 s | 0.82 s | 1.54 m | none |
| EF4 toss, 5 m from a Mature funnel | 1.12 s | 1.46 s | 4.33 m | 1.12 s |
| EF5 toss, 5 m from a Mature funnel | 1.36 s | 1.64 s | 5.37 m | 1.36 s |

The compact arena is flat (road and field), so there is no slope to launch from.

**Decision (Andy, 2026-10-04):** keep `big_air` as a toss goal for now ("remember we will have jumps and stuff
eventually"). `BigAirSeconds` = **1.0 s** counted: an EF4 or EF5 toss earns it, a jump never does. Revisit when
ramps, bigger jumps or the 2 km world land.

**Side finding for Sprint 9 (driving pass):** jumps never reach 1.8 m, so the Airtime style moment (and its boost
refill, `RefillAir`) never fires from a jump on this map, only from tosses.
