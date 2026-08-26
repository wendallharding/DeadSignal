# Relay Cooling Gantry

`create_relay_heat_exchanger.py` builds the UV-mapped Relay heat exchanger, saves the editable Blender source, exports the production FBX, and renders a preview.

Run with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python .\ArtSource\RelayCoolingGantry\create_relay_heat_exchanger.py
```

The transparent route decal was generated with the built-in image tool on 2026-08-26. Final prompt: “Create a clean overhead industrial cooling-loop insignia showing two parallel approach lanes curving around a central rectangular heat-exchanger symbol and reconnecting on the far side; genuinely transparent background; crisp game-ready sparse geometric strokes; centered square orthographic symbol; amber outbound path, cyan return path, tiny white-ceramic accents; subtle worn stencil breakup only inside the strokes; no text, letters, numbers, logos, watermark, gradients, or photorealism.” The unchanged source is retained as `RelayCoolingGantryRouteDecal.png` and copied into Unity Resources. Unity imports it with alpha transparency, mipmaps, clamp wrap, 2048 maximum size, and compressed high-quality texture compression.

The landmark uses dark alloy, white ceramic, copper, and cyan coolant meshes. `DeadSignalRelayCoolingGantrySetup` maps those named FBX parts to established project materials, creates the collision-authoritative prefab, authors the two-threshold room, and nests it in `RelayFoundryRegion.prefab`.
