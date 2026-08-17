using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Handles font choice and text size across the whole game, saves both
// and reapplies them to every scene as it loads
public class FontManager : MonoBehaviour
{
    // Other scripts grab this instead of needing a direct scene reference
    public static FontManager Instance { get; private set; }

    [Tooltip("Every font the player can choose between. The index into this array is what gets saved - keep it in the same order as the display names shown in your FontSelector UI.")]
    // The actual font assets, indexed the same way as the FontSelector UI
    [SerializeField] private TMP_FontAsset[] availableFonts;

    [Tooltip("Applied as a multiplier against each text element's own authored size (Small 75% / Default 100% / Large 150% / Maximum 200%). Keep in the same order as the labels shown in your FontSizeSelector UI.")]
    // The actual multiplier values, indexed the same way as the FontSizeSelector UI
    [SerializeField] private float[] sizeMultipliers = { 0.75f, 1.0f, 1.5f, 2.0f };

    // Key used to save and load the font choice from PlayerPrefs
    private const string FontIndexKey = "SelectedFontIndex";
    // Key used to save and load the size choice from PlayerPrefs
    private const string SizeIndexKey = "SelectedSizeIndex";

    // Currently selected font, index into availableFonts
    private int currentFontIndex = 0;
    // Currently selected size, index into sizeMultipliers, 1 = Default size
    private int currentSizeIndex = 1;

    // Stores each text's original size before any multiplier, so we always scale
    // from the real base and never end up multiplying an already-multiplied value
    private Dictionary<TMP_Text, float> baseFontSizes = new Dictionary<TMP_Text, float>();

    // Public read access to the current font index for UI scripts to read on load
    public int CurrentFontIndex => currentFontIndex;
    // Public read access to the current size index for UI scripts to read on load
    public int CurrentSizeIndex => currentSizeIndex;

    // The font asset currently selected, or null if nothing's set up
    public TMP_FontAsset CurrentFont =>
        (availableFonts != null && availableFonts.Length > 0) ? availableFonts[currentFontIndex] : null;

    // The size multiplier currently selected, or 1.0 (no change) if nothing's set up
    private float CurrentSizeMultiplier =>
        (sizeMultipliers != null && sizeMultipliers.Length > 0) ? sizeMultipliers[currentSizeIndex] : 1.0f;

    private void Awake()
    {
        // If another FontManager already exists from a previous scene, this one gets destroyed
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // This becomes the one and only instance
        Instance = this;
        // DontDestroyOnLoad only behaves properly on objects sitting at the root of the hierarchy
        transform.SetParent(null);
        // Keep this object alive across every scene change
        DontDestroyOnLoad(gameObject);

        // Load the saved font choice, or default to 0 if nothing's saved yet
        currentFontIndex = PlayerPrefs.GetInt(FontIndexKey, 0);
        // Clamp in case availableFonts got shorter since the save happened
        if (availableFonts != null && availableFonts.Length > 0)
            currentFontIndex = Mathf.Clamp(currentFontIndex, 0, availableFonts.Length - 1);

        // Load the saved size choice, or default to 1 (Default size) if nothing's saved yet
        currentSizeIndex = PlayerPrefs.GetInt(SizeIndexKey, 1);
        // Clamp in case sizeMultipliers got shorter since the save happened
        if (sizeMultipliers != null && sizeMultipliers.Length > 0)
            currentSizeIndex = Mathf.Clamp(currentSizeIndex, 0, sizeMultipliers.Length - 1);

        // So every scene loaded from here on gets font and size applied automatically
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Unsubscribe so a destroyed manager doesn't keep listening for scene loads
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Runs once when the manager first loads
    private void Start()
    {
        // sceneLoaded doesn't fire for the scene we're already in, so this needs to run manually once
        ApplyToScene();
    }

    // Called automatically every time a new scene finishes loading
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Same full pass as the initial scene load
        ApplyToScene();
    }

    // Called by FontSelector when the player picks a new font
    public void SetFont(int index)
    {
        // Bail out if there's nothing to pick from
        if (availableFonts == null || availableFonts.Length == 0) return;

        // Store the new choice, clamped to a valid index
        currentFontIndex = Mathf.Clamp(index, 0, availableFonts.Length - 1);
        // Save it so it's remembered next time the game opens
        PlayerPrefs.SetInt(FontIndexKey, currentFontIndex);
        PlayerPrefs.Save();

        // Push it out to every text on screen right away
        ApplyFontOnly();
    }

    // Called by FontSizeSelector when the player picks a new size step
    public void SetSize(int index)
    {
        // Bail out if there's nothing to pick from
        if (sizeMultipliers == null || sizeMultipliers.Length == 0) return;

        // Store the new choice, clamped to a valid index
        currentSizeIndex = Mathf.Clamp(index, 0, sizeMultipliers.Length - 1);
        // Save it so it's remembered next time the game opens
        PlayerPrefs.SetInt(SizeIndexKey, currentSizeIndex);
        PlayerPrefs.Save();

        // Push it out to every text on screen right away
        ApplySizeOnly();
    }

    // Runs on every scene load, grabs every piece of text (even inactive ones, like
    // an unopened settings panel) and applies both font and size fresh
    private void ApplyToScene()
    {
        // true here includes inactive objects in the search
        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);

        // Wipe the old cache since this is a new scene with new text objects
        baseFontSizes.Clear();

        // Grab the font and multiplier once instead of every loop iteration
        TMP_FontAsset fontToApply = CurrentFont;
        float multiplier = CurrentSizeMultiplier;

        foreach (TMP_Text text in allTexts)
        {
            // Apply the font if one is set up
            if (fontToApply != null)
                text.font = fontToApply;

            // Grab this before we touch it, it's the only moment we know it's untouched
            baseFontSizes[text] = text.fontSize;
            // Scale from that original size
            text.fontSize = text.fontSize * multiplier;

            // Make sure the layout catches up to the new size
            RefreshTextLayout(text);
        }

        // One clean rebuild after every text has been updated
        ForceFullLayoutRebuild();
    }

    // Only touches font, used for mid-scene font changes so size stays untouched
    private void ApplyFontOnly()
    {
        TMP_FontAsset fontToApply = CurrentFont;
        // Nothing to apply, bail out
        if (fontToApply == null) return;

        // true here includes inactive objects in the search
        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        foreach (TMP_Text text in allTexts)
        {
            // Just the font, size is left alone
            text.font = fontToApply;
            RefreshTextLayout(text);
        }

        ForceFullLayoutRebuild();
    }

    // Only touches size, rescales off the cached base sizes instead of re-scanning
    // the scene, otherwise it would end up scaling an already-scaled number
    private void ApplySizeOnly()
    {
        float multiplier = CurrentSizeMultiplier;

        foreach (KeyValuePair<TMP_Text, float> entry in baseFontSizes)
        {
            TMP_Text text = entry.Key;
            // Text got destroyed since scene load, skip it
            if (text == null) continue;

            // Scale from the cached original size, not the current on-screen size
            text.fontSize = entry.Value * multiplier;

            RefreshTextLayout(text);
        }

        ForceFullLayoutRebuild();
    }

    // Updates one text element's mesh and queues its layout for a rebuild
    private void RefreshTextLayout(TMP_Text text)
    {
        // Recalculates the mesh right away instead of waiting for the next frame
        text.ForceMeshUpdate();
        // Tells this text's layout parents they need to re-measure
        LayoutRebuilder.MarkLayoutForRebuild(text.rectTransform);
    }

    // Rebuilds every canvas in the scene and resets any scroll views back to the top
    private void ForceFullLayoutRebuild()
    {
        // true here includes inactive canvases too
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in allCanvases)
        {
            // Only root canvases need rebuilding, children rebuild along with them
            if (!canvas.isRootCanvas) continue;
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.GetComponent<RectTransform>());
        }

        // true here includes inactive scroll views too
        ScrollRect[] allScrollRects = FindObjectsOfType<ScrollRect>(true);
        foreach (ScrollRect scrollRect in allScrollRects)
        {
            // Snap every scroll view back to the top
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}