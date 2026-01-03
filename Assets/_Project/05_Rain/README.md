# Rain

## Purpose

GPU-based rain particle system using Unity 6 RenderGraph API. Explores compute shader particle simulation, frustum culling, and rim lighting techniques.

---

## Features

- **GPU Particle Simulation**: Compute shader handles physics (gravity, wind, respawn)
- **Cylindrical Spawn Area**: Camera-relative spawn with configurable radius and height
- **Frustum Culling**: Only visible rain particles are rendered
- **Multi-Light Support**: Up to 8 Point/Spot lights affect rain (requires `RainAdditionalLight` component)
- **Rim Lighting**: Rain particles glow when illuminated by light sources
- **Sparkle Effect**: Dynamic sparkle based on light contribution

---

## Architecture

```
RainRendererFeature (URP Renderer Feature)
    └── RainRenderPass (ScriptableRenderPass)
            ├── Rain.compute (GPU particle simulation)
            │       ├── InitRain kernel
            │       └── UpdateRain kernel (physics + culling)
            └── Rain.shader (instanced rendering)
                    └── RainLightData (light buffer)
```

---

## Scripts

| Script | Description |
|--------|-------------|
| `RainRendererFeature.cs` | URP Renderer Feature, holds settings |
| `RainRenderPass.cs` | Compute dispatch + DrawMeshInstancedIndirect |
| `RainAdditionalLight.cs` | Marker component for lights affecting rain |
| `RainLightData.cs` | Light data structure for GPU |

---

## Shaders

| Shader | Description |
|--------|-------------|
| `Rain.compute` | Particle simulation (InitRain, UpdateRain kernels) |
| `Rain.shader` | Billboard rendering with rim lighting |

---

## Settings (Inspector)

| Parameter | Description |
|-----------|-------------|
| `Rain Drop Count` | Number of particles (default: 20000) |
| `Spawn Radius` | Horizontal spawn radius from camera |
| `Height Min/Max` | Vertical spawn range relative to camera |
| `Gravity` | Fall acceleration |
| `Cull Distance` | Max render distance from camera |
| `Drop Scale` | Size multiplier for rain drops |
| `Base Alpha` | Alpha when not lit |
| `Lit Alpha` | Alpha when illuminated |

---

## Future Considerations

### Visual Improvements
- [ ] Ground splash effects on collision
- [ ] Mist/fog integration near ground level
- [ ] Motion blur support for fast-moving drops
- [ ] Wet surface shader integration

### Performance
- [ ] LOD system (reduce density/size at distance)
- [ ] Dynamic particle count based on camera speed
- [ ] Hybrid CPU/GPU culling for very large counts

### Weather Variations
- [ ] Snow mode (slower fall, different shape)
- [ ] Sleet/mixed precipitation
- [ ] Wind direction parameter (global wind)
- [ ] Intensity curves for storm simulation

### Technical
- [ ] Extract common code to `.hlsl` (RainDrop struct, RainLightData struct, constants)
- [ ] Shadow-based occlusion (skip rain in shadowed areas like VolumetricFog)
- [ ] Indoor/outdoor occlusion (raycast or volume-based)
- [ ] Multi-camera support
- [ ] VR stereo rendering optimization
- [ ] Integration with VolumetricFog light scattering

---

## Dependencies

- Unity 6 (RenderGraph API)
- URP (Universal Render Pipeline)

---

## References

- [URP RenderGraph Unsafe Pass](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/render-graph-unsafe-pass.html)
- [DrawMeshInstancedIndirect](https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html)
