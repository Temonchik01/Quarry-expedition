using UnityEngine;
 
namespace JRPG.Data
{
    /// <summary>
    /// ScriptableObject holding the base (unmodified) stats for a character archetype.
    /// Shared between Heroes and Enemies — each just references a different asset.
    ///
    /// Formula references:
    ///   MaxHP  = BaseDefense * 10
    ///   Ammo   = BaseAmmo    * 5
    /// These are computed at runtime by CharacterRuntime, not stored here.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterStats", menuName = "JRPG/Character Stats")]
    public sealed class CharacterStatsSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _characterName = "Unknown";
        [SerializeField] private Sprite _portrait;
 
        [Header("Base Stats")]
        [Tooltip("Drives MaxHP via formula: Defense * 10")]
        [SerializeField, Min(1)] private int _baseDefense = 10;
 
        [Tooltip("Raw attack power multiplier base")]
        [SerializeField, Min(1)] private int _baseAttack = 10;
 
        [Tooltip("Drives MaxAmmo via formula: Ammo * 5")]
        [SerializeField, Min(1)] private int _baseAmmo = 10;
 
        [Tooltip("Higher = acts earlier in turn order")]
        [SerializeField, Min(1)] private int _baseSpeed = 10;
 
        [Header("Attacks")]
        [Tooltip("All attacks this character can use")]
        [SerializeField] private AttackDataSO[] _availableAttacks = System.Array.Empty<AttackDataSO>();
 
        // ── Public read-only accessors ─────────────────────────────────────────
        public string CharacterName  => _characterName;
        public Sprite Portrait       => _portrait;
        public int    BaseDefense    => _baseDefense;
        public int    BaseAttack     => _baseAttack;
        public int    BaseAmmo       => _baseAmmo;
        public int    BaseSpeed      => _baseSpeed;
        public AttackDataSO[] AvailableAttacks => _availableAttacks;
    }
}
 
