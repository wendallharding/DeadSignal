# Signal Sapper Service Cradle

`create_siphon_pylon.py` builds the reusable three-part siphon pylon, exports its Unity FBX, saves the editable Blender source, and renders a review preview.

Run with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python .\ArtSource\SapperCradle\create_siphon_pylon.py
```

The pylon uses one UV-mapped black-violet armor mesh plus separate ceramic-yoke and magenta-energy meshes for persistent Unity URP materials.
