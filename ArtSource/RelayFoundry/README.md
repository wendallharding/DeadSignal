# Relay Foundry turbine

`create_relay_foundry_turbine.py` builds the editable Blender source, preview, and Unity FBX for the second-region landmark.

Run with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python .\ArtSource\RelayFoundry\create_relay_foundry_turbine.py
```

The source texture is an original generated atlas. The script creates UVs for every mesh and exports meshes only with Y-up / -Z-forward settings.

`RelayFoundryRouteDecal.png` is the editable production copy of an original built-in imagegen asset used at the foundry junction.
It was generated as a transparent, text-free top-down industrial route splitter using dark alloy, white ceramic, cyan network traces,
and amber hazard chevrons. The Unity copy lives under `Assets/DeadSignal/Resources/Environment` and is imported with preserved alpha.
