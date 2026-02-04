# AI Copilot Instructions - Multiplayer Pong Game

## Project Overview
Unity 2D multiplayer Pong game using **Unity Netcode for GameObjects** (v2.8.0) for networking. Two-player competitive game where players control paddles and a ball bounces between goals.

## Architecture

### Core Components
- **GameManager** ([Assets/Scripts/GameManager.cs](Assets/Scripts/GameManager.cs)) - Server-authoritative score tracking and ball spawning using `NetworkVariable<int>` for synchronized state
- **PlayerController** ([Assets/Scripts/PlayerController.cs](Assets/Scripts/PlayerController.cs)) - Input handling; color/position assignment by OwnerClientId (0=blue left, 1=red right)
- **BallController** ([Assets/Scripts/BallController.cs](Assets/Scripts/BallController.cs)) - Ball physics and collision; only server updates physics via `Rigidbody2D`
- **NetworkManagerUI** ([Assets/Scripts/NetworkManagerUI.cs](Assets/Scripts/NetworkManagerUI.cs)) - OnGUI host/client selection

### Networking Pattern
Uses `NetworkBehaviour` inheritance with explicit `IsServer`/`IsOwner` checks:
- **Server-side logic**: Physics updates, score increment, ball spawning (OnNetworkSpawn)
- **Client-side logic**: Input-only on IsOwner, UI updates via NetworkVariable callbacks
- **Synced state**: `NetworkVariable<int> scoreLeft/Right`, `NetworkVariable<bool> isGameOver`

### Key Data Flows
1. Client joins → NetworkManager triggers `OnClientConnectedCallback`
2. 2 clients connected → Server calls `SpawnBall()` to instantiate and spawn NetworkObject
3. Ball collision (server) → Reflects velocity or triggers goal zone
4. Goal scored → Server increments `NetworkVariable`, GameManager callback updates UI
5. First to 5 points → Sets `isGameOver=true`, activates win UI

## Development Conventions

### NetworkBehaviour Checklist
When modifying networked classes:
- Always check `if(!IsServer)` or `if(!IsOwner)` before game logic to prevent double-execution
- Use `NetworkVariable<T>` for synchronized state, not public fields
- Register/unregister callbacks in `OnNetworkSpawn()/OnNetworkDespawn()` to prevent memory leaks (see BallController cleanup)
- Avoid direct Instantiate; use `Instantiate()` + `NetworkObject.Spawn()` for networked objects

### Input Handling Pattern
Input is client-only (`if(!IsOwner) return;` in Update). No RPC calls needed because position is replicated via NetworkTransform.

### Physics Authority
Only the server executes physics (`rigid.linearVelocity`). Clients read replicated position from NetworkTransform.

## Build & Testing
- **Editor**: Use Multiplayer Play Mode (Netcode for GameObjects package provides test mode)
- **Build**: Standard Unity build; requires LAN or relay service for client-server communication
- **Physics System**: 2D Rigidbody; ensure ball prefab has Rigidbody2D + Colliders with correct tags (PlayerGoal1, PlayerGoal2, Wall, Player)

## Key Dependencies
- **Input System** (1.17.0) - Keyboard.current for W/S/Arrow keys
- **Netcode for GameObjects** (2.8.0) - `NetworkBehaviour`, `NetworkVariable`, `NetworkManager`, `NetworkObject`
- **TextMesh Pro** - Score UI rendering
- **2D packages** - Sprites, physics, animation

## Common Patterns to Replicate
1. **Server-authoritative physics**: Never modify Rigidbody on client
2. **Callback-based state sync**: Use `NetworkVariable.OnValueChanged` instead of polling
3. **Graceful cleanup**: Unsubscribe from NetworkManager callbacks in OnNetworkDespawn
4. **Client assignment logic**: Check `OwnerClientId` to differentiate player roles (see PlayerController color assignment)
