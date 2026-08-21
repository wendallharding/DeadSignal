# Signal Bolt art source

`SignalBolt.blend` is the editable Blender 5.2 source for DEAD SIGNAL's player projectile. It contains two gameplay-facing objects whose names and origins are part of the Unity integration contract:

- `Bolt Shell`
- `Bolt Energy`

The source texture is `Assets/DeadSignal/Resources/Projectiles/SignalBoltAlbedo.png`. Regenerate the editable source, preview, and Unity FBX from the repository root with:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python 'ArtSource\SignalBolt\create_signal_bolt.py'
```

The script exports `Assets/DeadSignal/Resources/Projectiles/SignalBoltModel.fbx` and renders `SignalBoltPreview.png`. Preserve the two object names and centered origins because runtime composition and PlayMode validation depend on them.

All geometry and script content are original to DEAD SIGNAL. The ceramic albedo was generated for this project with OpenAI's built-in image-generation tool using the production prompt recorded in `DEVLOG.md`.
