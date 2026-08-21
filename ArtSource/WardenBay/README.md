# Security Warden Staging Bay

`create_security_shield.py` builds the reusable three-part blast shield, exports its Unity FBX, saves the editable Blender source, and renders a review preview.

Run with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python .\ArtSource\WardenBay\create_security_shield.py
```

The FBX uses one UV-mapped charcoal armor mesh plus separate ceramic-brace and crimson-warning meshes for persistent Unity URP materials.
