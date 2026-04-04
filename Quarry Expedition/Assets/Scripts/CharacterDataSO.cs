using UnityEngine;

/// <summary>
/// Data-driven character configuration. Create instances via
/// Assets → Create → JRPG → CharacterData.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterData", menuName = "JRPG/CharacterData")]
public class CharacterDataSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _characterName;
    [SerializeField] private Sprite _battleSprite;

    [Header("Base Stats")]
    [SerializeField, Range(1, 999)] private int _baseHealth   = 100;
    [SerializeField, Range(1, 200)] private int _baseDefense  = 10;
    [SerializeField, Range(1, 200)] private int _baseAttack   = 10;
    [SerializeField, Range(1, 200)] private int _baseAmmo     = 20;
    [SerializeField, Range(1, 200)] private int _baseSpeed    = 10;

    [Header("Attacks")]
    [SerializeField] private AttackDataSO[] _learnedAttacks;

    public string CharacterName   => _characterName;
    public Sprite BattleSprite    => _battleSprite;
    public int    BaseHealth      => _baseHealth;
    public int    BaseDefense     => _baseDefense;
    public int    BaseAttack      => _baseAttack;
    public int    BaseAmmo        => _baseAmmo;
    public int    BaseSpeed       => _baseSpeed;
    public AttackDataSO[] Attacks => _learnedAttacks;
}