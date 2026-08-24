# Flux Bypass route decal

`FluxBypassRouteDecal.png` is an original 1254-by-1254 RGBA floor decal generated with the built-in image-generation tool for Run 93. It is copied unchanged into `Assets/DeadSignal/Resources/Environment` and imported by `DeadSignalFluxBypassSetup` with alpha transparency, mipmaps, clamp wrapping, high-quality compression, and a transparent URP unlit material.

Final prompt:

```text
Use case: stylized-concept
Asset type: top-down transparent floor-route decal for a Unity sci-fi action game
Primary request: an original text-free industrial flux-bypass route glyph: two amber entry chevrons connected by a narrow angular circuit path around a central dark break, converging into one cyan return arrow; add two restrained red interruption marks that imply danger along the outer route
Scene/backdrop: genuinely transparent background, decal only
Style/medium: crisp worn stencil paint and emissive circuit inlay, readable from an elevated gameplay camera
Composition/framing: centered square symbol with generous transparent margins; strong silhouette; no perspective
Color palette: cyan, amber, restrained red and magenta accents; no white background
Materials/textures: lightly distressed station-floor paint with subtle grime breakup, clean emissive edges
Constraints: actual alpha transparency; text-free; original abstract geometry; clear at small size; no logos, trademarks, letters, numbers, watermark, frame, floor, shadows, or background scene
Avoid: photorealistic environment, gradients filling the canvas, dense micro-detail, circular badge, UI panel
```

Inspection: the generated asset contains no text, logo, trademark, or watermark. All four corner alpha values are effectively transparent (`0, 0, 1, 0`). SHA-256: `C98518A1C6CD9AEC6E0BE390F5FBB79D32F6EEB7D00431B6C82056A21746E0A1`.
