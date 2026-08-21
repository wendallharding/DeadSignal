# Northwest Relay Fork

`create_relay_bank.py` builds the reusable four-part relay bank, exports its Unity FBX, saves the editable Blender source, and renders a review preview.

Run with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python .\ArtSource\RelayFork\create_relay_bank.py
```

The FBX uses one UV-mapped midnight-blue armor mesh plus separate ceramic-insulator, brass-coil, and cyan-signal meshes for persistent Unity URP materials.
