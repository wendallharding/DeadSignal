# Quench Loop art source

`create_quench_condenser.py` reproducibly creates the UV-mapped `QuenchCondenser.blend`, exports
`Assets/DeadSignal/Resources/Environment/QuenchCondenserModel.fbx`, and renders
`QuenchCondenserPreview.png`. Run it with Blender 5.2 in background mode.

`QuenchLoopRouteDecal.png` was generated with the built-in image-generation tool on 2026-08-24. Final prompt:

> Use case: stylized-concept. Transparent game-environment floor-route decal for DEAD SIGNAL's Quench Loop. An
> original square industrial floor sigil showing a bifurcating coolant loop that leaves a furnace lane, bends around
> a condenser core, and rejoins the return path. Clean hard-surface sci-fi stencil and emissive circuit decal,
> centered top-down with broad lines, restrained mechanical ticks, and generous transparent corners. Cyan
> powered-return line, amber coolant warnings, small red hazard interruptions, and white-ceramic registration marks.
> No text, letters, numbers, logos, brands, signatures, watermark, UI frame, characters, weapons, or vehicles.

The selected 1254x1254 RGBA source has transparent corners and SHA-256
`F19CCAB50BDD3D3E90A5B2360D30544137C7653F6566E538558A2189CC04789A`. It was inspected for text, logos,
watermarks, and unrelated branded imagery. Unity uses a clamped transparent unlit material; the image is not tiled.
