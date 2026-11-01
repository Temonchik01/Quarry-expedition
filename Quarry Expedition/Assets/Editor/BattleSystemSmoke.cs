using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BtlSys.Editor
{
    public static class BattleSystemSmoke
    {
        [MenuItem("Tools/Quarry Expedition/Validate Battle System")]
        public static void Run()
        {
            CharacterData heroData = ScriptableObject.CreateInstance<CharacterData>();
            CharacterData enemyData = ScriptableObject.CreateInstance<CharacterData>();
            AttackData basic = ScriptableObject.CreateInstance<AttackData>();
            AttackData charge = ScriptableObject.CreateInstance<AttackData>();
            AttackData priority = ScriptableObject.CreateInstance<AttackData>();
            GameObject heroObject = new GameObject("Battle Smoke Hero");
            GameObject enemyObject = new GameObject("Battle Smoke Enemy");
            SpriteRenderer enemySprite = enemyObject.AddComponent<SpriteRenderer>();

            try
            {
                heroData.maxHealth = enemyData.maxHealth = 100;
                heroData.defense = enemyData.defense = 20;
                heroData.attackPower = enemyData.attackPower = 30;
                heroData.maxAmmo = enemyData.maxAmmo = 20;
                heroData.speed = 10;
                enemyData.speed = 40;
                basic.attackName = "Basic";
                basic.power = 5;
                basic.accuracy = 100;
                basic.hits = 1;
                basic.defensePercent = 100;
                heroData.basicAttack = enemyData.basicAttack = basic;

                Combatant hero = heroObject.AddComponent<Combatant>();
                Combatant enemy = enemyObject.AddComponent<Combatant>();
                hero.data = heroData;
                hero.isPlayer = true;
                enemy.data = enemyData;
                hero.Initialize();
                enemy.Initialize();

                var team = new List<Combatant> { hero };
                var opponents = new List<Combatant> { enemy };
                var resolver = new BattleActionResolver();
                resolver.Resolve(BattleAction.UseAttack(hero, basic, enemy), team, opponents, _ => { });
                Check(enemy.currentHP == 85, "basic damage uses attack, power, and defense");

                Check(enemy.ApplyStatusEffect(StatusEffectType.Corrosion), "first status applies");
                Check(enemy.currentDefense == 0, "status penalty is clamped at zero");
                Check(enemySprite.color.g > enemySprite.color.r, "corrosion tints the unit green: " + enemySprite.color);
                Check(!enemy.ApplyStatusEffect(StatusEffectType.Ignite), "different status cannot replace it");
                enemy.CompleteTurn();
                Check(enemy.statusEffects[0].remainingTurns == 2, "status ticks after a turn");
                Check(enemy.ApplyStatusEffect(StatusEffectType.Corrosion), "same status refreshes");
                Check(enemy.statusEffects[0].remainingTurns == 3, "refresh restores three turns");
                enemy.CompleteTurn();
                enemy.CompleteTurn();
                enemy.CompleteTurn();
                Check(enemy.statusEffects.Count == 0 && enemy.currentDefense == 20, "status expires cleanly");
                Check(enemySprite.color == Color.white, "expired status restores the original color");
                Check(enemy.ApplyStatusEffect(StatusEffectType.Ignite), "ignite applies");
                Check(enemySprite.color.r > enemySprite.color.g, "ignite tints the unit red");
                enemy.CompleteTurn(); enemy.CompleteTurn(); enemy.CompleteTurn();
                Check(enemy.ApplyStatusEffect(StatusEffectType.Paralysis), "paralysis applies");
                Check(enemySprite.color.r > enemySprite.color.b && enemySprite.color.g > enemySprite.color.b,
                    "paralysis tints the unit yellow");
                enemy.CompleteTurn(); enemy.CompleteTurn(); enemy.CompleteTurn();

                hero.ModifyStat(StatType.Speed, 40);
                Check(hero.currentSpeed == 50, "temporary modifier applies");
                hero.CompleteTurn();
                hero.CompleteTurn();
                hero.CompleteTurn();
                Check(hero.currentSpeed == 10, "temporary modifier expires");

                priority.hasPriority = true;
                var actions = new List<BattleAction>
                {
                    BattleAction.UseAttack(enemy, basic, hero),
                    BattleAction.UseAttack(hero, priority, enemy)
                };
                actions[0].Sequence = 0;
                actions[1].Sequence = 1;
                actions.Sort(new BattleTurnOrder());
                Check(actions[0].User == hero, "priority outranks speed");

                charge.attackName = "Charge";
                charge.power = 25;
                charge.accuracy = 100;
                charge.ammoCost = 5;
                charge.hits = 1;
                charge.requiresCharge = true;
                charge.defensePercent = 50;
                BattleAction queued = null;
                resolver.Resolve(BattleAction.UseAttack(hero, charge, enemy), team, opponents, action => queued = action);
                Check(queued != null && enemy.currentHP == 85 && hero.currentAmmo == 15,
                    "charge pays ammo and waits");
                resolver.Resolve(queued, team, opponents, _ => { });
                Check(enemy.currentHP == 40, "charged hit uses half defense");

                AttackData reload = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/Data/Attacks/All_Reload.asset");
                Check(reload != null && reload.ammoRestore == 15, "catalog reload asset is configured");
                resolver.Resolve(BattleAction.UseAttack(hero, reload, hero), team, opponents, _ => { });
                Check(hero.currentAmmo == 20, "catalog reload restores ammo without damaging the user");

                Debug.Log("[BattleSystemSmoke] Battle rules passed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(heroObject);
                UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(heroData);
                UnityEngine.Object.DestroyImmediate(enemyData);
                UnityEngine.Object.DestroyImmediate(basic);
                UnityEngine.Object.DestroyImmediate(charge);
                UnityEngine.Object.DestroyImmediate(priority);
            }
        }

        private static void Check(bool condition, string rule)
        {
            if (!condition) throw new InvalidOperationException($"Battle rule failed: {rule}");
        }
    }
}
