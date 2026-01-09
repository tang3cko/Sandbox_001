# RenderingTest

## Purpose

Rendering pipeline comparison tool using runtime URP asset switching. Explores Forward, Forward+, and Deferred rendering trade-offs with dynamic light spawning.

---

## Features

- **Runtime Pipeline Switching**: Switch between Forward/Forward+/Deferred without restarting
- **Dynamic Light Spawning**: Add/remove point lights to observe performance impact
- **Performance Stats Display**: Real-time FPS, Draw Calls, Batches, SetPass monitoring
- **Light Animation**: Optional position animation to stress-test dynamic lighting
- **UI Toolkit Integration**: KeyDownEvent-based keyboard input handling

---

## Architecture

```
RenderPipelineSwitcher (switches URP Assets)
    └── GraphicsSettings.defaultRenderPipeline

LightSpawner (manages point lights)
    └── Light[] (spawned dynamically)

RenderingTestUI (UI Toolkit controller)
    ├── UIDocument + KeyDownEvent (keyboard input)
    ├── RenderingTestHUD.uxml
    └── RenderingTestUI.uss / Common.uss
```

---

## Scripts

| Script | Description |
|--------|-------------|
| `RenderPipelineSwitcher.cs` | Switches between URP Pipeline Assets at runtime |
| `LightSpawner.cs` | Spawns and manages multiple point lights |
| `UI/RenderingTestUI.cs` | UI Toolkit controller with KeyDownEvent input |

---

## UI (UI Toolkit)

| File | Description |
|------|-------------|
| `UI/UXML/RenderingTestHUD.uxml` | UI structure (mode dropdown, light slider, stats) |
| `UI/USS/Common.uss` | Design tokens (colors, fonts, spacing) |
| `UI/USS/RenderingTestUI.uss` | BEM-compliant component styles |

---

## Settings (Inspector)

### RenderPipelineSwitcher

| Parameter | Description |
|-----------|-------------|
| `Pipeline Configs` | Array of URP Assets for each rendering mode |

### LightSpawner

| Parameter | Description |
|-----------|-------------|
| `Initial Light Count` | Lights spawned on start (default: 10) |
| `Max Light Count` | Maximum lights allowed (default: 200) |
| `Light Intensity` | Brightness of spawned lights |
| `Light Range` | Range of spawned lights |
| `Enable Shadows` | Whether lights cast shadows |
| `Spawn Area Size` | Volume for random light placement |
| `Animate Lights` | Enable position animation |

---

## Usage

1. Create URP Assets for Forward, Forward+, Deferred (Settings folder contains examples)
2. Create scene with floor and objects
3. Add `RenderPipelineSwitcher` and `LightSpawner` components
4. Add `UIDocument` with `RenderingTestHUD.uxml` and `RenderingTestUI` component
5. Assign `PrismPanelSettings` to UIDocument's Panel Settings
6. Link references to RenderPipelineSwitcher and LightSpawner

### Controls

| Key | Action |
|-----|--------|
| `R` | Cycle rendering mode |
| `1` / `2` / `3` | Direct select Forward / Forward+ / Deferred |
| `Up` / `Down` | Add / Remove lights |
| `A` | Toggle light animation |
| `F1` | Toggle UI visibility |

---

## Future Considerations

### Experiments
- [ ] Profiler integration for detailed breakdown
- [ ] Transparent object stress test (Deferred limitation)
- [ ] Shadow cost comparison per pipeline

### Visual Improvements
- [ ] Graph visualization for FPS history
- [ ] Heat map of light density

### Extensions
- [ ] HDRP comparison mode
- [ ] Mobile build performance logging

---

## Dependencies

- Unity 6
- URP (Universal Render Pipeline)

---

## References

- [URP Rendering Paths](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/rendering/rendering-paths.html)
- [Forward+ Rendering](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/rendering/forward-plus-rendering-path.html)
- [Deferred Rendering](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/rendering/deferred-rendering-path.html)
