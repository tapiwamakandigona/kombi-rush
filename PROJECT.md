# Kombi Rush — project state

## Goal
An offline-first Android arcade game made in Unity for Zimbabwean phones: one-thumb kombi
driving, dodge potholes and roadblocks, pick up passengers, bank fares at the stop, upgrade
the kombi. Ships as a real APK from a real Unity project.

## Why this game (research, 2026-08-14)
Decided from live data, not taste:

- **Zimbabwe Play Store, top free games (pulled `gl=ZW` on 2026-08-14):** #1 Dream League Soccer
  2026, #2 Football League 2026, #3 Candy Crush Saga, #4 Royal Match, #5 EA SPORTS FC Mobile,
  #8 Street Racing 3D, #9 Subway Surfers, #12 Truckers of Europe 2, #15 Bus Simulator Indonesia,
  #17 Block Blast. **12 of the top 45 are driving / racing / vehicle-sim titles** — the largest
  single cluster, bigger than football itself. [play.google.com, 2026-08-14]
- **Offline is table stakes:** 15 of the ZW top 20 carry Play's "Offline" tag; 57% of players in
  a francophone-Africa study play offline by preference. Econet data runs roughly US$9 per 5GB.
  [gamesindustry.biz 2024, newsday.co.zw 2026-04]
- **Gender split (GeoPoll × Pan Africa Gaming Group, n=2,558):** puzzle is picked by 54% of women
  vs 27% of men; sports/football by 55% of men vs 16% of women; racing 34% overall. Arcade wins
  Day-1 retention, board/puzzle win Day-30 — so this build pairs an arcade core with a
  progression layer, and a board game (Tsoro) is the planned second title for the other wedge.
  [geopoll.com, 2024]
- **Device floor is an entry-level Transsion phone, not a flagship:** ZW mobile vendor share
  (StatCounter, 2026-07) Samsung 42.0%, Apple 12.8%, Huawei 12.7%, Itel 7.4%, Xiaomi 5.4%,
  Tecno 4.6%, Infinix 0.8%. Start.io names the Itel A56 Pro as the top device for ZW mobile
  gamers. Android version spread: 13 (18.6%), 16 (15.6%), 14 (14.7%), 15 (13.0%), 12 (11.2%),
  ≤9 about 8% → `minSdk 25`. [gs.statcounter.com, 2026-07]

## Standing decisions
- **Engine:** Unity **6000.3.22f1**, Universal RP 2D (from Unity's own 2D cross-platform template,
  so ProjectSettings are authoritative rather than hand-rolled).
- **Target:** Android, `minSdk 25`, IL2CPP, ARM64 + ARMv7, portrait only,
  `com.tsorostudios.kombirush`, company "Tsoro Studios".
- **Budget:** APK under 60MB (target under 40MB), 60fps on a 2GB-RAM Helio A22 class device,
  fully playable with no network permission.
- **Architecture rule:** all game rules live in `Assets/Scripts/Sim` as an engine-free assembly
  (`KombiRush.Sim`, `noEngineReferences: true`). Unity code only renders it and feeds input.
  That is what makes the game testable headlessly — see `tools/run_sim_tests.sh`.
- **Determinism:** a run is a pure function of (config, upgrades, seed, input). All randomness
  comes from `Sim/Rng.cs`.
- **Fairness contract:** every generated row leaves at least one open lane, an open lane is
  always reachable from the previous row using only 60% of the kombi's physical sideways reach
  (`SimConfig.ReachSafetyFactor`), and nothing spawns inside the reaction window.

## Status
- Simulation core, economy, save profile: **done, 20/20 headless checks green**.
- Unity presentation layer: in progress.
- Editor build script + CI: in progress.
- **Blocked on one thing:** the Unity editor in the build sandbox is unlicensed
  (`No valid Unity Editor license found`), so no APK has been produced yet. A free Unity
  Personal licence unblocks it. Everything else (editor 6000.3.22f1, Android module, NDK r27c,
  OpenJDK 17, SDK platforms 34/35/36, build-tools 36.0.0) is installed and verified.

## Not doing (yet)
- No ads, no IAP, no analytics, no accounts, no network calls in v1.
- No 3D. Sprites only.
- Second title (Tsoro) after v1 ships.
