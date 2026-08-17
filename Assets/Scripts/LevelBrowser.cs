using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelBrowser : MonoBehaviour
{
    private int pageNumber = 0;
    private List<GameObject> levelScenes = new List<GameObject>();
    [SerializeField] private Transform panelTransform;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    // Helper property to get a subject-specific save key
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

    public void loadPrevScene()
    {
        if (pageNumber <= 0) return;

        levelScenes[pageNumber].SetActive(false);
        pageNumber -= 1;
        levelScenes[pageNumber].SetActive(true);
        
        // Save the page for this specific subject
        PlayerPrefs.SetInt(PageSaveKey, pageNumber);
        controlLevelScenes();
    }

    public void loadNextScene()
    {
        if (pageNumber >= levelScenes.Count - 1) return;

        levelScenes[pageNumber].SetActive(false);
        pageNumber += 1;
        levelScenes[pageNumber].SetActive(true);

        // Save the page for this specific subject
        PlayerPrefs.SetInt(PageSaveKey, pageNumber);
        controlLevelScenes();
    }

    private void controlLevelScenes() 
    {
        prevButton.gameObject.SetActive(pageNumber > 0);
        nextButton.gameObject.SetActive(pageNumber < levelScenes.Count - 1);

        // Tell LevelManager to refresh the stars for this page
        FindObjectOfType<LevelManager>().LoadCategoryProgress(pageNumber);
    }
}