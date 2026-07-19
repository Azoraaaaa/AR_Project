using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LonelinessPhoneTaskController : MonoBehaviour
{
    enum TaskState
    {
        WaitingToStart,
        ChoosingAvatar,
        WaitingForPhoneClick,
        WaitingForReceiverPickup,
        DraggingReceiver,
        PlayingConversation,
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
    public class AvatarOption
    {
        public GameObject AvatarObject;
        public Button AvatarButton;
        public CanvasGroup AvatarCanvasGroup;
        public AudioClip VoiceClip;

        [TextArea(2, 5)]
        public string MessageText;

        [HideInInspector] public Vector3 StartPosition;
        [HideInInspector] public Vector3 StartLocalPosition;
        [HideInInspector] public Vector2 StartAnchoredPosition;
        [HideInInspector] public Vector3 StartLocalScale;
        [HideInInspector] public UnityAction ButtonAction;
    }

    [Header("Camera")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private bool autoFindCamera = true;
    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField] private float raycastDistance = 100f;

    [Header("Parent Task")]
    [SerializeField] private FlowerTaskInteractionController flowerTaskController;
    [SerializeField] private bool startAfterFlowerTaskStartedEvent = true;

    [Header("Butterfly Dialogue")]
    [SerializeField] private GameObject butterflyDialoguePanel;
    [SerializeField] private TMP_Text butterflyDialogueText;
    [SerializeField] private bool useVoiceClipLength = true;
    [SerializeField] private float dialogueTypingSeconds = LwyTypewriterText.DefaultCharacterSeconds;
    [SerializeField] private DialogueLine[] introDialogue = new DialogueLine[0];
    [SerializeField] private DialogueLine[] completedDialogue = new DialogueLine[0];

    [Header("Hint UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private float hintTypingSeconds = LwyTypewriterText.DefaultCharacterSeconds;

    [TextArea(2, 4)]
    [SerializeField] private string lonelinessHintText = "If you feel lonely, you can ask someone to stay with you";

    [Header("Message Canvas")]
    [SerializeField] private GameObject messageCanvasRoot;
    [SerializeField] private CanvasGroup messageCanvasGroup;
    [SerializeField] private Transform selectedAvatarCenter;
    [SerializeField] private AvatarOption[] avatars = new AvatarOption[3];
    [SerializeField] private float messageCanvasScaleSeconds = 0.35f;
    [SerializeField] private float avatarFadeSeconds = 0.35f;
    [SerializeField] private float selectedAvatarMoveSeconds = 0.45f;
    [SerializeField] private GameObject conversationTextPanel;
    [SerializeField] private TMP_Text conversationText;

    [Header("Phone")]
    [SerializeField] private GameObject phoneObject;
    [SerializeField] private Animator phoneAnimator;
    [SerializeField] private string phoneIsRingBoolName = "isRing";
    [SerializeField] private bool stopRingWhenReceiverPickedUp = true;

    [Header("Receiver")]
    [SerializeField] private Transform receiverObject;
    [SerializeField] private Transform receiverStartPoint;
    [SerializeField] private bool useReceiverStartPointOverride;
    [SerializeField] private Transform receiverDropTarget;
    [SerializeField] private float receiverAcceptDistance = 0.05f;
    [SerializeField] private bool snapReceiverToTarget = true;
    [SerializeField] private bool returnReceiverOnFailedDrop = true;
    [SerializeField] private Transform dragPlaneReference;
    [SerializeField] private float receiverLiftHeight = 0.01f;

    [Header("World Hints")]
    [SerializeField] private GameObject phoneHintImage;
    [SerializeField] private GameObject receiverPickupHintImage;
    [SerializeField] private GameObject receiverDropHintImage;

    [Header("Lonely Orb")]
    [SerializeField] private GameObject lonelyOrbObject;
    [SerializeField] private float lonelyOrbShrinkSeconds = 0.6f;

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
    [SerializeField] private AudioClip phoneClickedClip;
    [SerializeField] private AudioClip receiverPickupClip;
    [SerializeField] private AudioClip receiverReturnClip;
    [SerializeField] private AudioClip taskCompletedClip;

    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 1f;

    [Header("Events")]
    public UnityEvent TaskFinished = new UnityEvent();

    [Header("Debug")]
    [SerializeField] private bool logDebug = true;

    private TaskState state = TaskState.WaitingToStart;
    private int selectedAvatarIndex = -1;
    private bool subscribedToFlowerTask;
    private bool avatarListenersRegistered;
    private Coroutine taskRoutine;
    private Coroutine hintTypingRoutine;
    private Coroutine avatarRoutine;
    private Coroutine lonelyOrbRoutine;
    private Vector3 messageCanvasOriginalScale = Vector3.one;
    private Vector3 lonelyOrbOriginalScale = Vector3.one;
    private Vector3 receiverStartPosition;
    private Quaternion receiverStartRotation;
    private Vector3 receiverStartLocalPosition;
    private Quaternion receiverStartLocalRotation;
    private Plane receiverDragPlane;
    private Vector3 receiverDragOffset;

    private int AvatarCount
    {
        get { return avatars == null ? 0 : avatars.Length; }
    }

    private void Awake()
    {
        ResolveReferences();
        CacheInitialState();
        RegisterAvatarButtons();
        SetInitialVisibility();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToFlowerTask();
        RegisterAvatarButtons();
        ResetTask();

        if (!startAfterFlowerTaskStartedEvent || flowerTaskController == null)
            StartTask();
    }

    private void OnDisable()
    {
        UnsubscribeFromFlowerTask();
        UnregisterAvatarButtons();
    }

    private void Update()
    {
        bool pointerDown = PointerDown(out Vector2 pointerPosition);

        if (!pointerDown && state != TaskState.DraggingReceiver)
            return;

        if (state == TaskState.ChoosingAvatar && pointerDown)
        {
            int avatarIndex = GetClickedAvatarIndex(pointerPosition);
            if (avatarIndex >= 0)
                SelectAvatar(avatarIndex);
        }
        else if (state == TaskState.WaitingForPhoneClick && pointerDown)
        {
            LogDebug("Pointer down while waiting for phone click.");

            if (IsObjectClicked(pointerPosition, phoneObject))
                OnPhoneClicked();
        }
        else if (state == TaskState.WaitingForReceiverPickup && pointerDown)
        {
            if (IsReceiverClicked(pointerPosition))
                BeginReceiverDrag(pointerPosition);
        }

        if (state == TaskState.DraggingReceiver)
        {
            if (PointerHeld(out pointerPosition))
                DragReceiver(pointerPosition);

            if (PointerUp(out pointerPosition))
                EndReceiverDrag();
        }
    }

    private void StartTask()
    {
        if (taskRoutine != null)
            StopCoroutine(taskRoutine);

        taskRoutine = StartCoroutine(TaskRoutine());
    }

    private IEnumerator TaskRoutine()
    {
        state = TaskState.WaitingToStart;

        if (hideNextButtonUntilComplete)
            SetNextButtonVisible(false);

        yield return PlayDialogue(introDialogue);

        SetHintText(lonelinessHintText, true);
        PlayOneShot(showHintClip);

        yield return ShowMessageCanvasRoutine();

        state = TaskState.ChoosingAvatar;
    }

    private void SelectAvatar(int avatarIndex)
    {
        if (avatarIndex < 0 || avatarIndex >= AvatarCount || avatars[avatarIndex] == null)
            return;

        selectedAvatarIndex = avatarIndex;
        state = TaskState.WaitingToStart;
        SetAvatarButtonsInteractable(false);

        if (avatarRoutine != null)
            StopCoroutine(avatarRoutine);

        avatarRoutine = StartCoroutine(AvatarSelectedRoutine(avatarIndex));
    }

    private IEnumerator AvatarSelectedRoutine(int avatarIndex)
    {
        yield return AnimateAvatarSelectionRoutine(avatarIndex);

        SetObjectActive(phoneHintImage, true);
        PlayOneShot(showHintClip);
        state = TaskState.WaitingForPhoneClick;
    }

    private void OnPhoneClicked()
    {
        SetObjectActive(phoneHintImage, false);
        SetObjectActive(receiverPickupHintImage, false);
        SetObjectActive(receiverDropHintImage, false);
        SetHintText("", false);

        PlayOneShot(phoneClickedClip);

        if (phoneAnimator != null)
        {
            if (!string.IsNullOrEmpty(phoneIsRingBoolName))
                phoneAnimator.SetBool(phoneIsRingBoolName, true);
        }

        SetObjectActive(receiverPickupHintImage, true);
        SetObjectActive(receiverDropHintImage, true);
        PlayOneShot(showHintClip);
        state = TaskState.WaitingForReceiverPickup;
    }

    private void BeginReceiverDrag(Vector2 screenPosition)
    {
        if (receiverObject == null)
            return;

        SetObjectActive(receiverPickupHintImage, false);
        SetObjectActive(receiverDropHintImage, true);
        PlayOneShot(receiverPickupClip);

        if (stopRingWhenReceiverPickedUp && phoneAnimator != null && !string.IsNullOrEmpty(phoneIsRingBoolName))
            phoneAnimator.SetBool(phoneIsRingBoolName, false);

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        Vector3 planeNormal = dragPlaneReference != null ? dragPlaneReference.up : -arCamera.transform.forward;
        receiverDragPlane = new Plane(planeNormal, receiverObject.position);

        if (receiverDragPlane.Raycast(ray, out float enter))
            receiverDragOffset = receiverObject.position - ray.GetPoint(enter);
        else
            receiverDragOffset = Vector3.zero;

        state = TaskState.DraggingReceiver;
    }

    private void DragReceiver(Vector2 screenPosition)
    {
        if (receiverObject == null || arCamera == null)
            return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (!receiverDragPlane.Raycast(ray, out float enter))
            return;

        Vector3 planeNormal = dragPlaneReference != null ? dragPlaneReference.up : -arCamera.transform.forward;
        Vector3 targetPosition = ray.GetPoint(enter) + receiverDragOffset + planeNormal * receiverLiftHeight;

        receiverObject.position = targetPosition;
    }

    private void EndReceiverDrag()
    {
        if (IsReceiverInTarget())
        {
            if (snapReceiverToTarget && receiverDropTarget != null && receiverObject != null)
            {
                receiverObject.position = receiverDropTarget.position;
                receiverObject.rotation = receiverDropTarget.rotation;
            }

            SetObjectActive(receiverPickupHintImage, false);
            SetObjectActive(receiverDropHintImage, false);
            state = TaskState.PlayingConversation;
            StartCoroutine(ConversationRoutine());
            return;
        }

        if (returnReceiverOnFailedDrop)
            ReturnReceiverToStart();

        PlayOneShot(receiverReturnClip);
        SetObjectActive(receiverPickupHintImage, true);
        SetObjectActive(receiverDropHintImage, true);
        PlayOneShot(showHintClip);
        state = TaskState.WaitingForReceiverPickup;
    }

    private IEnumerator ConversationRoutine()
    {
        SetObjectActive(receiverPickupHintImage, false);
        SetObjectActive(receiverDropHintImage, false);

        AvatarOption selectedAvatar = GetSelectedAvatar();
        if (selectedAvatar != null)
        {
            SetConversationText(selectedAvatar.MessageText, true);
            PlayOneShot(selectedAvatar.VoiceClip);

            if (selectedAvatar.VoiceClip != null)
                yield return new WaitForSeconds(selectedAvatar.VoiceClip.length);
        }

        SetConversationText("", false);

        yield return ShrinkLonelyOrbRoutine();

        PlayOneShot(taskCompletedClip);
        yield return PlayDialogue(completedDialogue);

        SetNextButtonLabel();
        SetNextButtonVisible(true);
        TaskFinished.Invoke();
        state = TaskState.Complete;
    }

    private IEnumerator ShowMessageCanvasRoutine()
    {
        if (messageCanvasRoot == null)
            yield break;

        messageCanvasRoot.SetActive(true);

        if (messageCanvasGroup != null)
        {
            messageCanvasGroup.alpha = 1f;
            messageCanvasGroup.interactable = true;
            messageCanvasGroup.blocksRaycasts = true;
        }

        Vector3 targetScale = messageCanvasOriginalScale;
        messageCanvasRoot.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < messageCanvasScaleSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / messageCanvasScaleSeconds);
            messageCanvasRoot.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, Smooth(t));
            yield return null;
        }

        messageCanvasRoot.transform.localScale = targetScale;
    }

    private IEnumerator AnimateAvatarSelectionRoutine(int selectedIndex)
    {
        AvatarOption selectedAvatar = avatars[selectedIndex];
        RectTransform selectedRect = selectedAvatar.AvatarObject != null
            ? selectedAvatar.AvatarObject.GetComponent<RectTransform>()
            : null;

        Vector3 selectedStartWorld = selectedAvatar.AvatarObject != null
            ? selectedAvatar.AvatarObject.transform.position
            : Vector3.zero;

        Vector3 selectedTargetWorld = selectedAvatarCenter != null
            ? selectedAvatarCenter.position
            : selectedStartWorld;

        Vector2 selectedStartAnchored = selectedRect != null
            ? selectedRect.anchoredPosition
            : Vector2.zero;

        Vector2 selectedTargetAnchored = selectedStartAnchored;
        RectTransform centerRect = selectedAvatarCenter as RectTransform;
        if (selectedRect != null && centerRect != null)
            selectedTargetAnchored = centerRect.anchoredPosition;

        float elapsed = 0f;
        float seconds = Mathf.Max(avatarFadeSeconds, selectedAvatarMoveSeconds);

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float fadeT = avatarFadeSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / avatarFadeSeconds);
            float moveT = selectedAvatarMoveSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / selectedAvatarMoveSeconds);

            for (int i = 0; i < AvatarCount; i++)
            {
                AvatarOption avatar = avatars[i];
                if (avatar == null || avatar.AvatarObject == null || i == selectedIndex)
                    continue;

                CanvasGroup group = GetAvatarCanvasGroup(avatar);
                if (group != null)
                    group.alpha = Mathf.Lerp(1f, 0f, Smooth(fadeT));
            }

            if (selectedRect != null)
                selectedRect.anchoredPosition = Vector2.Lerp(selectedStartAnchored, selectedTargetAnchored, Smooth(moveT));
            else if (selectedAvatar.AvatarObject != null)
                selectedAvatar.AvatarObject.transform.position = Vector3.Lerp(selectedStartWorld, selectedTargetWorld, Smooth(moveT));

            yield return null;
        }

        for (int i = 0; i < AvatarCount; i++)
        {
            if (i == selectedIndex || avatars[i] == null || avatars[i].AvatarObject == null)
                continue;

            avatars[i].AvatarObject.SetActive(false);
        }
    }

    private IEnumerator ShrinkLonelyOrbRoutine()
    {
        if (lonelyOrbObject == null)
            yield break;

        Transform target = lonelyOrbObject.transform;
        Vector3 startScale = target.localScale;
        float elapsed = 0f;

        while (elapsed < lonelyOrbShrinkSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lonelyOrbShrinkSeconds);
            target.localScale = Vector3.Lerp(startScale, Vector3.zero, Smooth(t));
            yield return null;
        }

        lonelyOrbObject.SetActive(false);
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

            PlayOneShot(line.VoiceClip);

            float seconds = Mathf.Max(0f, line.DisplaySeconds);
            if (useVoiceClipLength && line.VoiceClip != null)
                seconds = Mathf.Max(seconds, line.VoiceClip.length);

            float typingSeconds = butterflyDialogueText != null
                ? LwyTypewriterText.GetTypingDuration(line.Text, dialogueTypingSeconds)
                : 0f;

            if (butterflyDialogueText != null)
                yield return LwyTypewriterText.TypeText(butterflyDialogueText, line.Text, dialogueTypingSeconds);

            float remainingSeconds = Mathf.Max(0f, seconds - typingSeconds);
            if (remainingSeconds > 0f)
                yield return new WaitForSeconds(remainingSeconds);
        }

        SetDialogueVisible(false);
    }

    private void ResetTask()
    {
        selectedAvatarIndex = -1;
        state = TaskState.WaitingToStart;

        SetInitialVisibility();
        ResetAvatars();
        ReturnReceiverToStart();

        if (lonelyOrbObject != null)
        {
            lonelyOrbObject.SetActive(true);
            lonelyOrbObject.transform.localScale = lonelyOrbOriginalScale;
        }

        SetAvatarButtonsInteractable(true);

        if (hideNextButtonUntilComplete)
            SetNextButtonVisible(false);
    }

    private void SetInitialVisibility()
    {
        SetHintText("", false);
        SetObjectActive(phoneHintImage, false);
        SetObjectActive(receiverPickupHintImage, false);
        SetObjectActive(receiverDropHintImage, false);
        SetObjectActive(conversationTextPanel, false);
        SetDialogueVisible(false);

        if (messageCanvasRoot != null)
            messageCanvasRoot.SetActive(false);
    }

    private void ResetAvatars()
    {
        for (int i = 0; i < AvatarCount; i++)
        {
            AvatarOption avatar = avatars[i];
            if (avatar == null || avatar.AvatarObject == null)
                continue;

            avatar.AvatarObject.SetActive(true);
            avatar.AvatarObject.transform.position = avatar.StartPosition;
            avatar.AvatarObject.transform.localPosition = avatar.StartLocalPosition;
            avatar.AvatarObject.transform.localScale = avatar.StartLocalScale;

            RectTransform rect = avatar.AvatarObject.GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = avatar.StartAnchoredPosition;

            CanvasGroup group = GetAvatarCanvasGroup(avatar);
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
        }
    }

    private void ReturnReceiverToStart()
    {
        if (receiverObject == null)
            return;

        if (useReceiverStartPointOverride && receiverStartPoint != null)
        {
            receiverObject.position = receiverStartPoint.position;
            receiverObject.rotation = receiverStartPoint.rotation;
        }
        else
        {
            receiverObject.localPosition = receiverStartLocalPosition;
            receiverObject.localRotation = receiverStartLocalRotation;
        }

    }

    private bool IsReceiverInTarget()
    {
        if (receiverObject == null || receiverDropTarget == null)
            return false;

        return Vector3.Distance(receiverObject.position, receiverDropTarget.position) <= receiverAcceptDistance;
    }

    private bool IsReceiverClicked(Vector2 screenPosition)
    {
        return receiverObject != null && IsObjectClicked(screenPosition, receiverObject.gameObject);
    }

    private bool IsObjectClicked(Vector2 screenPosition, GameObject target)
    {
        ResolveCamera();

        if (target == null || arCamera == null)
        {
            LogDebug("Click check failed. Target or camera is missing.");
            return false;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance, interactionLayers, QueryTriggerInteraction.Collide);
        if (hits.Length == 0)
        {
            LogDebug("Raycast did not hit anything when checking: " + target.name);
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform == target.transform || hits[i].transform.IsChildOf(target.transform))
            {
                LogDebug("Raycast included " + target.name + " at hit index " + i + ". First hit was " + hits[0].transform.name + ".");
                return true;
            }
        }

        LogDebug("Raycast first hit " + hits[0].transform.name + ". Expected " + target.name + ". Match: False");
        return false;
    }

    private int GetClickedAvatarIndex(Vector2 screenPosition)
    {
        for (int i = 0; i < AvatarCount; i++)
        {
            AvatarOption avatar = avatars[i];
            if (avatar == null || avatar.AvatarObject == null || !avatar.AvatarObject.activeInHierarchy)
                continue;

            RectTransform rect = avatar.AvatarObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                Canvas canvas = avatar.AvatarObject.GetComponentInParent<Canvas>();
                Camera canvasCamera = GetCanvasEventCamera(canvas);
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, canvasCamera))
                    return i;
            }
            else if (IsObjectClicked(screenPosition, avatar.AvatarObject))
            {
                return i;
            }
        }

        return -1;
    }

    private Camera GetCanvasEventCamera(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (canvas.worldCamera != null)
            return canvas.worldCamera;

        return arCamera;
    }

    private AvatarOption GetSelectedAvatar()
    {
        if (selectedAvatarIndex < 0 || selectedAvatarIndex >= AvatarCount)
            return null;

        return avatars[selectedAvatarIndex];
    }

    private CanvasGroup GetAvatarCanvasGroup(AvatarOption avatar)
    {
        if (avatar == null || avatar.AvatarObject == null)
            return null;

        if (avatar.AvatarCanvasGroup != null)
            return avatar.AvatarCanvasGroup;

        avatar.AvatarCanvasGroup = avatar.AvatarObject.GetComponent<CanvasGroup>();
        if (avatar.AvatarCanvasGroup == null)
            avatar.AvatarCanvasGroup = avatar.AvatarObject.AddComponent<CanvasGroup>();

        return avatar.AvatarCanvasGroup;
    }

    private void SetAvatarButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < AvatarCount; i++)
        {
            if (avatars[i] != null && avatars[i].AvatarButton != null)
                avatars[i].AvatarButton.interactable = interactable;
        }
    }

    private void SetConversationText(string message, bool visible)
    {
        SetObjectActive(conversationTextPanel, visible);

        if (conversationText != null)
            conversationText.text = message;
    }

    private void SetDialogueVisible(bool visible)
    {
        SetObjectActive(butterflyDialoguePanel, visible);
    }

    private void SetHintText(string message, bool visible)
    {
        if (hintTypingRoutine != null)
        {
            StopCoroutine(hintTypingRoutine);
            hintTypingRoutine = null;
        }

        SetObjectActive(hintPanel, visible);

        if (hintText != null)
        {
            if (visible && isActiveAndEnabled)
                hintTypingRoutine = StartCoroutine(LwyTypewriterText.TypeText(hintText, message, hintTypingSeconds));
            else
                LwyTypewriterText.SetImmediate(hintText, message);
        }
    }

    private void SetNextButtonLabel()
    {
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

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, audioVolume);
    }

    private void LogDebug(string message)
    {
        if (logDebug)
            Debug.Log("LonelinessPhoneTaskController: " + message);
    }

    private bool PointerDown(out Vector2 position)
    {
        position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }
#endif

        return false;
    }

    private bool PointerHeld(out Vector2 position)
    {
        position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.isPressed)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                position = touch.position;
                return true;
            }
        }

        if (Input.GetMouseButton(0))
        {
            position = Input.mousePosition;
            return true;
        }
#endif

        return false;
    }

    private bool PointerUp(out Vector2 position)
    {
        position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0 &&
            (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled))
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            position = Input.mousePosition;
            return true;
        }
#endif

        return false;
    }

    private void ResolveReferences()
    {
        ResolveFlowerTaskController();
        ResolveCamera();
        ResolveSceneUi();
        ResolveNextButton();
    }

    private void ResolveFlowerTaskController()
    {
        if (flowerTaskController != null)
            return;

        flowerTaskController = GetComponentInParent<FlowerTaskInteractionController>();
    }

    private void ResolveCamera()
    {
        if (!autoFindCamera || arCamera != null)
            return;

        arCamera = Camera.main;

        if (arCamera == null)
        {
#if UNITY_2023_1_OR_NEWER
            arCamera = FindFirstObjectByType<Camera>();
#else
            arCamera = FindObjectOfType<Camera>();
#endif
        }
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

    private void RegisterAvatarButtons()
    {
        if (avatarListenersRegistered)
            return;

        for (int i = 0; i < AvatarCount; i++)
        {
            int index = i;
            if (avatars[i] != null && avatars[i].AvatarButton != null)
                avatars[i].AvatarButton.onClick.AddListener(() => SelectAvatar(index));
        }

        avatarListenersRegistered = true;
    }

    private void UnregisterAvatarButtons()
    {
        if (!avatarListenersRegistered)
            return;

        for (int i = 0; i < AvatarCount; i++)
        {
            if (avatars[i] != null && avatars[i].AvatarButton != null)
                avatars[i].AvatarButton.onClick.RemoveAllListeners();
        }

        avatarListenersRegistered = false;
    }

    private void CacheInitialState()
    {
        if (messageCanvasRoot != null)
            messageCanvasOriginalScale = messageCanvasRoot.transform.localScale;

        for (int i = 0; i < AvatarCount; i++)
        {
            AvatarOption avatar = avatars[i];
            if (avatar == null || avatar.AvatarObject == null)
                continue;

            avatar.StartPosition = avatar.AvatarObject.transform.position;
            avatar.StartLocalPosition = avatar.AvatarObject.transform.localPosition;
            avatar.StartLocalScale = avatar.AvatarObject.transform.localScale;

            RectTransform rect = avatar.AvatarObject.GetComponent<RectTransform>();
            if (rect != null)
                avatar.StartAnchoredPosition = rect.anchoredPosition;
        }

        if (receiverObject != null)
        {
            receiverStartPosition = receiverObject.position;
            receiverStartRotation = receiverObject.rotation;
            receiverStartLocalPosition = receiverObject.localPosition;
            receiverStartLocalRotation = receiverObject.localRotation;
        }

        if (lonelyOrbObject != null)
            lonelyOrbOriginalScale = lonelyOrbObject.transform.localScale;
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
