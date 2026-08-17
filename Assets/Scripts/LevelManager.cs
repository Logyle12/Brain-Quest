using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pairs a category name with the sprite that should be used for its stage buttons
[System.Serializable]
public struct CategoryVisual
{
    [Tooltip("The exact name of the category (e.g., 'Reading', 'Reasoning', 'Grammar')")]
    // Needs to match the category name used everywhere else exactly, or the sprite won't get picked up
    public string categoryName;

    [Tooltip("The sprite to apply to all stage buttons in this category")]
    // The sprite that gets applied to every stage button under this category
    public Sprite stageSprite;
}

// Refreshes a category slot on the level select screen, its label, its stage button sprites
// and its star progress, based on whichever subject the player currently has selected
public class LevelManager : MonoBehaviour
{
    [Header("Category Menus")]
    // The three category menu roots, each one gets reused and relabeled depending on the active subject
    public GameObject[] categoryMenus;

    [Header("Custom Visuals")]
    // Optional category to sprite pairings, used to reskin stage buttons for a given category
    public List<CategoryVisual> categoryVisuals = new List<CategoryVisual>();

    // Maps each subject to the three category names shown in its menu slots, in the same order as categoryMenus
    private static readonly Dictionary<string, string[]> subjectCategories = new Dictionary<string, string[]>
    {
        { "English", new[] { "Spelling", "Grammar", "Reading" } },
        { "Maths",   new[] { "Numbers", "Geometry", "Reasoning" } },
        { "Science", new[] { "Living Things", "Physical", "Earth & Space" } }
    };

    // Called whenever a category slot needs to be repainted, usually after paging to a new category
    public void LoadCategoryProgress(int categoryIndex)
    {
        // Ignore calls for a slot that doesn't actually exist
        if (categoryIndex < 0 || categoryIndex >= categoryMenus.Length) return;

        // The menu object this category slot lives under
        GameObject menuRoot = categoryMenus[categoryIndex];

        // Fetch the current primary subject
        string currentSubject = PlayerPrefs.GetString("CurrentSubject", "English");

        // Resolve which category this slot represents for the current subject
        string categoryName = GetCategoryName(currentSubject, categoryIndex);

        // Find the text element that shows the category name on screen
        Transform screenLabelTransform = menuRoot.transform.Find("ScreenLabel");
        // Only touch it if it actually exists under this menu
        if (screenLabelTransform != null)
        {
            // Grab the text component off that object
            TextMeshProUGUI screenLabel = screenLabelTransform.GetComponent<TextMeshProUGUI>();
            // Show the resolved category name
            if (screenLabel != null) screenLabel.text = categoryName;
        }

        // Look up a custom sprite for this category, if one was set up in the Inspector
        Sprite customSprite = GetCategorySprite(categoryName);

        // Find the grid holding all the stage buttons for this category
        Transform groupStage = menuRoot.transform.Find("Stage_Grid");
        // Only loop through stages if the grid was actually found
        if (groupStage != null)
        {
            // Go through every stage button under this grid
            for (int i = 0; i < groupStage.childCount; i++)
            {
                // The stage button sitting at this position in the grid
                Transform stage = groupStage.GetChild(i);

                // Grab the button's script so its data can be filled in
                StageButton btnScript = stage.GetComponent<StageButton>();
                // Tell the button which category and stage number it represents
                if (btnScript != null) btnScript.SetupButtonData(categoryName, i);

                // Only swap the sprite if a custom one is actually assigned for this category
                if (customSprite != null)
                {
                    // Grab the image component that shows the button's background
                    Image stageImage = stage.GetComponent<Image>();
                    // Apply the custom sprite to it
                    if (stageImage != null) stageImage.sprite = customSprite;
                }

                // Build the key this stage's stars were saved under
                string saveKey = $"{currentSubject}_{categoryName}_Stage_{i}_Stars";
                // Look up how many stars were earned on this stage, or 0 if it's never been played
                int starsEarned = PlayerPrefs.GetInt(saveKey, 0);

                // Update the stage's star icons and focus ring to match
                UpdateStageVisuals(stage, starsEarned);
            }
        }
    }

    // Looks up the category name that belongs in a given slot for a given subject
    private string GetCategoryName(string subject, int slotIndex)
    {
        // Try to find this subject's category list, and make sure the slot actually falls inside it
        if (subjectCategories.TryGetValue(subject, out string[] categories) &&
            slotIndex >= 0 && slotIndex < categories.Length)
        {
            // Return the category name sitting at that slot
            return categories[slotIndex];
        }

        // Nothing matched, so warn that this subject and slot combo has no mapping
        Debug.LogWarning($"[LevelManager] No category mapping found for subject '{subject}' at slot {slotIndex}");
        // Fall back to a placeholder rather than letting this break
        return "Unknown";
    }

    // Finds the custom sprite assigned to a specific category, if one exists
    private Sprite GetCategorySprite(string targetCategoryName)
    {
        // Check every category and sprite pairing set up in the Inspector
        foreach (CategoryVisual visual in categoryVisuals)
        {
            // Found the pairing that matches this category
            if (visual.categoryName == targetCategoryName)
            {
                // Hand back its sprite
                return visual.stageSprite;
            }
        }
        // No custom sprite was set up for this category
        return null;
    }

    // Colors in a stage's star icons and shows or hides its focus ring based on stars earned
    private void UpdateStageVisuals(Transform stage, int starsEarned)
    {
        // Find the container holding this stage's star icons
        Transform starContainer = stage.Find("Star");
        // Only update stars if the container was actually found
        if (starContainer != null)
        {
            // Go through every star icon under this container
            for (int i = 0; i < starContainer.childCount; i++)
            {
                // Grab the image component for this star
                Image starIcon = starContainer.GetChild(i).GetComponent<Image>();
                // Only color it if the component exists
                if (starIcon != null)
                {
                    // Stars up to the earned count turn white, the rest stay black
                    starIcon.color = (i < starsEarned) ? Color.white : Color.black;
                }
            }

            // Find the optional ring that highlights a fully starred stage
            Transform focusTransform = stage.Find("Focus");
            // Only touch it if this stage actually has one
            if (focusTransform != null)
            {
                // Only counts as fully starred once every star slot has been earned
                bool hasAllStars = starsEarned >= starContainer.childCount;
                // Show the ring only on a full clear
                focusTransform.gameObject.SetActive(hasAllStars);
            }
        }
    }
}