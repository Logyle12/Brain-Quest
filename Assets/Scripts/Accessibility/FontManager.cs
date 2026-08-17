using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Persistent singleton - same pattern as AudioManager.cs, SceneNavigator.cs, and
// SettingsPanelController.cs. Owns the currently selected font AND size, saves
// both to PlayerPrefs, and re-applies them to every piece of TMP text in every
// scene as that scene loads - so a choice made in Settings sticks everywhere, forever.
public class FontManager : MonoBehaviour
{
    public static FontManager Instance { get; private set; }

    [Tooltip("Every font the player can choose between. The index into this array is what gets saved - keep it in the same order as the display names shown in your FontSelector UI.")]
    [SerializeField] private TMP_FontAsset[] availableFonts;

    [Tooltip("XAG 101 discrete size steps, applied as a multiplier against each text element's own authored size (Small 75% / Default 100% / Large 150% / Maximum 200%). Keep in the same order as the labels shown in your FontSizeSelector UI.")]
    [SerializeField] private float[] sizeMultipliers = { 0.75f, 1.0f, 1.5f, 2.0f };

    private const string FontIndexKey = "SelectedFontIndex";
    private const string SizeIndexKey = "SelectedSizeIndex";

    private int currentFontIndex = 0;
    private int currentSizeIndex = 1; // Default (100%) - the XAG 101 baseline

    // Each TMP_Text's own authored font size, captured fresh on every scene load -
    // before any multiplier is applied - so re-applying a multiplier never compounds
    // on top of a previously-applied one.
    private Dictionary<TMP_Text, float> baseFontSizes = new Dictionary<TMP_Text, float>();

    public int CurrentFontIndex => currentFontIndex;
    public int CurrentSizeIndex => currentSizeIndex;

    public TMP_FontAsset CurrentFont =>
        (availableFonts != null && availableFonts.Length > 0) ? availableFonts[currentFontIndex] : null;

    private float CurrentSizeMultiplier =>
        (sizeMultipliers != null && sizeMultipliers.Length > 0) ? sizeMultipliers[currentSizeIndex] : 1.0f;

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

        // 1. Load the saved choices (default to the first font / Default size if nothing saved yet)
        currentFontIndex = PlayerPrefs.GetInt(FontIndexKey, 0);
        if (availableFonts != null && availableFonts.Length > 0)
            currentFontIndex = Mathf.Clamp(currentFontIndex, 0, availableFonts.Length - 1);

        currentSizeIndex = PlayerPrefs.GetInt(SizeIndexKey, 1);
        if (sizeMultipliers != null && sizeMultipliers.Length > 0)
            currentSizeIndex = Mathf.Clamp(currentSizeIndex, 0, sizeMultipliers.Length - 1);

        // 2. From here on, catch every future scene load automatically.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Apply to whatever scene this object first woke up in - sceneLoaded
        // only fires for scenes loaded AFTER we subscribed, so the very first
        // scene needs this separate pass.
        ApplyToScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToScene();
    }

    // Called by a font-selector UI (e.g. FontSelector) when the player picks
    // a new font. Saves it and applies it immediately, everywhere on screen.
    public void SetFont(int index)
    {
        if (availableFonts == null || availableFonts.Length == 0) return;

        currentFontIndex = Mathf.Clamp(index, 0, availableFonts.Length - 1);
        PlayerPrefs.SetInt(FontIndexKey, currentFontIndex);
        PlayerPrefs.Save();

        ApplyFontOnly();
    }

    // Called by a FontSizeSelector UI when the player picks a new size step.
    // Saves it and rescales every known text immediately, using each one's
    // already-captured base size - never the currently-displayed size - so
    // stepping through sizes repeatedly (e.g. Large -> Maximum -> Default)
    // always lands on the correct absolute size instead of compounding.
    public void SetSize(int index)
    {
        if (sizeMultipliers == null || sizeMultipliers.Length == 0) return;

        currentSizeIndex = Mathf.Clamp(index, 0, sizeMultipliers.Length - 1);
        PlayerPrefs.SetInt(SizeIndexKey, currentSizeIndex);
        PlayerPrefs.Save();

        ApplySizeOnly();
    }

    // Full pass: run on every scene load. Finds every piece of TMP text currently
    // in the scene - including inside inactive objects, since a hidden panel (like
    // an unopened Settings menu) still needs to already be correct the instant it's
    // shown - captures each one's authored size, then applies both the current font
    // and the current size multiplier.
    private void ApplyToScene()
    {
        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);

        baseFontSizes.Clear();

        TMP_FontAsset fontToApply = CurrentFont;
        float multiplier = CurrentSizeMultiplier;

        foreach (TMP_Text text in allTexts)
        {
            if (fontToApply != null)
                text.font = fontToApply;

            // Capture BEFORE scaling - this is the one moment we know it's
            // still the un-multiplied, originally authored value.
            baseFontSizes[text] = text.fontSize;
            text.fontSize = text.fontSize * multiplier;

            RefreshTextLayout(text);
        }

        ForceFullLayoutRebuild();
    }

    // Lighter pass: only touches .font, used when the font choice changes mid-scene.
    // Doesn't re-touch size, so it can't disturb whatever multiplier is already applied.
    private void ApplyFontOnly()
    {
        TMP_FontAsset fontToApply = CurrentFont;
        if (fontToApply == null) return;

        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        foreach (TMP_Text text in allTexts)
        {
            text.font = fontToApply;
            RefreshTextLayout(text);
        }

        ForceFullLayoutRebuild();
    }

    // Lighter pass: only touches .fontSize, used when the size choice changes mid-scene.
    // Rescales from each text's cached base size rather than FindObjectsOfType again,
    // so it can never accidentally pick up an already-scaled value as a new base.
    private void ApplySizeOnly()
    {
        float multiplier = CurrentSizeMultiplier;

        foreach (KeyValuePair<TMP_Text, float> entry in baseFontSizes)
        {
            TMP_Text text = entry.Key;
            if (text == null) continue; // guards against text destroyed since scene load

            text.fontSize = entry.Value * multiplier;

            RefreshTextLayout(text);
        }

        ForceFullLayoutRebuild();
    }

    // Setting .fontSize or .font alone doesn't synchronously recompute TMP's wrapped
    // text bounds - TMP caches its mesh and may still report stale (pre-change)
    // preferred height to the Layout system for a frame. ForceMeshUpdate() makes TMP
    // recalculate immediately; MarkLayoutForRebuild() then tells every ancestor
    // Layout Group (TextBlock's Vertical Layout Group, the row's Horizontal Layout
    // Group, the Rows_Container's Vertical Layout Group) to re-measure using that
    // fresh value, instead of the size it had before this change.
    private void RefreshTextLayout(TMP_Text text)
    {
        text.ForceMeshUpdate();
        LayoutRebuilder.MarkLayoutForRebuild(text.rectTransform);
    }

    // MarkLayoutForRebuild (above) only *queues* a rebuild - Unity processes that
    // queue at the end of the frame, and with dozens of texts each queuing their
    // own ancestor chain, deeply nested Layout Groups / Content Size Fitters (e.g.
    // TitleBlock inside ContentHeader inside MainContent) can settle into a
    // slightly different final state depending on the order things happened to be
    // processed in - which is why the panel was landing in a different resting
    // position depending on whether the previous step was a size increase or
    // decrease, instead of always returning to the same place.
    //
    // Forcing one immediate, synchronous rebuild of each root Canvas after every
    // change replaces that "many small queued rebuilds, order not guaranteed"
    // behaviour with "one full top-to-bottom recalculation, every time" - so the
    // result is deterministic regardless of what the previous size step was.
    private void ForceFullLayoutRebuild()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in allCanvases)
        {
            if (!canvas.isRootCanvas) continue;
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.GetComponent<RectTransform>());
        }

        // A ScrollRect's scroll position is stored as a 0-1 fraction, not a pixel
        // offset. When the content's height changes (taller text at a larger size
        // step), the same fraction now points at a different pixel offset, which
        // reads as "it didn't scroll back to where it was". Snapping back to the
        // top on every font/size change keeps that consistent and predictable.
        ScrollRect[] allScrollRects = FindObjectsOfType<ScrollRect>(true);
        foreach (ScrollRect scrollRect in allScrollRects)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}