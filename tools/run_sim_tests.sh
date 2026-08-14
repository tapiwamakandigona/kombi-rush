#!/usr/bin/env bash
# Runs the engine-free simulation tests.
#
# Preferred path: a normal .NET SDK (`dotnet run --project tests/SimTests`).
# Fallback path: the Roslyn compiler and .NET runtime that ship inside the Unity
# editor, so the tests also run on a machine that only has Unity installed.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if command -v dotnet >/dev/null 2>&1; then
  echo "== running sim tests with the .NET SDK =="
  exec dotnet run --project tests/SimTests/SimTests.csproj -c Release
fi

UNITY_ROOT="${UNITY_ROOT:-}"
if [[ -z "$UNITY_ROOT" ]]; then
  for candidate in \
    /work/tools/unity/6000.3.22f1 \
    "$HOME/Unity/Hub/Editor/6000.3.22f1" \
    /opt/unity/editors/6000.3.22f1; do
    [[ -d "$candidate/Editor/Data" ]] && UNITY_ROOT="$candidate" && break
  done
fi
if [[ -z "$UNITY_ROOT" ]]; then
  echo "No dotnet SDK and no Unity install found. Set UNITY_ROOT=/path/to/editor." >&2
  exit 2
fi

DATA="$UNITY_ROOT/Editor/Data"
DOTNET="$DATA/NetCoreRuntime/dotnet"
CSC="$DATA/DotNetSdkRoslyn/csc.dll"
FW="$(ls -d "$DATA"/NetCoreRuntime/shared/Microsoft.NETCore.App/* | tail -1)"
OUT="$ROOT/.build/simtests"
mkdir -p "$OUT"

echo "== compiling sim tests with Unity's Roslyn ($(basename "$FW")) =="
REFS=()
for dll in "$FW"/System.*.dll "$FW"/netstandard.dll "$FW"/mscorlib.dll; do
  [[ -f "$dll" ]] && REFS+=("-r:$dll")
done

mapfile -t SOURCES < <(find Assets/Scripts/Sim tests/SimTests -name '*.cs' | sort)
"$DOTNET" "$CSC" -nologo -nostdlib -optimize -warnaserror+ -nullable:disable \
  -out:"$OUT/SimTests.dll" -target:exe "${REFS[@]}" "${SOURCES[@]}"

cat > "$OUT/SimTests.runtimeconfig.json" <<JSON
{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"6.0.0"}}}
JSON

echo "== running =="
exec "$DOTNET" "$OUT/SimTests.dll"
