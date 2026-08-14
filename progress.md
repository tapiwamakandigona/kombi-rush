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
