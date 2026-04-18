# Stencil

## Purpose

Closure-style visibility effect using the stencil buffer. A mask mesh writes a stencil value; hidden content renders only where that value is present. Explores stencil buffer operations and render queue ordering in URP.

---

## Features

- **Stencil Write Pass**: Mask geometry marks pixels in the stencil buffer without drawing color
- **Stencil Read Pass**: Hidden content renders only in marked pixels
- **Render Queue Ordering**: Mask (Geometry-1) always evaluates before content (Geometry)
- **URP-safe bit isolation**: Uses bit 1 only via `Ref 2`, `WriteMask 2`, and `ReadMask 2` to avoid URP deferred bits 4-7, stencil LOD bits 2-3, and the XR motion-vector bit 0 path

---

## Architecture

```
StencilMask.shader  (Queue = Geometry-1)
    -> Writes stencil bit 1, ColorMask 0, ZWrite Off
            |
            v
StencilContent.shader  (Queue = Geometry)
    -> Renders only where bit 1 is set (Comp Equal)
```

No scripts are required. Camera movement is handled by `FlyCameraController` from `_Common`.

---

## Shaders

| Shader | Description |
|--------|-------------|
| `StencilMask.shader` | Invisible mesh that writes stencil bit 1. Cull Off so all faces write. |
| `StencilContent.shader` | Unlit colored/textured surface rendered only inside the mask area. |

---

## Scene Setup

The following are already configured in `Scenes/Stencil.unity`:

### Layers

| Layer | Index | Purpose |
|-------|-------|---------|
| `StencilMask` | 8 | Mask geometry (sphere, camera child) |
| `StencilContent` | 9 | Hidden content quads on the wall |

### Materials

| Material | Shader | Settings |
|----------|--------|---------|
| `StencilMask.mat` | `Prism/Stencil/Mask` | No properties exposed |
| `StencilContent.mat` | `Prism/Stencil/Content` | Base Color = Yellow |

### Scene Hierarchy

```
Main Camera
├── FlyCameraController (+ PlayerInput with CameraActions, Send Messages)
└── StencilMask (Sphere, layer: StencilMask)
Directional Light
Floor (Plane)
Wall (Cube)
HiddenContent_Center (Quad, layer: StencilContent, Yellow)
HiddenContent_Left   (Quad, layer: StencilContent, Yellow)
HiddenContent_Right  (Quad, layer: StencilContent, Cyan)
```

### Controls

| Key | Action |
|-----|--------|
| `WASD` | Move camera |
| `Mouse` | Look around |
| `Shift` | Sprint |
| `Space` / `Ctrl` | Ascend / Descend |
| `Esc` | Unlock cursor |
| `Left Click` | Lock cursor |

### Render Queue Order

Mask material queue (1999, Geometry-1) is lower than content material queue (2000, Geometry). Unity executes lower queue values first.

---

## How the Effect Works

```
Frame render order:
  1. Mask mesh (queue 1999) -> Stencil buffer: bit 1 is set on pixels covered by the cone
  2. Wall mesh  (queue 2000) -> Stencil buffer: unchanged, renders normally
  3. Content mesh (queue 2000, Stencil Comp Equal) -> Renders only where stencil bit 1 is set
```

Moving the mask object at runtime (e.g., rotating the flashlight) changes which stencil pixels are written each frame, revealing different portions of the hidden content.

---

## Alternative: Render Objects Renderer Feature

Instead of embedding stencil settings in shaders, you can control stencil via URP's built-in **Render Objects** Renderer Feature. This lets you use Shader Graph materials and apply stencil at the renderer level.

**Renderer Feature setup:**

1. Open the active URP Renderer Data asset
2. Add **Render Objects** -> name it `Stencil Mask Writer`
   - Layer Mask: `StencilMask`
   - Stencil: Ref = 2, Read Mask = 2, Write Mask = 2, Comp = Always, Pass = Replace
   - Override Material: a material with `ColorMask 0` shader
   - Insert: Before Rendering Opaques
3. Add **Render Objects** -> name it `Stencil Content`
   - Layer Mask: `StencilContent`
   - Stencil: Ref = 2, Read Mask = 2, Comp = Equal
   - Insert: After Stencil Mask Writer

This approach decouples stencil configuration from the shader itself.

> Note: For this project's Unity 6 / URP 17.x setup, hand-written HLSL shaders are the direct path for embedding stencil state in the shader. Use Render Objects when the visible material should stay in Shader Graph.
> The embedded shaders are intended for the project's active Forward+ renderer. If the scene is switched to a Deferred renderer, prefer the Render Objects setup because URP's GBuffer and forward-only passes manage stencil state internally.

---

## Future Considerations

### Gameplay
- [ ] Input-driven flashlight rotation (mouse look)
- [ ] Multiple mask objects with different stencil reference values

### Visual Improvements
- [ ] Emissive hidden content for a UV-glow look
- [ ] Animated reveal (mask scale animation)

### Extensions
- [ ] Combine with Lv.1 Laser: reveal content only at laser hit points
- [ ] Portal effect using stencil (Impossible Geometry)

---

## Dependencies

- Unity 6
- URP (Universal Render Pipeline) 17.x

---

## References

- [URP Universal Renderer asset](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/urp-universal-renderer.html)
- [Stencil buffer operations (ShaderLab)](https://docs.unity3d.com/Manual/SL-Stencil.html)
- [Impossible Geometry with Stencils in URP](https://danielilett.com/2022-01-05-tut5-22-impossible-geom-stencils/)
- [Render Objects Renderer Feature](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/renderer-features/renderer-feature-render-objects.html)
