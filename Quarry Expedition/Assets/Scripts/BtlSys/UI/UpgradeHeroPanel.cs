using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace BtlSys.UI
{
    public class UpgradeHeroPanel : MonoBehaviour
    {
        [Header("Hero Info")]
        public Image portrait;
        public TextMeshProUGUI pointsText;

        [Header("Stats")]
        public StatRowUI[] statRows; // Assign in order: Health, Attack, Defense, Ammo, Speed

        [Header("Skills")]
        public Transform skillsContainer;
        public SkillItemUI skillPrefab;

        [Header("Description")]
        public GameObject descriptionPanel;
        public TextMeshProUGUI descTarget;
        public TextMeshProUGUI descPower;
        public TextMeshProUGUI descAccuracy;
        public TextMeshProUGUI descCost;
        public TextMeshProUGUI descBasic;
        public TextMeshProUGUI descUpgrade;

        private ProgressionManager.CombatantState _state;
        private List<SkillItemUI> _skillItems = new List<SkillItemUI>();
        private CharacterData _charData;

        public void Bind(ProgressionManager.CombatantState state)
        {
            _state = state;
            
            // Find character data by name
            // For now we search all CharacterData in project via Resources or a global list
            // Assuming we have a way to get it. 
            _charData = FindCharacterData(state.characterName);
            
            Refresh();
        }

        private CharacterData FindCharacterData(string name)
        {
            if (UpgradeUI.Instance != null)
            {
                return UpgradeUI.Instance.allHeroData.FirstOrDefault(d => d.characterName == name);
            }
            return null;
        }

        public void Refresh()
        {
            if (_state == null || _charData == null) return;

            if (portrait != null) portrait.sprite = _charData.portrait;
            if (pointsText != null) pointsText.text = $"POINTS: {_state.upgradePoints}";

            // Stats
            UpdateStatRow(0, StatType.Health, _charData.maxHealth, _state.healthBoost);
            UpdateStatRow(1, StatType.Attack, _charData.attackPower, _state.attackBoost);
            UpdateStatRow(2, StatType.Defense, _charData.defense, _state.defenseBoost);
            UpdateStatRow(3, StatType.Ammo, _charData.maxAmmo, _state.ammoBoost);
            UpdateStatRow(4, StatType.Speed, _charData.speed, _state.speedBoost);

            // Skills
            foreach (var item in _skillItems) Destroy(item.gameObject);
            _skillItems.Clear();

            foreach (var skill in _charData.specials)
            {
                var item = Instantiate(skillPrefab, skillsContainer);
                item.Bind(_state, skill, this);
                _skillItems.Add(item);
            }
            
            // Add placeholders if less than 8
            for (int i = _charData.specials.Count; i < 8; i++)
            {
                var item = Instantiate(skillPrefab, skillsContainer);
                item.SetPlaceholder();
                _skillItems.Add(item);
            }
        }

        private void UpdateStatRow(int index, StatType type, int baseVal, int boost)
        {
            if (index < statRows.Length)
            {
                statRows[index].Setup(_state, type, baseVal, boost, this);
            }
        }

        public void ShowDescription(AttackData skill, bool isUpgraded)
        {
            descriptionPanel.SetActive(true);
            descTarget.text = skill.targetType.ToString();
            descPower.text = GetPowerText(skill.power);
            descPower.color = GetPowerColor(skill.power);
            descAccuracy.text = $"Accuracy: {skill.accuracy}%";
            descCost.text = $"Cost: {skill.ammoCost} AM";
            descBasic.text = skill.description;
            descUpgrade.text = isUpgraded ? skill.upgradeDescription : "";
            descUpgrade.gameObject.SetActive(isUpgraded);
        }

        public void HideDescription()
        {
            descriptionPanel.SetActive(false);
        }

        private string GetPowerText(int power)
        {
            if (power < 20) return "Low";
            if (power < 50) return "Medium";
            if (power < 100) return "High";
            return "Severe";
        }

        private Color GetPowerColor(int power)
        {
            if (power < 20) return Color.green;
            if (power < 50) return Color.yellow;
            if (power < 100) return Color.red;
            return new Color(0.5f, 0, 0); // Dark red
        }
    }
}
