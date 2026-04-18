# ShadowMesh

## Purpose

Dynamic shadow mesh system where light visibility polygons become physical boundaries. A point light casts radial raycasts to detect obstacles, generates a triangle-fan mesh for the lit area, and updates a `PolygonCollider2D` so shadow edges act as walls. Explores dynamic mesh generation, 2D physics, and URP 2D lighting.

---

## Features

- **Radial Raycast**: 180 rays cast in a full circle to detect obstacle boundaries
- **Dynamic Mesh Generation**: Triangle-fan mesh rebuilt every frame from raycast results
- **Physical Shadow Boundary**: `PolygonCollider2D` updated from the visibility polygon edges
- **Mouse-Driven Light**: Light source follows the mouse cursor through the Unity Input System
- **URP 2D Integration**: Light2D for visual lighting, ShadowCaster2D on obstacles
- **Pipeline Auto-Switch**: `PipelineSwitcher2D` switches to 2D URP on scene load, restores on unload

---

## Architecture

```
FollowMouse2D (mouse cursor -> light position)
    |
    v
ShadowMeshGenerator (MonoBehaviour on LightSource)
    ├── Physics2D.Raycast x 180 (radial, per frame)
    ├── Triangle-fan mesh (MeshFilter)
    ├── PolygonCollider2D (shadow boundary)
    └── Light2D (visual 2D lighting)

PipelineSwitcher2D (on Main Camera)
    └── Swaps GraphicsSettings.defaultRenderPipeline to 2D URP
```

---

## Scripts

| Script | Description |
|--------|-------------|
| `ShadowMeshGenerator.cs` | Radial raycast, mesh generation, collider update |
| `FollowMouse2D.cs` | Moves the light to follow the mouse cursor |
| `PipelineSwitcher2D.cs` | Switches to 2D URP pipeline on scene load |

---

## Scene Setup

The following are already configured in `Scenes/ShadowMesh.unity`:

### Scene Hierarchy

```
Main Camera (Orthographic, size=8)
├── PipelineSwitcher2D (pipeline2DAsset = ShadowMesh2D_RPAsset)
LightSource
├── Light2D (Point, radius=10)
├── ShadowMeshGenerator (180 rays, distance=10, obstacles=Default layer)
├── FollowMouse2D (targetCamera optional; falls back to Main Camera)
├── MeshFilter + MeshRenderer (LightArea.mat)
└── PolygonCollider2D
Obstacle_Center  (BoxCollider2D + ShadowCaster2D)
Obstacle_Left    (BoxCollider2D + ShadowCaster2D)
Obstacle_Top     (BoxCollider2D + ShadowCaster2D)
Obstacle_BottomRight (BoxCollider2D + ShadowCaster2D)
Wall_Top / Wall_Bottom / Wall_Left / Wall_Right (boundaries)
```

### Controls

| Input | Action |
|-------|--------|
| Mouse Move | Move the light source |

---

## Settings (Inspector)

### ShadowMeshGenerator

| Parameter | Description |
|-----------|-------------|
| `Ray Count` | Number of radial rays (default: 180, min: 12) |
| `Max Distance` | Maximum reach of each ray (default: 10) |
| `Obstacle Layers` | LayerMask for shadow-casting objects |

### PipelineSwitcher2D

| Parameter | Description |
|-----------|-------------|
| `Pipeline 2D Asset` | The URP 2D pipeline asset to use for this scene |

---

## How It Works

```
Per frame:
  1. Cast 180 rays from light position in all directions
  2. Record nearest obstacle hit points, ignoring the generated boundary collider
  3. Build triangle-fan mesh: center = light, outer ring = hit points
  4. Update PolygonCollider2D path from outer ring
  5. Light2D illuminates the same area visually
```

Moving the mouse changes the light position. Rays that hit obstacles are shorter, creating shadow shapes behind them. The `PolygonCollider2D` makes the shadow boundary a physical wall.

---

## Future Considerations

### Accuracy
- [ ] Corner-aware raycasting (cast toward obstacle vertices for sharper edges)
- [ ] Sub-pixel edge interpolation

### Performance
- [ ] Dirty flag to skip rebuild when nothing moves
- [ ] Compute Shader for parallel raycast (project plan goal)
- [ ] Job System / Burst for raycast batching

### Gameplay
- [ ] Player with Rigidbody2D that collides with shadow walls
- [ ] Multiple light sources with merged visibility polygons
- [ ] Dynamic obstacles (moving platforms)

---

## Unity 6.5 Migration Note

Unity 6.5 (Beta as of 2026-04) renames `LowLevelPhysics2D` to `PhysicsCore2D` with namespace changes ([Qiita](https://qiita.com/RyotaMurohoshi/items/288df747212e3bc932a8)). When upgrading, verify that `Physics2D.Raycast` and related APIs used in `ShadowMeshGenerator` are not affected by the migration.

---

## Dependencies

- Unity 6
- URP (Universal Render Pipeline) 17.x
- Physics2D module

---

## References

- [Physics2D.Raycast](https://docs.unity3d.com/ScriptReference/Physics2D.Raycast.html)
- [PolygonCollider2D](https://docs.unity3d.com/ScriptReference/PolygonCollider2D.html)
- [Light2D](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/2d-light-types.html)
- [ShadowCaster2D](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/2DShadows.html)
