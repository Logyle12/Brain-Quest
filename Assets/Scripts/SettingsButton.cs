using UnityEngine;
using UnityEngine.UI;

public class SettingsButton : MonoBehaviour
{
    [Tooltip("This scene's settings menu, hidden while the Settings panel is open")]
    [SerializeField] private GameObject settingsMenu;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnSettingsClicked);
    }

    private void OnSettingsClicked()
    {
        if (SettingsPanelController.Instance == null)
        {
            Debug.LogError("[SettingsButton] No persistent SettingsPanelController found yet.");
            return;
        }

        SettingsPanelController.Instance.Open(settingsMenu);
    }
}