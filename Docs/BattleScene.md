# Battle scene layout and pixel art

## Hierarchy

```text
Battle
├─ Cameras
│  └─ BattleCamera                 Orthographic Pixel Perfect Camera
├─ Environment
│  ├─ Background                   Cave backdrop
│  └─ Battlefield_Ground           Upright foreground floor
├─ PlayerUnits
│  ├─ Drilldozer
│  ├─ Mecha
│  └─ Tank
├─ EnemyUnits
│  ├─ Golem
│  └─ Boulder
├─ BattleSystems
│  ├─ BattleManager
│  └─ EventSystem
├─ Templates
│  └─ CombatantUI_Template         Inactive scene template
├─ BattleCanvas                   Screen-space HUD and Specials
├─ WorldSpaceBattleCanvas         Action menu and tooltip
├─ SelectionCursor                World-space menu and target cursor
└─ ProgressionManager             Persistent root; must stay root for DontDestroyOnLoad
```

The grouping objects have identity transforms. Combatant and UI script references still point to their existing objects.

## Pixel-perfect settings

- **BattleCamera:** Built-in `UnityEngine.U2D.PixelPerfectCamera` (`com.unity.2d.pixel-perfect` 5.1.1), orthographic, 100 assets PPU, 1920×1080 reference, pixel snapping on, both crop axes on, no upscale render texture.
- **Game sprites:** `Assets/Sprites/Enemies.png`, `Placeholders.png`, `Scene.png`, and `UI.png` use 100 PPU, Point filtering, no mipmaps, and uncompressed imports.
- **Quality:** Anti-aliasing and anisotropic filtering are disabled across all quality tiers.
- **BattleCanvas:** Screen Space Overlay, Pixel Perfect enabled, 960×540 Canvas Scaler reference. This gives a 2× UI scale at the target 1920×1080 resolution.
- **Environment:** The ground faces the camera and the backdrop is scaled to frame the orthographic view. Both were checked with a 1920×1080 camera render.

Use a **1920×1080, 16:9 Game view** to inspect the intended framing. The pixel-perfect camera uses black bars at larger non-matching resolutions and crops the view below the reference size. Map and Camp still use their existing perspective cameras; their framing needs a separate pass before applying orthographic pixel-perfect cameras there.
