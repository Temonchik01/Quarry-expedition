using UnityEngine;
using System.Collections.Generic;
using BtlSys;

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
        
        // Upgrades
        public int upgradePoints = 2;
        public int healthBoost = 0;
        public int attackBoost = 0;
        public int defenseBoost = 0;
        public int ammoBoost = 0;
        public int speedBoost = 0;
        public List<string> upgradedSkills = new List<string>();
    }

    public List<CombatantState> playerStates = new List<CombatantState>();
    public bool hasHealedInCamp = false;

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
        foreach (var c in team)
        {
            var state = playerStates.Find(s => s.characterName == c.data.characterName);
            if (state == null)
            {
                state = new CombatantState { characterName = c.data.characterName };
                playerStates.Add(state);
            }
            
            state.hp = c.currentHP;
            state.ammo = c.currentAmmo;
            state.maxHP = c.data.maxHealth + state.healthBoost;
            state.maxAmmo = c.data.maxAmmo + state.ammoBoost;
        }
    }

    public void LoadPlayerState(List<Combatant> team)
    {
        foreach (var c in team)
        {
            var state = playerStates.Find(s => s.characterName == c.data.characterName);
            if (state != null)
            {
                c.LoadState(state.hp, state.ammo);
                // Apply boosts
                c.ApplyPermanentBoosts(state.healthBoost, state.attackBoost, state.defenseBoost, state.ammoBoost, state.speedBoost);
            }
            else
            {
                // New hero state
                var newState = new CombatantState { 
                    characterName = c.data.characterName,
                    hp = c.data.maxHealth,
                    ammo = c.data.maxAmmo,
                    maxHP = c.data.maxHealth,
                    maxAmmo = c.data.maxAmmo
                };
                playerStates.Add(newState);
            }
        }
    }

    public void ResetProgression()
    {
        playerStates.Clear();
        hasHealedInCamp = false;
    }

    public void FullHealStoredState()
    {
        foreach (var state in playerStates)
        {
            state.hp = state.maxHP;
            state.ammo = state.maxAmmo;
        }
        hasHealedInCamp = true;
    }
}
