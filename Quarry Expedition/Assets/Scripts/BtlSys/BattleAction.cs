using UnityEngine;

namespace BtlSys
{
    public enum BattleActionType
    {
        Attack,
        Defend,
        Reload
    }

    public sealed class BattleAction
    {
        public Combatant User { get; }
        public Combatant Target { get; }
        public AttackData Attack { get; }
        public BattleActionType Type { get; }
        public bool IsCharged { get; }
        public int UpgradeLevel { get; }
        public int Sequence { get; set; }

        private BattleAction(Combatant user, BattleActionType type, AttackData attack, Combatant target, bool isCharged, int upgradeLevel)
        {
            User = user;
            Type = type;
            Attack = attack;
            Target = target;
            IsCharged = isCharged;
            UpgradeLevel = upgradeLevel;
        }

        public static BattleAction UseAttack(Combatant user, AttackData attack, Combatant target, int upgradeLevel = 0)
        {
            return new BattleAction(user, BattleActionType.Attack, attack, target, false, upgradeLevel);
        }

        public static BattleAction Defend(Combatant user)
        {
            return new BattleAction(user, BattleActionType.Defend, null, null, false, 0);
        }

        public static BattleAction Reload(Combatant user)
        {
            return new BattleAction(user, BattleActionType.Reload, null, null, false, 0);
        }

        public BattleAction AsCharged()
        {
            return new BattleAction(User, Type, Attack, Target, true, UpgradeLevel);
        }

        public bool HasPriority => Attack != null && Attack.hasPriority;

        public int Speed => User != null ? User.currentSpeed : 0;

        // Health and ammo are compared at their maximum values as character stats.
        public int StatTotal => User != null && User.data != null
            ? User.data.maxHealth + User.currentDefense + User.currentAttack + User.data.maxAmmo + User.currentSpeed
            : 0;
    }
}
