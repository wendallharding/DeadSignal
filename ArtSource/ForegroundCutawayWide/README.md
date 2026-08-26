# Wide foreground cutaway footprint

This source bitmap supports the frame-coverage cutaway added after the Run 109 rendered Required Extraction review. It is a presentation-only replacement for broad foreground wall faces that consume at least ten percent of the visible frame. Movement collision, projectile blocking, NavMesh authority, objectives, entrances, powered territory, and combat rules remain owned by their existing scene-authored systems.

## Generation provenance

- Mode: built-in image generation (`stylized-concept`)
- Generated source: `ForegroundCutawayFootprintWide.png`
- Dimensions: 1254×1254 RGBA
- SHA-256: `F8B502CA58CE6CDABE1F77C8B1DCCA6C865A9FB1319CA8F0DECD7BA9F4467D31`
- Alpha audit at 16-pixel intervals: 5,907 transparent samples (`alpha <= 8`), 121 opaque samples (`alpha >= 230`), center alpha `0`

Final prompt:

> Use case: stylized-concept. Asset type: Unity top-down sci-fi floor cutaway cue for wide foreground boundary shells in DEAD SIGNAL. Primary request: create one original square transparent industrial boundary footprint that communicates a large solid station wall has temporarily cut away without filling or obscuring the tactical space. Scene/backdrop: genuinely transparent background and transparent center. Subject: a very thin broken cyan rectangular perimeter with four restrained amber corner brackets and a few tiny white-ceramic alignment ticks. Style/medium: crisp hard-surface game decal, orthographic, clean emissive stencil with subtle wear. Composition/framing: centered square footprint, geometry confined to the outer 10 percent of the image, at least 75 percent of the total image fully transparent, all four edges and corners readable from a distant top-down camera. Lighting/mood: controlled low-intensity emissive cue, not a glow bloom slab. Color palette: cyan dominant, restrained amber corners, tiny white-ceramic accents. Constraints: actual alpha transparency; transparent center and corners outside the marks; no filled panels; no text, letters, numbers, logos, watermark, border frame, arrows, red, magenta, perspective, background floor, shadows, or scenery. Avoid: dense circuitry, opaque center, broad glow, photographic texture, visual clutter.

## Unity import contract

- Alpha is transparency; clamp wrap; bilinear filtering; mipmaps enabled.
- Maximum texture size 1024 with high-quality compression.
- Used by `ForegroundCutawayFootprintWide.mat` only when a collision-authoritative foreground group occupies at least ten percent of the clipped frame and sits closer to the camera than the drone.
- The transparent center is intentional: the cue identifies blocked space without replacing one occluding slab with another.
