# Capacitor Spine art source

The Capacitor Spine is assembled from the existing purpose-built `DepartureCapacitor` and `SignalTowerAssembly` modular prefabs. The capacitor's editable Blender 5.2 source, UV-mapped FBX export, texture, and reproduction script remain under `ArtSource/DepartureChannel`; this region reuses that production asset as a transfer bank and protective shield instead of duplicating geometry.

`CapacitorSpineRouteDecal.png` is the editable production copy of an original built-in image-generation asset. It marks one incoming cyan circuit splitting around a barred transfer capacitor and reconverging toward one amber salvage objective. The decal is a 1254-by-1254 RGBA PNG with transparent corners and SHA-256 `865861E2F11F4345D69883DB8D9B5AA1826201087566FC2289D1BF527FFB99D9`. The Unity copy lives under `Assets/DeadSignal/Resources/Environment` and is imported with alpha, mipmaps, clamped wrapping, and high-quality compression.

Final generation prompt:

```text
Use case: stylized-concept
Asset type: transparent top-down Unity floor decal for DEAD SIGNAL's Capacitor Spine route split
Primary request: create a single square industrial navigation sigil showing one incoming cyan circuit path splitting into two curved lanes around a central barred capacitor symbol, then reconverging toward one amber salvage diamond at the far edge
Scene/backdrop: genuinely transparent background, isolated decal only
Style/medium: crisp production-ready sci-fi floor stencil, slightly worn screen-print and emissive circuitry, readable from a high tactical camera
Composition/framing: centered symmetrical top-down square; thick paths and generous transparent negative space; no perspective
Lighting/mood: self-lit cyan network traces with restrained amber hazard accents
Color palette: dark alloy outlines, white-ceramic separators, cyan path, amber salvage endpoint, tiny red warning interruptions
Materials/textures: subtle scratched paint and metal stencil wear inside the graphic only
Constraints: actual alpha transparency; no text, letters, numbers, logos, watermark, border, mockup floor, shadows, or background; one incoming path, exactly two alternate lanes, one central obstacle, one amber endpoint; strong gameplay readability at small scale
Avoid: photorealistic scene, perspective, UI panel, excessive detail, magenta dominance
```
