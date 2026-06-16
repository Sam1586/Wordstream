using UnityEngine.EventSystems;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class RulebookButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Panel")]
    [SerializeField] private GameObject rulebookPanel;
    [SerializeField] private Button closeButton;

    [Header("Blocking Panels")]
    [SerializeField] private GameObject[] blockingPanels;

    [Header("Animation")]
    [SerializeField] private string closeAnimationStateName = "Close_UI";
    [SerializeField] private float closeAnimationDuration = 0.35f;

    [Header("Hover")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite hoveredSprite;

    private Button openButton;
    private bool isClosing;
    private Coroutine closeRoutine;

    void Awake()
    {
        openButton = GetComponent<Button>();
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (defaultSprite == null && targetImage != null)
        {
            defaultSprite = targetImage.sprite;
        }

        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenRulebook);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseRulebook);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && IsRulebookActive())
        {
            CloseRulebook();
        }
    }

    void OnDisable()
    {
        SetButtonSprite(defaultSprite);
    }

    public void OpenRulebook()
    {
        if (rulebookPanel == null || IsBlocked())
        {
            return;
        }

        isClosing = false;
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }

        SetButtonSprite(defaultSprite);
        rulebookPanel.SetActive(true);
    }

    public void CloseRulebook()
    {
        if (rulebookPanel == null || isClosing || !rulebookPanel.activeInHierarchy)
        {
            return;
        }

        isClosing = true;

        Animator panelAnimator = rulebookPanel.GetComponent<Animator>();
        if (panelAnimator != null && !string.IsNullOrEmpty(closeAnimationStateName) &&
            panelAnimator.HasState(0, Animator.StringToHash(closeAnimationStateName)))
        {
            panelAnimator.Play(closeAnimationStateName, 0, 0f);
            closeRoutine = StartCoroutine(DeactivateAfterCloseAnimation());
        }
        else
        {
            DeactivateRulebookPanel();
        }
    }

    public void DeactivateRulebookPanel()
    {
        if (rulebookPanel != null)
        {
            rulebookPanel.SetActive(false);
        }

        isClosing = false;
        closeRoutine = null;
    }

    private IEnumerator DeactivateAfterCloseAnimation()
    {
        yield return new WaitForSecondsRealtime(closeAnimationDuration);
        DeactivateRulebookPanel();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetButtonSprite(hoveredSprite);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetButtonSprite(defaultSprite);
    }

    private void SetButtonSprite(Sprite sprite)
    {
        if (targetImage != null && sprite != null)
        {
            targetImage.sprite = sprite;
        }
    }

    private bool IsRulebookActive()
    {
        return rulebookPanel != null && rulebookPanel.activeInHierarchy;
    }

    private bool IsBlocked()
    {
        for (int i = 0; i < blockingPanels.Length; i++)
        {
            if (blockingPanels[i] != null && blockingPanels[i].activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }
}
