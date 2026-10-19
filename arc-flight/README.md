# ARC FLIGHT // Mark Zero

Playable Iron-Man-inspired aerial arcade action prototype. Original geometric armor and procedural city (no film assets or copyrighted character models).

## Play
Open `arc-flight/index.html` using a local HTTP server (ES modules will not work correctly from `file://`):

```bash
python3 -m http.server 8000
# visit http://localhost:8000/arc-flight/
```

No npm, paid assets, external CDN, Unity Editor, or API key is required.

## Controls
- WASD / arrows: horizontal flight; W/Space up and S/C down.
- Shift: afterburner (uses energy).
- F or left mouse: repulsors (hold to repeat).
- E: homing missile if a target is locked (20 energy).
- P: pause. R: restart after game over. M: mute.
- On mobile: touch D-pad, FIRE, BOOST, MISSILE.

## Gameplay
Infinite scrolling procedural 3D-looking city drawn with a software perspective painter, reactive HUD, waves of drones, projectiles, missiles, armor integrity, reactor energy, score, repair rewards, and game-over/restart. Rendering uses HTML Canvas 2D perspective projection (not Unity/Blender or true WebGL); 3D polygons are created and projected on each frame.

## Verification
```bash
node --check arc-flight/game.mjs
node --check arc-flight/main.mjs
node --test arc-flight/tests/game.test.mjs
```

GitHub Actions runs tests on the branch. Browser runtime/screenshot may require a browser environment. This is a prototype, not an exported Unity game.

## Status
This lives in an isolated branch to preserve the existing `Game` project's main branch.
