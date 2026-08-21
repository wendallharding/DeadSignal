# Coolant Reclamation Gauntlet

`create_coolant_baffle.py` builds the reusable four-part coolant baffle, exports its Unity FBX, saves the editable Blender source, and renders a review preview.

Run with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python .\ArtSource\CoolantGauntlet\create_coolant_baffle.py
```

The generated texture is original project art. The FBX uses one UV-mapped armor mesh plus separate ceramic-fin, copper-pipe, and cyan-light meshes so Unity can persist coherent URP materials.
