using UnityEngine;
using UnityEngine.UI;

// Persistent controller for the one settings panel shared across every scene, remembers
// which scene's menu opened it so that menu can be restored when the panel closes
public class SettingsPanelController : MonoBehaviour
{
    // Other scripts grab this instead of needing a direct scene reference
    public static SettingsPanelController Instance { get; private set; }

    [Tooltip("The child object that actually slides in/out - this is what gets shown/hidden, not this GameObject")]
    // Handles the actual slide in and out animation for the panel
    [SerializeField] private UIPanelSlide panelSlide;

    // Every save button across the panel that should close it once clicked
    [SerializeField] private Button[] saveButtons;

    // The local settings menu that was hidden when this panel was opened, restored on close
    private GameObject callersettingsMenu;

    // Runs as soon as this object wakes up, sets up the singleton before anything else can use it
    void Awake()
    {
        // Duplicate protection: if a new scene brings in another copy of this,
        // destroy the newcomer and keep the original persistent one.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // This becomes the one and only instance
        Instance = this;
        // Keep this object alive across every scene change
        DontDestroyOnLoad(gameObject);

        // Warn early if the slide reference was never hooked up in the Inspector
        if (panelSlide == null)
        {
            Debug.LogError("[SettingsPanelController] panelSlide reference not assigned in the Inspector.");
        }
    }

    // Hooks up every save button to close the panel once clicked
    void Start()
    {
        // Go through each save button assigned in the Inspector
        foreach (Button button in saveButtons)
        {
            // Only hook up buttons that actually exist
            if (button != null)
                button.onClick.AddListener(OnSaveClicked);
        }
    }

    // Called by a scene's SettingsButton when its gear icon is clicked
    public void Open(GameObject settingsMenu)
    {
        // Remember which local menu triggered this, so it can be shown again later
        callersettingsMenu = settingsMenu;
        // Hide that local menu while the settings panel is open over it
        if (callersettingsMenu != null) callersettingsMenu.SetActive(false);
        // Slide the settings panel into view
        if (panelSlide != null) panelSlide.Show();
    }

    // Called whenever any of the save buttons is clicked, closes the panel and hands control back
    private void OnSaveClicked()
    {
        // Slide the settings panel back out of view
        if (panelSlide != null) panelSlide.Hide();
        // Show the local menu that was hidden when this panel opened
        if (callersettingsMenu != null) callersettingsMenu.SetActive(true);
        // Clear the reference now that it's been handled
        callersettingsMenu = null;
    }
}