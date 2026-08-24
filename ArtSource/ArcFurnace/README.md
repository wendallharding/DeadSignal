# Arc Furnace art source

`create_arc_furnace.py` reproducibly creates the UV-mapped `ArcFurnace.blend`, exports
`Assets/DeadSignal/Resources/Environment/ArcFurnaceModel.fbx`, and renders `ArcFurnacePreview.png`.
Run it with Blender 5.2 in background mode.

`ArcFurnaceRouteDecal.png` was generated with the built-in image-generation tool on 2026-08-24. Final prompt:

> Use case: stylized-concept. Transparent floor-routing decal for a top-down Unity sci-fi action game. A square
> industrial route emblem for an Arc Furnace crossing, showing two amber entry branches around a central red circular
> furnace core, with one branch tightly zig-zagging behind white-ceramic shield marks and the other forming a long
> exposed arc, then both converging into a thin cyan return trace. Crisp geometric stencil and emissive circuit inlay;
> dark alloy accents, amber approaches, restrained security red, cyan return, and small white-ceramic highlights.
> Centered and readable from above with generous transparent corners. No words, letters, numbers, logos, watermark,
> perspective floor, scenery, characters, border, or opaque background.

The selected 1254x1254 RGBA source has transparent corners and SHA-256
`3B6F37425F903095B5C0FCE62391BCBBB759AF30654887C53AD5240A0999B981`. It was inspected for text, logos,
watermarks, and unrelated branded imagery. Unity uses a clamped transparent unlit material; the image is not tiled.
