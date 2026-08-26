# Foreground Cutaway Footprint

`ForegroundCutawayFootprint.png` is the unchanged original bitmap produced with Codex built-in image generation for the collision-preserving foreground-cutaway milestone.

## Final prompt

```text
Use case: stylized-concept
Asset type: transparent game floor decal for a top-down sci-fi action game
Primary request: a square collision-preserving cutaway footprint marker, showing the footprint of a temporarily hidden industrial wall as a clean technical boundary
Scene/backdrop: genuinely transparent background
Subject: thin rectangular perimeter trace with restrained diagonal maintenance hatching near the edges and small circuit breaks; empty transparent center
Style/medium: crisp production-ready raster decal, original industrial sci-fi maintenance graphics
Composition/framing: centered square, generous transparent margin, seamless enough to stretch across rectangular wall footprints, no perspective
Lighting/mood: emissive but restrained, readable over near-black alloy flooring
Color palette: muted cyan-blue primary lines with sparse desaturated amber warning accents; avoid magenta and red
Materials/textures: slightly worn technical paint and faint emissive edge bloom, sharp silhouette
Constraints: actual alpha transparency; center must remain mostly transparent; no text; no numbers; no symbols resembling real brands; no logos; no watermark; no opaque background; no scene objects
Avoid: bright solid fill, dense noise, large focal emblem, photorealistic scene, white background
```

## Source and import contract

- Source dimensions: 1254 × 1254 RGBA.
- SHA-256: `E3FFAC76E3675F85A7FDC53FBAD8880476C5E2DABB97C638F2171A5929FAC72D`.
- The center alpha is zero. A 16-pixel sample grid measured 5,218 of 6,241 samples at alpha 8 or lower and 928 at alpha 230 or higher.
- Unity imports the project copy from `Assets/DeadSignal/Resources/VFX/ForegroundCutawayFootprint.png` with alpha transparency, mipmaps, clamp wrapping, bilinear filtering, 1024 maximum size, and high-quality compression.
- `ForegroundCutawayFootprint.mat` uses the existing runtime particle template as an alpha-cutout floor cue. It has no collider and appears only while the corresponding authored obstacle renderer is cut away.
- The bitmap is presentation only. `AuthoredMapObstacle` remains the sole movement and projectile footprint authority.
