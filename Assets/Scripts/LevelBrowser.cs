using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Handles paging through a subject's category pages (e.g. Grammar, Reading),
// keeping track of which page you're on and saving that per subject
public class LevelBrowser : MonoBehaviour
{
    // Which page is currently showing
    private int pageNumber = 0;
    // Every page under panelTransform, populated on Start
    private List<GameObject> levelScenes = new List<GameObject>();
    [SerializeField] private Transform panelTransform;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    // Builds a PlayerPrefs key that's unique per subject, so each subject
    // remembers its own page instead of sharing one
    private string PageSaveKey 
    {
        get 
        {
            string currentSubject = PlayerPrefs.GetString("CurrentSubject", "English");
            return $"{currentSubject}_ActivePage";
        }
    }

    void Start()
    {
        // Setup buttons
        prevButton.onClick.AddListener(loadPrevScene);
        nextButton.onClick.AddListener(loadNextScene);

        // Populate list
        foreach (Transform levelScene in panelTransform) 
        {
            levelScenes.Add(levelScene.gameObject);
            // Hide every page for now, only the current one gets shown below
            levelScene.gameObject.SetActive(false);
        }

        // 1. Load the saved page index for this specific subject
        pageNumber = PlayerPrefs.GetInt(PageSaveKey, 0);
        
        // Failsafe in case a subject has fewer pages than the saved index
        if (levelScenes.Count > 0)
        {
            pageNumber = Mathf.Clamp(pageNumber, 0, levelScenes.Count - 1);
        }
        else
        {
            pageNumber = 0;
        }

        // 2. Set the view
        if (levelScenes.Count > 0)
        {
            levelScenes[pageNumber].SetActive(true);
        }

        // 3. Update UI
        controlLevelScenes();
    }

    // Called by the previous button
    public void loadPrevScene()
    {
        // Already on the first page, nothing to do
        if (pageNumber <= 0) return;

        // Hide the current page and show the one before it
        levelScenes[pageNumber].SetActive(false);
        pageNumber -= 1;
        levelScenes[pageNumber].SetActive(true);
        
        // Save the page for this specific subject
        PlayerPrefs.SetInt(PageSaveKey, pageNumber);
        controlLevelScenes();
    }

    // Called by the next button
    public void loadNextScene()
    {
        // Already on the last page, nothing to do
        if (pageNumber >= levelScenes.Count - 1) return;

        // Hide the current page and show the next one
        levelScenes[pageNumber].SetActive(false);
        pageNumber += 1;
        levelScenes[pageNumber].SetActive(true);

        // Save the page for this specific subject
        PlayerPrefs.SetInt(PageSaveKey, pageNumber);
        controlLevelScenes();
    }

    // Updates the prev/next buttons and refreshes the current page's progress
    private void controlLevelScenes() 
    {
        // Hide prev/next buttons when there's nowhere left to go in that direction
        prevButton.gameObject.SetActive(pageNumber > 0);
        nextButton.gameObject.SetActive(pageNumber < levelScenes.Count - 1);

        // Tell LevelManager to refresh the stars for this page
        FindObjectOfType<LevelManager>().LoadCategoryProgress(pageNumber);
    }
}