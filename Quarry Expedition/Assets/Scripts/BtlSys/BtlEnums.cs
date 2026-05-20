namespace BtlSys
{
    public enum TargetType
    {
        SingleTarget,
        EntireParty,
        Self
    }

    public enum StatType
    {
        Health,
        Defense,
        Attack,
        Ammo,
        Speed
    }

    public enum StatusEffectType
    {
        None,
        Ignite,
        Paralysis,
        Corrosion
    }

    public enum BattleState
    {
        Initializing,
        PlayerInput,
        ActionPhase,
        Resolution,
        Victory,
        Defeat
    }
}
