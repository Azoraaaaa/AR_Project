using System.Collections;
using TMPro;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FlowerEmotionGuideController : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string Text;

        public AudioClip VoiceClip;
        public float DisplaySeconds = 3f;
    }

    [Header("Flower Interaction")]
    [SerializeField] private FlowerTaskInteractionController flowerController;

    [Header("Camera")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private bool autoFindCamera = true;

    [Header("Butterfly Dialogue")]
    [SerializeField] private GameObject butterflyDialoguePanel;
    [SerializeField] private TMP_Text butterflyDialogueText;
    [SerializeField] private bool useVoiceClipLength = true;
    [SerializeField] private float dialogueTypingSeconds = LwyTypewriterText.DefaultCharacterSeconds;
    [SerializeField] private DialogueLine[] openingDialogue = new DialogueLine[0];
    [SerializeField] private DialogueLine[] emotionOrbDialogue = new DialogueLine[0];
    [SerializeField] private DialogueLine[] allOrbsCompletedDialogue = new DialogueLine[0];
    [SerializeField] private DialogueLine[] flowerPlacedDialogue = new DialogueLine[0];

    [Header("Hint UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private float hintTypingSeconds = LwyTypewriterText.DefaultCharacterSeconds;

    [TextArea(2, 4)]
    [SerializeField] private string startFlowerHintText = "Touch the flower first";

    [TextArea(2, 4)]
    [SerializeField] private string orbHintText = "Choose one emotion light";

    [TextArea(2, 4)]
    [SerializeField] private string finalFlowerHintText = "Drag the flower into the glowing circle";

    [TextArea(2, 4)]
    [SerializeField] private string flowerPlacedHintText = "The flower is in the right place";

    [Header("Final Flower Click")]
    [SerializeField] private bool waitForFlowerClickAfterPlacedDialogue = true;
    [SerializeField] private bool initializePropsBarImagesOnAwake = true;
    [SerializeField] private bool hideFlowerAfterFinalClick = true;
    [SerializeField] private Transform finalFlowerClickTarget;
    [SerializeField] private GameObject collectedFlowerObject;
    [SerializeField] private float finalFlowerClickRaycastDistance = 100f;
    [SerializeField] private LayerMask finalFlowerClickLayers = ~0;
    [SerializeField] private GameObject propsBarBeforeImage;
    [SerializeField] private GameObject propsBarAfterImage;

    [Header("Auto Find Scene UI")]
    [SerializeField] private bool autoFindSceneUi = true;
    [SerializeField] private string butterflyDialoguePanelName = "ButterflyDialoguePanel";
    [SerializeField] private string butterflyDialogueTextName = "ButterflyDialogueText";
    [SerializeField] private string hintPanelName = "HintPanel";
    [SerializeField] private string hintTextName = "HintText";
    [SerializeField] private string placementCircleHintName = "PlacementCircleHint";
    [SerializeField] private string orbCircleHintName = "OrbCircleHint";
    [SerializeField] private string fingerHintName = "FingerHint";
    [SerializeField] private string propsBarBeforeImageName = "PropsBarBeforeImage";
    [SerializeField] private string propsBarAfterImageName = "PropsBarAfterImage";

    [Header("World Hints")]
    [Tooltip("Hint circle canvas for the flower drop position")]
    [SerializeField] private GameObject placementCircleHint;

    [Tooltip("Hint circle canvas for choosing emotion orbs. Position it manually in the prefab or scene")]
    [SerializeField] private GameObject orbCircleHint;

    [Tooltip("Finger hint canvas. Supports World Space and Screen Space UI")]
    [SerializeField] private GameObject fingerHint;

    [Tooltip("Target used when the finger points to the flower. Uses FlowerObject when empty")]
    [SerializeField] private Transform flowerHintTarget;

    [Tooltip("Target used when the finger points to the placed flower after the final dialogue")]
    [SerializeField] private Transform placedFlowerHintTarget;

    [Tooltip("Fallback target used when the finger points to an orb. Uses the first unfinished orb when empty")]
    [SerializeField] private Transform firstOrbHintTarget;

    [SerializeField] private bool moveFingerToTarget = true;
    [SerializeField] private bool faceCameraWhenWorldSpace = true;
    [SerializeField] private Vector3 fingerWorldOffset = new Vector3(0f, 0.04f, 0f);
    [SerializeField] private Vector2 fingerScreenOffset = new Vector2(0f, 60f);

    [Header("Timing")]
    [SerializeField] private bool playOpeningGuideOnStart = true;
    [SerializeField] private float openingDelay = 0.2f;
    [SerializeField] private float nextOrbHintDelay = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip showHintClip;
    [SerializeField] private AudioClip hideHintClip;
    [SerializeField] private AudioClip flowerTouchedClip;
    [SerializeField] private AudioClip flowerDragStartedClip;
    [SerializeField] private AudioClip orbSelectedClip;
    [SerializeField] private AudioClip taskNextButtonClickClip;
    [SerializeField] private AudioClip taskCompletedClip;
    [SerializeField] private AudioClip allOrbsCompletedClip;
    [SerializeField] private AudioClip flowerPlacedClip;
    [SerializeField] private AudioClip finalFlowerClickClip;
    [SerializeField] private AudioClip propsBarChangedClip;

    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 1f;

    private Coroutine currentRoutine;
    private Coroutine hintTypingRoutine;
    private bool subscribed;
    private bool waitingForFinalFlowerClick;

    private void Awake()
    {
        ResolveReferences();
        InitializePropsBarImages();
        HideAllHints(false);
        SetDialogueVisible(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToFlowerController();
    }

    private void Start()
    {
        if (playOpeningGuideOnStart)
            StartGuideRoutine(OpeningGuideRoutine());
    }

    private void Update()
    {
        if (!waitingForFinalFlowerClick)
            return;

        if (!PointerDown(out Vector2 pointerPosition))
            return;

        if (IsFlowerClicked(pointerPosition))
            CompleteFinalFlowerClick();
    }

    private void OnDisable()
    {
        UnsubscribeFromFlowerController();
    }

    public void PlayOpeningGuide()
    {
        StartGuideRoutine(OpeningGuideRoutine());
    }

    private IEnumerator OpeningGuideRoutine()
    {
        yield return new WaitForSeconds(openingDelay);

        HideAllHints(false);
        yield return PlayDialogue(openingDialogue);

        ShowFlowerPlacementGuides(startFlowerHintText);
    }

    private IEnumerator FlowerTouchedRoutine()
    {
        HideAllHints(true);
        PlayOneShot(flowerTouchedClip);

        yield return PlayDialogue(emotionOrbDialogue);

        ShowOrbHint();
    }

    private IEnumerator NextOrbHintRoutine()
    {
        yield return new WaitForSeconds(nextOrbHintDelay);

        if (flowerController != null && flowerController.HasRemainingTasks)
            ShowOrbHint();
    }

    private IEnumerator AllOrbsCompletedRoutine()
    {
        HideAllHints(true);
        PlayOneShot(allOrbsCompletedClip);

        yield return PlayDialogue(allOrbsCompletedDialogue);

        ShowFlowerPlacementGuides(finalFlowerHintText);
    }

    private IEnumerator FlowerPlacedRoutine()
    {
        HideAllHints(true);
        PlayOneShot(flowerPlacedClip);

        yield return PlayDialogue(flowerPlacedDialogue);

        if (!string.IsNullOrEmpty(flowerPlacedHintText))
            SetHintText(flowerPlacedHintText, true);

        ShowFingerHint(GetPlacedFlowerHintTarget());

        waitingForFinalFlowerClick = waitForFlowerClickAfterPlacedDialogue;

        if (!waitingForFinalFlowerClick)
            CompleteFinalFlowerClick();
        else
            PlayOneShot(showHintClip);
    }

    private void OnFlowerTouched()
    {
        StartGuideRoutine(FlowerTouchedRoutine());
    }

    private void OnOrbSelected(int taskIndex)
    {
        StopGuideRoutine();
        HideAllHints(true);
        PlayOneShot(orbSelectedClip);
    }

    private void OnTaskStarted(int taskIndex)
    {
        StopGuideRoutine();
        HideAllHints(false);
    }

    private void OnTaskCompleteButtonClicked()
    {
        PlayOneShot(taskNextButtonClickClip);
    }

    private void OnTaskCompleted(int taskIndex)
    {
        if (flowerController == null || !flowerController.HasRemainingTasks)
            return;

        PlayOneShot(taskCompletedClip);
        StartGuideRoutine(NextOrbHintRoutine());
    }

    private void OnAllTasksCompleted()
    {
        StartGuideRoutine(AllOrbsCompletedRoutine());
    }

    private void OnFlowerPlaced()
    {
        StartGuideRoutine(FlowerPlacedRoutine());
    }

    private void OnFlowerDragStarted()
    {
        SetObjectActive(fingerHint, false);
        PlayOneShot(flowerDragStartedClip);
    }

    private void OnFlowerDropFailed()
    {
        if (flowerController != null && flowerController.HasRemainingTasks)
            return;

        SetHintText(finalFlowerHintText, true);
        SetObjectActive(placementCircleHint, true);
        SetObjectActive(orbCircleHint, false);
        ShowFingerHint(GetFlowerHintTarget());
        PlayOneShot(showHintClip);
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

    private void ShowFlowerPlacementGuides(string message)
    {
        SetHintText(message, true);
        SetObjectActive(placementCircleHint, true);
        SetObjectActive(orbCircleHint, false);
        ShowFingerHint(GetFlowerHintTarget());
        PlayOneShot(showHintClip);
    }

    private void ShowOrbHint()
    {
        SetHintText(orbHintText, true);
        SetObjectActive(placementCircleHint, false);
        SetObjectActive(fingerHint, false);
        SetObjectActive(orbCircleHint, true);
        PlayOneShot(showHintClip);
    }

    private void HideAllHints(bool playSound)
    {
        SetHintText("", false);
        SetObjectActive(placementCircleHint, false);
        SetObjectActive(orbCircleHint, false);
        SetObjectActive(fingerHint, false);

        if (playSound)
            PlayOneShot(hideHintClip);
    }

    private void ShowFingerHint(Transform target)
    {
        if (fingerHint == null)
            return;

        if (target == null)
        {
            fingerHint.SetActive(false);
            return;
        }

        fingerHint.SetActive(true);

        if (!moveFingerToTarget)
            return;

        MoveFingerToTarget(target);
    }

    private void MoveFingerToTarget(Transform target)
    {
        Canvas canvas = fingerHint.GetComponentInParent<Canvas>(true);
        RectTransform fingerRect = fingerHint.GetComponent<RectTransform>();

        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace && fingerRect != null)
        {
            ResolveCamera();

            if (arCamera == null)
                return;

            Vector3 screenPosition = arCamera.WorldToScreenPoint(target.position + fingerWorldOffset);
            screenPosition.x += fingerScreenOffset.x;
            screenPosition.y += fingerScreenOffset.y;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                fingerRect.position = screenPosition;
            }
            else
            {
                Camera canvasCamera = canvas.worldCamera != null ? canvas.worldCamera : arCamera;
                RectTransform canvasRect = canvas.transform as RectTransform;
                if (canvasRect != null &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, canvasCamera, out Vector2 localPosition))
                {
                    fingerRect.anchoredPosition = localPosition;
                }
            }

            return;
        }

        fingerHint.transform.position = target.position + target.TransformVector(fingerWorldOffset);

        if (faceCameraWhenWorldSpace)
            FaceCamera(fingerHint.transform);
    }

    private void FaceCamera(Transform target)
    {
        ResolveCamera();

        if (target == null || arCamera == null)
            return;

        Vector3 direction = target.position - arCamera.transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        target.rotation = Quaternion.LookRotation(direction, arCamera.transform.up);
    }

    private Transform GetFlowerHintTarget()
    {
        if (flowerHintTarget != null)
            return flowerHintTarget;

        return flowerController != null ? flowerController.GetFlowerTransform() : null;
    }

    private Transform GetOrbHintTarget()
    {
        if (firstOrbHintTarget != null && firstOrbHintTarget.gameObject.activeInHierarchy)
            return firstOrbHintTarget;

        return flowerController != null ? flowerController.GetFirstAvailableOrbTransform() : null;
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

    private void CompleteFinalFlowerClick()
    {
        if (!waitingForFinalFlowerClick && waitForFlowerClickAfterPlacedDialogue)
            return;

        waitingForFinalFlowerClick = false;
        HideAllHints(false);

        GameObject collectedFlower = GetCollectedFlowerObject();
        PlayOneShot(finalFlowerClickClip);

        if (hideFlowerAfterFinalClick)
            SetObjectActive(collectedFlower, false);

        SetObjectActive(propsBarBeforeImage, false);
        SetObjectActive(propsBarAfterImage, true);

        PlayOneShot(propsBarChangedClip);
        ShowNextPageCanvas();
    }

    private void ShowNextPageCanvas()
    {
        if (SimpleCloudRecoEventHandler.Instance != null)
            SimpleCloudRecoEventHandler.Instance.ShowNextPageCanvas();
    }

    private bool IsFlowerClicked(Vector2 screenPosition)
    {
        ResolveCamera();

        Transform flowerTransform = GetFinalFlowerClickTarget();
        if (arCamera == null || flowerTransform == null)
            return false;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, finalFlowerClickRaycastDistance, finalFlowerClickLayers, QueryTriggerInteraction.Collide))
            return false;

        return hit.transform == flowerTransform || hit.transform.IsChildOf(flowerTransform);
    }

    private Transform GetFinalFlowerClickTarget()
    {
        if (finalFlowerClickTarget != null)
            return finalFlowerClickTarget;

        return flowerController != null ? flowerController.GetFlowerTransform() : null;
    }

    private Transform GetPlacedFlowerHintTarget()
    {
        if (placedFlowerHintTarget != null)
            return placedFlowerHintTarget;

        if (finalFlowerClickTarget != null)
            return finalFlowerClickTarget;

        return flowerController != null ? flowerController.GetFlowerTransform() : null;
    }

    private GameObject GetCollectedFlowerObject()
    {
        if (collectedFlowerObject != null)
            return collectedFlowerObject;

        Transform flowerTransform = flowerController != null ? flowerController.GetFlowerTransform() : null;
        if (flowerTransform != null)
            return flowerTransform.gameObject;

        return finalFlowerClickTarget != null ? finalFlowerClickTarget.gameObject : null;
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

    private void StartGuideRoutine(IEnumerator routine)
    {
        StopGuideRoutine();

        currentRoutine = StartCoroutine(routine);
    }

    private void StopGuideRoutine()
    {
        waitingForFinalFlowerClick = false;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        SetDialogueVisible(false);
    }

    private void ResolveReferences()
    {
        if (flowerController == null)
            flowerController = GetComponent<FlowerTaskInteractionController>();

        if (flowerController == null)
            flowerController = GetComponentInParent<FlowerTaskInteractionController>();

        if (flowerController == null)
            flowerController = GetComponentInChildren<FlowerTaskInteractionController>(true);

        ResolveCamera();
        ResolveSceneUi();
    }

    private void InitializePropsBarImages()
    {
        if (!initializePropsBarImagesOnAwake)
            return;

        SetObjectActive(propsBarBeforeImage, true);
        SetObjectActive(propsBarAfterImage, false);
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

        if (placementCircleHint == null)
            placementCircleHint = FindSceneGameObject(placementCircleHintName);

        if (orbCircleHint == null)
            orbCircleHint = FindSceneGameObject(orbCircleHintName);

        if (fingerHint == null)
            fingerHint = FindSceneGameObject(fingerHintName);

        if (propsBarBeforeImage == null)
            propsBarBeforeImage = FindSceneGameObject(propsBarBeforeImageName);

        if (propsBarAfterImage == null)
            propsBarAfterImage = FindSceneGameObject(propsBarAfterImageName);
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

    private void SubscribeToFlowerController()
    {
        if (subscribed || flowerController == null)
            return;

        flowerController.FlowerTouched.AddListener(OnFlowerTouched);
        flowerController.OrbSelected.AddListener(OnOrbSelected);
        flowerController.TaskStarted.AddListener(OnTaskStarted);
        flowerController.TaskCompleteButtonClicked.AddListener(OnTaskCompleteButtonClicked);
        flowerController.TaskCompleted.AddListener(OnTaskCompleted);
        flowerController.AllTasksCompleted.AddListener(OnAllTasksCompleted);
        flowerController.FlowerDragStarted.AddListener(OnFlowerDragStarted);
        flowerController.FlowerDropFailed.AddListener(OnFlowerDropFailed);
        flowerController.FlowerPlaced.AddListener(OnFlowerPlaced);

        subscribed = true;
    }

    private void UnsubscribeFromFlowerController()
    {
        if (!subscribed || flowerController == null)
            return;

        flowerController.FlowerTouched.RemoveListener(OnFlowerTouched);
        flowerController.OrbSelected.RemoveListener(OnOrbSelected);
        flowerController.TaskStarted.RemoveListener(OnTaskStarted);
        flowerController.TaskCompleteButtonClicked.RemoveListener(OnTaskCompleteButtonClicked);
        flowerController.TaskCompleted.RemoveListener(OnTaskCompleted);
        flowerController.AllTasksCompleted.RemoveListener(OnAllTasksCompleted);
        flowerController.FlowerDragStarted.RemoveListener(OnFlowerDragStarted);
        flowerController.FlowerDropFailed.RemoveListener(OnFlowerDropFailed);
        flowerController.FlowerPlaced.RemoveListener(OnFlowerPlaced);

        subscribed = false;
    }
}
