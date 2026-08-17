using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1. Define a struct to hold the category name and its custom sprite
[System.Serializable]
public struct CategoryVisual
{
    [Tooltip("The exact name of the category (e.g., 'Reading', 'Reasoning', 'Grammar')")]
    public string categoryName;
    
    [Tooltip("The sprite to apply to all stage buttons in this category")]
    public Sprite stageSprite;
}

public class LevelManager : MonoBehaviour
{
    [Header("Category Menus")]
    public GameObject[] categoryMenus; // Same 3 slots, repurposed per subject

    [Header("Custom Visuals")]
    public List<CategoryVisual> categoryVisuals = new List<CategoryVisual>();

    // Category name for each slot, per subject. Order must match categoryMenus' slot order.
    private static readonly Dictionary<string, string[]> subjectCategories = new Dictionary<string, string[]>
    {
        { "English", new[] { "Spelling", "Grammar", "Reading" } },
        { "Maths",   new[] { "Numbers", "Geometry", "Reasoning" } },
        { "Science", new[] { "Living Things", "Physical", "Earth & Space" } }
    };

    public void LoadCategoryProgress(int categoryIndex)
    {
        if (categoryIndex < 0 || categoryIndex >= categoryMenus.Length) return;

        GameObject menuRoot = categoryMenus[categoryIndex];

        // Fetch the current primary subject
        string currentSubject = PlayerPrefs.GetString("CurrentSubject", "English");

        // Resolve which category this slot represents for the current subject
        string categoryName = GetCategoryName(currentSubject, categoryIndex);

        // Update the on-screen label
        Transform screenLabelTransform = menuRoot.transform.Find("ScreenLabel");
        if (screenLabelTransform != null)
        {
            TextMeshProUGUI screenLabel = screenLabelTransform.GetComponent<TextMeshProUGUI>();
            if (screenLabel != null) screenLabel.text = categoryName;
        }

        // 2. Fetch the custom sprite for this specific category (if one is assigned)
        Sprite customSprite = GetCategorySprite(categoryName);

        // Updated string to match your hierarchy!
        Transform groupStage = menuRoot.transform.Find("Stage_Grid"); 
        if (groupStage != null)
        {
            for (int i = 0; i < groupStage.childCount; i++)
            {
                Transform stage = groupStage.GetChild(i);

                // Pass the data to the button
                StageButton btnScript = stage.GetComponent<StageButton>();
                if (btnScript != null) btnScript.SetupButtonData(categoryName, i);

                // 3. Apply the custom sprite to the button's background image
                if (customSprite != null)
                {
                    Image stageImage = stage.GetComponent<Image>();
                    if (stageImage != null) stageImage.sprite = customSprite;
                }

                // Fetch current stars
                string saveKey = $"{currentSubject}_{categoryName}_Stage_{i}_Stars";
                int starsEarned = PlayerPrefs.GetInt(saveKey, 0);

                UpdateStageVisuals(stage, starsEarned);
            }
        }
    }

    private string GetCategoryName(string subject, int slotIndex)
    {
        if (subjectCategories.TryGetValue(subject, out string[] categories) &&
            slotIndex >= 0 && slotIndex < categories.Length)
        {
            return categories[slotIndex];
        }

        Debug.LogWarning($"[LevelManager] No category mapping found for subject '{subject}' at slot {slotIndex}");
        return "Unknown";
    }

    // Helper method to find the sprite associated with the category name
    private Sprite GetCategorySprite(string targetCategoryName)
    {
        foreach (CategoryVisual visual in categoryVisuals)
        {
            if (visual.categoryName == targetCategoryName)
            {
                return visual.stageSprite;
            }
        }
        return null; // Returns null if no custom sprite is defined
    }

    private void UpdateStageVisuals(Transform stage, int starsEarned)
    {
        Transform starContainer = stage.Find("Star");
        if (starContainer != null)
        {
            for (int i = 0; i < starContainer.childCount; i++)
            {
                Image starIcon = starContainer.GetChild(i).GetComponent<Image>();
                if (starIcon != null)
                {
                    // If starsEarned is 1, index 0 is White, rest are Black
                    starIcon.color = (i < starsEarned) ? Color.white : Color.black;
                }
            }

            // Optional: Update a focus/active ring
            Transform focusTransform = stage.Find("Focus");
            if (focusTransform != null)
            {
                bool hasAllStars = starsEarned >= starContainer.childCount;
                focusTransform.gameObject.SetActive(hasAllStars);
            }
        }
    }
}