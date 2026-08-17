using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections;
using TMPro;

// Handles all the volume settings for the game, master, music and sfx,
// and keeps them saved and synced across every scene
public class AudioManager : MonoBehaviour
{
    // Other scripts grab this instead of needing a direct scene reference
    public static AudioManager Instance { get; private set; }

    // The mixer asset that actually controls volume in the project
    [SerializeField] private AudioMixer audioMixer;

    // The three sliders in the settings menu
    [SerializeField] private Slider masterAudioSlider;
    [SerializeField] private Slider musicAudioSlider;
    [SerializeField] private Slider sfxAudioSlider;

    [Header("Value Labels")]
    [Tooltip("Shows the current value as e.g. '100%' - same label pattern as DisplayManager's brightness label")]
    // Text next to each slider showing the percentage
    [SerializeField] private TextMeshProUGUI masterValueLabel;
    [SerializeField] private TextMeshProUGUI musicValueLabel;
    [SerializeField] private TextMeshProUGUI sfxValueLabel;

    [Header("Volume Preview")]
    [Tooltip("AudioSource routed to the SFX mixer group - assign its Clip directly on the component")]
    // Plays a short sound so the player can hear what the new sfx volume sounds like
    [SerializeField] private AudioSource sfxPreviewSource;

    [Tooltip("How long the slider must stay still before a preview plays")]
    // Stops the preview firing constantly while the slider is being dragged
    [SerializeField] private float previewDelay = 0.15f;

    // These strings need to exactly match the parameter names exposed in the AudioMixer asset
    private const string MasterVolumeParam = "MasterVolume";
    private const string MusicVolumeParam = "MusicVolume";
    private const string SFXVolumeParam = "SFXVolume";

    // Keeps track of the currently running preview delay so it can be cancelled
    private Coroutine sfxPreviewRoutine;

    private void Awake()
    {
        // If another AudioManager already exists from a previous scene, this one gets destroyed
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
        // Grab the saved volumes, or default to full volume if the player has none saved yet
        float savedMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);

        // Apply straight to the mixer rather than any single source, so every sound in every scene follows it
        ApplyMasterVolume(savedMasterVolume);
        ApplyMusicVolume(savedMusicVolume);
        ApplySFXVolume(savedSFXVolume);

        // Set the master slider to match the saved value and start listening for changes
        if (masterAudioSlider != null)
        {
            masterAudioSlider.value = savedMasterVolume;
            masterAudioSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        // Same for the music slider
        if (musicAudioSlider != null)
        {
            musicAudioSlider.value = savedMusicVolume;
            musicAudioSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        // Same for the sfx slider
        if (sfxAudioSlider != null)
        {
            sfxAudioSlider.value = savedSFXVolume;
            sfxAudioSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        // Update all three percentage labels so they match what just got loaded
        UpdateVolumeLabel(masterValueLabel, savedMasterVolume);
        UpdateVolumeLabel(musicValueLabel, savedMusicVolume);
        UpdateVolumeLabel(sfxValueLabel, savedSFXVolume);
    }

    // Called by the master slider whenever the player moves it
    public void OnMasterVolumeChanged(float linearVolume)
    {
        // Send the new value to the mixer
        ApplyMasterVolume(linearVolume);
        // Save it so it's remembered next time the game opens
        PlayerPrefs.SetFloat("MasterVolume", linearVolume);
        PlayerPrefs.Save();

        // Update the label to show the new percentage
        UpdateVolumeLabel(masterValueLabel, linearVolume);

        // Master sits above sfx in the mixer chain, so changing it changes how loud the preview sounds too
        QueueSFXPreview();
    }

    // Called by the music slider whenever the player moves it
    public void OnMusicVolumeChanged(float linearVolume)
    {
        ApplyMusicVolume(linearVolume);
        PlayerPrefs.SetFloat("MusicVolume", linearVolume);
        PlayerPrefs.Save();

        UpdateVolumeLabel(musicValueLabel, linearVolume);
    }

    // Called by the sfx slider whenever the player moves it
    public void OnSFXVolumeChanged(float linearVolume)
    {
        ApplySFXVolume(linearVolume);
        PlayerPrefs.SetFloat("SFXVolume", linearVolume);
        PlayerPrefs.Save();

        UpdateVolumeLabel(sfxValueLabel, linearVolume);

        // Let the player hear the new sfx volume
        QueueSFXPreview();
    }

    // Pushes the master volume value into the mixer
    private void ApplyMasterVolume(float linearVolume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat(MasterVolumeParam, LinearToDecibel(linearVolume));
    }

    // Pushes the music volume value into the mixer
    private void ApplyMusicVolume(float linearVolume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat(MusicVolumeParam, LinearToDecibel(linearVolume));
    }

    // Pushes the sfx volume value into the mixer
    private void ApplySFXVolume(float linearVolume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat(SFXVolumeParam, LinearToDecibel(linearVolume));
    }

    // Sets a label's text to the given volume as a rounded percentage
    private void UpdateVolumeLabel(TextMeshProUGUI label, float linearVolume)
    {
        if (label != null)
            label.text = $"{Mathf.RoundToInt(linearVolume * 100f)}%";
    }

    // Converts a slider's linear 0 to 1 value into the decibels the mixer expects
    private float LinearToDecibel(float linearVolume)
    {
        // Log of zero is undefined, so treat anything near silent as the mixer's floor
        if (linearVolume <= 0.0001f) return -80f;
        return Mathf.Log10(linearVolume) * 20f;
    }

    // Restarts the preview timer, so only the value the player settles on actually gets played
    private void QueueSFXPreview()
    {
        // Cancel any preview that's currently waiting to play
        if (sfxPreviewRoutine != null) StopCoroutine(sfxPreviewRoutine);
        sfxPreviewRoutine = StartCoroutine(PlayPreviewAfterDelay(sfxPreviewSource));
    }

    // Waits a short moment then plays the preview clip at whatever volume was just set
    private IEnumerator PlayPreviewAfterDelay(AudioSource source)
    {
        yield return new WaitForSeconds(previewDelay);

        // By now the volume changes above have already reached the mixer, so this plays at the right level
        if (source != null && source.clip != null)
        {
            source.PlayOneShot(source.clip);
        }
    }
}