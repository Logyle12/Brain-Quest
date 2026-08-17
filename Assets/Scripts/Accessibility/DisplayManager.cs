using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Handles brightness and fullscreen settings, and keeps them saved and
// synced across every scene
public class DisplayManager : MonoBehaviour
{
    // Other scripts grab this instead of needing a direct scene reference
    public static DisplayManager Instance { get; private set; }

    [Header("Brightness")]
    // The brightness slider in the settings menu
    [SerializeField] private Slider brightnessSlider;
    [Tooltip("Shows the current value as e.g. '100%' - the ValueLabel already sitting in the Settings scene")]
    // Text next to the slider showing the percentage
    [SerializeField] private TextMeshProUGUI brightnessValueLabel;

    // WebGL can't actually touch screen brightness, so this fakes it with a dark overlay instead
    [SerializeField] private CanvasGroup dimOverlay;
    // Caps how dark the overlay can get, never fully black
    [SerializeField] private float maxOverlayAlpha = 0.85f;

    [Header("Fullscreen")]
    // The fullscreen toggle in the settings menu
    [SerializeField] private Toggle fullscreenToggle;
    [Tooltip("The toggle's background graphic - recolored directly on/off, rather than relying on the built-in hover/press tint states")]
    // Background image on the toggle, recolored manually based on state
    [SerializeField] private Image fullscreenToggleBackground;
    [SerializeField] private Color fullscreenOnColor = new Color(0.298f, 0.588f, 0.882f);
    [SerializeField] private Color fullscreenOffColor = new Color(0.4f, 0.4f, 0.4f);

    // Keys used to save and load these settings from PlayerPrefs
    private const string BrightnessKey = "Brightness";
    private const string FullscreenKey = "Fullscreen";

    private void Awake()
    {
        // If another DisplayManager already exists from a previous scene, this one gets destroyed
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad only behaves properly on objects sitting at the root of the hierarchy
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    // Runs once when the manager first loads, pulls saved settings and gets everything in sync
    private void Start()
    {
        // Grab the saved settings, or default to full brightness and fullscreen off
        float savedBrightness = PlayerPrefs.GetFloat(BrightnessKey, 1.0f);
        bool savedFullscreen = PlayerPrefs.GetInt(FullscreenKey, 0) == 1;

        // Apply right away so the game boots looking correct
        ApplyBrightness(savedBrightness);
        ApplyFullscreen(savedFullscreen);

        // Set the brightness slider to match the saved value and start listening for changes
        if (brightnessSlider != null)
        {
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }

        // Same for the fullscreen toggle
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = savedFullscreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        // Update the label and toggle color so they match what just got loaded
        UpdateBrightnessLabel(savedBrightness);
        UpdateToggleColor(savedFullscreen);
    }

    // Called by the brightness slider whenever the player moves it
    public void OnBrightnessChanged(float linearValue)
    {
        ApplyBrightness(linearValue);
        PlayerPrefs.SetFloat(BrightnessKey, linearValue);
        PlayerPrefs.Save();

        UpdateBrightnessLabel(linearValue);
    }

    // Called by the fullscreen toggle whenever the player flips it
    public void OnFullscreenChanged(bool isOn)
    {
        ApplyFullscreen(isOn);
        PlayerPrefs.SetInt(FullscreenKey, isOn ? 1 : 0);
        PlayerPrefs.Save();

        UpdateToggleColor(isOn);
    }

    // Fades the dim overlay in or out based on the brightness value
    private void ApplyBrightness(float linearValue)
    {
        if (dimOverlay == null) return;

        // Full brightness means the overlay is invisible, zero brightness means it's near-opaque
        dimOverlay.alpha = (1f - Mathf.Clamp01(linearValue)) * maxOverlayAlpha;
    }

    // Switches the game between fullscreen and windowed
    private void ApplyFullscreen(bool isOn)
    {
        Screen.fullScreen = isOn;
    }

    // Sets the brightness label's text to the given value as a rounded percentage
    private void UpdateBrightnessLabel(float linearValue)
    {
        if (brightnessValueLabel != null)
            brightnessValueLabel.text = $"{Mathf.RoundToInt(linearValue * 100f)}%";
    }

    // Recolors the fullscreen toggle background based on whether it's on or off
    private void UpdateToggleColor(bool isOn)
    {
        if (fullscreenToggleBackground != null)
            fullscreenToggleBackground.color = isOn ? fullscreenOnColor : fullscreenOffColor;
    }
}