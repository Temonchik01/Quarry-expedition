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
        playerStates.Clear();
        foreach (var c in team)
        {
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
        if (playerStates.Count == 0) return;

        foreach (var c in team)
        {
            var state = playerStates.Find(s => s.characterName == c.data.characterName);
            if (state != null)
            {
                c.LoadState(state.hp, state.ammo);
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
