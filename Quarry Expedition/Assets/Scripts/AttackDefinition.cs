using System.Collections.Generic;

/// <summary>
/// Runtime wrapper around AttackDataSO. Resolves the correct
/// IAttackStrategy and owns attack execution logic.
/// </summary>
public class AttackDefinition
{
    private readonly AttackDataSO _data;
    private readonly IAttackStrategy _strategy;

    public string Name           => _data.AttackName;
    public int    AmmoCost       => _data.AmmoCost;
    public bool   HasTurnPriority => _data.HasTurnPriority;

    public AttackDefinition(AttackDataSO data)
    {
        _data     = data;
        _strategy = AttackStrategyFactory.Create(data);
    }

    /// <summary>
    /// Executes this attack. Returns true if it connected.
    /// </summary>
    public bool Execute(ICharacterUnit attacker, IReadOnlyList<ICharacterUnit> targets)
    {
        if (!attacker.Stats.TryConsumeAmmo(_data.AmmoCost))
        {
            UnityEngine.Debug.Log($"[{attacker.Name}] not enough ammo for {Name}.");
            return false;
        }

        /// attacker.PlayAnimation(UnitAnimationType.Attack);
        return _strategy.Execute(_data, attacker, targets);
    }
}