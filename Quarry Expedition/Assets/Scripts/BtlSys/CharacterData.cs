using UnityEngine;
using System.Collections.Generic;

namespace BtlSys
{
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "BtlSys/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string characterName;
        public int maxHealth;
        public int defense;
        public int attackPower;
        public int maxAmmo;
        public int speed;
        public Sprite portrait;
        public Sprite battleSprite;

        public List<AttackData> specials;
    }
}
