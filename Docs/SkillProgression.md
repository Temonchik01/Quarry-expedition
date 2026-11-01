# Skill upgrades and recycling

This document covers the attack data and run-state APIs. It does not add a Skills menu.

## Attack assets

Each `AttackData` asset stores up to two `AttackUpgradeData` tiers. A tier contains its displayed description, upgrade-point cost, and gameplay deltas: power, ammo cost, extra hits, target scope, and a bonus status effect/chance.

Upgrade definitions live on the shared ScriptableObject. An equipped copy's level and points spent live on `ProgressionManager.EquippedSkillState`, keyed by character and slot. Upgrading one character's copy therefore does not upgrade the same shared asset for every character.

The battle Special button shows a green `+`/`++` from the equipped copy's level. The fight details show its current tier in green and the next available tier, along with effective power, ammo cost, and target scope. The resolver uses the same effective values for target selection, damage, ammo, hits, and upgrade status effects.

The implementation uses **one upgrade point per tier**. The catalog specifies tier effects but not costs; this matches the economy note's example where every filled upgrade cell consumes one point.

## ProgressionManager API

The existing persistent `ProgressionManager` owns skill state during a run:

- `AddSkillUpgradePoints(amount)` adds points awarded by a battle, event, or shop.
- `AddSkillToReserve(attack, count)` adds copies of an `AttackData` asset to the reserve stack.
- `TryInstallSkill(character, slotIndex, attack)` consumes one reserve copy and installs it into an empty slot.
- `TryUpgradeSkill(character, slotIndex)` spends points and raises that installed copy by one tier.
- `RemoveEquippedSkill(character, slotIndex)` returns the copy to reserve and refunds its spent upgrade points.
- `TryRecycleReservedSkill(attack, out scrapAwarded)` removes one reserve copy and awards 10 scrap.
- `TryRecycleEquippedSkill(character, slotIndex, out scrapAwarded)` removes the equipped copy, refunds its invested points, and awards `10 + 15 × upgrade level` scrap.
- `SkillUpgradePoints`, `MetalScrap`, and `SkillReserve` expose read-only values for a future menu.

Basic attacks can be upgraded and appear with plus markers when selected in battle. Basic attacks stay installed and cannot be removed or recycled.

A future menu should ask for confirmation before calling install, remove, or recycle operations. These methods update state immediately and do not present UI themselves.

## Catalog tiers

| Attack | Tier 1 | Tier 2 |
| --- | --- | --- |
| Drilldozer Ram | +5 damage | 10% paralysis chance |
| Drilldozer Charge | +5 damage, +2 ammo cost | — |
| Drilldozer Drill | −5 ammo cost | 20% corrosion chance |
| Drilldozer Fortification | +40 Defense for all allies, +15 ammo cost | — |
| Mecha Jab | +5 damage | 10% paralysis chance |
| Mecha Sonic Punch | +5 damage, +2 ammo cost | — |
| Mecha Drive | +40 Speed for all allies, +15 ammo cost | — |
| Tank Fire | +5 damage | 10% ignite chance |
| Tank Shrapnel | 25% ignite chance per enemy hit | — |
| Tank Preparation | +40 Attack for all allies, +15 ammo cost | — |
| Boulder Roll | +2 hits | — |
| All Zap / Ignite / Corrode | Target all enemies, +5 ammo cost | — |
| Golem / Magician Arc Fist | 10% paralysis chance | — |
| Reload | Not defined | — |

`Tank_Preparetion` preserves the current catalog spelling in its asset file name.
