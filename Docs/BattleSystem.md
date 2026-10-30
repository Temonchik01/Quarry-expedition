# Battle system

## Unity setup

- `BattleManager` stays on the existing object in `Battle.unity`; it discovers active `Combatant` components in that scene and starts automatically.
- Each `Combatant` references one `CharacterData` asset and has its `isPlayer` flag set. Each `CharacterData` has a `basicAttack` and optional `specials` list. Add new attacks with **Create > BtlSys > AttackData** and reference them from the character asset.
- `BattleUI` keeps the existing Canvas, buttons, HUD prefab, cursor, and TextMeshPro references. Set victory and defeat text in its Inspector. The Items button remains unavailable until an item system exists.
- `ProgressionManager` keeps HP and ammo between battle scenes. The existing map return and restart buttons remain wired by `BattleUI`.

## Script setup and responsibilities

| Script | Unity setup | Responsibility |
| --- | --- | --- |
| `BattleManager` | Keep on the existing Battle scene object | Phases, team discovery, queued actions, victory and defeat events |
| `Combatant` | Keep on each Battle scene fighter; set `data` and `isPlayer` | HP, ammo, temporary stats, one status, change events |
| `BattleUI` | Keep on the existing Canvas; use Inspector references and set victory/defeat text | Player choices and presentation |
| `CharacterData` / `AttackData` | Create and edit assets in the Project window | Character and attack configuration |
| `BattleAction`, `BattleTurnOrder`, `EnemyActionPlanner`, `BattleActionResolver` | No component or Inspector setup | Choices, turn order, enemy AI, and action effects |

## Prototype rules and open decisions

Players choose one action for each living hero before the round resolves. Priority attacks act first, then higher Speed, then higher total character stats. Fully tied actions keep their queued order until a final tie rule is designed. Damage currently uses the existing `max(1, attacker Attack + Power - defender Defense)` formula; the final formula and evasion rule are still open in the Obsidian notes.

Reload restores 15 ammo and costs one turn. Statuses last three turns of the affected combatant, with one status at a time. Reapplying the same status refreshes its duration; another status is ignored. Paralysis has a 50% skip chance on each action attempt. Stat modifiers last three turns of the affected combatant. Defend gives +100 Defense until the next round. Effective Attack, Defense, and Speed are clamped to 0–250.

Charge spends ammo when selected, waits one round, then strikes with 50% of the target's Defense. It is interrupted if its user is defeated; if its target is gone, it strikes the first living opponent. A status applies only after an attack lands. These decisions can be changed in the relevant small scripts when the draft rules are finalized.

## Validation

Run **Tools > Quarry Expedition > Validate Battle System** in the Unity Editor. It checks basic damage, status replacement and expiry, temporary stat expiry, priority, and Charge. The Battle scene still needs a manual Play Mode pass for UI interaction and map transitions.
