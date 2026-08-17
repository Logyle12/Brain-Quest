using UnityEngine;
using UnityEngine.UI;

public class SubjectManager : MonoBehaviour
{
    [Header("Subject Buttons")]
    [SerializeField] private Button englishButton;
    [SerializeField] private Button mathButton;
    [SerializeField] private Button scienceButton;

    // Constant string to prevent runtime spelling errors when accessing PlayerPrefs
    private const string SubjectPrefKey = "CurrentSubject";

    void Start()
    {
        // Bind listeners dynamically
        if (englishButton != null) 
            englishButton.onClick.AddListener(() => SetSubject("English"));
            
        if (mathButton != null) 
            mathButton.onClick.AddListener(() => SetSubject("Maths"));
            
        if (scienceButton != null) 
            scienceButton.onClick.AddListener(() => SetSubject("Science"));
    }

    private void SetSubject(string subjectName)
    {
        PlayerPrefs.SetString(SubjectPrefKey, subjectName);
        PlayerPrefs.Save();
        
        Debug.Log($"[SubjectManager] Current subject overwritten and saved as: {subjectName}");
    }
}