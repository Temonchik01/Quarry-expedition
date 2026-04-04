// AttackStrategyFactory.cs
public static class AttackStrategyFactory
{
    public static IAttackStrategy Create(AttackDataSO data)
    {
        if (data.IsUtility) return new UtilityStrategy();

        return data.TargetType switch
        {
            AttackTargetType.AllEnemies => new AllTargetsStrategy(),
            AttackTargetType.AllAllies  => new AllTargetsStrategy(),
            _                           => new SingleTargetStrategy()
        };
    }
}