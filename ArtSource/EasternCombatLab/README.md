# Eastern combat laboratory target

`EasternCombatLabTarget.png` is the original transparent floor insignia used to identify the scene-authored combat-lab player and camera center. It was generated with OpenAI's built-in image generation mode and copied unchanged into `Assets/DeadSignal/Resources/Environment/EasternCombatLabTarget.png`. SHA-256: `1F2EB529E215F05B08322B5EE7541A24DCCFA97F1E47DB8FAECD423291959297`.

Final prompt:

> Use case: stylized-concept. Asset type: transparent top-down Unity floor decal for the DEAD SIGNAL eastern-room combat laboratory. Create one centered industrial combat-test target emblem with a compact cyan-white player core, four clearly separated magenta threat vectors aimed inward from the cardinal diagonals, and a restrained amber safe-frame ring. Use a crisp sci-fi maintenance stencil and emissive circuit inlay with broad distant-camera-readable shapes in dark alloy, white ceramic, cyan, magenta, and restrained amber. Genuine transparent background; no text, letters, numbers, logos, brands, characters, scenery, rectangular background plate, border clipping, drop shadow, watermark, red, or photorealism.

Unity import contract: alpha transparency enabled, mipmaps enabled, clamp wrapping, 2048 maximum size, and high-quality compression. The setup script assigns the image to a transparent URP Unlit material and places it below the authored player anchor without collision.

## Evasion-language exploration (Run 104)

`EasternCombatLabEvasion.png` is the non-destructive second visual-development pass for the live-balance combat/evasion policy. It makes the intended response grammar explicit: magenta vectors press toward the cyan drone core while amber arcs communicate lateral escape rather than retreat. It remains in `ArtSource` until the concurrent Arc Furnace serialization work is resolved and a fresh rendered lab frame proves that replacing the imported target improves, rather than crowds, top-down readability. SHA-256: `D03C7CC3152C4290C2EE5C3D739DFEDC444793B7F511DFE2AB50F14B99FF66AD`.

Final prompt:

> Use case: stylized-concept. Asset type: Unity top-down floor decal for the DEAD SIGNAL eastern combat laboratory. Create a single original radial combat-and-evasion calibration insignia, viewed perfectly orthographic from above, showing a compact cyan player core, four evenly spaced magenta threat vectors aimed inward, and four amber curved escape lanes sweeping between those vectors. Use a high-polish hard-surface sci-fi decal with crisp distant-camera-readable silhouettes, controlled cyan/amber/magenta emissive energy, dark alloy, and small white-ceramic segments. Center it with generous transparent margin and genuine alpha outside and between elements. No text, numbers, logos, watermark, scenery, perspective, opaque corners, dense micro-detail, excessive bloom, weapons, skulls, or hazard-stripe clichés.
