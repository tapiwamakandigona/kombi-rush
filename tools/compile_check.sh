#!/usr/bin/env bash
# Compiles every game assembly against Unity's own reference assemblies without opening the
# editor. Catches real compile errors on a machine with no Unity licence, which is the only
# static check available before a licensed build runs.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

UNITY_ROOT="${UNITY_ROOT:-/work/tools/unity/6000.3.22f1}"
DATA="$UNITY_ROOT/Editor/Data"
[[ -d "$DATA" ]] || { echo "Unity not found at $UNITY_ROOT (set UNITY_ROOT)" >&2; exit 2; }

DOTNET="$DATA/NetCoreRuntime/dotnet"
CSC="$DATA/DotNetSdkRoslyn/csc.dll"
NETSTANDARD="$DATA/NetStandard/ref/2.1.0/netstandard.dll"
UGUI_DIR="$(dirname "$(find "$DATA/Resources/PackageManager/ProjectTemplates/libcache" -name UnityEngine.UI.dll | head -1)")"
OUT="$ROOT/.build/compile"
mkdir -p "$OUT"

REFS=("-r:$NETSTANDARD")
for dll in "$DATA"/Managed/UnityEngine/UnityEngine*.dll; do REFS+=("-r:$dll"); done

echo "== KombiRush.Sim (engine-free) =="
"$DOTNET" "$CSC" -nologo -nostdlib -target:library -warnaserror+ -langversion:9.0 \
  -out:"$OUT/KombiRush.Sim.dll" "-r:$NETSTANDARD" \
  $(find Assets/Scripts/Sim -name '*.cs')

echo "== KombiRush.Game (UnityEngine + uGUI) =="
"$DOTNET" "$CSC" -nologo -nostdlib -target:library -warnaserror+ -langversion:9.0 \
  -out:"$OUT/KombiRush.Game.dll" "${REFS[@]}" \
  "-r:$OUT/KombiRush.Sim.dll" "-r:$UGUI_DIR/UnityEngine.UI.dll" \
  $(find Assets/Scripts/Game -name '*.cs')

echo "== KombiRush.Editor (UnityEditor) =="
"$DOTNET" "$CSC" -nologo -nostdlib -target:library -warnaserror+ -langversion:9.0 \
  -out:"$OUT/KombiRush.Editor.dll" "${REFS[@]}" \
  "-r:$DATA/Managed/UnityEditor.dll" \
  $(find "$DATA/Managed" -maxdepth 1 -name 'UnityEditor.*Module.dll' -printf '-r:%p\n') \
  "-r:$OUT/KombiRush.Sim.dll" "-r:$OUT/KombiRush.Game.dll" "-r:$UGUI_DIR/UnityEngine.UI.dll" \
  $(find Assets/Scripts/Editor -name '*.cs')

echo
echo "all assemblies compiled clean"
