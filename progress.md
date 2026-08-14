# Progress log (append only, newest at the bottom)

## 2026-08-14 — research and decision
- Pulled the live Zimbabwe Play Store top-free games chart (`gl=ZW`), StatCounter ZW device and
  Android version share, POTRAZ/ZIMSTAT connectivity numbers, GeoPoll x PAGG gender split and
  GameAnalytics retention benchmarks. Findings and citations are in PROJECT.md.
- Chose an arcade kombi driver over a puzzle or board game for v1: the driving/vehicle cluster is
  the biggest in the actual ZW chart, arcade leads Day-1 retention, and the theme is unowned on
  Play so it can earn local coverage instead of paid installs.

## 2026-08-14 — toolchain
- Installed real Unity **6000.3.22f1** (Linux editor tarball, size-matched 4,462,973,216 bytes).
  Sandbox has no root, so GTK3 and friends were staged into a local prefix from 201 Debian
  packages and exported via `LD_LIBRARY_PATH`. `Unity -version` prints `6000.3.22f1`.
- Installed Android Build Support: extracted Unity's `.pkg` with 7-Zip (xar + cpio), then added
  OpenJDK 17.0.18+8, NDK r27c, SDK build-tools 36.0.0, platform-tools, platforms 34/35/36 and
  cmdline-tools per Unity's own release manifest. `java -version` confirms Temurin 17.0.18+8.
- **Blocker:** the editor refuses to do any work unlicensed:
  `No valid Unity Editor license found. Please activate your license.` A free Unity Personal
  licence (or a `.ulf`) unblocks scene generation and APK builds. Asked the operator.

## 2026-08-14 — simulation core
- Project scaffolded from Unity's bundled `com.unity.template.2d-cross-platform` so ProjectSettings
  are the editor's own defaults, then patched for Kombi Rush (portrait, minSdk 24, IL2CPP,
  ARM64+ARMv7, bundle id, product/company name, input handling "both").
- Wrote `KombiRush.Sim`: Rng, SimConfig, RoadSim, Economy, Profile. No engine references at all.
- Built a headless test harness that compiles the sim with the Roslyn compiler shipped inside the
  Unity editor and runs it on Unity's bundled .NET runtime, so tests run with no licence and no
  .NET SDK. `tools/run_sim_tests.sh` also uses a normal `dotnet` SDK when one is present (CI).
- The tests immediately caught three genuine defects, all fixed:
  1. **Hitboxes were too fat.** Kombi 0.62m + obstacle 0.70m half-widths meant a 1.32m lethal
     radius on 1.6m lanes, so any lane change alongside an obstacle clipped it. Narrowed to
     0.50m/0.55m (and shortened the longitudinal box), which restored a real gap to steer through.
  2. **The probe bot ignored obstacles level with the kombi** (cutoff at -1m while the collision
     window is ~1.7m), so it steered side-on into traffic. It now ignores an obstacle only once it
     is fully past the collision window, and refuses any lane change whose *path* crosses traffic
     it cannot clear in time.
  3. **The generator relied on the kombi's absolute physical reach.** At full difficulty a row
     could demand a 3-lane slide at the limit. Added `SimConfig.ReachSafetyFactor = 0.6` so the
     guaranteed gap is always reachable with margin.
- Result: bot median run 181s, worst of 25 seeds 53s/622m, stock kombi averages 2,606m against
  7,557m fully upgraded. 20/20 checks green.

## 2026-08-14 — Unity layer
- Wrote the whole presentation layer: `RoadView` (tarmac, red-and-white kerbs, scrolling lane
  markings, roadside scenery, pooled entity sprites), `GameRoot` (fixed 1/60 step accumulator so
  the game plays the same at 30 and 60fps, camera follow with hit shake, engine pitch driven by
  speed), `InputSteering` (tap a side or hold-and-slide, arrow keys in the editor), `Hud`
  (fares, distance, fuel, hull pips, riders, combo, floating toasts), `Screens` (menu, garage with
  five upgrade rows, end-of-shift summary), `SaveIO` (atomic write to persistentDataPath).
- **All art and audio are generated at runtime** — `SpriteFactory` bakes sprites on a small
  software canvas with distance-based anti-aliasing, `AudioKit` synthesises the engine loop and
  every SFX. The APK ships no texture or audio payload.
- `Assets/Scripts/Editor/BuildAndroid.cs` is the single build entry point, configured entirely
  from `KOMBI_*` environment variables; `SceneBuilder.cs` regenerates the Boot scene from code so
  it can never drift from the components it hosts.
- `tools/compile_check.sh` compiles all three assemblies against Unity's own reference DLLs with
  warnings-as-errors, using the editor's bundled Roslyn. This is the only static verification
  available while the editor is unlicensed, and it already caught one real problem:
  `AndroidApiLevel24` is obsolete in Unity 6000.3 (minimum is 25 / Android 7.1), so minSdk moved
  to 25 in both the build script and ProjectSettings. Per StatCounter that costs about 2.5% of
  Zimbabwean devices, and the engine leaves no choice.
- CI: `sim tests` runs the rules on every push with no licence; `android apk` builds through
  GameCI on demand and fails fast with a clear message when no licence secret is present.

**Verified:** 20/20 sim checks green, all three assemblies compile clean.
**Not verified (needs a licence):** the scene actually loads, the game renders, the APK builds.

## 2026-08-14 — the preview caught a design flaw, so the world got rescaled
Rendered a real sim frame at phone resolution (`tools/preview/`) and the framing was wrong: at
24 m/s with a 1.6m lane width the player could only see about 11m of road ahead - under a second
of reaction time. Nothing in the tests could catch that, because it is a camera problem.

Rescaled the whole world to real dimensions and retuned around a reaction-time budget:
- lane 3.2m (a real traffic lane), kombi hitbox 1.84 x 4.7m, oncoming car 1.72 x 4.3m,
  pothole 1.7 x 1.2m, roadblock spans two lanes
- speeds 8.5 -> 17 m/s (31 -> 61 km/h), lane change 2.6 lanes/s
- camera keeps a 3m verge either side, kombi sits 20% up the screen: about **27m of road visible
  ahead, close to two seconds at top speed**
- rows every 2.1s falling to 1.35s, nothing spawns closer than 30m, at most 2 of 4 lanes blocked
- per-kind hitboxes replaced the single obstacle box

Balance work off the back of it:
- **Fuel deaths were arbitrary.** Every "worst seed" ended at exactly 45.0s - the tank size, not a
  mistake. Capacity is now 60s base (+14s per upgrade), cans are commoner and worth more, and a new
  test asserts both failure modes occur but fuel is not the usual one (now 22 wrecks / 3 dry of 25).
- **The fare loop barely fired.** Stops sat in a single lane every 300m, so the bot got paid once a
  run. A stop is now a bay painted across every lane every 180m: reach it alive and you get paid.
  Payouts went from 1 to 13 per run and the combo reaches its x3 cap.
- **Passengers now wait in an outer lane** when one is open, so going for a fare pulls you toward
  the kerb - authentic, and it creates a real decision instead of a free pickup.
- Base hull 4 instead of 3, since an oncoming car costs 2.

Result: 21/21 checks green, bot median run 181s, worst of 25 seeds 85s / 927m, stock kombi
averages 2,461m against 5,515m fully upgraded.
