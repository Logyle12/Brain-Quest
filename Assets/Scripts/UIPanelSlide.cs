using UnityEngine;
using System.Collections;

// Lives on the child "Panel" object under SettingsPanelController. This object is
// fine to be inactive by default and to be toggled with SetActive - persistence is
// now handled by SettingsPanelController living on an always-active parent, so this
// script no longer needs a dontDestroyOnLoad flag of its own.
public class UIPanelSlide : MonoBehaviour
{
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup dimmer;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float dimmerFadeDuration = 0.2f;

    private Vector2 hiddenPos;
    private int pendingHideTweens;
    private bool initialized;

    void Awake()
    {
        InitializeHiddenPos();
    }

    private void InitializeHiddenPos()
    {
        if (initialized) return;
        hiddenPos = new Vector2(0, -panelRect.rect.height);
        initialized = true;
    }

    private IEnumerator LogPositionOverTime()
    {
        float t = 0f;
        while (t < 1.5f)
        {
            Debug.Log($"[UIPanelSlide] t={t:F2} anchoredPos={panelRect.anchoredPosition} timeScale={Time.timeScale}");
            yield return new WaitForSeconds(0.1f);
            t += 0.1f;
        }
    }

    public void Show()
    {
        // SetActive(true) must come first: on the very first activation this is what
        // triggers Awake() to run, which is what makes panelRect.rect.height reliable
        // (some layout setups only resolve real dimensions once the object is active).
        // Calling InitializeHiddenPos() before this line reads that height too early.
        gameObject.SetActive(true);
        InitializeHiddenPos();
        panelRect.anchoredPosition = hiddenPos;

        Debug.Log($"[UIPanelSlide] rect.height={panelRect.rect.height}, hiddenPos={hiddenPos}, current anchoredPos={panelRect.anchoredPosition}");

        if (dimmer != null)
        {
            dimmer.alpha = 0;
            dimmer.interactable = true;
            dimmer.blocksRaycasts = true;
            dimmer.LeanAlpha(1, duration);
        }

        // Tween anchoredPosition directly instead of LeanMoveLocalY (which tweens
        // transform.localPosition). anchoredPosition and localPosition only match
        // when the parent's pivot lines up with this rect's anchor point - in this
        // scene it doesn't, which is what caused the panel to stop halfway.
        // The setOnComplete snap corrects for easeOutExpo landing at ~0.999 of the
        // distance instead of exactly 1.0, which otherwise leaves a hairline gap.
        LeanTween.value(gameObject, panelRect.anchoredPosition.y, 0f, duration)
                 .setOnUpdate((float y) => panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, y))
                 .setEaseOutExpo()
                 .setOnComplete(() => panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, 0f));
        StartCoroutine(LogPositionOverTime());
    }

    public void Hide()
    {
        pendingHideTweens = dimmer != null ? 2 : 1;

        if (dimmer != null)
        {
            dimmer.interactable = false;
            dimmer.blocksRaycasts = false;
            dimmer.LeanAlpha(0, dimmerFadeDuration)
                  .setEaseOutExpo()
                  .setOnComplete(OnHideTweenFinished);
        }

        LeanTween.value(gameObject, panelRect.anchoredPosition.y, hiddenPos.y, duration)
                 .setOnUpdate((float y) => panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, y))
                 .setEaseOutExpo()
                 .setOnComplete(OnHideTweenFinished);
    }

    private void OnHideTweenFinished()
    {
        pendingHideTweens--;
        if (pendingHideTweens > 0) return;

        if (dimmer != null) dimmer.alpha = 0;
        panelRect.anchoredPosition = hiddenPos;

        gameObject.SetActive(false);
    }
}