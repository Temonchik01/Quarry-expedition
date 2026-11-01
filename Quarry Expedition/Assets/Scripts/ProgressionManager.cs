using System;
using System.Collections.Generic;
using BtlSys;
using UnityEngine;

[Serializable]
public sealed class AttackStackState
{
    public AttackData attack;
    [Min(0)] public int count;
}

[Serializable]
public sealed class EquippedSkillState
{
    public string characterName;
    public int slotIndex;
    public bool isBasicAttack;
    public AttackData attack;
    [Min(0)] public int upgradeLevel;
    [Min(0)] public int upgradePointsSpent;
}

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    [System.Serializable]
    public class CombatantState
    {
        public string characterName;
        public int hp;
        public int ammo;
        public int maxHP;
        public int maxAmmo;
    }

    public List<CombatantState> playerStates = new List<CombatantState>();
    public bool hasHealedInCamp = false;

    [Header("Skill progression")]
    [SerializeField] private List<AttackStackState> skillReserve = new List<AttackStackState>();
    [SerializeField] private List<EquippedSkillState> equippedSkills = new List<EquippedSkillState>();
    [SerializeField] private List<string> initializedSkillLoadouts = new List<string>();
    [SerializeField, Min(0)] private int skillUpgradePoints;
    [SerializeField, Min(0)] private int metalScrap;

    public int SkillUpgradePoints => skillUpgradePoints;
    public int MetalScrap => metalScrap;
    public IReadOnlyList<AttackStackState> SkillReserve => skillReserve;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SavePlayerState(List<Combatant> team)
    {
        playerStates.Clear();
        foreach (Combatant c in team)
        {
            if (c == null || c.data == null) continue;
            playerStates.Add(new CombatantState
            {
                characterName = c.data.characterName,
                hp = c.currentHP,
                ammo = c.currentAmmo,
                maxHP = c.data.maxHealth,
                maxAmmo = c.data.maxAmmo
            });
        }
    }

    public void LoadPlayerState(List<Combatant> team)
    {
        foreach (Combatant c in team)
        {
            if (c == null || c.data == null) continue;
            CombatantState state = playerStates.Find(s => s.characterName == c.data.characterName);
            if (state != null) c.LoadState(state.hp, state.ammo);
        }
    }

    public void InitializeSkillLoadouts(List<Combatant> team)
    {
        foreach (Combatant combatant in team)
            if (combatant != null && combatant.data != null) EnsureSkillLoadout(combatant.data);
    }

    public IReadOnlyList<EquippedSkillState> GetEquippedSpecials(CharacterData character)
    {
        EnsureSkillLoadout(character);
        var result = new List<EquippedSkillState>();
        if (character == null) return result;
        foreach (EquippedSkillState skill in equippedSkills)
            if (skill.characterName == character.characterName && !skill.isBasicAttack && skill.attack != null)
                result.Add(skill);
        result.Sort((a, b) => a.slotIndex.CompareTo(b.slotIndex));
        return result;
    }

    public int GetEquippedUpgradeLevel(CharacterData character, AttackData attack)
    {
        EnsureSkillLoadout(character);
        EquippedSkillState skill = FindEquipped(character, attack);
        return skill == null ? 0 : skill.upgradeLevel;
    }

    public bool IsEquipped(CharacterData character, AttackData attack)
    {
        EnsureSkillLoadout(character);
        return FindEquipped(character, attack) != null;
    }

    public bool IsEquipped(CharacterData character, AttackData attack, int upgradeLevel)
    {
        EnsureSkillLoadout(character);
        if (character == null || attack == null) return false;
        return equippedSkills.Exists(x => x.characterName == character.characterName &&
            x.attack == attack && x.upgradeLevel == upgradeLevel);
    }

    public void AddSkillUpgradePoints(int amount)
    {
        if (amount > 0) skillUpgradePoints += amount;
    }

    public void AddSkillToReserve(AttackData attack, int count = 1)
    {
        if (attack == null || count <= 0) return;
        AttackStackState stack = skillReserve.Find(x => x.attack == attack);
        if (stack == null)
        {
            stack = new AttackStackState { attack = attack };
            skillReserve.Add(stack);
        }
        stack.count += count;
    }

    public bool TryInstallSkill(CharacterData character, int slotIndex, AttackData attack)
    {
        EnsureSkillLoadout(character);
        if (character == null || attack == null || slotIndex < 0) return false;
        AttackStackState stack = skillReserve.Find(x => x.attack == attack && x.count > 0);
        if (stack == null) return false;

        EquippedSkillState slot = FindSlot(character.characterName, slotIndex, false);
        if (slot == null)
        {
            slot = new EquippedSkillState { characterName = character.characterName, slotIndex = slotIndex };
            equippedSkills.Add(slot);
        }
        if (slot.attack != null) return false;

        slot.attack = attack;
        slot.upgradeLevel = 0;
        slot.upgradePointsSpent = 0;
        stack.count--;
        if (stack.count == 0) skillReserve.Remove(stack);
        return true;
    }

    public bool TryUpgradeSkill(CharacterData character, int slotIndex)
    {
        EnsureSkillLoadout(character);
        if (character == null) return false;
        EquippedSkillState skill = FindSlot(character.characterName, slotIndex, true);
        if (skill == null || skill.attack == null || skill.upgradeLevel >= skill.attack.MaximumUpgradeLevel) return false;

        AttackUpgradeData nextUpgrade = skill.attack.GetUpgrade(skill.upgradeLevel);
        if (nextUpgrade == null || skillUpgradePoints < nextUpgrade.upgradePointCost) return false;
        skillUpgradePoints -= nextUpgrade.upgradePointCost;
        skill.upgradePointsSpent += nextUpgrade.upgradePointCost;
        skill.upgradeLevel++;
        return true;
    }

    public bool RemoveEquippedSkill(CharacterData character, int slotIndex)
    {
        EnsureSkillLoadout(character);
        EquippedSkillState skill = character == null ? null : FindSlot(character.characterName, slotIndex, false);
        if (skill == null || skill.attack == null || skill.isBasicAttack) return false;
        AddSkillToReserve(skill.attack);
        RefundUpgradePoints(skill);
        ClearSkillSlot(skill);
        return true;
    }

    public bool TryRecycleReservedSkill(AttackData attack, out int scrapAwarded)
    {
        scrapAwarded = 0;
        AttackStackState stack = skillReserve.Find(x => x.attack == attack && x.count > 0);
        if (stack == null) return false;
        stack.count--;
        if (stack.count == 0) skillReserve.Remove(stack);
        scrapAwarded = GetRecycleValue(0);
        metalScrap += scrapAwarded;
        return true;
    }

    public bool TryRecycleEquippedSkill(CharacterData character, int slotIndex, out int scrapAwarded)
    {
        scrapAwarded = 0;
        EnsureSkillLoadout(character);
        EquippedSkillState skill = character == null ? null : FindSlot(character.characterName, slotIndex, false);
        if (skill == null || skill.attack == null || skill.isBasicAttack) return false;
        RefundUpgradePoints(skill);
        scrapAwarded = GetRecycleValue(skill.upgradeLevel);
        metalScrap += scrapAwarded;
        ClearSkillSlot(skill);
        return true;
    }

    public static int GetRecycleValue(int upgradeLevel)
    {
        return 10 + 15 * Mathf.Max(0, upgradeLevel);
    }

    public void ResetProgression()
    {
        playerStates.Clear();
        skillReserve.Clear();
        equippedSkills.Clear();
        initializedSkillLoadouts.Clear();
        skillUpgradePoints = 0;
        metalScrap = 0;
        hasHealedInCamp = false;
    }

    public void FullHealStoredState()
    {
        foreach (CombatantState state in playerStates)
        {
            state.hp = state.maxHP;
            state.ammo = state.maxAmmo;
        }
        hasHealedInCamp = true;
    }

    private void EnsureSkillLoadout(CharacterData character)
    {
        if (character == null || initializedSkillLoadouts.Contains(character.characterName)) return;
        if (character.basicAttack != null)
        {
            equippedSkills.Add(new EquippedSkillState
            {
                characterName = character.characterName,
                slotIndex = -1,
                isBasicAttack = true,
                attack = character.basicAttack
            });
        }
        if (character.specials != null)
        {
            for (int i = 0; i < character.specials.Count; i++)
            {
                AttackData attack = character.specials[i];
                if (attack == null) continue;
                equippedSkills.Add(new EquippedSkillState
                {
                    characterName = character.characterName,
                    slotIndex = i,
                    attack = attack
                });
            }
        }
        initializedSkillLoadouts.Add(character.characterName);
    }

    private EquippedSkillState FindEquipped(CharacterData character, AttackData attack)
    {
        if (character == null || attack == null) return null;
        return equippedSkills.Find(x => x.characterName == character.characterName && x.attack == attack);
    }

    private EquippedSkillState FindSlot(string characterName, int slotIndex, bool includeBasic)
    {
        return equippedSkills.Find(x => x.characterName == characterName && x.slotIndex == slotIndex &&
            (includeBasic || !x.isBasicAttack));
    }

    private void RefundUpgradePoints(EquippedSkillState skill)
    {
        skillUpgradePoints += Mathf.Max(0, skill.upgradePointsSpent);
    }

    private static void ClearSkillSlot(EquippedSkillState skill)
    {
        skill.attack = null;
        skill.upgradeLevel = 0;
        skill.upgradePointsSpent = 0;
    }
}
