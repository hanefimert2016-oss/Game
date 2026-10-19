# ARC SQUAD — Neon Village (Unity 6 / 3D co-op)

**Art direction:** original non-photoreal colorful sci-fi armored flyers, pastel tiny houses, pitched roofs, candy trees, glowing roads, sunset skyline, cel-shaded materials. Not a licensed Iron Man character.

**Multiplayer architecture:** Unity Netcode for GameObjects 2.7 + Unity Transport. Direct-IP UDP hosting, maximum eight players. Server-authoritative movement, projectile hit evaluation, shield, energy, drone spawns, score and waves. Clients send only input intentions to host. NetworkTransform distributes host-controlled positions to remote peers. Dynamic drone prefab registered on each peer before networking.

## Requirements and first launch

- Unity **6000.3.7f1** with a valid Unity license, Internet for first package installation
- Built-in Render Pipeline (procedural Toon shader in `Assets/ArcSquad/Shaders/`)
- Active Input Handling: **Both** or **Input Manager (Old)** under Edit → Project Settings → Player; restart Editor if prompted.

1. Open **ArcSquadUnity/** using Unity Hub or `unity open ./ArcSquadUnity`.
2. Wait for Package Manager to install Netcode for GameObjects and compile all scripts.
3. The Editor bootstrap builds the initial `Assets/ArcSquad/Scenes/NeonVillage.unity`. If that didn't happen use **Tools → Arc Squad → Rebuild Stylized Village**. It generates and saves native prefabs, materials, roof mesh and the scene.
4. Open the scene and click **Play**.
5. Press HOST GAME. A second build/Editor instance can JOIN GAME with the host's LAN IPv4 and port **7777 UDP**.

## Controls
WASD fly forward/back/left/right, Space ascend, C descend, Shift turbo, mouse to aim with locked cursor, F or left click repulsor, R or right click rocket, Tab unlock cursor. The first pilot spawns near (0,15,0).

## Network scope and limitations

- LAN / direct IP multiplayer; **not** cloud matchmaking or Unity Relay. To play over the internet, configure UDP 7777 forwarding on the host **or** use a trusted private VPN such as Tailscale. Do not open unrelated firewall ports.
- Cooperative PvE against network-synchronized flying drones (no player-vs-player damage).
- No accounts, database, voice chat, public dedicated-server fleet, cross-platform build artifacts or paid assets.
- No completed Unity Editor Play-mode test is claimed yet: CI uses .NET game-rule tests and static C# syntax/asset checks only. Unity's Editor license has previously blocked headless GUI testing.
- The first load can be slow because Package Manager imports Unity Netcode packages, shaders and several hundred decorative primitives.

## Project layout
`Scripts/SquadRules.cs` - deterministic gameplay rules tested under .NET without Unity
`Scripts/ArcPilot.cs` - authoritative network player, sync shield and movement
`Scripts/ArcDrone.cs` - enemy AI and authoritative hits
`Scripts/ArcMission.cs` - co-op waves/spawning
`Scripts/ArcLobby.cs` - host/join + HUD
`Scripts/ArcCameraRig.cs` - owner-follow 3rd person camera
`Scripts/ArcEffects.cs` - tracer FX
`Editor/ArcSquadSceneFactory.cs` - saved Unity scene, village, prefab and material generation
`Shaders/NeonToon.shader` - stylized stepped lighting and neon rim
`Art/Kenney/Models/` - 24 CC0 OBJ models sourced from Kenney City Kit, optional if missing

## Tests
```bash
dotnet run --project tests/ArcSquadRules.csproj -c Release
python3 tests/check_project.py
```
The C# script parser is a *static check*, not evidence that Unity's Package Manager imported all APIs correctly.

## Recording actual editor gameplay on Arch Linux
Use OBS Studio → PipeWire Screen Capture (Wayland) to capture the actual Game view in Play mode. The presence of a script or a demo render is not an actual gameplay recording.

## Licensing
The new suit/level generator/shader code is original. Kenney City Kit mesh assets, if vendored, are CC0 1.0. They are not from the Iron Man film.
