using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

// Drives the quiz screen during gameplay, shows questions and answers, plays feedback,
// and swaps over to the win or fail screen once the round ends
public class QuizUIManager : MonoBehaviour
{
    [Header("Manager References")]
    // The manager that holds the actual question data and scoring logic
    public QuizManager quizManager;

    [Header("Gameplay UI")]
    // The canvas holding the live question and answer buttons
    public GameObject gameplayCanvas;
    // Text element showing the current question
    public TextMeshProUGUI questionText;
    // Text labels on each answer button
    public TextMeshProUGUI[] optionButtonTexts;
    // The four answer buttons themselves
    public Button[] optionButtons;

    [Header("End Screen Panels")]
    // Shown when the round ends in a win
    public GameObject winPanel;
    // Shown when the round ends in a fail or partial pass
    public GameObject failPanel;

    [Header("Win Screen - Stars")]
    [Tooltip("Assign in order: Star_1, Star_2, Star_3")]
    // Parent object holding the win screen's star images, also used to grab the background image below
    public GameObject starsContainer;
    // Background image behind the stars, cached from starsContainer, only shown on a perfect clear
    private Image starsBackground;
    // The three star icons shown on the win screen
    public Image[] winStarImages = new Image[3];

    [Header("Win Screen - FX")]
    [Tooltip("Only activates once all 3 stars are earned")]
    // Extra glow effect that only turns on for a full 3 star clear
    public GameObject fxBackLight;
    [Tooltip("Assign in order: Fx_Clear_1, Fx_Clear_2, Fx_Clear_3")]
    // Effect played next to each star as it gets earned
    public GameObject[] starClearFx = new GameObject[3];
    // Color a star turns once it has been earned
    public Color starEarnedColor = Color.white;
    // Color a star stays at if it hasn't been earned
    public Color starUnearnedColor = Color.black;

    [Header("Play Again")]
    // Button shown on the end screen to start a new round
    public Button playAgainButton;
    // Second button that does the same thing, likely sitting on a different panel
    public Button restartButton;

    [Header("Progress")]
    // Fills up as the player answers questions correctly
    public Slider progressBar;
    // Shows the numeric progress alongside the bar
    public TextMeshProUGUI progressText;

    [Header("Feedback Sprites")]
    // Neutral sprite an answer button resets to between questions
    public Sprite defaultSprite;
    // Sprite shown on the button that was the correct answer
    public Sprite correctSprite;
    // Sprite shown on a button when it was picked incorrectly
    public Sprite wrongSprite;

    [Header("Feedback SFX")]
    [Tooltip("AudioSource used to play the correct/wrong feedback sounds. Should be routed to the SFX mixer group.")]
    // Source used to play the correct and wrong feedback sounds
    [SerializeField] private AudioSource feedbackAudioSource;
    // Plays when the player picks the right answer
    [SerializeField] private AudioClip correctSound;
    // Plays when the player picks the wrong answer
    [SerializeField] private AudioClip wrongSound;

    [Header("Round Result SFX")]
    [Tooltip("Plays once when the win or fail panel is shown. Can reuse feedbackAudioSource or use a dedicated one.")]
    // Source used to play the win or fail sound once per round
    [SerializeField] private AudioSource roundResultAudioSource;
    // Plays on a normal win, not a perfect clear
    [SerializeField] private AudioClip roundWinSound;
    // Plays when the round ends in a fail
    [SerializeField] private AudioClip roundFailSound;

    [Header("Perfect Clear (3-Star) SFX")]
    [Tooltip("The main perfect-clear stinger, played at full volume")]
    // The main stinger played on a full 3 star clear
    [SerializeField] private AudioClip perfectClearSound;
    [Tooltip("Dedicated AudioSource for the accent clip (e.g. the powerup 'clap'). Needs to be its own source, separate from roundResultAudioSource, so it can be started/stopped independently mid-way through perfectClearSound.")]
    // Its own source so the accent can start and stop independently mid way through the main stinger
    [SerializeField] private AudioSource perfectClearAccentSource;
    [Tooltip("Short accent clip played during a window inside perfectClearSound's timeline")]
    // Short accent layered on top of the stinger partway through
    [SerializeField] private AudioClip perfectClearAccentSound;
    [Range(0f, 1f)]
    [Tooltip("Volume of perfectClearAccentSound relative to perfectClearSound, so it adds texture without overpowering it")]
    // Keeps the accent quieter than the main stinger so it doesn't overpower it
    [SerializeField] private float perfectClearAccentVolumeScale = 0.4f;
    [Range(0f, 1f)]
    [Tooltip("Accent starts playing once perfectClearSound has reached this fraction of its length (e.g. 0.75 = 75% of the way through)")]
    // How far into the stinger the accent should start playing
    [SerializeField] private float perfectClearAccentStartFraction = 0.75f;
    [Range(0f, 1f)]
    [Tooltip("Accent stops once perfectClearSound reaches this fraction of its length (e.g. 0.85 = 85% of the way through)")]
    // How far into the stinger the accent should stop playing
    [SerializeField] private float perfectClearAccentEndFraction = 0.85f;

    [Header("Timing")]
    // How long feedback stays on screen before moving on to the next question
    [SerializeField] float delayBetweenQuestions = 1.5f;

    // Blocks extra clicks while an answer is already being processed
    private bool isProcessingInput = false;

    // Caches the win screen's background image before anything else needs it
    void Awake()
    {
        // Only grab it if the stars container was actually assigned
        if (starsContainer != null)
        {
            // Cache the background image sitting on the same object as the stars
            starsBackground = starsContainer.GetComponent<Image>();
        }
    }

    // Wires up the buttons and gets the first question ready to show
    void Start()
    {
        // Hook up the play again button if one was assigned
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);

        // Same for the restart button, in case a different panel uses it instead
        if (restartButton != null)
            restartButton.onClick.AddListener(OnPlayAgainClicked);

        // Make sure the screen starts on the gameplay canvas with both end panels hidden
        ResetUIState();
        // Hook up each answer button to know its own index
        InitializeButtonListeners();
        // Wait for the quiz manager to have a question ready, then show it
        StartCoroutine(InitializeUI());
    }

    // Puts the screen back into its starting state, gameplay showing, end panels and win FX hidden
    private void ResetUIState()
    {
        // Show the live gameplay canvas
        if (gameplayCanvas != null) gameplayCanvas.SetActive(true);
        // Hide the win panel
        if (winPanel != null) winPanel.SetActive(false);
        // Hide the fail panel
        if (failPanel != null) failPanel.SetActive(false);
        // Turn off the perfect clear backlight
        if (fxBackLight != null) fxBackLight.SetActive(false);
        // Turn off the perfect clear background image
        if (starsBackground != null) starsBackground.enabled = false;
    }

    // Starts a brand new round from scratch, called by both the play again and restart buttons
    public void OnPlayAgainClicked()
    {
        // Ask the quiz manager to pick a fresh set of questions
        quizManager.InitializeQuiz();

        // Put the UI back to its starting state
        ResetUIState();
        // Allow answer clicks again
        isProcessingInput = false;

        // Wait for the new first question to be ready, then show it
        StartCoroutine(InitializeUI());
    }

    // Hooks up each answer button so clicking it reports its own index back
    private void InitializeButtonListeners()
    {
        // Go through every answer button
        for (int index = 0; index < optionButtons.Length; index++)
        {
            // Capture the index in a local variable so the closure below uses the right one
            int capturedIndex = index;
            // Only hook up buttons that actually exist
            if (optionButtons[index] != null)
                optionButtons[index].onClick.AddListener(() => OnOptionClicked(capturedIndex));
        }
    }

    // Waits until the quiz manager actually has a question ready, then sets up the progress bar and shows it
    IEnumerator InitializeUI()
    {
        // Keep waiting a frame at a time until there's a manager and a question to show
        while (quizManager == null || quizManager.GetCurrentQuestion() == null)
            yield return null;

        // Only touch the progress bar if one was assigned
        if (progressBar != null)
        {
            // Set the bar's max to however many questions this round has
            progressBar.maxValue = quizManager.totalQuestionsThisRound;
            // Start the bar empty
            progressBar.value = 0;
        }

        // Fill in the question text and answer options
        UpdateQuestionUI();
        // Sync the progress bar and its label to the current score
        UpdateProgressBar();
    }

    // Called when the player taps an answer button, kicks off the whole answer sequence
    public void OnOptionClicked(int index)
    {
        // Ignore taps while an answer is already being processed
        if (isProcessingInput) return;
        // Run the feedback, scoring and next question sequence
        StartCoroutine(ProcessAnswerSequence(index));
    }

    // Plays the click animation and feedback for the chosen answer, updates score and progress,
    // waits, then either shows the next question or wraps up the round
    IEnumerator ProcessAnswerSequence(int index)
    {
        // Block further clicks until this sequence finishes
        isProcessingInput = true;

        // The button the player actually clicked
        Button clickedButton = optionButtons[index];
        // Play its click animation if it has one
        if (clickedButton != null)
            clickedButton.GetComponent<ButtonAnimation>()?.buttonClicked();

        // Fade out every button except the one that was picked
        DimNonSelectedButtons(index);

        // The question that was just answered
        QuestionData currentQuestionData = quizManager.GetCurrentQuestion();
        // Whether the picked index matches the correct answer
        bool isAnswerCorrect = (index == currentQuestionData.correctAnswerIndex);

        // Swap the clicked button's sprite to show right or wrong
        SetButtonFeedbackSprite(index, isAnswerCorrect);
        // Play the matching feedback sound
        PlayFeedbackSound(isAnswerCorrect);
        // Tell the quiz manager the result so it can update score and saved progress
        quizManager.ProcessAnswer(isAnswerCorrect);
        // Refresh the progress bar and its label
        UpdateProgressBar();

        // Give the player a moment to see the feedback before moving on
        yield return new WaitForSeconds(delayBetweenQuestions);

        // Reset every button back to its default look
        ResetButtonVisuals();

        // Still more questions left in this round
        if (quizManager.GetCurrentQuestion() != null)
        {
            // Show the next question
            UpdateQuestionUI();
            // Allow clicks again
            isProcessingInput = false;
        }
        else
        {
            // No questions left, wrap up the round
            quizManager.EvaluateRound();
        }
    }

    // Switches from the gameplay canvas to the win or fail panel and plays the matching round result sound
    public void ShowEndScreen(RoundResult result)
    {
        // Hide the live gameplay canvas
        if (gameplayCanvas != null) gameplayCanvas.SetActive(false);
        // Whether this round counts as a win
        bool isWin = (result == RoundResult.Win);
        // Show the win panel only on a win
        if (winPanel != null) winPanel.SetActive(isWin);
        // Show the fail panel for anything that isn't a win
        if (failPanel != null) failPanel.SetActive(!isWin);

        // Same threshold UpdateWinScreenStars uses for the backlight and FX, a full
        // 3-star clear gets its own sound instead of the normal win sound
        bool isPerfectClear = isWin && quizManager != null && quizManager.starsEarned >= 3;
        // Play whichever sound matches this outcome
        PlayRoundResultSound(isWin, isPerfectClear);

        // Only update the stars and FX if the round was actually won
        if (isWin)
        {
            UpdateWinScreenStars();
        }
    }

    // Picks and plays the right one shot sound for how the round ended
    private void PlayRoundResultSound(bool isWin, bool isPerfectClear)
    {
        // Nothing to play through if no source was assigned
        if (roundResultAudioSource == null) return;

        // A perfect clear gets its own dedicated stinger instead of the normal win sound
        if (isPerfectClear)
        {
            // Only play if a clip is actually assigned
            if (perfectClearSound != null)
            {
                // Play the main stinger
                roundResultAudioSource.PlayOneShot(perfectClearSound);

                // Layer in the accent clip partway through, if both the clip and its source exist
                if (perfectClearAccentSound != null && perfectClearAccentSource != null)
                {
                    StartCoroutine(PlayAccentDuringWindow(perfectClearSound.length));
                }
            }

            // Perfect clear handled, nothing more to do
            return;
        }

        // Otherwise fall back to the normal win or fail sound
        AudioClip clipToPlay = isWin ? roundWinSound : roundFailSound;
        // Only play if a clip is actually assigned
        if (clipToPlay != null)
        {
            roundResultAudioSource.PlayOneShot(clipToPlay);
        }
    }

    // Waits until perfectClearSound has reached perfectClearAccentStartFraction of its
    // length, plays the accent clip, then stops it once perfectClearAccentEndFraction
    // is reached, so the accent only sounds during that window inside the stinger
    private IEnumerator PlayAccentDuringWindow(float mainClipLength)
    {
        // How far into the stinger the accent should start
        float startTime = mainClipLength * perfectClearAccentStartFraction;
        // How far into the stinger the accent should stop
        float endTime = mainClipLength * perfectClearAccentEndFraction;
        // How long the accent actually plays for, never negative
        float windowDuration = Mathf.Max(0f, endTime - startTime);

        // Wait until the stinger reaches the accent's start point
        yield return new WaitForSeconds(startTime);

        // Set up the accent source with this clip
        perfectClearAccentSource.clip = perfectClearAccentSound;
        // Play it once, not looped
        perfectClearAccentSource.loop = false;
        // Keep it quieter than the main stinger
        perfectClearAccentSource.volume = perfectClearAccentVolumeScale;
        // Start the accent playing
        perfectClearAccentSource.Play();

        // Let it play for the length of its window
        yield return new WaitForSeconds(windowDuration);

        // Cut it off once the window closes
        perfectClearAccentSource.Stop();
    }

    // Colors in the win screen's stars, toggles their clear FX, and turns on the perfect
    // clear backlight if all three were earned
    private void UpdateWinScreenStars()
    {
        // Nothing to update without a quiz manager to read stars from
        if (quizManager == null) return;

        // How many stars were earned on this stage
        int starsEarned = quizManager.starsEarned;

        // Go through each star image
        for (int i = 0; i < winStarImages.Length; i++)
        {
            // Skip any that weren't assigned
            if (winStarImages[i] == null) continue;
            // Set each star's color based on whether it has been earned
            winStarImages[i].color = (i < starsEarned) ? starEarnedColor : starUnearnedColor;
        }

        // Go through each star's clear effect
        for (int i = 0; i < starClearFx.Length; i++)
        {
            // Skip any that weren't assigned
            if (starClearFx[i] == null) continue;
            // Toggle each star's clear FX based on whether it has been earned
            starClearFx[i].SetActive(i < starsEarned);
        }

        // Backlight only activates on a full 3 star clear
        if (fxBackLight != null) fxBackLight.SetActive(starsEarned >= 3);
        // Same rule for the background behind the stars
        if (starsBackground != null) starsBackground.enabled = starsEarned >= 3;
    }

    // Syncs the progress bar and its label to the quiz manager's current score
    private void UpdateProgressBar()
    {
        // Move the bar to match how many questions have been answered correctly
        if (progressBar != null) progressBar.value = quizManager.correctAnswers;
        // Update the label to show the same thing as text
        if (progressText != null) progressText.text = $"{quizManager.correctAnswers} / {quizManager.totalQuestionsThisRound}";
    }

    // Fades out every answer button except the one the player just picked, so the choice stands out
    private void DimNonSelectedButtons(int selectedButtonIndex)
    {
        // Go through every answer button
        for (int i = 0; i < optionButtons.Length; i++)
        {
            // Skip any that weren't assigned
            if (optionButtons[i] == null) continue;
            // Grab this button's image component
            Image buttonImage = optionButtons[i].GetComponent<Image>();
            // Start from its current color
            Color targetColor = buttonImage.color;
            // Full opacity for the picked button, faded for every other
            targetColor.a = (i == selectedButtonIndex) ? 1.0f : 0.3f;
            // Apply the new opacity
            buttonImage.color = targetColor;
        }
    }

    // Puts every answer button back to its default color and sprite between questions
    private void ResetButtonVisuals()
    {
        // Go through every answer button
        foreach (Button currentButton in optionButtons)
        {
            // Skip any that weren't assigned
            if (currentButton == null) continue;
            // Grab this button's image component
            Image buttonImage = currentButton.GetComponent<Image>();
            // Reset it back to fully visible
            buttonImage.color = new Color(1, 1, 1, 1);
            // Reset it back to the neutral sprite
            buttonImage.sprite = defaultSprite;
        }
    }

    // Plays the correct or wrong sound depending on how the player answered
    private void PlayFeedbackSound(bool isCorrect)
    {
        // Nothing to play through if no source was assigned
        if (feedbackAudioSource == null) return;

        // Pick whichever clip matches the result
        AudioClip clipToPlay = isCorrect ? correctSound : wrongSound;
        // Only play if a clip is actually assigned
        if (clipToPlay != null)
        {
            // PlayOneShot rather than Play, so this can't get cut off by another
            // sound sharing the same AudioSource
            feedbackAudioSource.PlayOneShot(clipToPlay);
        }
    }

    // Swaps the clicked answer button's sprite to show whether it was right or wrong
    private void SetButtonFeedbackSprite(int index, bool isCorrect)
    {
        // Make sure the index is actually valid and the button exists
        if (index >= 0 && index < optionButtons.Length && optionButtons[index] != null)
        {
            // Apply the correct or wrong sprite to that button
            optionButtons[index].GetComponent<Image>().sprite = isCorrect ? correctSprite : wrongSprite;
        }
    }

    // Pulls the current question from the quiz manager and fills in the question text and answer button labels
    private void UpdateQuestionUI()
    {
        // The question that should currently be shown
        QuestionData questionData = quizManager.GetCurrentQuestion();
        // Only update the UI if there's actually a question to show
        if (questionData != null)
        {
            // Set the question text
            questionText.text = questionData.questionText;
            // Go through every answer button's text label
            for (int i = 0; i < optionButtonTexts.Length; i++)
            {
                // Only fill it in if this question actually has that many options
                if (i < questionData.options.Length)
                    optionButtonTexts[i].text = questionData.options[i];
            }
        }
    }
}