using UnityEngine;
using System.Collections;

// Handles the slide in/out and dimmer fade animation for a UI panel, used by the
// settings panel to animate open and closed
public class UIPanelSlide : MonoBehaviour
{
    // The panel's own rect transform, the one that actually gets moved
    [SerializeField] private RectTransform panelRect;
    // Optional dark overlay behind the panel that fades in and out with it
    [SerializeField] private CanvasGroup dimmer;
    // How long the slide animation takes
    [SerializeField] private float duration = 0.5f;
    // How long the dimmer takes to fade, separate from the slide duration
    [SerializeField] private float dimmerFadeDuration = 0.2f;

    // The off screen position the panel sits at while hidden
    private Vector2 hiddenPos;
    // How many of the hide animation's tweens are still running, used to know when it's fully done
    private int pendingHideTweens;
    // Whether hiddenPos has already been calculated
    private bool initialized;

    // Runs as soon as this object wakes up, works out where the panel should sit while hidden
    void Awake()
    {
        // Calculate the hidden position now so it's ready before Show or Hide ever get called
        InitializeHiddenPos();
    }

    // Works out the panel's hidden position based on its own height, only needs to run once
    private void InitializeHiddenPos()
    {
        // Already calculated, no need to do it again
        if (initialized) return;
        // Sits one full panel height below its resting position
        hiddenPos = new Vector2(0, -panelRect.rect.height);
        // Mark this as done so it doesn't get recalculated
        initialized = true;
    }

    // Debug helper that logs the panel's position over time while it's animating
    private IEnumerator LogPositionOverTime()
    {
        // Tracks how long this logging has been running
        float t = 0f;
        // Keep logging for a set window after the panel starts showing
        while (t < 1.5f)
        {
            // Print the current position and timescale for debugging
            Debug.Log($"[UIPanelSlide] t={t:F2} anchoredPos={panelRect.anchoredPosition} timeScale={Time.timeScale}");
            // Wait a short moment before logging again
            yield return new WaitForSeconds(0.1f);
            // Advance the tracked time
            t += 0.1f;
        }
    }

    // Slides the panel into view and fades in its dimmer, called when the panel should open
    public void Show()
    {

        // Make sure the panel object is actually active before animating it
        gameObject.SetActive(true);
        // Make sure the hidden position has been calculated
        InitializeHiddenPos();
        // Snap the panel to its hidden position before animating in, so it always slides from the same place
        panelRect.anchoredPosition = hiddenPos;

        // Log the starting state, useful for tracking down layout issues
        Debug.Log($"[UIPanelSlide] rect.height={panelRect.rect.height}, hiddenPos={hiddenPos}, current anchoredPos={panelRect.anchoredPosition}");

        // Only animate the dimmer if one was assigned
        if (dimmer != null)
        {
            // Start fully transparent
            dimmer.alpha = 0;
            // Let it start blocking and receiving input as soon as it appears
            dimmer.interactable = true;
            dimmer.blocksRaycasts = true;
            // Fade it up to fully visible
            dimmer.LeanAlpha(1, duration);
        }

        // Animate the panel's y position from hidden up to its resting spot at 0
        LeanTween.value(gameObject, panelRect.anchoredPosition.y, 0f, duration)
                 .setOnUpdate((float y) => panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, y))
                 .setEaseOutExpo()
                 .setOnComplete(() => panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, 0f));
        // Start logging the panel's position while it animates, for debugging
        StartCoroutine(LogPositionOverTime());
    }

    // Slides the panel back out of view and fades out its dimmer, called when the panel should close
    public void Hide()
    {
        // Track two tweens to wait for if there's a dimmer, otherwise just the one for the panel
        pendingHideTweens = dimmer != null ? 2 : 1;

        // Only animate the dimmer if one was assigned
        if (dimmer != null)
        {
            // Stop it from blocking or receiving input as it fades away
            dimmer.interactable = false;
            dimmer.blocksRaycasts = false;
            // Fade it back down to transparent
            dimmer.LeanAlpha(0, dimmerFadeDuration)
                  .setEaseOutExpo()
                  .setOnComplete(OnHideTweenFinished);
        }

        // Animate the panel's y position from wherever it is back down to hidden
        LeanTween.value(gameObject, panelRect.anchoredPosition.y, hiddenPos.y, duration)
                 .setOnUpdate((float y) => panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, y))
                 .setEaseOutExpo()
                 .setOnComplete(OnHideTweenFinished);
    }

    // Called once each hide tween finishes, only finalizes the hidden state once every tween is done
    private void OnHideTweenFinished()
    {
        // One less tween left to wait for
        pendingHideTweens--;
        // Still waiting on another tween, don't finalize yet
        if (pendingHideTweens > 0) return;

        // Make sure the dimmer ends fully transparent
        if (dimmer != null) dimmer.alpha = 0;
        // Snap the panel exactly to its hidden position
        panelRect.anchoredPosition = hiddenPos;

        // Deactivate the panel now that it's fully hidden
        gameObject.SetActive(false);
    }
}