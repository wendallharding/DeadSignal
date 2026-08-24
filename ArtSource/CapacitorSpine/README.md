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

`CapacitorSpineActivationDecal.png` is the editable production copy of the built-in image-generation asset used at the third tower berth. It is a 1254-by-1254 RGBA PNG with transparent corners and SHA-256 `7741C3A23C9E12D76E3A7AA0C36C518FE8BBB2F6721E4313DD296B08EDBB39F4`. The Unity copy is imported with alpha, mipmaps, clamped wrapping, and high-quality compression. The generated design is original, text-free, and contains no logos or franchise motifs.

Final activation-decal prompt:

```text
Use case: stylized-concept
Asset type: top-down transparent floor decal for a Unity science-fiction action game
Primary request: create an original industrial capacitor-network activation sigil for the far third Signal tower berth, communicating a cyan power node that amplifies an already-chosen weapon route
Scene/backdrop: genuinely transparent background with transparent corners; isolated floor marking only
Subject: concentric broken cyan capacitor rings around a compact central hexagonal node, two branching circuit paths that each intensify into a brighter outer pulse, restrained amber transaction chevrons at the perimeter
Style/medium: crisp hand-authored game decal, slightly worn industrial stencil and emissive circuit inlay, readable beneath a perspective top-down camera
Composition/framing: centered square, orthographic top-down, strong simple silhouette, generous transparent margin
Lighting/mood: self-luminous cyan with subtle amber accents, no cast shadow
Color palette: cyan and white-ceramic highlights, restrained amber, tiny dark-alloy wear; no dominant red or magenta
Materials/textures: painted deck stencil, etched metal wear, emissive circuitry; clean alpha edges
Constraints: no text, no letters, no numerals, no logos, no watermark, no characters, no weapons, no perspective, no background plate, no franchise motifs; actual alpha transparency; commercially safe original design
```
