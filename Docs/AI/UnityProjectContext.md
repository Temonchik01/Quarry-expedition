# Quarry Expedition Unity context

Analyzed 2026-10-30 from `cb85ce6` on `quarry-expedition-remake` and the local Obsidian design notes.

- **Project root:** `Quarry Expedition/`. Unity `6000.3.27f1`, URP `17.3.0`.
- **Scenes:** `Map` starts the build; `Battle` and `Camp` are enabled. `MapGenerator` loads room scenes additively, and `BattleUI` returns to the map.
- **Input and UI:** both input backends are enabled. Battle UI uses a uGUI Canvas with TextMeshPro labels and legacy mouse/keyboard input.
- **Code:** first-party runtime scripts use the predefined `Assembly-CSharp`, mainly under `Assets/Scripts/`. `ProgressionManager` persists the party's HP and ammo between scenes. Battle logic is under `Assets/Scripts/BtlSys/` and uses `CharacterData` and `AttackData` ScriptableObjects.
- **Tests:** Unity Test Framework is installed. `Assets/Tests/Tests.asmdef` contains generated sample tests. `Assets/Editor/BattleSystemSmoke.cs` checks core battle rules from the Editor menu.
- **Tooling:** no Unity Editor MCP bridge was detected in the project or this Codex session. The installed Unity CLI can run the Editor in batch mode.
- **Constraints:** preserve scene and prefab GUIDs and serialized field names. Battle design notes are drafts; final damage, evasion, and several tie rules are undecided. See `Docs/BattleSystem.md` for current prototype decisions.

Sources: `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `Assets/Scripts/BtlSys/`, `Assets/Scripts/ProgressionManager.cs`, `Assets/Scene/Battle.unity`, and the Obsidian notes in `Game Design/Core Loop.md` and `Mechanics/`.
