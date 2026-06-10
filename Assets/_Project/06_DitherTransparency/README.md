# 06_DitherTransparency

## Overview

Dither transparency implementation comparison between Shader Graph and HLSL.

When a character is occluded by an object, dithering creates a pseudo-transparent effect that is lighter than true transparency and avoids draw order issues.

## Reference

- [ディザ抜きで一度に複数モデルが透過するのを防ぐには？](https://gamemakers.jp/article/2026_01_08_128090/) - UE5 implementation by Logical Beat

## Goals

- [x] Shader Graph version
- [x] HLSL version
- [ ] Compare performance and readability between both approaches

## Key Concepts

- Depth-based dithering
- Screen-space dither patterns
- Per-instance coordinate handling (for GPU instancing support)

## Directory Structure

```text
06_DitherTransparency/
├── Scenes/
│   └── DitherTransparency.unity    # Camera + player capsule + occluder walls
├── Shaders/
│   ├── ShaderGraph/
│   │   └── DitherTransparencySG.shadergraph  # Lit + Dither node + Alpha Clip
│   └── HLSL/
│       └── DitherTransparency.shader  # 4x4 Bayer dither clip (Forward/Shadow/Depth passes)
├── Materials/
│   ├── DitherWall.mat              # HLSL version (Wall_Center, Wall_Back)
│   ├── DitherWallSG.mat            # Shader Graph version (Wall_Left)
│   ├── Player.mat
│   └── Ground.mat
└── Scripts/
    └── DitherOcclusionFader.cs     # SphereCast occlusion detection + per-renderer MPB fade
```

## Implementation Notes (HLSL version)

- Stays in the Opaque queue: `clip()` against a screen-space 4x4 Bayer threshold,
  so depth writes and sorting remain opaque-correct (no draw order issues)
- `DitherOcclusionFader` fades only renderers actually occluding the target via
  MaterialPropertyBlock, so other instances sharing the material stay opaque
- ShadowCaster / DepthOnly passes apply the same dither, so shadows lighten as
  the occluder fades
- Edit mode support via `[ExecuteAlways]` + `Physics.SyncTransforms()` before queries
- Shader Graph version: Lit target + built-in Dither node into Alpha with
  Alpha Clip Threshold ~0 — same `clip(alpha - threshold)` structure. Note the
  SG Dither node uses a /17-normalized Bayer matrix vs /16 in the HLSL version,
  and the circular hole mode is HLSL-only for now (`_DitherAlpha` works on both)
- Two fade modes on `DitherOcclusionFader`:
  - `WholeObject` — uniform dither across the occluding renderer
  - `CircularHole` — screen-space circular cutout around the target's viewport
    position (`GetNormalizedScreenSpaceUV` + aspect-corrected distance +
    `smoothstep` edge); the hole is view-dependent, so the ShadowCaster pass
    intentionally ignores it and shadows stay intact

> [!WARNING]
> `CircularHole` mode drives the HLSL-only hole properties (`_HoleRadius` etc.).
> Renderers using the Shader Graph material (`DitherWallSG`, e.g. Wall_Left) do
> NOT visibly fade in this mode — the properties simply don't exist there.
> Switch `Fade Mode` to `WholeObject` to see the Shader Graph version fade.
