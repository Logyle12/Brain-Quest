using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections;
using TMPro;

public class AudioManager : MonoBehaviour
{
    // Singleton instance to prevent duplicates
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixer audioMixer;

    [SerializeField] private Slider masterAudioSlider;
    [SerializeField] private Slider musicAudioSlider;
    [SerializeField] private Slider sfxAudioSlider;

    [Header("Value Labels")]
    [Tooltip("Shows the current value as e.g. '100%' - same label pattern as DisplayManager's brightness label")]
    [SerializeField] private TextMeshProUGUI masterValueLabel;
    [SerializeField] private TextMeshProUGUI musicValueLabel;
    [SerializeField] private TextMeshProUGUI sfxValueLabel;

    [Header("Volume Preview")]
    [Tooltip("AudioSource routed to the SFX mixer group - assign its Clip directly on the component")]
    [SerializeField] private AudioSource sfxPreviewSource;

    [Tooltip("How long the slider must stay still before a preview plays")]
    [SerializeField] private float previewDelay = 0.15f;

    private const string MasterVolumeParam = "MasterVolume";
    private const string MusicVolumeParam = "MusicVolume";
    private const string SFXVolumeParam = "SFXVolume";

    private Coroutine sfxPreviewRoutine;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Ensure object is at the root hierarchy before applying DontDestroyOnLoad
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 1. Retrieve the saved data, defaulting to 1.0f (max volume) if no data exists
        float savedMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);

        // 2. Apply retrieved data directly to the Mixer, not to any single AudioSource.
        //    Every AudioSource routed to these groups (any button, any scene) is affected at once.
        ApplyMasterVolume(savedMasterVolume);
        ApplyMusicVolume(savedMusicVolume);
        ApplySFXVolume(savedSFXVolume);

        // 3. Sync UI Sliders to the loaded data and bind distinct listeners
        if (masterAudioSlider != null)
        {
            masterAudioSlider.value = savedMasterVolume;
            masterAudioSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicAudioSlider != null)
        {
            musicAudioSlider.value = savedMusicVolume;
            musicAudioSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxAudioSlider != null)
        {
            sfxAudioSlider.value = savedSFXVolume;
            sfxAudioSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        // 4. Initialize the value labels to match
        UpdateVolumeLabel(masterValueLabel, savedMasterVolume);
        UpdateVolumeLabel(musicValueLabel, savedMusicVolume);
        UpdateVolumeLabel(sfxValueLabel, savedSFXVolume);
    }

    public void OnMasterVolumeChanged(float linearVolume)
    {
        ApplyMasterVolume(linearVolume);
        PlayerPrefs.SetFloat("MasterVolume", linearVolume);
        PlayerPrefs.Save();

        UpdateVolumeLabel(masterValueLabel, linearVolume);

        // Master affects the SFX preview's audible loudness too (it sits downstream
        // of Master in the mixer), so let the player hear where Master landed.
        QueueSFXPreview();
    }

    public void OnMusicVolumeChanged(float linearVolume)
    {
        ApplyMusicVolume(linearVolume);
        PlayerPrefs.SetFloat("MusicVolume", linearVolume);
        PlayerPrefs.Save();

        UpdateVolumeLabel(musicValueLabel, linearVolume);
    }

    public void OnSFXVolumeChanged(float linearVolume)
    {
        ApplySFXVolume(linearVolume);
        PlayerPrefs.SetFloat("SFXVolume", linearVolume);
        PlayerPrefs.Save();

        UpdateVolumeLabel(sfxValueLabel, linearVolume);

        QueueSFXPreview();
    }

    private void ApplyMasterVolume(float linearVolume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat(MasterVolumeParam, LinearToDecibel(linearVolume));
    }

    private void ApplyMusicVolume(float linearVolume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat(MusicVolumeParam, LinearToDecibel(linearVolume));
    }

    private void ApplySFXVolume(float linearVolume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat(SFXVolumeParam, LinearToDecibel(linearVolume));
    }

    private void UpdateVolumeLabel(TextMeshProUGUI label, float linearVolume)
    {
        if (label != null)
            label.text = $"{Mathf.RoundToInt(linearVolume * 100f)}%";
    }

    // AudioMixer volume parameters are in decibels (logarithmic), while UI
    // sliders are linear (0-1). This converts between the two.
    private float LinearToDecibel(float linearVolume)
    {
        // Log10(0) is undefined - treat near-silent as the mixer's floor (-80dB)
        if (linearVolume <= 0.0001f) return -80f;
        return Mathf.Log10(linearVolume) * 20f;
    }

    // Restarts the delay timer on every change, so only the value the slider
    // settles on actually gets previewed - not every intermediate value while dragging.
    private void QueueSFXPreview()
    {
        if (sfxPreviewRoutine != null) StopCoroutine(sfxPreviewRoutine);
        sfxPreviewRoutine = StartCoroutine(PlayPreviewAfterDelay(sfxPreviewSource));
    }

    private IEnumerator PlayPreviewAfterDelay(AudioSource source)
    {
        yield return new WaitForSeconds(previewDelay);

        // By the time this plays, ApplySFXVolume/ApplyMasterVolume have already
        // updated the Mixer, so the preview is heard at the newly set volume.
        if (source != null && source.clip != null)
        {
            source.PlayOneShot(source.clip);
        }
    }
}