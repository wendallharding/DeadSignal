# Departure capacitor

`create_departure_capacitor.py` reproducibly builds the low-poly Signal capacitor bank used by the extraction departure channel.

Run with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python .\ArtSource\DepartureChannel\create_departure_capacitor.py
```

The script writes the editable `.blend`, rendered preview, and FBX imported by Unity. Unity assigns persistent URP materials after import.
