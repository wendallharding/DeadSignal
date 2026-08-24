# Convergence Chamber art source

`create_convergence_busbar.py` reproducibly creates the UV-mapped `ConvergenceBusbar.blend`, exports
`Assets/DeadSignal/Resources/Environment/ConvergenceBusbarModel.fbx`, and renders `ConvergenceBusbarPreview.png`.
Run it with Blender 5.2 in background mode.

`ConvergenceChamberRouteDecal.png` was generated with the built-in image-generation tool on 2026-08-24. Final prompt:

> Use case: stylized-concept. Transparent floor-routing decal for a top-down Unity sci-fi action game. A square
> industrial circuit-routing emblem for a deep security convergence chamber, with two amber approach chevrons
> converging toward a red warning node and thin cyan return traces splitting back outward. Crisp geometric stencil
> and emissive circuit inlay; dark alloy, amber, restrained red, cyan, and white ceramic. Centered and readable from
> above. No words, letters, numbers, logos, watermark, perspective floor, border, or opaque background.

The selected 1254x1254 RGBA source has transparent corners and SHA-256
`BAC3F8B3C537993D8C513A024FAC6922A025A3D3EC1A85D477EEDF773421E035`. It was inspected for text, logos,
watermarks, and unrelated branded imagery. Unity uses a clamped transparent unlit material; the image is not tiled.
