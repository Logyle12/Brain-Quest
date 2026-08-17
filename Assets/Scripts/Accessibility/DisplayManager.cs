using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Persistent singleton - same pattern as AudioManager.cs. Owns Brightness and
// Fullscreen together as one "Display" domain, the same way AudioManager owns
// Music+SFX. Sliders/toggle are wired directly here, same as AudioManager does
// for its own sliders, rather than through separate selector scripts.
public class DisplayManager : MonoBehaviour
{
    public static DisplayManager Instance { get; private set; }

    [Header("Brightness")]
    [SerializeField] private Slider brightnessSlider;
    [Tooltip("Shows the current value as e.g. '100%' - the ValueLabel already sitting in the Settings scene")]
    [SerializeField] private TextMeshProUGUI brightnessValueLabel;

    [Tooltip("Full-screen dark overlay used to fake brightness. WebGL has no OS/hardware brightness API to call, so this dims the view instead - 100% = fully transparent (screen exactly as bright as the player's own monitor), lower values fade this overlay in. This can only dim relative to the player's actual display, never brighten beyond it.")]
    [SerializeField] private CanvasGroup dimOverlay;
    [Tooltip("How dark the overlay gets at its darkest (0% brightness). Kept below 1 so it dims heavily without going to a dead black screen.")]
    [SerializeField] private float maxOverlayAlpha = 0.85f;

    [Header("Fullscreen")]
    [SerializeField] private Toggle fullscreenToggle;
    [Tooltip("The toggle's background graphic - recolored directly on/off, rather than relying on the built-in hover/press tint states")]
    [SerializeField] private Image fullscreenToggleBackground;
    [SerializeField] private Color fullscreenOnColor = new Color(0.298f, 0.588f, 0.882f);
    [SerializeField] private Color fullscreenOffColor = new Color(0.4f, 0.4f, 0.4f);

    private const string BrightnessKey = "Brightness";
    private const string FullscreenKey = "Fullscreen";

    private void Awake()
    {
        // Singleton pattern - identical to AudioManager.cs
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 1. Retrieve the saved data - default to 100% brightness and Fullscreen ON,
        //    since that's the state the game should boot into before any save exists.
        float savedBrightness = PlayerPrefs.GetFloat(BrightnessKey, 1.0f);
        bool savedFullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;

        // 2. Apply immediately
        ApplyBrightness(savedBrightness);
        ApplyFullscreen(savedFullscreen);

        // 3. Sync UI to the loaded data and bind distinct listeners
        if (brightnessSlider != null)
        {
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = savedFullscreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        UpdateBrightnessLabel(savedBrightness);
        UpdateToggleColor(savedFullscreen);
    }

    public void OnBrightnessChanged(float linearValue)
    {
        ApplyBrightness(linearValue);
        PlayerPrefs.SetFloat(BrightnessKey, linearValue);
        PlayerPrefs.Save();

        UpdateBrightnessLabel(linearValue);
    }

    public void OnFullscreenChanged(bool isOn)
    {
        ApplyFullscreen(isOn);
        PlayerPrefs.SetInt(FullscreenKey, isOn ? 1 : 0);
        PlayerPrefs.Save();

        UpdateToggleColor(isOn);
    }

    private void ApplyBrightness(float linearValue)
    {
        if (dimOverlay == null) return;

        // 100% brightness -> alpha 0 (fully transparent, overlay invisible)
        // 0% brightness   -> alpha maxOverlayAlpha (overlay near-opaque)
        dimOverlay.alpha = (1f - Mathf.Clamp01(linearValue)) * maxOverlayAlpha;
    }

    private void ApplyFullscreen(bool isOn)
    {
        Screen.fullScreen = isOn;
    }

    private void UpdateBrightnessLabel(float linearValue)
    {
        if (brightnessValueLabel != null)
            brightnessValueLabel.text = $"{Mathf.RoundToInt(linearValue * 100f)}%";
    }

    private void UpdateToggleColor(bool isOn)
    {
        if (fullscreenToggleBackground != null)
            fullscreenToggleBackground.color = isOn ? fullscreenOnColor : fullscreenOffColor;
    }
}