using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConfuseCardTaskController : MonoBehaviour
{
    enum TaskState
    {
        WaitingToStart,
        Playing,
        Complete
    }

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string Text;

        public AudioClip VoiceClip;
        public float DisplaySeconds = 3f;
    }

    [System.Serializable]
    public class CardStep
    {
        public RectTransform CardRect;
        public Image CardImage;
        public CanvasGroup CardCanvasGroup;
        public RectTransform CorrectSlotRect;
        public Image SlotImage;
        public Sprite SolvedSlotSprite;

        [HideInInspector] public bool Solved;
        [HideInInspector] public Transform StartParent;
        [HideInInspector] public int StartSiblingIndex;
        [HideInInspector] public Vector2 StartAnchoredPosition;
        [HideInInspector] public Vector3 StartLocalScale;
        [HideInInspector] public Quaternion StartLocalRotation;
        [HideInInspector] public Sprite SlotStartSprite;
        [HideInInspector] public bool SlotStartEnabled;
    }

    [Header("Parent Task")]
    [SerializeField] private FlowerTaskInteractionController flowerTaskController;
    [SerializeField] private bool startAfterFlowerTaskStartedEvent = true;

    [Header("Game Canvas")]
    [SerializeField] private GameObject gameCanvasRoot;
    [SerializeField] private CanvasGroup gameCanvasGroup;
    [SerializeField] private bool hideGameCanvasUntilIntroEnds = true;
    [SerializeField] private bool hideGameCanvasOnComplete = true;

    [Header("Cards")]
    [SerializeField] private CardStep[] cards = new CardStep[4];
    [SerializeField] private bool requireCardsInListedOrder = true;
    [SerializeField] private bool hideCardWhenPlaced = true;
    [SerializeField] private bool bringCardToFrontOnDrag = true;
    [SerializeField] private float wrongReturnSeconds = 0.25f;

    [Header("Confuse Orb")]
    [SerializeField] private GameObject confuseOrbObject;
    [SerializeField] private Transform confuseOrbScaleRoot;
    [SerializeField] private float confuseExpandScale = 1.25f;
    [SerializeField] private float confuseShrinkScaleMultiplier = 0.85f;
    [SerializeField] private float confuseScaleAnimSeconds = 0.35f;

    [Header("Butterfly Dialogue")]
    [SerializeField] private GameObject butterflyDialoguePanel;
    [SerializeField] private TMP_Text butterflyDialogueText;
    [SerializeField] private bool useVoiceClipLength = true;
    [SerializeField] private DialogueLine[] introDialogue = new DialogueLine[0];
    [SerializeField] private DialogueLine[] completedDialogue = new DialogueLine[0];

    [Header("Hint UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;

    [TextArea(2, 4)]
    [SerializeField] private string orderCardsHintText = "Drag the cards into the correct boxes in order";

    [Header("Next Button")]
    [SerializeField] private Button nextButton;
    [SerializeField] private bool autoFindNextButton = true;
    [SerializeField] private string nextButtonName = "TaskCompleteButton";
    [SerializeField] private string nextButtonLabel = "Next";
    [SerializeField] private TMP_Text nextButtonText;
    [SerializeField] private bool hideNextButtonUntilComplete = true;

    [Header("Auto Find Scene UI")]
    [SerializeField] private bool autoFindSceneUi = true;
    [SerializeField] private string butterflyDialoguePanelName = "ButterflyDialoguePanel";
    [SerializeField] private string butterflyDialogueTextName = "ButterflyDialogueText";
    [SerializeField] private string hintPanelName = "HintPanel";
    [SerializeField] private string hintTextName = "HintText";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip showHintClip;
    [SerializeField] private AudioClip dragStartClip;
    [SerializeField] private AudioClip correctDropClip;
    [SerializeField] private AudioClip wrongDropClip;
    [SerializeField] private AudioClip dogBarkClip;
    [SerializeField] private AudioClip taskCompletedClip;

    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 1f;
    [SerializeField] private bool waitForDogBarkBeforeDialogue = true;

    [Header("Events")]
    public UnityEvent TaskFinished = new UnityEvent();

    [Header("Debug")]
    [SerializeField] private bool logDebug;

    private TaskState state = TaskState.WaitingToStart;
    private bool subscribedToFlowerTask;
    private Coroutine taskRoutine;
    private Coroutine confuseOrbRoutine;
    private Coroutine[] cardReturnRoutines = new Coroutine[0];
    private Vector2[] dragPointerOffsets = new Vector2[0];
    private Vector3 confuseOriginalScale = Vector3.one;
    private Vector3 confuseCurrentTargetScale = Vector3.one;
    private int solvedCardCount;
    private int draggingCardIndex = -1;

    private int CardCount
    {
        get { return cards == null ? 0 : cards.Length; }
    }

    private void Awake()
    {
        ResolveReferences();
        CacheInitialState();
        PrepareCards();
        SetInitialVisibility();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToFlowerTask();
        ResetTask();

        if (!startAfterFlowerTaskStartedEvent || flowerTaskController == null)
            StartTask();
    }

    private void OnDisable()
    {
        UnsubscribeFromFlowerTask();
        StopRunningRoutines();
    }

    private void StartTask()
    {
        StopRunningRoutines();
        taskRoutine = StartCoroutine(TaskRoutine());
    }

    private IEnumerator TaskRoutine()
    {
        state = TaskState.WaitingToStart;

        if (hideNextButtonUntilComplete)
            SetNextButtonVisible(false);

        SetGameCanvasVisible(!hideGameCanvasUntilIntroEnds);
        SetHintText("", false);

        yield return PlayDialogue(introDialogue);

        SetHintText(orderCardsHintText, true);
        PlayOneShot(showHintClip);
        SetGameCanvasVisible(true);

        state = TaskState.Playing;
    }

    public void BeginCardDrag(int cardIndex, PointerEventData eventData)
    {
        if (state != TaskState.Playing || !IsValidCardIndex(cardIndex))
            return;

        CardStep card = cards[cardIndex];
        if (card.Solved || card.CardRect == null)
            return;

        draggingCardIndex = cardIndex;

        if (cardReturnRoutines[cardIndex] != null)
        {
            StopCoroutine(cardReturnRoutines[cardIndex]);
            cardReturnRoutines[cardIndex] = null;
        }

        if (bringCardToFrontOnDrag)
            card.CardRect.SetAsLastSibling();

        CanvasGroup group = GetCardCanvasGroup(card);
        if (group != null)
            group.blocksRaycasts = false;

        dragPointerOffsets[cardIndex] = GetDragPointerOffset(card.CardRect, eventData);
        PlayOneShot(dragStartClip);
    }

    public void DragCard(int cardIndex, PointerEventData eventData)
    {
        if (draggingCardIndex != cardIndex || !IsValidCardIndex(cardIndex))
            return;

        RectTransform cardRect = cards[cardIndex].CardRect;
        if (cardRect == null)
            return;

        RectTransform parentRect = cardRect.parent as RectTransform;
        if (parentRect == null)
            return;

        Camera eventCamera = GetEventCamera(cardRect, eventData);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventCamera, out Vector2 localPoint))
            cardRect.anchoredPosition = localPoint + dragPointerOffsets[cardIndex];
    }

    public void EndCardDrag(int cardIndex, PointerEventData eventData)
    {
        if (draggingCardIndex != cardIndex || !IsValidCardIndex(cardIndex))
            return;

        draggingCardIndex = -1;

        CardStep card = cards[cardIndex];
        CanvasGroup group = GetCardCanvasGroup(card);
        if (group != null)
            group.blocksRaycasts = true;

        bool correctOrder = !requireCardsInListedOrder || cardIndex == GetNextUnsolvedCardIndex();
        bool correctSlot = IsPointInsideSlot(card, eventData);

        if (correctOrder && correctSlot)
        {
            PlaceCard(cardIndex);
            return;
        }

        ReturnCard(cardIndex);
    }

    private void PlaceCard(int cardIndex)
    {
        CardStep card = cards[cardIndex];
        card.Solved = true;
        solvedCardCount++;

        Sprite solvedSprite = card.SolvedSlotSprite != null
            ? card.SolvedSlotSprite
            : card.CardImage != null ? card.CardImage.sprite : null;

        if (card.SlotImage != null)
        {
            card.SlotImage.sprite = solvedSprite;
            card.SlotImage.enabled = solvedSprite != null;
        }

        if (hideCardWhenPlaced && card.CardRect != null)
            card.CardRect.gameObject.SetActive(false);
        else if (card.CardRect != null && card.CorrectSlotRect != null)
            card.CardRect.position = card.CorrectSlotRect.position;

        PlayOneShot(correctDropClip);

        bool finalCard = AllCardsSolved();
        Coroutine orbRoutine = StartConfuseOrbFeedback(finalCard);

        if (finalCard)
            taskRoutine = StartCoroutine(CompleteTaskRoutine(orbRoutine));
    }

    private void ReturnCard(int cardIndex)
    {
        PlayOneShot(wrongDropClip);

        if (cardReturnRoutines[cardIndex] != null)
            StopCoroutine(cardReturnRoutines[cardIndex]);

        cardReturnRoutines[cardIndex] = StartCoroutine(ReturnCardRoutine(cardIndex));
    }

    private IEnumerator ReturnCardRoutine(int cardIndex)
    {
        CardStep card = cards[cardIndex];
        if (card == null || card.CardRect == null)
            yield break;

        Vector2 start = card.CardRect.anchoredPosition;
        Vector2 target = card.StartAnchoredPosition;
        float elapsed = 0f;

        while (elapsed < wrongReturnSeconds)
        {
            elapsed += Time.deltaTime;
            float t = wrongReturnSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / wrongReturnSeconds);
            float smoothT = Smooth(t);
            card.CardRect.anchoredPosition = Vector2.Lerp(start, target, smoothT);
            yield return null;
        }

        card.CardRect.anchoredPosition = target;
        cardReturnRoutines[cardIndex] = null;
    }

    private IEnumerator CompleteTaskRoutine(Coroutine orbRoutine)
    {
        state = TaskState.Complete;
        SetHintText("", false);

        if (orbRoutine != null)
            yield return orbRoutine;

        PlayOneShot(dogBarkClip);

        if (dogBarkClip != null && (waitForDogBarkBeforeDialogue || hideGameCanvasOnComplete))
            yield return new WaitForSeconds(dogBarkClip.length);

        if (hideGameCanvasOnComplete)
            SetGameCanvasVisible(false);

        PlayOneShot(taskCompletedClip);
        yield return PlayDialogue(completedDialogue);

        SetNextButtonLabel();
        SetNextButtonVisible(true);
        TaskFinished.Invoke();
    }

    private Coroutine StartConfuseOrbFeedback(bool finalCard)
    {
        if (confuseOrbRoutine != null)
            StopCoroutine(confuseOrbRoutine);

        confuseOrbRoutine = StartCoroutine(ConfuseOrbFeedbackRoutine(finalCard));
        return confuseOrbRoutine;
    }

    private IEnumerator ConfuseOrbFeedbackRoutine(bool finalCard)
    {
        Transform target = GetConfuseOrbScaleTarget();
        if (target == null)
            yield break;

        if (confuseOrbObject != null)
            confuseOrbObject.SetActive(true);

        Vector3 startScale = target.localScale;
        Vector3 expandedScale = startScale * confuseExpandScale;
        Vector3 targetScale = finalCard
            ? Vector3.zero
            : confuseOriginalScale * Mathf.Pow(confuseShrinkScaleMultiplier, solvedCardCount);

        float halfSeconds = Mathf.Max(0.001f, confuseScaleAnimSeconds * 0.5f);
        float elapsed = 0f;

        while (elapsed < halfSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfSeconds);
            target.localScale = Vector3.Lerp(startScale, expandedScale, Smooth(t));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfSeconds);
            target.localScale = Vector3.Lerp(expandedScale, targetScale, Smooth(t));
            yield return null;
        }

        target.localScale = targetScale;
        confuseCurrentTargetScale = targetScale;

        if (finalCard && confuseOrbObject != null)
            confuseOrbObject.SetActive(false);

        confuseOrbRoutine = null;
    }

    private void ResetTask()
    {
        StopRunningRoutines();

        state = TaskState.WaitingToStart;
        solvedCardCount = 0;
        draggingCardIndex = -1;

        EnsureRuntimeArrays();

        for (int i = 0; i < CardCount; i++)
        {
            CardStep card = cards[i];
            if (card == null)
                continue;

            card.Solved = false;

            if (card.CardRect != null)
            {
                card.CardRect.gameObject.SetActive(true);

                if (card.StartParent != null && card.CardRect.parent != card.StartParent)
                    card.CardRect.SetParent(card.StartParent, false);

                card.CardRect.SetSiblingIndex(card.StartSiblingIndex);
                card.CardRect.anchoredPosition = card.StartAnchoredPosition;
                card.CardRect.localScale = card.StartLocalScale;
                card.CardRect.localRotation = card.StartLocalRotation;
            }

            CanvasGroup group = GetCardCanvasGroup(card);
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            if (card.SlotImage != null)
            {
                card.SlotImage.sprite = card.SlotStartSprite;
                card.SlotImage.enabled = card.SlotStartEnabled;
            }
        }

        if (confuseOrbObject != null)
            confuseOrbObject.SetActive(true);

        Transform orbScaleTarget = GetConfuseOrbScaleTarget();
        if (orbScaleTarget != null)
            orbScaleTarget.localScale = confuseOriginalScale;

        confuseCurrentTargetScale = confuseOriginalScale;

        if (hideGameCanvasUntilIntroEnds)
            SetGameCanvasVisible(false);

        SetHintText("", false);
        SetDialogueVisible(false);

        if (hideNextButtonUntilComplete)
            SetNextButtonVisible(false);
    }

    private void StopRunningRoutines()
    {
        if (taskRoutine != null)
        {
            StopCoroutine(taskRoutine);
            taskRoutine = null;
        }

        if (confuseOrbRoutine != null)
        {
            StopCoroutine(confuseOrbRoutine);
            confuseOrbRoutine = null;
        }

        if (cardReturnRoutines != null)
        {
            for (int i = 0; i < cardReturnRoutines.Length; i++)
            {
                if (cardReturnRoutines[i] != null)
                {
                    StopCoroutine(cardReturnRoutines[i]);
                    cardReturnRoutines[i] = null;
                }
            }
        }
    }

    private void CacheInitialState()
    {
        EnsureRuntimeArrays();

        for (int i = 0; i < CardCount; i++)
        {
            CardStep card = cards[i];
            if (card == null)
                continue;

            ResolveCardReferences(card);

            if (card.CardRect != null)
            {
                card.StartParent = card.CardRect.parent;
                card.StartSiblingIndex = card.CardRect.GetSiblingIndex();
                card.StartAnchoredPosition = card.CardRect.anchoredPosition;
                card.StartLocalScale = card.CardRect.localScale;
                card.StartLocalRotation = card.CardRect.localRotation;
            }

            if (card.SlotImage != null)
            {
                card.SlotStartSprite = card.SlotImage.sprite;
                card.SlotStartEnabled = card.SlotImage.enabled;
            }
        }

        Transform orbScaleTarget = GetConfuseOrbScaleTarget();
        if (orbScaleTarget != null)
            confuseOriginalScale = orbScaleTarget.localScale;

        confuseCurrentTargetScale = confuseOriginalScale;
    }

    private void PrepareCards()
    {
        EnsureRuntimeArrays();
        EnsureGameCanvasRaycaster();

        if (EventSystem.current == null)
            Debug.LogWarning("ConfuseCardTaskController: No EventSystem was found. UI drag events will not work until an EventSystem exists in the scene.");

        for (int i = 0; i < CardCount; i++)
        {
            CardStep card = cards[i];
            if (card == null)
                continue;

            ResolveCardReferences(card);

            if (card.CardImage != null)
                card.CardImage.raycastTarget = true;

            if (card.CardRect != null)
            {
                ConfuseCardDragProxy proxy = card.CardRect.GetComponent<ConfuseCardDragProxy>();
                if (proxy == null)
                    proxy = card.CardRect.gameObject.AddComponent<ConfuseCardDragProxy>();

                proxy.Initialize(this, i);
            }
        }
    }

    private void ResolveReferences()
    {
        if (flowerTaskController == null)
            flowerTaskController = GetComponent<FlowerTaskInteractionController>();

        if (flowerTaskController == null)
            flowerTaskController = GetComponentInParent<FlowerTaskInteractionController>();

        if (flowerTaskController == null)
            flowerTaskController = GetComponentInChildren<FlowerTaskInteractionController>(true);

        if (gameCanvasGroup == null && gameCanvasRoot != null)
            gameCanvasGroup = gameCanvasRoot.GetComponent<CanvasGroup>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ResolveSceneUi();
        ResolveNextButton();
    }

    private void ResolveSceneUi()
    {
        if (!autoFindSceneUi)
            return;

        if (butterflyDialoguePanel == null)
            butterflyDialoguePanel = FindSceneGameObject(butterflyDialoguePanelName);

        if (butterflyDialogueText == null)
            butterflyDialogueText = FindSceneText(butterflyDialogueTextName, butterflyDialoguePanel);

        if (hintPanel == null)
            hintPanel = FindSceneGameObject(hintPanelName);

        if (hintText == null)
            hintText = FindSceneText(hintTextName, hintPanel);
    }

    private void ResolveNextButton()
    {
        if (!autoFindNextButton || nextButton != null || string.IsNullOrEmpty(nextButtonName))
            return;

        GameObject buttonObject = FindSceneGameObject(nextButtonName);
        if (buttonObject != null)
            nextButton = buttonObject.GetComponent<Button>();

        if (nextButton != null && nextButtonText == null)
            nextButtonText = nextButton.GetComponentInChildren<TMP_Text>(true);
    }

    private void ResolveCardReferences(CardStep card)
    {
        if (card == null)
            return;

        if (card.CardImage == null && card.CardRect != null)
            card.CardImage = card.CardRect.GetComponent<Image>();

        if (card.CardCanvasGroup == null && card.CardRect != null)
            card.CardCanvasGroup = card.CardRect.GetComponent<CanvasGroup>();

        if (card.SlotImage == null && card.CorrectSlotRect != null)
            card.SlotImage = card.CorrectSlotRect.GetComponent<Image>();
    }

    private CanvasGroup GetCardCanvasGroup(CardStep card)
    {
        if (card == null || card.CardRect == null)
            return null;

        if (card.CardCanvasGroup == null)
            card.CardCanvasGroup = card.CardRect.GetComponent<CanvasGroup>();

        if (card.CardCanvasGroup == null)
            card.CardCanvasGroup = card.CardRect.gameObject.AddComponent<CanvasGroup>();

        return card.CardCanvasGroup;
    }

    private void EnsureRuntimeArrays()
    {
        if (cardReturnRoutines == null || cardReturnRoutines.Length != CardCount)
            cardReturnRoutines = new Coroutine[CardCount];

        if (dragPointerOffsets == null || dragPointerOffsets.Length != CardCount)
            dragPointerOffsets = new Vector2[CardCount];
    }

    private void EnsureGameCanvasRaycaster()
    {
        Canvas canvas = gameCanvasRoot != null
            ? gameCanvasRoot.GetComponentInParent<Canvas>(true)
            : GetComponentInParent<Canvas>(true);

        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private bool IsValidCardIndex(int cardIndex)
    {
        return cardIndex >= 0 && cardIndex < CardCount && cards[cardIndex] != null;
    }

    private int GetNextUnsolvedCardIndex()
    {
        for (int i = 0; i < CardCount; i++)
        {
            if (cards[i] != null && !cards[i].Solved)
                return i;
        }

        return -1;
    }

    private bool AllCardsSolved()
    {
        bool hasCard = false;

        for (int i = 0; i < CardCount; i++)
        {
            if (cards[i] == null || cards[i].CardRect == null)
                continue;

            hasCard = true;

            if (!cards[i].Solved)
                return false;
        }

        return hasCard;
    }

    private Vector2 GetDragPointerOffset(RectTransform cardRect, PointerEventData eventData)
    {
        RectTransform parentRect = cardRect != null ? cardRect.parent as RectTransform : null;
        if (cardRect == null || parentRect == null)
            return Vector2.zero;

        Camera eventCamera = GetEventCamera(cardRect, eventData);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventCamera, out Vector2 localPoint))
            return cardRect.anchoredPosition - localPoint;

        return Vector2.zero;
    }

    private bool IsPointInsideSlot(CardStep card, PointerEventData eventData)
    {
        if (card == null || card.CorrectSlotRect == null)
            return false;

        Camera eventCamera = GetEventCamera(card.CorrectSlotRect, eventData);
        return RectTransformUtility.RectangleContainsScreenPoint(card.CorrectSlotRect, eventData.position, eventCamera);
    }

    private Camera GetEventCamera(RectTransform rect, PointerEventData eventData)
    {
        Canvas canvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (eventData != null && eventData.pressEventCamera != null)
            return eventData.pressEventCamera;

        if (canvas.worldCamera != null)
            return canvas.worldCamera;

        return Camera.main;
    }

    private Transform GetConfuseOrbScaleTarget()
    {
        if (confuseOrbScaleRoot != null)
            return confuseOrbScaleRoot;

        return confuseOrbObject != null ? confuseOrbObject.transform : null;
    }

    private IEnumerator PlayDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            yield break;

        SetDialogueVisible(true);

        for (int i = 0; i < lines.Length; i++)
        {
            DialogueLine line = lines[i];
            if (line == null)
                continue;

            if (butterflyDialogueText != null)
                butterflyDialogueText.text = line.Text;

            PlayOneShot(line.VoiceClip);

            float seconds = Mathf.Max(0f, line.DisplaySeconds);
            if (useVoiceClipLength && line.VoiceClip != null)
                seconds = Mathf.Max(seconds, line.VoiceClip.length);

            if (seconds > 0f)
                yield return new WaitForSeconds(seconds);
        }

        SetDialogueVisible(false);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, audioVolume);
    }

    private void SetInitialVisibility()
    {
        SetDialogueVisible(false);
        SetHintText("", false);
        SetGameCanvasVisible(false);

        if (hideNextButtonUntilComplete)
            SetNextButtonVisible(false);
    }

    private void SetDialogueVisible(bool visible)
    {
        SetObjectActive(butterflyDialoguePanel, visible);
    }

    private void SetHintText(string message, bool visible)
    {
        SetObjectActive(hintPanel, visible);

        if (hintText != null)
            hintText.text = message;
    }

    private void SetGameCanvasVisible(bool visible)
    {
        SetObjectActive(gameCanvasRoot, visible);

        if (gameCanvasGroup != null)
        {
            gameCanvasGroup.alpha = visible ? 1f : 0f;
            gameCanvasGroup.interactable = visible;
            gameCanvasGroup.blocksRaycasts = visible;
        }
    }

    private void SetNextButtonLabel()
    {
        ResolveNextButton();

        if (nextButtonText != null)
            nextButtonText.text = nextButtonLabel;
    }

    private void SetNextButtonVisible(bool visible)
    {
        ResolveNextButton();

        if (nextButton != null)
            nextButton.gameObject.SetActive(visible);
    }

    private void SetObjectActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private TMP_Text FindSceneText(string objectName, GameObject fallbackRoot)
    {
        GameObject textObject = FindSceneGameObject(objectName);
        if (textObject != null)
        {
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            if (text != null)
                return text;
        }

        return fallbackRoot != null ? fallbackRoot.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private GameObject FindSceneGameObject(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
            return activeObject;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null || target.name != objectName)
                continue;

            if (!target.gameObject.scene.IsValid())
                continue;

            return target.gameObject;
        }

        return null;
    }

    private void SubscribeToFlowerTask()
    {
        if (subscribedToFlowerTask || flowerTaskController == null)
            return;

        flowerTaskController.TaskStarted.AddListener(OnParentTaskStarted);
        subscribedToFlowerTask = true;
    }

    private void UnsubscribeFromFlowerTask()
    {
        if (!subscribedToFlowerTask || flowerTaskController == null)
            return;

        flowerTaskController.TaskStarted.RemoveListener(OnParentTaskStarted);
        subscribedToFlowerTask = false;
    }

    private void OnParentTaskStarted(int taskIndex)
    {
        StartTask();
    }

    private void LogDebug(string message)
    {
        if (logDebug)
            Debug.Log("ConfuseCardTaskController: " + message);
    }
}

class ConfuseCardDragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ConfuseCardTaskController controller;
    private int cardIndex;

    public void Initialize(ConfuseCardTaskController owner, int index)
    {
        controller = owner;
        cardIndex = index;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller != null)
            controller.BeginCardDrag(cardIndex, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (controller != null)
            controller.DragCard(cardIndex, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (controller != null)
            controller.EndCardDrag(cardIndex, eventData);
    }
}
