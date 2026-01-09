# Reflection

## Purpose

Laser reflection puzzle system using `Physics.Raycast` and `Vector3.Reflect`. Explores trajectory calculation fundamentals and line-of-sight detection basics.

---

## Features

- **Raycast-based Detection**: Detects hit points and surface normals
- **Reflection Calculation**: Uses `Vector3.Reflect` for accurate bounce direction
- **LineRenderer Visualization**: Real-time laser path rendering
- **Selective Reflection**: Only objects with `Reflector` component reflect the laser
- **Configurable Settings**: Max reflections, distance, layer mask, visual properties

---

## Architecture

```
LaserEmitter (MonoBehaviour)
    ├── Physics.Raycast (hit detection)
    ├── Vector3.Reflect (direction calculation)
    ├── TryGetComponent<Reflector> (selective reflection)
    └── LineRenderer (visualization)

Reflector (Marker Component)
    └── Attached to reflective surfaces
```

---

## Scripts

| Script | Description |
|--------|-------------|
| `LaserEmitter.cs` | Raycast loop, reflection calculation, LineRenderer control |
| `Reflector.cs` | Marker component for reflective surfaces |

---

## Shaders

| Shader | Description |
|--------|-------------|
| `Laser.shader` | URP unlit shader with HDR color and vertex color support |

---

## Settings (Inspector)

### LaserEmitter

| Parameter | Description |
|-----------|-------------|
| `Max Distance` | Maximum travel distance per segment (default: 100) |
| `Max Reflections` | Maximum number of reflections (default: 10) |
| `Layer Mask` | Layers to detect with raycast |
| `Laser Width` | Width of the laser beam |
| `Laser Color` | Color of the laser (affects LineRenderer) |
| `Draw Gizmos` | Enable debug visualization in Scene view |

---

## Usage

1. Create an empty GameObject and add `LaserEmitter` component
2. Create a material using `Prism/Laser` shader
3. Assign the material to the auto-added `LineRenderer`
4. Place objects with `Collider` in the scene
5. Add `Reflector` component to objects that should reflect the laser

---

## Future Considerations

### Gameplay (Plan B)
- [ ] Interactive mirror placement (drag & drop)
- [ ] Mirror rotation controls
- [ ] Target detection (goal object)

### Visual Improvements
- [ ] Hit point particle effects
- [ ] Glow/bloom on laser
- [ ] Animated laser texture

### Extensions
- [ ] Flashlight prototype integration (Recommended starting point)
- [ ] Combine with Lv.3 Stencil for visibility effects

---

## Dependencies

- Unity 6
- URP (Universal Render Pipeline)

---

## References

- [Physics.Raycast](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html)
- [Vector3.Reflect](https://docs.unity3d.com/ScriptReference/Vector3.Reflect.html)
- [LineRenderer](https://docs.unity3d.com/ScriptReference/LineRenderer.html)
