# 06_DitherTransparency

## Overview

Dither transparency implementation comparison between Shader Graph and HLSL.

When a character is occluded by an object, dithering creates a pseudo-transparent effect that is lighter than true transparency and avoids draw order issues.

## Reference

- [ディザ抜きで一度に複数モデルが透過するのを防ぐには？](https://gamemakers.jp/article/2026_01_08_128090/) - UE5 implementation by Logical Beat

## Goals

- [ ] Shader Graph version
- [ ] HLSL version
- [ ] Compare performance and readability between both approaches

## Key Concepts

- Depth-based dithering
- Screen-space dither patterns
- Per-instance coordinate handling (for GPU instancing support)

## Directory Structure

```text
06_DitherTransparency/
├── Scenes/
├── Shaders/
│   ├── ShaderGraph/
│   └── HLSL/
├── Materials/
└── Scripts/
```
