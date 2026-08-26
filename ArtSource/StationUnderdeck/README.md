# Station Underdeck Backdrop

`StationUnderdeckAlbedo.png` is the unchanged 1254×1254 RGB output generated with the built-in image-generation tool for Run 106. SHA-256: `5C359DCF29415E58CE5531EB9B5782D2C38F53289B557D46B86718279F173E11`.

## Final prompt

```text
Use case: stylized-concept
Asset type: seamless tileable game texture for a Unity station under-deck camera backdrop
Primary request: a square top-down texture of recessed dark sci-fi station substructure that visually continues beyond authored room edges
Scene/backdrop: dense layered maintenance plating, shallow trenches, cable channels, vent grids, and sparse inset conduits, viewed perfectly orthographically from above
Subject: dark alloy industrial surface with broad readable panel rhythm and no single focal object
Style/medium: polished stylized-realistic PBR-ish game texture, hand-authored production asset rather than concept art
Composition/framing: square, edge-to-edge, designed to tile seamlessly on all four edges; evenly distributed detail and scale
Lighting/mood: extremely low-key neutral overhead lighting baked minimally; mysterious station depth without pure black void
Color palette: near-black blue-gray alloy, restrained desaturated steel, very sparse dim cyan and amber micro-indicators occupying under 3 percent of the image
Materials/textures: worn powder-coated alloy, subtle scratches, recessed grilles, thin cable seams; no raised perspective silhouettes
Constraints: seamless edges; top-down orthographic; low contrast so player, enemies, projectiles, powered cyan territory, magenta fields, amber objectives, and red threats remain dominant; no text; no symbols; no letters or numbers; no logos; no watermark
Avoid: bright areas, large glowing lines, circular hero motifs, doors, walls, props, characters, enemies, weapons, perspective depth, horizon, vignette, border, obvious repeated focal elements, baked directional shadows
```

## Unity contract

- The unchanged source is copied to `Assets/DeadSignal/Resources/Environment/StationUnderdeckAlbedo.png`.
- `DeadSignalStationBackdropSetup` imports it as repeating, trilinear, mipmapped, high-quality-compressed color data capped at 1024 pixels.
- The authored 150×100-metre quad sits at y = -1.1 below the playable decks, has no collider, and uses 15×10 texture tiling. Its 15-metre minimum margin beyond every arena edge covers the camera's target-visibility correction at region boundaries.
- The subdued material is presentation-only. It does not define navigation, collision, powered territory, objectives, entrances, or encounter state.
