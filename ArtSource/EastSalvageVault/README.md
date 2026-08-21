# East Salvage Vault source

`create_east_salvage_vault.py` regenerates the editable Blender scene, Unity FBX, and preview for the optional east-side salvage room.

Run with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python ArtSource\EastSalvageVault\create_east_salvage_vault.py
```

The FBX contains separate floor, wall, route-splitter, and energy-guide meshes. Unity attaches object-aligned movement bounds to the five wall sections and central splitter; the floor and energy guides remain presentation-only. Geometry uses Blender X/Y as the horizontal plane and Z as height so Unity imports it on X/Z with Y up.
