# Project plan

## Purpose

This document defines the learning objectives, level structure, and implementation approach for Prism. Prism is a Unity project for learning optics, rendering pipelines, and game mathematics.

This project focuses on practical game mechanics and visual techniques, not architecture patterns.

---

## Level themes

### Lv.1: Laser reflection

A puzzle where you place mirrors to reflect lasers. This level focuses on math and physics fundamentals.

You use the following technologies:

- `Physics.Raycast` to detect hit points and surface normals
- `Vector3.Reflect(direction, normal)` to calculate reflection vectors
- `LineRenderer` to draw laser paths
- Recursive or loop-based logic for multiple reflections

You learn trajectory calculation fundamentals and line-of-sight detection basics.

---

### Lv.2: Rendering load experiment

Compare Forward, Forward+, and Deferred rendering with many lights. This level focuses on rendering pipeline understanding.

You use the following technologies:

- URP/HDRP configuration
- Per Object Limit settings
- Forward+ Rendering (URP 14+)

You perform these experiments:

- Forward: Observe Draw Call increase as light count grows using Profiler
- Forward+: Verify near-constant cost with many lights due to tiling
- Deferred: Examine G-Buffer construction cost and translucent object limitations

You learn optimization intuition for mobile light count limits and rendering pipeline trade-offs.

---

### Lv.3: Stencil mask

Hidden text becomes visible only where light shines (Closure-style effect). This level focuses on shader and visual effects.

You use the following technologies:

- Stencil buffer (bit 1, `Ref 2` / `WriteMask 2` / `ReadMask 2` for URP-safe isolation)
- Hand-written HLSL shaders (Shader Graph does not support stencil operations as of Unity 6)
- Render queue ordering (`Geometry-1` for mask, `Geometry` for content)

You implement the effect as follows:

- Mask Shader (`Prism/Stencil/Mask`): Write stencil bit 1 with `ColorMask 0` (invisible) and `ZWrite Off`
- Content Shader (`Prism/Stencil/Content`): Render only where stencil bit 1 is set (`Stencil Comparison: Equal`)
- Render queue ordering ensures mask evaluates before content without Render Objects configuration

You learn portal effects, X-ray vision techniques, and stencil buffer bit management in URP.

---

### Lv.4: Dynamic shadow mesh

In 2D, shadows from lights become physical walls. This level focuses on compute shader and advanced techniques.

You use the following technologies:

- Dynamic mesh generation
- Compute Shader
- URP 2D Shadow Caster

You implement the effect as follows:

- Raycast from light source to get vertices
- Connect vertices to generate polygon mesh
- Dynamically update `PolygonCollider2D` for collision detection
- Research URP 2D Lights and `Shadow Caster 2D` integration

You learn custom physics behavior beyond Unity's built-in features.

---

## Recommended starting point

Build a flashlight and enemy detection prototype. The player holds a flashlight (Spot Light). Enemies freeze when illuminated (Red Light, Green Light style).

You explore these technical elements:

- Detection: Compare Trigger Collider vs Raycast vs vector calculation (`Vector3.Dot` for forward direction check)
- Synchronization: Use EventChannels (VariableSO) to sync flashlight on/off state and angle

You can extend this prototype:

- Combine with Lv.1 for accurate light detection
- Combine with Lv.3 for enemies visible only in light

---

## Project structure

```text
Assets/
├── _Common/              # Shared scripts, materials, URP settings
│   ├── Materials/        # Reflective, matte, emissive materials
│   ├── Scripts/          # Gizmo drawing, FPS controller, debug UI
│   └── Settings/         # Shared URP Asset for Forward/Deferred switching
├── 01_Reflection/        # Lv.1 Laser reflection
├── 02_RenderingTest/     # Lv.2 Rendering load experiment
├── 03_Stencil/           # Lv.3 Stencil mask effects
├── 04_ShadowMesh/        # Lv.4 Dynamic shadow mesh
├── 05_Rain/              # GPU-based rain rendering (Compute Shader + RenderGraph)
└── 06_DitherTransparency/# Dither transparency (planned)
```

---

## Benefits of unified repository

- Reduced setup overhead: Render Pipeline settings and packages configured once
- Asset reuse: Share PlayerController and debug UI across experiments
- Easy comparison: Switch scenes to reference past implementations

---

## Scope boundaries

This project focuses on game mathematics, rendering tricks, and optimization experiments.

For architecture patterns and data coupling systems, see the EventChannels sample project.
