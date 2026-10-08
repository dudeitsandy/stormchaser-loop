# CA-1 verification — 2026-10-08

Technical checks complete; subjective listening acceptance remains pending Andy.
No presentation defect found. The entire live/listening gate is not marked closed.

## Candidate

Frozen local WebGL **0.8.8**, copied from the October 5 18:31 build; seed 554.
WASM.unityweb SHA256: `4F7D645A74572C3DC1EA24F03D64C451C573F93651CE5A709722CD7E460574D2`.
Chrome 154, Radeon 890M ANGLE/D3D11, 960×600 canvas.
Native integration: current source, Unity 6000.6.0f1, ArtTest.
Browser captures use actual keyboard controls, without physProbe or fabricated events.
Native tests place/launch the real truck into scenery or the live director's near-miss
band, and use scripted vehicle inputs for drift. Presentation never raises events.

## X7-02 VFX

| Item | Evidence | Result |
|---|---|---|
| Sliding smoke | `feedback/feedback-slide.png` | Visible in WebGL |
| Landing dust | Both `feedback/feedback-jump*-landing.png` | Visible after normal jumps |
| Impact sparks | `impact-integration.xml`, silo/pole collisions | Installed spark renderers activate; appearance not clearly captured in WebGL |
| Boost flame/streaks | `feedback/feedback-boost-start.png`, held, release, settled | Visible while boosting; absent after release |
| AIR popup | Both landing captures: AIR 0.6s | Visible and clear of HUD |
| DRIFT / NEAR MISS | Native real events: DRIFT 2.6s / NEAR MISS | Label text verified; fresh rendered captures still absent |

The shared popup layout is at 44% screen height; AIR captures show HUD clearance.
No presentation thresholds needed retuning.

## X7-03 audio

`feedback/audio-observations.json` preserves markers, source starts, playback rates,
downstream gains and console output. Duration/tone clip identification is a diagnostic
inference grounded in synthesized source clips. Paired dummy/real browser nodes are
not counted as duplicate gameplay events.

| Item | Observation |
|---|---|
| Engine speed/load response | Playback rate spans 0.8–2.1; gain peaks at 0.144 |
| Skid | Gain spans 0–0.24 and returns to zero; playback rate 0.85–1.25 |
| Landing thump | Two jump touchdown groups start the 0.25s clip; gain about 0.333 |
| Boost | 0.22s ignition at onset; roar gain reaches 0.30, fades, and source ends after release |
| Impact severity | 0.45s crunch: touchdown contact gain about 0.249; harder silo hit about 0.536 |

These verify triggering/scaling, **not perceived audio quality or balance**.
Andy was asked to confirm the five sounds; no answer was available when written.

## X7-07 scenery

`scenery-playmode.xml`: **9/9 pass**, existing real-scene collision tests.
`impact-integration.xml`: **3/3 pass**, silo/pole impact VFX and real drift/near-miss labels.

| Prop | Peak prop speed | Truck speed after hit |
|---|---:|---:|
| Mailbox, 20 kg | 22.4 m/s | 20.7 m/s |
| Sign, 15 kg | 22.2 m/s | 20.6 m/s |
| Crates, 30 kg | 22.3 m/s | 20.1 m/s |
| Bale, 250 kg | 19.6 m/s | 18.1 m/s |

All launched at 22.4 m/s. Light props raise no impact; the bale raises one light
impact and costs no HP. Fence speed 13.4 m/s, truck retains 16.3 m/s.
Barn/tree blocking passes. Silo/pole tests retain the truck on the approach side,
−2.32 m / −1.05 m from the centre, with real impacts and active sparks.
Slow push and Retry restoration pass.
The natural WebGL post-boost silo collision blocks the truck and removes one HP:
`impact-feedback/feedback-coast-impact-2.png` onward. Older solidity tests disable
gameplay input: their no-HP result does not establish that running solid hits are free.

## Reproduce / limitations

Browser probe: `PRESENTATION_PROBE_FEEDBACK=1`, `PRESENTATION_PROBE_CA1=1`,
`PRESENTATION_PROBE_STATIONARY=1`, `PRESENTATION_PROBE_QUERY=seed=554`,
the frozen build path, sample seconds 0. CA1 adds boost and brief coasting captures.
Native PlayMode filters: `LooseSceneryPlayTests;ScenerySolidityTests`, then
`Ca1ImpactIntegrationTests`. No editor held the project before each launch.
Sandboxed Unity failed before testing on Package Manager IPC; approved retry succeeded.
The first new EditMode harness failed before collisions; corrected PlayMode assembly
passes all three cases. Batchmode screenshot output was unavailable; native assertions
are distinguished from rendered browser captures. Both retained browser feedback runs
have zero browser errors. This is not a performance gate.

Full traces and initial duplicate captures remain locally in
`builds/ca1-snapshot-2026-10-08/raw-diagnostics/`; compact audio evidence is retained here.
To close CA-1 strictly: confirm the five audio items by listening and visually accept
brief impact sparks and DRIFT/NEAR MISS placement, or explicitly accept native evidence
for those remaining visual items. No runtime code, gameplay values, scenes, prefabs,
commit or push changed.
