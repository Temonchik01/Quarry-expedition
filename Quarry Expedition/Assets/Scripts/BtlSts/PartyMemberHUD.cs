using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace BtlSts
{
    public class PartyMemberHUD : MonoBehaviour
    {
        public BattleCharacter character;
        
        [Header("References")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI spText;
        public UnityEngine.UI.Image[] bpStars; // Array of star icons
        public UnityEngine.UI.Slider hpSlider;
        public UnityEngine.UI.Slider spSlider;
        public UnityEngine.UI.Image backgroundGradient;
        
        [Header("Settings")]
        public Color activeColor = Color.white;
        public Color inactiveColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        public void Setup(BattleCharacter target)
        {
            character = target;
            character.OnStatsChanged += UpdateHUD;
            UpdateHUD();
        }

        public void SetActive(bool isActive)
        {
            if (backgroundGradient != null)
                backgroundGradient.gameObject.SetActive(isActive);
            
            nameText.color = isActive ? activeColor : inactiveColor;
        }

        private void UpdateHUD()
        {
            if (character == null) return;

            nameText.text = character.stats.characterName;
            
            hpText.text = $"{character.currentHealth} / {character.stats.baseHealth}";
            if (hpSlider != null)
            {
                hpSlider.maxValue = character.stats.baseHealth;
                hpSlider.value = character.currentHealth;
            }

            spText.text = $"{character.currentAmmo} / {character.stats.baseAmmo}";
            if (spSlider != null)
            {
                spSlider.maxValue = character.stats.baseAmmo;
                spSlider.value = character.currentAmmo;
            }

            // Update BP Stars
            for (int i = 0; i < bpStars.Length; i++)
            {
                if (i < character.currentBP)
                    bpStars[i].gameObject.SetActive(true);
                else
                    bpStars[i].gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (character != null) character.OnStatsChanged -= UpdateHUD;
        }
    }
}
