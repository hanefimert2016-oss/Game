# ARC FLIGHT • Unity 6 Native 3D Project

This is a **Unity Editor project**, not the older HTML/Canvas prototype.
The source contains a procedural scene builder, an original red-and-gold flying armor, flight and combat scripts, a follow camera, waves of drones, projectiles, armor and energy HUD, and optional Kenney CC0 OBJ skyscrapers.

## Set up on Arch Linux

Requirements: Unity 6 Editor installed via Hub or official Unity CLI, activated Unity license, GPU/display driver, .NET tooling Unity bundles.

\`\`\`bash
# From the repository root:
unity open ./UnityArcFlight
# In Unity: Tools > Arc Flight > Build or Rebuild City (automatic for an empty project)
# Click Play
\`\`\`

Controls: **WASD/arrows** strafing, **Space/W** ascend, **C/S** descend, **Shift** boost, **F/left mouse** repulsors, **E** missiles, **P** pause, **R** restart after game over.

### Unity CLI / MCP

Unity CLI is not the Editor itself and does not grant remote control of a user's PC. Once Unity 6 has opened the project and a license is active:

\`\`\`bash
cd UnityArcFlight
unity auth login
unity pipeline install
unity pipeline list
unity mcp configure codex
unity status
\`\`\`

The CLI documentation may differ by version; check \`unity --help\`. After \`unity pipeline install\`, allow the Editor to recompile and make sure \`unity pipeline list\` reports Pipeline Installed before using MCP.

### Recording *real Unity game footage*

Run Unity Editor in Play mode and use one of the capture scripts from \`../tools/\` in the same desktop session. Those scripts record pixels from the real screen: **they do not generate synthetic gameplay videos**. Wayland needs \`wf-recorder\` and its screen capture permissions; X11 needs \`ffmpeg\` with \`x11grab\`.

### External licensed 3D assets

Kenney City Kit (Commercial) 2.1 — CC0. Downloaded from
https://opengameart.org/content/city-kit-commercial
via GitHub Actions, select OBJ models and import into \`Assets/ArcFlight/Vendor/Kenney/\`.
Asset importer will use the OBJ models as actual building meshes; it falls back to Unity procedural buildings when unavailable. No premium assets or copyrighted Iron Man film models are used.

## Status

C# and project files committed. Do not treat a passed GitHub static check as a Unity compilation, license activation, GUI play-test, or video recording. Only real Unity Editor runs can verify that. 
