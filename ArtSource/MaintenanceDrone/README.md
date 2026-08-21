# Maintenance Drone art source

`MaintenanceDrone.blend` is the editable Blender 5.2 source for the DEAD SIGNAL player model. It contains four gameplay-facing objects whose names and origins are part of the Unity integration contract:

- `Drone Chassis`
- `Drone Signal Ring`
- `Drone Core`
- `Drone Tool`

The source texture is `Assets/DeadSignal/Resources/Actors/MaintenanceDroneHullAlbedo.png`. The Blender file and Unity FBX can be regenerated from the repository root with:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python 'ArtSource\MaintenanceDrone\create_maintenance_drone.py'
```

The script exports `Assets/DeadSignal/Resources/Actors/MaintenanceDroneModel.fbx` and renders `MaintenanceDronePreview.png`. Keep the four object names and the `Drone Tool` origin stable because runtime presentation and PlayMode validation depend on them.

All geometry and script content are original to DEAD SIGNAL. The hull albedo was generated for this project with OpenAI's built-in image generation tool using the production prompt recorded in `DEVLOG.md`.
