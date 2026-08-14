# Kombi Rush

An offline-first Android arcade game made in Unity, built for the phones people actually use in
Zimbabwe. You drive a kombi: weave through potholes, oncoming traffic and roadblocks, pick up
passengers on the roadside, bank the fares at the stop, and spend what you earn on the kombi.

- **Engine:** Unity 6000.3.22f1, Universal RP (2D renderer)
- **Target:** Android, `minSdk 25`, IL2CPP, ARM64 + ARMv7, portrait, `com.tsorostudios.kombirush`
- **Constraints it is designed around:** no network permission, plays fully offline, under 60MB,
  60fps on a 2GB entry-level device

## Why a kombi game

Chosen from live market data rather than taste — the full reasoning and citations are in
[PROJECT.md](PROJECT.md). Short version: driving and vehicle-sim titles are the biggest single
cluster in Zimbabwe's own Play Store chart (12 of the top 45), almost every top title works
offline, mobile data is expensive enough that APK size affects installs, and the kombi theme is
unowned on Play.

## Layout

```
Assets/Scripts/Sim      game rules, engine-free (assembly KombiRush.Sim, noEngineReferences)
Assets/Scripts/Game     Unity layer: rendering, input, HUD, menus, audio, saving
Assets/Scripts/Editor    build entry point and the Boot scene generator
tests/SimTests          headless tests for the rules
tools/                  test and compile scripts that work without opening the editor
```

The hard rule: **no game rule lives in a MonoBehaviour.** `RoadSim` is a pure state machine
stepped at a fixed 1/60s, so a run is reproducible from `(config, upgrades, seed, input)` and can
be tested, replayed and balanced without the engine.

## Running the tests

No Unity licence and no .NET SDK needed — the script falls back to the Roslyn compiler and .NET
runtime that ship inside the Unity editor:

```bash
tools/run_sim_tests.sh          # 20 checks: determinism, fairness, playability, economy, saves
tools/compile_check.sh          # compiles all three assemblies against Unity's own DLLs
```

`tools/compile_check.sh` expects an editor at `/work/tools/unity/6000.3.22f1`, or set
`UNITY_ROOT=/path/to/editor`.

## Building the APK

Open the project in Unity 6000.3.22f1 with Android Build Support installed, then
**Kombi Rush → Build Android APK**. Headless:

```bash
Unity -quit -batchmode -nographics -projectPath . \
      -executeMethod KombiRush.EditorTools.BuildAndroid.Build -logFile -
```

Environment variables the build reads: `KOMBI_OUTPUT`, `KOMBI_VERSION`, `KOMBI_VERSION_CODE`,
`KOMBI_DEVELOPMENT`, and for release signing `KOMBI_KEYSTORE`, `KOMBI_KEYSTORE_PASS`,
`KOMBI_KEY_ALIAS`, `KOMBI_KEY_PASS`.

CI: `sim tests` runs on every push (no licence required); `android apk` builds the APK on demand
and needs a Unity licence in repository secrets — see `.github/workflows/android.yml`.

## Controls

Tap the left or right half of the screen to shift one lane, or hold and slide to steer straight
to a lane. Arrow keys / A and D work in the editor.

## Design notes

- **Art and audio are generated in code** (`SpriteFactory`, `AudioKit`). The APK carries no
  textures or audio files, which keeps the download small on metered data, and the whole look is
  tuned by editing numbers instead of reopening an art tool.
- **Fairness is a contract, not a hope.** The generator guarantees every row leaves an open lane,
  that lane is reachable using only 60% of the kombi's sideways reach, and nothing spawns inside
  the reaction window. All three are asserted by the tests over thousands of generated rows.
- **Runs end for a reason:** the tank runs dry or the body is finished. Fares are banked when
  passengers board and paid out at the stop with a combo multiplier, so the risky play is
  carrying a full kombi a long way.
