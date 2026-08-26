# Authored foreground cutaway footprint

`ForegroundCutawayFootprintAuthored.png` is the unchanged built-in image-generation output used by explicitly scene-bound wall shells when their presentation cuts away. The source and Unity consumer are byte-identical.

## Final prompt

```text
Use case: stylized-concept
Asset type: Unity top-down combat readability decal, transparent PNG
Primary request: create one square collision-footprint engineering frame for a hidden industrial wall shell in the sci-fi game DEAD SIGNAL
Scene/backdrop: genuinely transparent background and transparent center
Subject: a thin rectangular perimeter made from broken cyan light rails, restrained amber corner brackets, and four small inward-facing interruption chevrons that communicate a solid blocker remains present
Style/medium: crisp high-resolution game VFX decal, clean hard-surface industrial UI language, not a logo
Composition/framing: perfectly top-down, centered, orthographic, square canvas; wide empty transparent center; marks confined to the outer 15 percent of the canvas
Lighting/mood: emissive but controlled, highly readable on very dark alloy floors
Color palette: cyan primary, amber secondary, tiny white-hot highlights; no red or magenta
Materials/textures: subtle worn ceramic edge texture and faint scanline breakup, clean alpha falloff
Constraints: actual alpha transparency; center must be fully transparent; corners must remain transparent outside the marks; no text, letters, numbers, icons, watermark, checkerboard, background, floor, shadow, perspective, gradients filling the center, or opaque panel
Avoid: dense ornament, circular frame, hazard stripes, photoreal scene, bloom covering the transparent center
```

## Provenance and import contract

- Built-in image-generation mode; generated 2026-08-26.
- Source SHA-256: `14A604D4849797AE4445BFCDC6701641F05EB9000C7854E4DB1DC76EFA4498A5`.
- Dimensions: 1254 x 1254, 32-bit RGBA.
- Center alpha: 0. Corner alpha: 0, 0, 1, 0.
- Unity import: alpha transparency, clamp, bilinear filtering, mipmaps, 1024 maximum size, high-quality compression.
- Runtime contract: presentation only. The decal has no collider and never changes movement, projectile, NavMesh, or objective authority.
