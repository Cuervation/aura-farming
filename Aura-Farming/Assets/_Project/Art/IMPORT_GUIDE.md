# Art Import Guide

## Card art

Apply these settings in Unity after import:

- Texture Type: **Sprite (2D and UI)**.
- Sprite Mode: **Single**.
- Mesh Type: **Full Rect**.
- Alpha Is Transparency: **enabled** only for assets that contain alpha.
- Generate Mip Maps: **disabled** for Canvas card art.
- Compression: test desktop quality first; use a mobile override only after visual review.

## Naming

- Card back: `aura-signal-card-back-vN.png`.
- Signal fronts: `signal-NN-kebab-name-vN.png`.
- Never replace an approved asset; create a new version.

## UI binding

Use native Unity Image components in `Game.unity`. Keep the selected-card state private until the Reveal panel is active.
