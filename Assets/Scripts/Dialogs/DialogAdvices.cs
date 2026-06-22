using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class DialogAdvices : MonoBehaviour
{
    private enum AdviceCorner
    {
        TopLeft,
        TopRight
    }

    [Header("Content")]
    [TextArea(2, 6)]
    [SerializeField] private string[] lines;
    [SerializeField] private string adviceId;
    [SerializeField] private TMP_FontAsset adviceFont;

    [Header("Show Options")]
    [SerializeField] private bool showOnlyOnce = true;
    [SerializeField] private bool useDelay = false;
    [SerializeField] private float delayBeforeShow = 0.5f;
    [SerializeField] private bool buttonRequired = false;
    [SerializeField] private Key continueKey = Key.Enter;

    [Header("Functions to block while showing")]
    [SerializeField] private MonoBehaviour[] componentsToDisableWhileShowing;

    [Header("Placement")]
    [SerializeField] private AdviceCorner corner = AdviceCorner.TopLeft;
    [SerializeField] private Vector2 screenOffset = new Vector2(36f, 36f);
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private bool useExistingCanvas = false;

    [Header("Visual")]
    [SerializeField] private Vector2 panelSize = new Vector2(1040f, 300f);

    [Header("Text Container")]
    [SerializeField] private Vector2 textContainerSize = new Vector2(500f, 200f);
    [SerializeField] private Vector2 textContainerOffset = new Vector2(-80f, 50f);

    [SerializeField] private Color backgroundTint = Color.white;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private float textSize = 40f;

    [Header("Timing")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float charactersPerSecond = 40f;
    [SerializeField] private float secondsPerLine = 3f;

    private bool hasShown;
    private bool isWaitingToShow;
    private bool isShowing;
    private bool isWaiting;
    private float waitTime;
    private float lineTime;
    private float typewriterProgress;
    private int lineIndex;
    private string currentFullLine = string.Empty;

    private Canvas adviceCanvas;
    private RectTransform panelRect;
    private Image panelImage;
    private TextMeshProUGUI adviceText;
    private RectTransform textContainerRect;

    private SpriteRenderer adviceSpriteRendererSource;
    private Animator showAnimation;
    private bool animatorHasWaitingParameter;

    public bool IsWaiting => isWaiting;

    private void Awake()
    {
        ResolveLocalComponents();
    }

    private void Start()
    {
        BuildAdviceUI();
        HideAdvice();
        ResetPendingDialogue();
    }

    private void Update()
    {
        SyncBackgroundSprite();

        if (isWaitingToShow)
        {
            UpdatePendingDialogue();
        }

        if (isShowing)
        {
            UpdateActiveDialogue();
        }
    }

    public void ActivateDialogue()
    {
        if (!CanShowDialogue())
        {
            return;
        }

        if (!useDelay)
        {
            ShowDialogue();
            return;
        }

        isWaitingToShow = true;
        waitTime = Mathf.Max(0f, delayBeforeShow);
    }

    private void UpdatePendingDialogue()
    {
        waitTime -= Time.deltaTime;

        if (waitTime <= 0f)
        {
            ShowDialogue();
            ResetPendingDialogue();
        }
    }

    private void UpdateActiveDialogue()
    {
        if (UpdateTypewriterLine())
        {
            return;
        }

        if (buttonRequired)
        {
            UpdateWaitingForContinue();
            return;
        }

        lineTime -= Time.deltaTime;

        if (lineTime > 0f)
        {
            return;
        }

        lineIndex++;

        if (lineIndex >= lines.Length)
        {
            HideAdvice();
            return;
        }

        SetCurrentLine();
    }

    private bool UpdateTypewriterLine()
    {
        if (!useTypewriter || adviceText == null || string.IsNullOrEmpty(currentFullLine))
        {
            return false;
        }

        int visibleCharacters = Mathf.Clamp(Mathf.FloorToInt(typewriterProgress), 0, currentFullLine.Length);
        adviceText.text = currentFullLine.Substring(0, visibleCharacters);

        if (visibleCharacters < currentFullLine.Length)
        {
            typewriterProgress += Mathf.Max(1f, charactersPerSecond) * Time.deltaTime;
            return true;
        }

        if (buttonRequired)
        {
            isWaiting = true;
            SyncWaitingAnimationState();
            return false;
        }

        lineTime -= Time.deltaTime;
        if (lineTime > 0f)
        {
            return true;
        }

        AdvanceDialogueLine();
        return true;
    }

    private void UpdateWaitingForContinue()
    {
        isWaiting = true;
        SyncWaitingAnimationState();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[continueKey].wasPressedThisFrame)
        {
            AdvanceDialogueLine();
        }
    }

    private bool CanShowDialogue()
    {
        if (showOnlyOnce && hasShown)
        {
            return false;
        }

        if (showOnlyOnce && string.IsNullOrWhiteSpace(adviceId))
        {
            Debug.LogWarning($"{name}: DialogAdvices necesita adviceId para usar showOnlyOnce.", this);
            return false;
        }

        if (showOnlyOnce && SaveManager.Instance != null && SaveManager.Instance.WasAdviceDialogShown(adviceId))
        {
            return false;
        }

        if (isShowing || isWaitingToShow)
        {
            return false;
        }

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("DialogAdvices no tiene lineas para mostrar.");
            return false;
        }

        if (!BuildAdviceUI())
        {
            return false;
        }

        return true;
    }

    private void ShowDialogue()
    {
        ApplyLayout();
        ApplyVisuals();
        ShowSourceVisual();
        SetGameplayEnabled(false);

        lineIndex = 0;
        isShowing = true;
        SetCurrentLine();

        if (panelRect != null)
        {
            panelRect.gameObject.SetActive(true);
        }

        if (SaveManager.Instance != null && !string.IsNullOrWhiteSpace(adviceId))
        {
            SaveManager.Instance.MarkAdviceDialogAsShown(adviceId);
        }

        hasShown = true;
    }

    private bool BuildAdviceUI()
    {
        if (adviceCanvas != null && panelRect != null && panelImage != null && adviceText != null && textContainerRect != null)
            return true;

        if (useExistingCanvas && targetCanvas != null)
        {
            return BuildAdviceUIUnderCanvas(targetCanvas);
        }

        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;

        GameObject canvasObject = new GameObject("DialogAdvicesCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        adviceCanvas = canvasObject.GetComponent<Canvas>();
        adviceCanvas.renderMode = cameraToUse != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
        adviceCanvas.worldCamera = cameraToUse;
        adviceCanvas.overrideSorting = true;
        adviceCanvas.sortingLayerID = adviceSpriteRendererSource != null ? adviceSpriteRendererSource.sortingLayerID : 0;
        adviceCanvas.sortingOrder = adviceSpriteRendererSource != null ? adviceSpriteRendererSource.sortingOrder + 25 : 220;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return BuildPanelContent(canvasObject.transform);
    }

    private bool BuildAdviceUIUnderCanvas(Canvas canvas)
    {
        adviceCanvas = canvas;

        GameObject panelRoot = new GameObject("DialogAdvicesRoot", typeof(RectTransform));
        panelRoot.transform.SetParent(canvas.transform, false);

        return BuildPanelContent(panelRoot.transform);
    }

    private bool BuildPanelContent(Transform parent)
    {
        GameObject panelObject = new GameObject("AdvicePanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        panelRect = panelObject.GetComponent<RectTransform>();
        panelImage = panelObject.GetComponent<Image>();
        panelImage.raycastTarget = false;
        panelImage.type = Image.Type.Simple;
        panelImage.preserveAspect = true;

        GameObject textContainerObject = new GameObject("TextContainer", typeof(RectTransform));
        textContainerObject.transform.SetParent(panelObject.transform, false);
        textContainerRect = textContainerObject.GetComponent<RectTransform>();

        GameObject textObject = new GameObject("AdviceText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(textContainerObject.transform, false);

        adviceText = textObject.GetComponent<TextMeshProUGUI>();
        RectTransform textRect = adviceText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        adviceText.enableWordWrapping = true;
        adviceText.overflowMode = TextOverflowModes.Overflow;
        adviceText.raycastTarget = false;

        return true;
    }

    private void ApplyLayout()
    {
        if (panelRect == null)
        {
            return;
        }

        panelRect.sizeDelta = panelSize;

        switch (corner)
        {
            case AdviceCorner.TopLeft:

                panelRect.anchorMin = new Vector2(0, 1);
                panelRect.anchorMax = new Vector2(0, 1);
                panelRect.pivot = new Vector2(0, 1);

                panelRect.anchoredPosition = new Vector2(
                    screenOffset.x,
                    -screenOffset.y
                );

                break;


            case AdviceCorner.TopRight:

                panelRect.anchorMin = new Vector2(1, 1);
                panelRect.anchorMax = new Vector2(1, 1);
                panelRect.pivot = new Vector2(1, 1);

                panelRect.anchoredPosition = new Vector2(
                    -screenOffset.x,
                    -screenOffset.y
                );

                break;
        }


        if (textContainerRect != null)
        {
            textContainerRect.anchorMin = new Vector2(0.5f,0.5f);
            textContainerRect.anchorMax = new Vector2(0.5f,0.5f);
            textContainerRect.pivot = new Vector2(0.5f,0.5f);

            textContainerRect.sizeDelta = textContainerSize;
            textContainerRect.anchoredPosition = textContainerOffset;
        }
    }

    private void ApplyVisuals()
    {
        if (panelImage != null)
        {
            panelImage.color = backgroundTint;
        }

        if (adviceText != null)
        {
            if (adviceFont != null)
            {
                adviceText.font = adviceFont;
            }

            adviceText.color = textColor;
            adviceText.fontSize = textSize;
            adviceText.alignment = TextAlignmentOptions.TopLeft;
            adviceText.enableWordWrapping = true;
            adviceText.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private void SyncBackgroundSprite()
    {
        if (panelImage == null || adviceSpriteRendererSource == null)
        {
            return;
        }

        Sprite currentSprite = adviceSpriteRendererSource.sprite;
        if (panelImage.sprite != currentSprite)
        {
            panelImage.sprite = currentSprite;
        }

        if (panelImage.enabled != (currentSprite != null))
        {
            panelImage.enabled = currentSprite != null;
        }
    }

    private void SetCurrentLine()
    {
        if (adviceText == null || lines == null || lines.Length == 0)
        {
            return;
        }

        adviceText.text = lines[lineIndex] ?? string.Empty;
        currentFullLine = lines[lineIndex] ?? string.Empty;
        typewriterProgress = 0f;

        isWaiting = buttonRequired;
        SyncWaitingAnimationState();

        if (!buttonRequired)
        {
            lineTime = Mathf.Max(0.2f, secondsPerLine);
        }

        if (!useTypewriter && adviceText != null)
        {
            adviceText.text = currentFullLine;
        }
    }

    private void AdvanceDialogueLine()
    {
        isWaiting = false;
        SyncWaitingAnimationState();

        lineIndex++;
        currentFullLine = string.Empty;
        typewriterProgress = 0f;

        if (lineIndex >= lines.Length)
        {
            HideAdvice();
            return;
        }

        SetCurrentLine();
    }

    private void HideAdvice()
    {
        isShowing = false;
        isWaiting = false;
        SyncWaitingAnimationState();
        SetGameplayEnabled(true);
        HideSourceVisual();

        if (panelRect != null)
        {
            panelRect.gameObject.SetActive(false);
        }
    }

    public void SetTargetCamera(Camera cameraToUse)
    {
        targetCamera = cameraToUse;

        if (adviceCanvas == null)
        {
            return;
        }

        adviceCanvas.renderMode = targetCamera != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
        adviceCanvas.worldCamera = targetCamera;
    }

    private void ResolveLocalComponents()
    {
        adviceSpriteRendererSource = GetComponent<SpriteRenderer>();
        showAnimation = GetComponent<Animator>();
        animatorHasWaitingParameter = HasAnimatorBoolParameter("isWaiting");

        if (adviceSpriteRendererSource == null)
        {
            Debug.LogWarning($"{name}: No se encontró SpriteRenderer en el mismo GameObject.", this);
        }

        if (showAnimation == null)
        {
            Debug.LogWarning($"{name}: No se encontró Animator en el mismo GameObject.", this);
        }
    }

    private void OnDisable()
    {
        HideAdvice();
        ResetPendingDialogue();
    }

    private void OnDestroy()
    {
        if (adviceCanvas != null)
        {
            Destroy(adviceCanvas.gameObject);
        }
    }

    private void ResetPendingDialogue()
    {
        isWaitingToShow = false;
        waitTime = 0f;
    }

    private void ShowSourceVisual()
    {
        if (adviceSpriteRendererSource != null)
            adviceSpriteRendererSource.enabled = true;

        if (showAnimation != null)
            showAnimation.enabled = true;
    }

    private void HideSourceVisual()
    {
        if (adviceSpriteRendererSource != null)
            adviceSpriteRendererSource.enabled = false;

        if (showAnimation != null)
            showAnimation.enabled = false;
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (componentsToDisableWhileShowing == null)
        {
            return;
        }

        for (int i = 0; i < componentsToDisableWhileShowing.Length; i++)
        {
            MonoBehaviour component = componentsToDisableWhileShowing[i];
            if (component != null)
            {
                component.enabled = enabled;
            }
        }
    }

    private void SyncWaitingAnimationState()
    {
        if (showAnimation == null || !animatorHasWaitingParameter)
        {
            return;
        }

        showAnimation.SetBool("isWaiting", isWaiting);
    }

    private bool HasAnimatorBoolParameter(string parameterName)
    {
        if (showAnimation == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = showAnimation.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter != null && parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                return true;
            }
        }

        return false;
    }
}