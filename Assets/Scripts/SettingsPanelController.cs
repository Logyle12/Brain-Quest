using UnityEngine;
using UnityEngine.UI;

// Self-contained persistent singleton - same pattern as SceneNavigator.cs and Music.cs.
// This GameObject must stay ACTIVE at all times (do not SetActive(false) it) so that
// Awake() runs immediately on scene load. It no longer needs to be nested under
// SceneNavigator - it protects itself directly.
public class SettingsPanelController : MonoBehaviour
{
    public static SettingsPanelController Instance { get; private set; }

    [Tooltip("The child object that actually slides in/out - this is what gets shown/hidden, not this GameObject")]
    [SerializeField] private UIPanelSlide panelSlide;

    [SerializeField] private Button[] saveButtons;

    private GameObject callersettingsMenu;

    void Awake()
    {
        // Duplicate protection: if a new scene brings in another copy of this,
        // destroy the newcomer and keep the original persistent one.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (panelSlide == null)
        {
            Debug.LogError("[SettingsPanelController] panelSlide reference not assigned in the Inspector.");
        }
    }

    void Start()
    {
        foreach (Button button in saveButtons)
        {
            if (button != null)
                button.onClick.AddListener(OnSaveClicked);
        }
    }

    // Called by a scene's SettingsButton when its gear icon is clicked
    public void Open(GameObject settingsMenu)
    {
        callersettingsMenu = settingsMenu;
        if (callersettingsMenu != null) callersettingsMenu.SetActive(false);
        if (panelSlide != null) panelSlide.Show();
    }

    private void OnSaveClicked()
    {
        if (panelSlide != null) panelSlide.Hide();
        if (callersettingsMenu != null) callersettingsMenu.SetActive(true);
        callersettingsMenu = null;
    }
}