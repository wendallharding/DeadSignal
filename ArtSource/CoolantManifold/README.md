# Coolant Manifold art source

`CoolantManifold.blend` is the editable Blender 5.2 source for DEAD SIGNAL's scene-authored tower-junction obstacle. It contains two gameplay-facing objects whose names are part of the Unity integration contract:

- `Coolant Manifold Body`
- `Coolant Manifold Conduit`

The source texture is `Assets/DeadSignal/Resources/Environment/CoolantManifoldAlbedo.png`. Regenerate the editable source, preview, and Unity FBX from the repository root with:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python 'ArtSource\CoolantManifold\create_coolant_manifold.py'
```

The script exports `Assets/DeadSignal/Resources/Environment/CoolantManifoldModel.fbx` and renders `CoolantManifoldPreview.png`. Preserve the two object names and centered origins because the Unity prefab setup and PlayMode validation depend on them.

All geometry and script content are original to DEAD SIGNAL. The armor albedo was generated for this project with OpenAI's built-in image-generation tool using the production prompt recorded in `DEVLOG.md`.
