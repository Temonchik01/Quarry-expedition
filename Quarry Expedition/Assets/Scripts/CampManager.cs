using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CampManager : MonoBehaviour
{
    public Button healButton;
    public Button continueButton;
    public string mapSceneName = "Map";

    private void Start()
    {
        UpdateUI();

        if (healButton != null) healButton.onClick.AddListener(HealTeam);
        if (continueButton != null) continueButton.onClick.AddListener(ContinueExploration);
    }

    private void UpdateUI()
    {
        if (ProgressionManager.Instance != null)
        {
            if (healButton != null) healButton.interactable = !ProgressionManager.Instance.hasHealedInCamp;
        }
    }

    private void HealTeam()
    {
        if (ProgressionManager.Instance != null && !ProgressionManager.Instance.hasHealedInCamp)
        {
            ProgressionManager.Instance.FullHealStoredState();
            UpdateUI();
            Debug.Log("Team Healed!");
        }
    }

    private void ContinueExploration()
    {
        if (MapGenerator.Instance != null)
        {
            MapGenerator.Instance.ReactivateMap();
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }
        else
        {
            SceneManager.LoadScene(mapSceneName);
        }
    }
}
