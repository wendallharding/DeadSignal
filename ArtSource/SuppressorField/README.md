# Suppressor Field Active Edge

`SuppressorFieldActive.png` is the original bitmap source for the active Suppressor denial field. The unchanged source is copied to
`Assets/DeadSignal/Resources/VFX/SuppressorFieldActive.png`; Unity imports it as a clamped, bilinear, mipmapped, high-quality compressed RGBA texture.

The runtime `SuppressorFieldTelegraph` uses a thin amber `LineRenderer` during the escape warning, then replaces it with this edge-weighted texture at
0.62 maximum renderer alpha during the active phase. The center remains transparent so the player, threat origin, cover, objective direction, and escape
lane remain visible. The visual is presentation-only and has no collider.

## Reproduction

- Tool: Codex built-in image generation (`image_gen`), default built-in mode.
- Use-case slug: `stylized-concept`.
- Generated source: 1254×1254 RGBA PNG.
- SHA-256: `B21C0F2DC07060E921E673C053A886B713D3D1BFDD2A2A34EBBEF807D85E97FA`.
- Alpha validation: center `0`; corners `0, 0, 1, 0`; 17.00% of pixels have alpha at or above 230; runtime alpha further caps the complete texture at 0.62.

Final prompt:

```text
Use case: stylized-concept
Asset type: top-down game VFX texture for a Unity URP suppression-field warning
Primary request: an original circular tactical suppression-field decal with a genuinely transparent background; the center must remain almost fully transparent so characters, cover, and floor stay readable; concentrate energy into a thin irregular outer ring with three subtle inward broken arcs and sparse directional ticks that clearly communicate leave the radius
Scene/backdrop: transparent canvas only
Subject: one centered circular field boundary, no objects or environment
Style/medium: clean emissive sci-fi game VFX texture, crisp hard-surface signal graphics with restrained plasma noise
Composition/framing: centered orthographic top-down circle filling about 88 percent of the square canvas, generous transparent center and corners, rotationally balanced
Lighting/mood: urgent but readable security warning
Color palette: magenta outer edge with small amber warning accents; transparent black-free interior
Materials/textures: emissive signal lines, soft feathered alpha only at the ring edge
Constraints: genuine alpha transparency; no opaque central disc; no text; no letters; no numbers; no characters; no logo; no brand; no watermark; no border outside the circular field; preserve a clean silhouette for bilinear sampling
Avoid: solid fill, dense fog, rectangular framing, background, floor texture, cyan, red, photorealism
```

One neutral-tint edit was rejected because it flattened a checkerboard into RGB pixels and removed alpha. It is not retained or consumed by the project.
