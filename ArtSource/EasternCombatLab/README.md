# Eastern combat laboratory target

`EasternCombatLabTarget.png` is the original transparent floor insignia used to identify the scene-authored combat-lab player and camera center. It was generated with OpenAI's built-in image generation mode and copied unchanged into `Assets/DeadSignal/Resources/Environment/EasternCombatLabTarget.png`. SHA-256: `1F2EB529E215F05B08322B5EE7541A24DCCFA97F1E47DB8FAECD423291959297`.

Final prompt:

> Use case: stylized-concept. Asset type: transparent top-down Unity floor decal for the DEAD SIGNAL eastern-room combat laboratory. Create one centered industrial combat-test target emblem with a compact cyan-white player core, four clearly separated magenta threat vectors aimed inward from the cardinal diagonals, and a restrained amber safe-frame ring. Use a crisp sci-fi maintenance stencil and emissive circuit inlay with broad distant-camera-readable shapes in dark alloy, white ceramic, cyan, magenta, and restrained amber. Genuine transparent background; no text, letters, numbers, logos, brands, characters, scenery, rectangular background plate, border clipping, drop shadow, watermark, red, or photorealism.

Unity import contract: alpha transparency enabled, mipmaps enabled, clamp wrapping, 2048 maximum size, and high-quality compression. The setup script assigns the image to a transparent URP Unlit material and places it below the authored player anchor without collision.
