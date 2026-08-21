# Signal Sapper art source

`SignalSapper.blend` is the editable Blender 5.2 source for DEAD SIGNAL's tower-draining threat. It contains four gameplay-facing objects whose names and origins are part of the Unity integration contract:

- `Sapper Chassis`
- `Sapper Fork Left`
- `Sapper Fork Right`
- `Sapper Drain Core`

The source texture is `Assets/DeadSignal/Resources/Actors/SignalSapperArmorAlbedo.png`. Regenerate the editable source, preview, and Unity FBX from the repository root with:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python 'ArtSource\SignalSapper\create_signal_sapper.py'
```

The script exports `Assets/DeadSignal/Resources/Actors/SignalSapperModel.fbx` and renders `SignalSapperPreview.png`. Preserve the four object names and origins because runtime presentation, drain-core animation, and PlayMode validation depend on them.

All geometry and script content are original to DEAD SIGNAL. The armor albedo was generated for this project with OpenAI's built-in image-generation tool using the production prompt recorded in `DEVLOG.md`.
