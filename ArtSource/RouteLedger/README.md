# Route Ledger Insignia

`RouteLedgerInsignia.png` is the production-resolution source for the end-of-run route ledger shown by the serialized DEAD SIGNAL HUD. The same 768-by-768 RGBA bitmap is packaged at `Assets/DeadSignal/Resources/UI/RouteLedgerInsignia.png`.

## Generation

- Tool: built-in OpenAI image generation
- Use case: `stylized-concept`
- Generated: 2026-08-24
- Original output: 1254-by-1254 RGBA with transparent pixels
- Production conversion: Lanczos downsample to 768-by-768, optimized PNG, alpha preserved

Prompt:

> Create a clean, text-free Unity debrief emblem showing a maintenance-drone route splitting around a central station node, with one restrained cyan return path and one riskier amber branch reconnecting at extraction. Use a centered circular industrial route-ledger composition, crisp hard-surface sci-fi stencil treatment, dark alloy and white ceramic, subtle cyan and amber emission, actual transparent background, and no text, logos, watermark, weapons, characters, or busy scenery.

## Unity import

- Texture type: Default / RawImage-compatible
- sRGB: enabled
- Alpha source: input texture alpha
- Alpha is transparency: enabled
- Mipmaps: disabled
- Wrap mode: clamp
- Maximum import size: 2048 (source is 768)

The cyan and amber route split is intentionally readable without labels: cyan communicates the required withdrawal, while amber communicates the longer optional-greed commitment. Do not add text to the bitmap; route names remain localized runtime copy.
