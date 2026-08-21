# Security Warden art source

`SecurityWarden.blend` is the editable Blender 5.2 source for DEAD SIGNAL's first pursuing threat. It contains three gameplay-facing objects whose names and origins are part of the Unity integration contract:

- `Warden Chassis`
- `Warden Eye`
- `Warden Crown`

The source texture is `Assets/DeadSignal/Resources/Actors/SecurityWardenArmorAlbedo.png`. Regenerate the editable source, preview, and Unity FBX from the repository root with:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python 'ArtSource\SecurityWarden\create_security_warden.py'
```

The script exports `Assets/DeadSignal/Resources/Actors/SecurityWardenModel.fbx` and renders `SecurityWardenPreview.png`. Preserve the three object names and origins because runtime presentation and PlayMode validation depend on them.

All geometry and script content are original to DEAD SIGNAL. The armor albedo was generated for this project with OpenAI's built-in image-generation tool using the production prompt recorded in `DEVLOG.md`.
