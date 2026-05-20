using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BtlSts
{
    public class CharacterHUD : MonoBehaviour
    {
        public BattleCharacter character;
        public UnityEngine.UI.Slider hpSlider;
        public UnityEngine.UI.Slider ammoSlider;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI ammoText;
        
        [Header("Floating Settings")]
        public Vector3 offset = new Vector3(0, 2.5f, 0);
        private Camera mainCam;

        private void Start()
        {
            mainCam = Camera.main;
        }

        public void Setup(BattleCharacter target)
        {
            character = target;
            character.OnStatsChanged += UpdateHUD;
            UpdateHUD();
        }

        private void LateUpdate()
        {
            if (character != null && mainCam != null)
            {
                // Follow character in world space
                Vector3 screenPos = mainCam.WorldToScreenPoint(character.transform.position + offset);
                transform.position = screenPos;
                
                // Hide if behind camera
                gameObject.SetActive(screenPos.z > 0);
            }
        }

        private void UpdateHUD()
        {
            if (character == null) return;

            if (hpSlider != null)
            {
                hpSlider.maxValue = character.stats.baseHealth;
                hpSlider.value = character.currentHealth;
            }
            if (hpText != null)
                hpText.text = $"{character.currentHealth}/{character.stats.baseHealth}";

            if (ammoSlider != null)
            {
                ammoSlider.maxValue = character.stats.baseAmmo;
                ammoSlider.value = character.currentAmmo;
            }
            if (ammoText != null)
                ammoText.text = $"{character.currentAmmo}/{character.stats.baseAmmo}";
        }

        private void OnDestroy()
        {
            if (character != null) character.OnStatsChanged -= UpdateHUD;
        }
    }
}
