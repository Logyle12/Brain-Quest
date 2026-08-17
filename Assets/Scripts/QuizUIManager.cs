using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class QuizUIManager : MonoBehaviour
{
    [Header("Manager References")]
    public QuizManager quizManager;

    [Header("Gameplay UI")]
    public GameObject gameplayCanvas;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI[] optionButtonTexts;
    public Button[] optionButtons;

    [Header("End Screen Panels")]
    public GameObject winPanel;
    public GameObject failPanel;

    [Header("Win Screen - Stars")]
    [Tooltip("Assign in order: Star_1, Star_2, Star_3")]
    public GameObject starsContainer;
    private Image starsBackground;
    public Image[] winStarImages = new Image[3];

    [Header("Win Screen - FX")]
    [Tooltip("Only activates once all 3 stars are earned")]
    public GameObject fxBackLight;
    [Tooltip("Assign in order: Fx_Clear_1, Fx_Clear_2, Fx_Clear_3")]
    public GameObject[] starClearFx = new GameObject[3];
    public Color starEarnedColor = Color.white;
    public Color starUnearnedColor = Color.black;

    [Header("Play Again")]
    public Button playAgainButton; 
    public Button restartButton;

    [Header("Progress")]
    public Slider progressBar;
    public TextMeshProUGUI progressText;

    [Header("Feedback Sprites")]
    public Sprite defaultSprite;
    public Sprite correctSprite;
    public Sprite wrongSprite;

    [Header("Feedback SFX")]
    [Tooltip("AudioSource used to play the correct/wrong feedback sounds. Should be routed to the SFX mixer group.")]
    [SerializeField] private AudioSource feedbackAudioSource;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;

    [Header("Round Result SFX")]
    [Tooltip("Plays once when the win or fail panel is shown. Can reuse feedbackAudioSource or use a dedicated one.")]
    [SerializeField] private AudioSource roundResultAudioSource;
    [SerializeField] private AudioClip roundWinSound;
    [SerializeField] private AudioClip roundFailSound;

    [Header("Perfect Clear (3-Star) SFX")]
    [Tooltip("The main perfect-clear stinger, played at full volume")]
    [SerializeField] private AudioClip perfectClearSound;
    [Tooltip("Dedicated AudioSource for the accent clip (e.g. the powerup 'clap'). Needs to be its own source, separate from roundResultAudioSource, so it can be started/stopped independently mid-way through perfectClearSound.")]
    [SerializeField] private AudioSource perfectClearAccentSource;
    [Tooltip("Short accent clip played during a window inside perfectClearSound's timeline")]
    [SerializeField] private AudioClip perfectClearAccentSound;
    [Range(0f, 1f)]
    [Tooltip("Volume of perfectClearAccentSound relative to perfectClearSound, so it adds texture without overpowering it")]
    [SerializeField] private float perfectClearAccentVolumeScale = 0.4f;
    [Range(0f, 1f)]
    [Tooltip("Accent starts playing once perfectClearSound has reached this fraction of its length (e.g. 0.75 = 75% of the way through)")]
    [SerializeField] private float perfectClearAccentStartFraction = 0.75f;
    [Range(0f, 1f)]
    [Tooltip("Accent stops once perfectClearSound reaches this fraction of its length (e.g. 0.85 = 85% of the way through)")]
    [SerializeField] private float perfectClearAccentEndFraction = 0.85f;

    [Header("Timing")]
    [SerializeField] float delayBetweenQuestions = 1.5f;

    private bool isProcessingInput = false;

    void Awake()
    {
        if (starsContainer != null)
        {
            starsBackground = starsContainer.GetComponent<Image>();
        }
    }

    void Start()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnPlayAgainClicked);

        ResetUIState();
        InitializeButtonListeners();
        StartCoroutine(InitializeUI());
    }

    private void ResetUIState()
    {
        if (gameplayCanvas != null) gameplayCanvas.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);
        if (failPanel != null) failPanel.SetActive(false);
        if (fxBackLight != null) fxBackLight.SetActive(false);
        if (starsBackground != null) starsBackground.enabled = false;
    }

    public void OnPlayAgainClicked()
    {
        // 1. Reset logic and questions in manager
        quizManager.InitializeQuiz();

        // 2. Reset UI
        ResetUIState();
        isProcessingInput = false; 

        // 3. Restart the UI coroutine to show the new first question
        StartCoroutine(InitializeUI());
    }

    private void InitializeButtonListeners()
    {
        for (int index = 0; index < optionButtons.Length; index++)
        {
            int capturedIndex = index;
            if (optionButtons[index] != null)
                optionButtons[index].onClick.AddListener(() => OnOptionClicked(capturedIndex));
        }
    }

    IEnumerator InitializeUI()
    {
        while (quizManager == null || quizManager.GetCurrentQuestion() == null) 
            yield return null;

        if (progressBar != null)
        {
            progressBar.maxValue = quizManager.totalQuestionsThisRound;
            progressBar.value = 0;
        }
        
        UpdateQuestionUI();
        UpdateProgressBar();
    }

    public void OnOptionClicked(int index)
    {
        if (isProcessingInput) return;
        StartCoroutine(ProcessAnswerSequence(index));
    }

    IEnumerator ProcessAnswerSequence(int index)
    {
        isProcessingInput = true; 
        
        Button clickedButton = optionButtons[index];
        if (clickedButton != null)
            clickedButton.GetComponent<ButtonAnimation>()?.buttonClicked();

        DimNonSelectedButtons(index);

        QuestionData currentQuestionData = quizManager.GetCurrentQuestion();
        bool isAnswerCorrect = (index == currentQuestionData.correctAnswerIndex);
        
        SetButtonFeedbackSprite(index, isAnswerCorrect);
        PlayFeedbackSound(isAnswerCorrect);
        quizManager.ProcessAnswer(isAnswerCorrect);
        UpdateProgressBar();

        yield return new WaitForSeconds(delayBetweenQuestions);

        ResetButtonVisuals();
        
        if (quizManager.GetCurrentQuestion() != null)
        {
            UpdateQuestionUI();
            isProcessingInput = false; 
        }
        else
        {
            quizManager.EvaluateRound(); 
        }
    }

    public void ShowEndScreen(RoundResult result)
    {
        if (gameplayCanvas != null) gameplayCanvas.SetActive(false);
        bool isWin = (result == RoundResult.Win);
        if (winPanel != null) winPanel.SetActive(isWin);
        if (failPanel != null) failPanel.SetActive(!isWin);

        // Same threshold UpdateWinScreenStars uses for the backlight/FX -
        // a full 3-star clear gets its own sound instead of the normal win sound.
        bool isPerfectClear = isWin && quizManager != null && quizManager.starsEarned >= 3;
        PlayRoundResultSound(isWin, isPerfectClear);

        if (isWin)
        {
            UpdateWinScreenStars();
        }
    }

    private void PlayRoundResultSound(bool isWin, bool isPerfectClear)
    {
        if (roundResultAudioSource == null) return;

        if (isPerfectClear)
        {
            if (perfectClearSound != null)
            {
                roundResultAudioSource.PlayOneShot(perfectClearSound);

                if (perfectClearAccentSound != null && perfectClearAccentSource != null)
                {
                    StartCoroutine(PlayAccentDuringWindow(perfectClearSound.length));
                }
            }

            return;
        }

        AudioClip clipToPlay = isWin ? roundWinSound : roundFailSound;
        if (clipToPlay != null)
        {
            roundResultAudioSource.PlayOneShot(clipToPlay);
        }
    }

    // Waits until perfectClearSound has reached perfectClearAccentStartFraction of its
    // length, plays the accent clip, then stops it once perfectClearAccentEndFraction
    // is reached - so the accent only sounds during that window inside the stinger.
    private IEnumerator PlayAccentDuringWindow(float mainClipLength)
    {
        float startTime = mainClipLength * perfectClearAccentStartFraction;
        float endTime = mainClipLength * perfectClearAccentEndFraction;
        float windowDuration = Mathf.Max(0f, endTime - startTime);

        yield return new WaitForSeconds(startTime);

        perfectClearAccentSource.clip = perfectClearAccentSound;
        perfectClearAccentSource.loop = false;
        perfectClearAccentSource.volume = perfectClearAccentVolumeScale;
        perfectClearAccentSource.Play();

        yield return new WaitForSeconds(windowDuration);

        perfectClearAccentSource.Stop();
    }

    private void UpdateWinScreenStars()
    {
        if (quizManager == null) return;

        int starsEarned = quizManager.starsEarned;

        // Set each star's color based on whether it has been earned
        for (int i = 0; i < winStarImages.Length; i++)
        {
            if (winStarImages[i] == null) continue;
            winStarImages[i].color = (i < starsEarned) ? starEarnedColor : starUnearnedColor;
        }

        // Toggle each star's clear FX based on whether it has been earned
        for (int i = 0; i < starClearFx.Length; i++)
        {
            if (starClearFx[i] == null) continue;
            starClearFx[i].SetActive(i < starsEarned);
        }

        // Backlight only activates on a full 3-star clear
        if (fxBackLight != null) fxBackLight.SetActive(starsEarned >= 3);
        if (starsBackground != null) starsBackground.enabled = starsEarned >= 3;
    }

    private void UpdateProgressBar()
    {
        if (progressBar != null) progressBar.value = quizManager.correctAnswers;
        if (progressText != null) progressText.text = $"{quizManager.correctAnswers} / {quizManager.totalQuestionsThisRound}";
    }

    private void DimNonSelectedButtons(int selectedButtonIndex)
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) continue;
            Image buttonImage = optionButtons[i].GetComponent<Image>();
            Color targetColor = buttonImage.color;
            targetColor.a = (i == selectedButtonIndex) ? 1.0f : 0.3f;
            buttonImage.color = targetColor;
        }
    }

    private void ResetButtonVisuals()
    {
        foreach (Button currentButton in optionButtons)
        {
            if (currentButton == null) continue;
            Image buttonImage = currentButton.GetComponent<Image>();
            buttonImage.color = new Color(1, 1, 1, 1);
            buttonImage.sprite = defaultSprite;
        }
    }

    private void PlayFeedbackSound(bool isCorrect)
    {
        if (feedbackAudioSource == null) return;

        AudioClip clipToPlay = isCorrect ? correctSound : wrongSound;
        if (clipToPlay != null)
        {
            // PlayOneShot rather than Play, so this can't get cut off by another
            // sound sharing the same AudioSource.
            feedbackAudioSource.PlayOneShot(clipToPlay);
        }
    }

    private void SetButtonFeedbackSprite(int index, bool isCorrect)
    {
        if (index >= 0 && index < optionButtons.Length && optionButtons[index] != null)
        {
            optionButtons[index].GetComponent<Image>().sprite = isCorrect ? correctSprite : wrongSprite;
        }
    }

    private void UpdateQuestionUI()
    {
        QuestionData questionData = quizManager.GetCurrentQuestion();
        if (questionData != null)
        {
            questionText.text = questionData.questionText;
            for (int i = 0; i < optionButtonTexts.Length; i++)
            {
                if (i < questionData.options.Length)
                    optionButtonTexts[i].text = questionData.options[i];
            }
        }
    }
}