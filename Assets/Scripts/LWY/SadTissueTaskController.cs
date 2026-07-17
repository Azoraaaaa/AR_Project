using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SadTissueTaskController : MonoBehaviour
{
    enum TaskState
    {
        WaitingToStart,
        WaitingForTissueBox,
        WaitingBeforeTears,
        CatchingTears,
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
    [SerializeField] private DialogueLine[] introDialogue = new DialogueLine[0];
    [SerializeField] private DialogueLine[] completedDialogue = new DialogueLine[0];

    [Header("Hint UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;

    [TextArea(2, 4)]
    [SerializeField] private string tissueBoxHintText = "Take a tissue";

    [TextArea(2, 4)]
    [SerializeField] private string catchTearHintText = "Catch the tears";

    [Header("World Hints")]
    [SerializeField] private GameObject tissueBoxHintImage;

    [Header("Tissue Box")]
    [SerializeField] private GameObject tissueBoxObject;
    [SerializeField] private AudioClip tissueBoxClickClip;

    [Header("Tissue")]
    [SerializeField] private Transform tissueObject;
    [SerializeField] private Rigidbody tissueRigidbody;
    [SerializeField] private bool hideTissueOnStart = true;
    [SerializeField] private bool followPointerWithoutPress = true;
    [SerializeField] private Transform dragPlaneReference;
    [SerializeField] private float tissueLiftHeight = 0.01f;

    [Header("Tears")]
    [SerializeField] private GameObject tearPrefab;
    [SerializeField] private Transform tearSpawnAreaCenter;
    [SerializeField] private Vector2 tearSpawnAreaSize = new Vector2(0.25f, 0.18f);
    [SerializeField] private float tearSpawnHeight = 0.12f;
    [SerializeField] private float tearFallSpeed = 0.05f;
    [SerializeField] private float tearCatchDistance = 0.035f;
    [SerializeField] private float tearDespawnDistanceBelowArea = 0.08f;
    [SerializeField] private float tearStartDelaySeconds = 2f;
    [SerializeField] private float secondsBetweenTears = 0.6f;
    [SerializeField] private int requiredCatchCount = 3;
    [SerializeField] private AudioClip tearCaughtClip;

    [Header("Sad Orb")]
    [SerializeField] private GameObject sadOrbObject;
    [SerializeField] private Transform sadOrbScaleRoot;
    [SerializeField] private float sadExpandScale = 1.25f;
    [SerializeField] private float sadShrinkScaleMultiplier = 0.85f;
    [SerializeField] private float sadScaleAnimSeconds = 0.35f;
    [SerializeField] private float sadDisappearSeconds = 0.45f;

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
    [SerializeField] private AudioClip taskCompletedClip;

    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 1f;

    [Header("Events")]
    public UnityEvent TaskFinished = new UnityEvent();

    [Header("Debug")]
    [SerializeField] private bool logDebug;
    [SerializeField] private bool logCatchDebug;
    [SerializeField] private float catchDebugInterval = 0.25f;

    private TaskState state = TaskState.WaitingToStart;
    private bool subscribedToFlowerTask;
    private bool waitingForNextTear;
    private int caughtTearCount;
    private GameObject activeTear;
    private Coroutine taskRoutine;
    private Coroutine tearSpawnRoutine;
    private Coroutine sadScaleRoutine;
    private Vector3 tissueStartLocalPosition;
    private Quaternion tissueStartLocalRotation;
    private Vector3 sadOriginalScale = Vector3.one;
    private Vector3 sadCurrentTargetScale = Vector3.one;
    private float nextCatchDebugTime;

    private void Awake()
    {
        ResolveReferences();
        CacheInitialState();
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

    private void Update()
    {
        if (state == TaskState.WaitingForTissueBox && PointerDown(out Vector2 clickPosition))
        {
            if (IsObjectClicked(clickPosition, tissueBoxObject))
                OnTissueBoxClicked();
        }

        if (state == TaskState.WaitingBeforeTears || state == TaskState.CatchingTears)
            UpdateTissueFollow();

        if (state == TaskState.CatchingTears)
            UpdateActiveTear();
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

        SetHintText(tissueBoxHintText, true);
        SetObjectActive(tissueBoxHintImage, true);
        PlayOneShot(showHintClip);

        state = TaskState.WaitingForTissueBox;
    }

    private void OnTissueBoxClicked()
    {
        LogDebug("Tissue box clicked.");

        SetObjectActive(tissueBoxHintImage, false);
        SetHintText(catchTearHintText, true);
        PlayOneShot(tissueBoxClickClip);

        if (tissueObject != null)
            tissueObject.gameObject.SetActive(true);

        state = TaskState.WaitingBeforeTears;

        if (tearSpawnRoutine != null)
            StopCoroutine(tearSpawnRoutine);

        tearSpawnRoutine = StartCoroutine(StartTearsAfterDelayRoutine());
    }

    private IEnumerator StartTearsAfterDelayRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, tearStartDelaySeconds));

        state = TaskState.CatchingTears;
        waitingForNextTear = false;
        SpawnNextTear();
        tearSpawnRoutine = null;
    }

    private void UpdateTissueFollow()
    {
        if (tissueObject == null || arCamera == null)
            return;

        bool hasPointer = followPointerWithoutPress
            ? TryGetPointerPosition(out Vector2 pointerPosition)
            : PointerHeld(out pointerPosition);

        if (!hasPointer)
            return;

        Ray ray = arCamera.ScreenPointToRay(pointerPosition);
        Vector3 planeNormal = dragPlaneReference != null ? dragPlaneReference.up : -arCamera.transform.forward;
        Plane dragPlane = new Plane(planeNormal, tissueObject.position);

        if (!dragPlane.Raycast(ray, out float enter))
            return;

        Vector3 targetPosition = ray.GetPoint(enter) + planeNormal * tissueLiftHeight;

        if (tissueRigidbody != null)
            tissueRigidbody.MovePosition(targetPosition);
        else
            tissueObject.position = targetPosition;
    }

    private void SpawnNextTear()
    {
        if (tearPrefab == null || caughtTearCount >= requiredCatchCount)
            return;

        Transform spawnArea = tearSpawnAreaCenter != null ? tearSpawnAreaCenter : transform;
        Vector3 randomOffset =
            spawnArea.right * Random.Range(-tearSpawnAreaSize.x * 0.5f, tearSpawnAreaSize.x * 0.5f) +
            spawnArea.forward * Random.Range(-tearSpawnAreaSize.y * 0.5f, tearSpawnAreaSize.y * 0.5f) +
            spawnArea.up * tearSpawnHeight;

        Vector3 spawnPosition = spawnArea.position + randomOffset;
        activeTear = Instantiate(tearPrefab, spawnPosition, spawnArea.rotation, transform);
        activeTear.SetActive(true);
    }

    private void UpdateActiveTear()
    {
        if (activeTear == null)
        {
            if (!waitingForNextTear)
                SpawnNextTear();

            return;
        }

        Transform spawnArea = tearSpawnAreaCenter != null ? tearSpawnAreaCenter : transform;
        Vector3 fallDirection = -spawnArea.up;
        activeTear.transform.position += fallDirection * tearFallSpeed * Time.deltaTime;

        if (IsTearCaught(activeTear.transform))
        {
            CatchActiveTear();
            return;
        }

        float distanceBelowArea = Vector3.Dot(spawnArea.position - activeTear.transform.position, spawnArea.up);
        if (distanceBelowArea > tearSpawnHeight + tearDespawnDistanceBelowArea)
        {
            Destroy(activeTear);
            activeTear = null;
            StartNextTearDelay();
        }
    }

    private IEnumerator SpawnNextTearAfterDelayRoutine()
    {
        waitingForNextTear = true;
        yield return new WaitForSeconds(Mathf.Max(0f, secondsBetweenTears));

        waitingForNextTear = false;

        if (state == TaskState.CatchingTears && activeTear == null)
            SpawnNextTear();

        tearSpawnRoutine = null;
    }

    private void StartNextTearDelay()
    {
        if (!waitingForNextTear)
            tearSpawnRoutine = StartCoroutine(SpawnNextTearAfterDelayRoutine());
    }

    private bool IsTearCaught(Transform tearTransform)
    {
        if (tearTransform == null || tissueObject == null || !tissueObject.gameObject.activeInHierarchy)
        {
            LogCatchDebug("Catch check failed. Tear=" + (tearTransform != null) + ", Tissue=" + (tissueObject != null) + ", TissueActive=" + (tissueObject != null && tissueObject.gameObject.activeInHierarchy));
            return false;
        }

        float distance = Vector3.Distance(tearTransform.position, tissueObject.position);
        bool caught = distance <= tearCatchDistance;
        LogCatchDebug("Catch distance=" + distance.ToString("F4") + ", threshold=" + tearCatchDistance.ToString("F4") + ", caught=" + caught);
        return caught;
    }

    private void CatchActiveTear()
    {
        if (activeTear != null)
        {
            Destroy(activeTear);
            activeTear = null;
        }

        caughtTearCount++;
        LogDebug("Tear caught. Count=" + caughtTearCount + "/" + requiredCatchCount);
        PlayOneShot(tearCaughtClip);
        AnimateSadOrb();

        if (caughtTearCount >= requiredCatchCount)
        {
            StartCoroutine(CompleteTaskRoutine());
            return;
        }

        StartNextTearDelay();
    }

    private IEnumerator CompleteTaskRoutine()
    {
        state = TaskState.Complete;
        SetObjectActive(hintPanel, false);

        if (activeTear != null)
        {
            Destroy(activeTear);
            activeTear = null;
        }

        yield return ShrinkAndHideSadOrbRoutine();

        PlayOneShot(taskCompletedClip);
        yield return PlayDialogue(completedDialogue);

        SetNextButtonLabel();
        SetNextButtonVisible(true);
        TaskFinished.Invoke();
    }

    private void AnimateSadOrb()
    {
        Transform target = GetSadScaleTarget();
        if (target == null)
            return;

        if (sadScaleRoutine != null)
            StopCoroutine(sadScaleRoutine);

        sadScaleRoutine = StartCoroutine(SadScaleRoutine(target));
    }

    private IEnumerator SadScaleRoutine(Transform target)
    {
        Vector3 startScale = target.localScale;
        Vector3 expandedScale = startScale * sadExpandScale;
        Vector3 finalScale = sadCurrentTargetScale * sadShrinkScaleMultiplier;
        sadCurrentTargetScale = finalScale;

        yield return ScaleOverTime(target, startScale, expandedScale, sadScaleAnimSeconds * 0.45f);
        yield return ScaleOverTime(target, expandedScale, finalScale, sadScaleAnimSeconds * 0.55f);

        sadScaleRoutine = null;
    }

    private IEnumerator ShrinkAndHideSadOrbRoutine()
    {
        Transform target = GetSadScaleTarget();
        if (target == null)
            yield break;

        if (sadScaleRoutine != null)
            StopCoroutine(sadScaleRoutine);

        yield return ScaleOverTime(target, target.localScale, Vector3.zero, sadDisappearSeconds);

        if (sadOrbObject != null)
            sadOrbObject.SetActive(false);
        else
            target.gameObject.SetActive(false);
    }

    private IEnumerator ScaleOverTime(Transform target, Vector3 from, Vector3 to, float seconds)
    {
        if (target == null)
            yield break;

        if (seconds <= 0f)
        {
            target.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);
            target.localScale = Vector3.Lerp(from, to, Smooth(t));
            yield return null;
        }

        target.localScale = to;
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

            yield return new WaitForSeconds(seconds);
        }

        SetDialogueVisible(false);
    }

    private void ResetTask()
    {
        StopRunningRoutines();

        state = TaskState.WaitingToStart;
        caughtTearCount = 0;
        waitingForNextTear = false;

        if (activeTear != null)
        {
            Destroy(activeTear);
            activeTear = null;
        }

        SetInitialVisibility();
        ResetTissue();

        Transform sadScaleTarget = GetSadScaleTarget();
        if (sadScaleTarget != null)
            sadScaleTarget.localScale = sadOriginalScale;

        sadCurrentTargetScale = sadOriginalScale;

        if (sadOrbObject != null)
            sadOrbObject.SetActive(true);

        if (hideNextButtonUntilComplete)
            SetNextButtonVisible(false);
    }

    private void ResetTissue()
    {
        if (tissueObject == null)
            return;

        tissueObject.localPosition = tissueStartLocalPosition;
        tissueObject.localRotation = tissueStartLocalRotation;

        if (hideTissueOnStart)
            tissueObject.gameObject.SetActive(false);

        if (tissueRigidbody != null)
        {
            tissueRigidbody.linearVelocity = Vector3.zero;
            tissueRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void SetInitialVisibility()
    {
        SetObjectActive(hintPanel, false);
        SetObjectActive(tissueBoxHintImage, false);
        SetDialogueVisible(false);
    }

    private Transform GetSadScaleTarget()
    {
        if (sadOrbScaleRoot != null)
            return sadOrbScaleRoot;

        return sadOrbObject != null ? sadOrbObject.transform : null;
    }

    private bool IsObjectClicked(Vector2 screenPosition, GameObject target)
    {
        ResolveCamera();

        if (target == null || arCamera == null)
            return false;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance, interactionLayers, QueryTriggerInteraction.Collide);
        if (hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform == target.transform || hits[i].transform.IsChildOf(target.transform))
            {
                LogDebug("Raycast included " + target.name + ". First hit was " + hits[0].transform.name + ".");
                return true;
            }
        }

        LogDebug("Raycast first hit " + hits[0].transform.name + ". Expected " + target.name + ".");
        return false;
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

    private bool TryGetPointerPosition(out Vector2 position)
    {
        position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        position = Input.mousePosition;
        return true;
#else
        return false;
#endif
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

    private void CacheInitialState()
    {
        if (tissueObject != null)
        {
            tissueStartLocalPosition = tissueObject.localPosition;
            tissueStartLocalRotation = tissueObject.localRotation;
        }

        Transform sadScaleTarget = GetSadScaleTarget();
        if (sadScaleTarget != null)
        {
            sadOriginalScale = sadScaleTarget.localScale;
            sadCurrentTargetScale = sadOriginalScale;
        }
    }

    private void StopRunningRoutines()
    {
        if (taskRoutine != null)
        {
            StopCoroutine(taskRoutine);
            taskRoutine = null;
        }

        if (tearSpawnRoutine != null)
        {
            StopCoroutine(tearSpawnRoutine);
            tearSpawnRoutine = null;
        }

        if (sadScaleRoutine != null)
        {
            StopCoroutine(sadScaleRoutine);
            sadScaleRoutine = null;
        }
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
            Debug.Log("SadTissueTaskController: " + message);
    }

    private void LogCatchDebug(string message)
    {
        if (!logCatchDebug || Time.time < nextCatchDebugTime)
            return;

        nextCatchDebugTime = Time.time + Mathf.Max(0.01f, catchDebugInterval);
        Debug.Log("SadTissueTaskController Catch: " + message);
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
