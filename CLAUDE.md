# Devil's Diner - Claude Code Project Guide

## Project Overview
- **Game Title**: Devil's Diner
- **Engine**: Unity 6 (6000.3.10f1)
- **Language**: C# (.NET Standard 2.1 / Unity compatible)
- **VCS**: Git (Plastic SCM ignore rules migrated to .gitignore)

## Directory Structure
```
Assets/
  Scenes/          # Unity scenes
  Scripts/         # All C# game scripts
    Editor/        # Editor-only scripts (CustomEditor, PropertyDrawer, etc.)
  Settings/        # Unity Render Pipeline / project settings assets
  Prefabs/         # Reusable prefabs
  Materials/       # Materials and shaders
  Textures/        # Sprites, UI textures, etc.
  Audio/           # SE and BGM
  Animations/      # Animation clips and controllers
  UI/              # UI Toolkit (UXML/USS) or Canvas-based UI assets
```

## Coding Conventions

### Naming
- **Classes / Structs / Enums**: PascalCase (e.g., `PlayerController`)
- **Public methods / properties**: PascalCase
- **Private fields**: camelCase with `_` prefix (e.g., `_moveSpeed`)
- **Local variables / parameters**: camelCase
- **Constants**: UPPER_SNAKE_CASE (e.g., `MAX_HEALTH`)
- **File names** must match the class name exactly (Unity requirement)

### Code Style
- Use `[SerializeField]` instead of public fields for Inspector exposure
- Prefer `TryGetComponent` over `GetComponent` for null safety
- Use `CompareTag("TagName")` instead of `gameObject.tag == "TagName"`
- Avoid `Find` / `FindObjectOfType` at runtime; cache references in `Awake()` or use dependency injection
- Keep `Update()` lightweight; offload heavy logic to coroutines or async methods
- Use `#region` sparingly; prefer small, focused classes
- One MonoBehaviour per file

### Architecture Principles
- Favor composition over inheritance
- Use ScriptableObjects for shared data and configuration
- Separate game logic from presentation (MVC/MVP where practical)
- Use events (`System.Action`, `UnityEvent`) for decoupling

## Unity-Specific Rules
- **Never modify** files under `Library/`, `Temp/`, `Logs/`, or `UserSettings/`
- **Never modify** `.meta` files manually — Unity auto-generates them
- Keep scene files minimal; prefer prefab-based workflows
- Use Assembly Definition files (`.asmdef`) for larger modules to improve compile times
- Target platform: TBD (default Standalone)

## Build & Test
- Unity Test Framework: `Edit > Window > General > Test Runner`
- Play Mode tests go in `Assets/Tests/PlayMode/`
- Edit Mode tests go in `Assets/Tests/EditMode/`
- No CLI build pipeline configured yet (future: Unity batch mode)

## Important Notes for Agents
- This is a Unity project — **do not run `dotnet build`** or `msbuild` directly
- C# scripts are compiled by Unity Editor; changes take effect on domain reload
- When creating new scripts, always include the `using UnityEngine;` directive
- When creating Editor scripts, wrap with `#if UNITY_EDITOR` or place in `Editor/` folder
- Avoid modifying ProjectSettings YAML files directly unless absolutely necessary
