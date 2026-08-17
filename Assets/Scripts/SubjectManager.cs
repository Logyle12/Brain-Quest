using UnityEngine;
using UnityEngine.UI;

// Sits on the subject select screen, saves which subject the player picked so every
// other scene knows to load English, Maths or Science content
public class SubjectManager : MonoBehaviour
{
    [Header("Subject Buttons")]
    // Button for picking the English subject
    [SerializeField] private Button englishButton;
    // Button for picking the Maths subject
    [SerializeField] private Button mathButton;
    // Button for picking the Science subject
    [SerializeField] private Button scienceButton;

    // The PlayerPrefs key the chosen subject gets saved under
    private const string SubjectPrefKey = "CurrentSubject";

    // Hooks up each subject button to save its own subject when clicked
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

    // Saves the chosen subject so every other scene can read it back
    private void SetSubject(string subjectName)
    {
        // Overwrite whatever subject was saved before with this new one
        PlayerPrefs.SetString(SubjectPrefKey, subjectName);
        PlayerPrefs.Save();

        // Confirms the save happened and what it was set to, useful when debugging
        Debug.Log($"[SubjectManager] Current subject overwritten and saved as: {subjectName}");
    }
}