# Layout preview (approximation, not a screenshot)

`DumpFrame.cs` plays a real `RoadSim` run headlessly and dumps one frame of state as JSON.
`render_preview.py` draws that frame at 1080x1920 with shapes that approximate what
`SpriteFactory` bakes in-game.

It exists so camera framing, entity density and HUD readability can be checked on a portrait
canvas **before** a licensed Unity build exists. The images in `docs/preview/` came from here, so
treat them as layout mock-ups: positions, sizes, distances and HUD values are real, the drawing
is an approximation of the in-game art.

```bash
# compile the dumper with the Roslyn compiler inside the Unity editor, then render
UNITY_DATA=/path/to/Unity/Editor/Data
$UNITY_DATA/NetCoreRuntime/dotnet $UNITY_DATA/DotNetSdkRoslyn/csc.dll -nologo -nostdlib \
  -out:Dump.dll -target:exe $(ls $UNITY_DATA/NetCoreRuntime/shared/Microsoft.NETCore.App/*/System.*.dll | sed 's/^/-r:/') \
  $(find Assets/Scripts/Sim -name '*.cs') tests/SimTests/Autopilot.cs tools/preview/DumpFrame.cs
python3 tools/preview/render_preview.py frame.json preview.png
```
