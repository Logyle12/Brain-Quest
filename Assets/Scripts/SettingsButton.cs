using UnityEngine;
using UnityEngine.UI;

// Sits on a scene's gear icon button and opens the persistent settings panel when clicked
public class SettingsButton : MonoBehaviour
{
    [Tooltip("This scene's settings menu, hidden while the Settings panel is open")]
    // The local menu this button belongs to, gets hidden while the settings panel is open
    [SerializeField] private GameObject settingsMenu;

    // Hooks up the click listener as soon as this button loads
    void Start()
    {
        // Listen for clicks on this same object's Button component
        GetComponent<Button>().onClick.AddListener(OnSettingsClicked);
    }

    // Called when the gear icon is clicked, opens the settings panel over this scene
    private void OnSettingsClicked()
    {
        // The persistent settings panel hasn't loaded yet, nothing to open
        if (SettingsPanelController.Instance == null)
        {
            // Warn so it's obvious why nothing happened
            Debug.LogError("[SettingsButton] No persistent SettingsPanelController found yet.");
            return;
        }

        // Tell the settings panel to open, passing along which local menu called it
        SettingsPanelController.Instance.Open(settingsMenu);
    }
}