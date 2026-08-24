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

`RelayFoundryWeaponCalibrationDecal.png` is the editable production copy of the original built-in imagegen asset beside the Relay tower.
Its cyan straight-through and amber single-rebound paths communicate the Piercing Pulse versus Controlled Ricochet choice without text.
The Unity copy is imported with preserved alpha; its SHA-256 is `AEF9B839F531FE32B57BC8341BD1A22E8E977630A5C98779224D2B46E436560F`.

`RelayFoundryLockdownDecal.png` is the editable production copy of the original built-in imagegen asset marking both authored
Foundry reinforcement gates. It uses an amber/red concentric suppression ring with inward mechanical chevrons, cyan circuit
interruptions, and a barred central aperture on a transparent background. The Unity copy is imported with preserved alpha; its
SHA-256 is `5149081AE09011B0F4CF85F5A8A5E39DE064715D94F6765FBE0E544A2BC04FCC`.
