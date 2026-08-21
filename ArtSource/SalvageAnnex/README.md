# Salvage annex barrier

`create_salvage_annex_barrier.py` reproducibly builds the low-poly cargo barrier used by the authored salvage annex.

Run with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python .\ArtSource\SalvageAnnex\create_salvage_annex_barrier.py
```

The script writes the editable `.blend`, a rendered preview, and the FBX imported by Unity. Its armor mesh maps the project-owned `SalvageAnnexAlbedo.png`; Unity assigns persistent URP materials after import.
