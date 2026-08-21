# Security Warden Staging Bay

`create_security_shield.py` builds the reusable three-part blast shield, exports its Unity FBX, saves the editable Blender source, and renders a review preview.

`create_bypass_marker.py` builds the low-profile floor arrow used to mark the bay's northern bypass, exports its Unity FBX, saves the editable Blender source, and renders a review preview.

Run with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python .\ArtSource\WardenBay\create_security_shield.py
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python .\ArtSource\WardenBay\create_bypass_marker.py
```

The FBX uses one UV-mapped charcoal armor mesh plus separate ceramic-brace and crimson-warning meshes for persistent Unity URP materials.
The route marker is a purpose-built, beveled mesh that reuses the game's cyan navigation material and has no gameplay collider.
