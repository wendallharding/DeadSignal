# Departure capacitor

`create_departure_capacitor.py` reproducibly builds the low-poly Signal capacitor bank used by the extraction departure channel.

Run with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python .\ArtSource\DepartureChannel\create_departure_capacitor.py
```

The script writes the editable `.blend`, rendered preview, and FBX imported by Unity. Unity assigns persistent URP materials after import.

## Cargo-release return decal

`DepartureCargoReturnDecal.png` is the original transparent floor cue for the departure-channel cargo shutter. It was generated with OpenAI's built-in image generation mode, then copied unchanged into
`Assets/DeadSignal/Resources/Environment/DepartureCargoReturnDecal.png` for Unity import. SHA-256:
`23537781E2EBF4F558659ED77429B45E87224091FE94166235AFC7683FB1B399`.

Final prompt:

> Use case: stylized-concept. Asset type: transparent top-down sci-fi floor route decal for a Unity game. Create a centered industrial circuit-routing emblem showing two amber outbound paths splitting around a solid central blast-shutter symbol, then recombining into one bold cyan return path through the opened center. Use broad, distant-camera-readable graphite, white-ceramic, cyan, amber, and tiny red-lock shapes on genuine transparency. No text, letters, numbers, logos, brands, characters, watermark, scene backdrop, edge clipping, or tiny detail.

Unity import contract: alpha transparency enabled, mipmaps enabled, clamp wrapping, 2048 maximum size, and high-quality compression. The setup script assigns the texture to a transparent URP Unlit material and keeps the decal hidden until all three required regional payloads make extraction ready.

## Capacitor-surge decal

`DepartureCapacitorSurgeDecal.png` is the original transparent one-shot recharge cue for the released direct lane. It was generated with OpenAI's built-in image generation mode and copied unchanged into `Assets/DeadSignal/Resources/Environment/DepartureCapacitorSurgeDecal.png`. SHA-256: `8A194A6EF6531D59BCFC6DA35F5DC6F900CEB094657C7B454D0036292529F8C0`.

Final prompt:

> Use case: stylized-concept. Asset type: Unity top-down floor decal for the DEAD SIGNAL extraction return shortcut. Create a text-free capacitor-surge emblem showing three cyan-white maintenance circuits converging into one central cell and bold forward chevron, with restrained amber lock remnants, genuine transparency, and a distant-camera-readable silhouette. No text, letters, numbers, logo, brand, character, scenery, background plate, border, shadow, watermark, red, or magenta.

Unity import contract: alpha transparency enabled, mipmaps enabled, clamp wrapping, 2048 maximum size, and high-quality compression. The setup script assigns a transparent URP Unlit material. Runtime reveals the cue with extraction readiness and hides it after the player crosses the direct centerline and consumes the one-shot reserve.
