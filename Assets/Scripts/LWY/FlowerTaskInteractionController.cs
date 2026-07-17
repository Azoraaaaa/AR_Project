using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FlowerTaskInteractionController : MonoBehaviour
{
    [System.Serializable]
    public class TaskIndexEvent : UnityEvent<int>
    {
    }

    enum InteractionState
    {
        WaitingForFirstFlowerTouch,
        ChoosingOrb,
        ShowingPanel,
        RunningTask,
        FlowerDraggable,
        FlowerPlaced
    }

    [System.Serializable]
    public class TaskStep
    {
        [Header("Scene Objects")]
        public GameObject OrbObject;
        public GameObject TaskRoot;

        [Header("Orb Dissolve")]
        public GameObject OrbDissolveEffect;
        public AudioClip OrbDissolveClip;

        [Header("Intro Panel")]
        public Sprite IntroImage;
        [TextArea(2, 5)] public string IntroText;
        public float IntroPanelSeconds = 3f;

        [Header("Completion Panel")]
        public Sprite CompletionImage;
        [TextArea(2, 5)] public string CompletionText;
        public float CompletionPanelSeconds = 3f;

        [HideInInspector] public bool Completed;
    }

    [Header("Camera")]
    public Camera InteractionCamera;
    public bool AutoFindInteractionCamera = true;
    public string CameraTag = "MainCamera";
    public LayerMask InteractionLayers = ~0;
    public float RaycastDistance = 100f;
    public bool BlockWorldTouchesOverUi = false;

    [Header("Flower")]
    public GameObject FlowerObject;
    public bool HideFlowerDuringTask = true;
    public bool AutoCreateMissingColliders = true;

    [Header("Drop Zone")]
    public Collider DropZoneCollider;
    public Transform DropSnapPoint;
    public float DropAcceptDistance = 0.05f;
    public bool SnapToDropPoint = true;

    [Header("Scene Models")]
    public GameObject[] ModelsToHideDuringTask = new GameObject[0];

    [Header("Tasks")]
    public TaskStep[] Tasks = new TaskStep[0];
    public bool ActivateOrbParentsWhenShown = true;

    [Header("Orb Dissolve Effect")]
    public GameObject DefaultOrbDissolveEffect;
    public AudioSource OrbDissolveAudioSource;
    public AudioClip DefaultOrbDissolveClip;
    public float OrbDissolveDelaySeconds = 1.2f;
    public float OrbShrinkSeconds = 0.7f;
    public float OrbDissolveParticleFadeSeconds = 0.8f;
    public bool MoveDissolveEffectToOrb = true;
    public bool DetachDissolveEffectDuringPlay = true;

    [Header("Main Canvas Sidebar")]
    public GameObject MainCanvasSidebar;
    public bool AutoFindMainCanvasSidebar = true;
    public string MainCanvasSidebarName = "Sidebar";
    public bool HideMainCanvasSidebarDuringTask = true;

    [Header("Full Screen Panel")]
    public CanvasGroup FullScreenPanel;
    public Image PanelImage;
    public TMP_Text PanelText;
    public float PanelFadeSeconds = 0.5f;

    [Header("Testing")]
    public Button TaskCompleteButton;
    public bool AutoFindTaskCompleteButton = true;
    public string TaskCompleteButtonName = "TaskCompleteButton";
    public bool AutoConfigureUiCanvases = true;
    public bool UseTaskCompleteButtonFallbackClick = true;

    [Header("Debug")]
    public bool LogTouchDebug = true;
    public bool ShowDebugOverlay = true;

    [Header("Guide Events")]
    public UnityEvent FlowerTouched = new UnityEvent();
    public TaskIndexEvent OrbSelected = new TaskIndexEvent();
    public TaskIndexEvent TaskStarted = new TaskIndexEvent();
    public TaskIndexEvent TaskCompleted = new TaskIndexEvent();
    public UnityEvent AllTasksCompleted = new UnityEvent();
    public UnityEvent FlowerDragStarted = new UnityEvent();
    public UnityEvent FlowerDropFailed = new UnityEvent();
    public UnityEvent FlowerPlaced = new UnityEvent();

    InteractionState mState = InteractionState.WaitingForFirstFlowerTouch;
    readonly Dictionary<GameObject, bool> mOriginalModelStates = new Dictionary<GameObject, bool>();
    readonly Dictionary<GameObject, Vector3> mOriginalOrbScales = new Dictionary<GameObject, Vector3>();
    Plane mDragPlane;
    Vector3 mDragOffset;
    Vector3 mFlowerStartLocalPosition;
    Quaternion mFlowerStartLocalRotation;
    int mCurrentTaskIndex = -1;
    int mPendingOrbDissolveIndex = -1;
    bool mIsBusy;
    bool mIsDraggingFlower;
    bool mHasFlowerStartPose;
    bool mHasMainCanvasSidebarInitialState;
    bool mMainCanvasSidebarInitialState;
    string mLastDebugMessage = "";
    Button mRegisteredTaskCompleteButton;

    int TaskCount
    {
        get { return Tasks == null ? 0 : Tasks.Length; }
    }

    public bool HasRemainingTasks
    {
        get { return HasUnfinishedTask(); }
    }

    struct PointerInput
    {
        public Vector2 Position;
        public int PointerId;
        public bool Down;
        public bool Held;
        public bool Up;
    }

    void Awake()
    {
        ResolveInteractionCamera();
        ResolveTaskCompleteButton();
        ResolveMainCanvasSidebar();
        ConfigureUiCanvases();

        CacheOriginalModelStates();
        CacheOriginalOrbScales();
        CacheFlowerStartPose();
        CacheMainCanvasSidebarState();
        SetOrbDissolveEffectsVisible(false);

        RegisterTaskCompleteButtonListener();
    }

    void OnEnable()
    {
        ResolveInteractionCamera();
        ResolveTaskCompleteButton();
        ResolveMainCanvasSidebar();
        ConfigureUiCanvases();
        CacheMainCanvasSidebarState();
        RegisterTaskCompleteButtonListener();
    }

    void Start()
    {
        PrepareClickableObjects();
        ValidateSetup();
        CacheOriginalOrbScales();
        CacheFlowerStartPose();
        ResetInteraction();
    }

    void OnDestroy()
    {
        UnregisterTaskCompleteButtonListener();
    }

    void Update()
    {
        if (mIsBusy)
            return;

        if (InteractionCamera == null)
            ResolveInteractionCamera();

        if (InteractionCamera == null)
            return;

        ResolveTaskCompleteButton();
        RegisterTaskCompleteButtonListener();
        ConfigureUiCanvases();

        PointerInput pointer;
        if (!TryGetPrimaryPointer(out pointer))
            return;

        if (pointer.Down && UseTaskCompleteButtonFallbackClick && IsTaskCompleteButtonScreenHit(pointer.Position))
        {
            LogDebug("Task complete button clicked by fallback screen check.");
            CompleteCurrentTask();
            return;
        }

        if (pointer.Down && BlockWorldTouchesOverUi && IsPointerOverUi(pointer.PointerId))
        {
            LogDebug("Touch ignored because it is over UI.");
            return;
        }

        if (pointer.Down)
            HandlePointerDown(pointer.Position);

        if (mIsDraggingFlower && pointer.Held)
            DragFlower(pointer.Position);

        if (mIsDraggingFlower && pointer.Up)
            EndFlowerDrag();
    }

    public void ResetInteraction()
    {
        StopAllCoroutines();

        mIsBusy = false;
        mIsDraggingFlower = false;
        mCurrentTaskIndex = -1;
        mPendingOrbDissolveIndex = -1;
        mState = InteractionState.WaitingForFirstFlowerTouch;

        for (int i = 0; i < TaskCount; i++)
        {
            if (Tasks[i] == null)
                continue;

            Tasks[i].Completed = false;
        }

        SetMainModelsVisible(true);
        SetMainCanvasSidebarVisible(true);
        SetAllTaskRootsVisible(false);
        SetAllOrbsVisible(false);
        RestoreOriginalOrbScales();
        SetOrbDissolveEffectsVisible(false);
        SetTaskCompleteButtonVisible(false);
        SetPanelVisibleInstant(false);
        ReturnFlowerToStart();
    }

    public void CompleteCurrentTask()
    {
        if (mState != InteractionState.RunningTask || mCurrentTaskIndex < 0 || mCurrentTaskIndex >= TaskCount)
            return;

        if (!mIsBusy)
            StartCoroutine(CompleteCurrentTaskRoutine());
    }

    void HandlePointerDown(Vector2 screenPosition)
    {
        LogDebug("Pointer down. Current state: " + mState);

        if (mState == InteractionState.WaitingForFirstFlowerTouch)
        {
            if (IsObjectHit(screenPosition, FlowerObject))
            {
                LogDebug("Flower touched. Showing available orbs.");
                ShowAvailableOrbs();
                mState = HasUnfinishedTask() ? InteractionState.ChoosingOrb : InteractionState.FlowerDraggable;
                FlowerTouched.Invoke();
            }

            return;
        }

        if (mState == InteractionState.ChoosingOrb)
        {
            int taskIndex = GetHitTaskOrbIndex(screenPosition);
            if (taskIndex >= 0)
            {
                LogDebug("Orb touched. Starting task index: " + taskIndex);
                OrbSelected.Invoke(taskIndex);
                StartCoroutine(StartTaskRoutine(taskIndex));
            }

            return;
        }

        if (mState == InteractionState.FlowerDraggable && IsObjectHit(screenPosition, FlowerObject))
        {
            BeginFlowerDrag(screenPosition);
        }
    }

    IEnumerator StartTaskRoutine(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= TaskCount)
            yield break;

        TaskStep task = Tasks[taskIndex];
        if (task == null || task.Completed)
            yield break;

        mIsBusy = true;
        mState = InteractionState.ShowingPanel;
        mCurrentTaskIndex = taskIndex;
        SetMainCanvasSidebarVisible(false);

        yield return ShowTimedPanel(task.IntroImage, task.IntroText, task.IntroPanelSeconds, () =>
        {
            SetAllOrbsVisible(false);
            SetMainModelsVisible(false);
            SetAllTaskRootsVisible(false);

            if (task.TaskRoot != null)
                task.TaskRoot.SetActive(true);
        });

        SetTaskCompleteButtonVisible(true);
        mState = InteractionState.RunningTask;
        TaskStarted.Invoke(taskIndex);
        mIsBusy = false;
    }

    IEnumerator CompleteCurrentTaskRoutine()
    {
        mIsBusy = true;
        mState = InteractionState.ShowingPanel;
        SetTaskCompleteButtonVisible(false);

        int completedTaskIndex = mCurrentTaskIndex;
        TaskStep task = mCurrentTaskIndex >= 0 && mCurrentTaskIndex < TaskCount ? Tasks[mCurrentTaskIndex] : null;

        if (task != null)
        {
            mPendingOrbDissolveIndex = completedTaskIndex;

            yield return ShowTimedPanel(task.CompletionImage, task.CompletionText, task.CompletionPanelSeconds, () =>
            {
                if (task.TaskRoot != null)
                    task.TaskRoot.SetActive(false);

                task.Completed = true;

                SetMainModelsVisible(true);

                ShowAvailableOrbs();
            });

            SetMainCanvasSidebarVisible(true);

            yield return PlayCompletedOrbDissolveRoutine(task);
            mPendingOrbDissolveIndex = -1;

            if (HasUnfinishedTask())
                ShowAvailableOrbs();
            else
                SetAllOrbsVisible(false);
        }
        else
        {
            SetMainModelsVisible(true);
            SetMainCanvasSidebarVisible(true);
        }

        mCurrentTaskIndex = -1;

        bool hasUnfinishedTask = HasUnfinishedTask();
        if (hasUnfinishedTask)
        {
            mState = InteractionState.ChoosingOrb;
        }
        else
        {
            SetAllOrbsVisible(false);
            mState = InteractionState.FlowerDraggable;
        }

        TaskCompleted.Invoke(completedTaskIndex);

        if (!hasUnfinishedTask)
            AllTasksCompleted.Invoke();

        mIsBusy = false;
    }

    IEnumerator ShowTimedPanel(Sprite image, string message, float seconds, System.Action onFullyVisible = null)
    {
        if (FullScreenPanel == null)
        {
            if (onFullyVisible != null)
                onFullyVisible();

            yield return new WaitForSeconds(Mathf.Max(0f, seconds));
            yield break;
        }

        if (PanelImage != null)
        {
            PanelImage.sprite = image;
            PanelImage.enabled = image != null;
        }

        if (PanelText != null)
            PanelText.text = message;

        yield return FadePanel(1f);

        if (onFullyVisible != null)
            onFullyVisible();

        yield return new WaitForSeconds(Mathf.Max(0f, seconds));
        yield return FadePanel(0f);
    }

    IEnumerator FadePanel(float targetAlpha)
    {
        float startAlpha = FullScreenPanel.alpha;
        float elapsed = 0f;

        FullScreenPanel.gameObject.SetActive(true);
        FullScreenPanel.blocksRaycasts = true;
        FullScreenPanel.interactable = true;

        if (PanelFadeSeconds <= 0f)
        {
            FullScreenPanel.alpha = targetAlpha;
        }
        else
        {
            while (elapsed < PanelFadeSeconds)
            {
                elapsed += Time.deltaTime;
                FullScreenPanel.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / PanelFadeSeconds);
                yield return null;
            }

            FullScreenPanel.alpha = targetAlpha;
        }

        bool visible = targetAlpha > 0.001f;
        FullScreenPanel.blocksRaycasts = visible;
        FullScreenPanel.interactable = visible;

        if (!visible)
            FullScreenPanel.gameObject.SetActive(false);
    }

    void BeginFlowerDrag(Vector2 screenPosition)
    {
        if (FlowerObject == null)
            return;

        Ray ray = InteractionCamera.ScreenPointToRay(screenPosition);
        mDragPlane = new Plane(-InteractionCamera.transform.forward, FlowerObject.transform.position);

        float enter;
        if (mDragPlane.Raycast(ray, out enter))
            mDragOffset = FlowerObject.transform.position - ray.GetPoint(enter);
        else
            mDragOffset = Vector3.zero;

        mIsDraggingFlower = true;
        FlowerDragStarted.Invoke();
    }

    void DragFlower(Vector2 screenPosition)
    {
        if (FlowerObject == null)
            return;

        Ray ray = InteractionCamera.ScreenPointToRay(screenPosition);
        float enter;
        if (mDragPlane.Raycast(ray, out enter))
            FlowerObject.transform.position = ray.GetPoint(enter) + mDragOffset;
    }

    void EndFlowerDrag()
    {
        mIsDraggingFlower = false;

        if (!IsFlowerInDropZone())
        {
            ReturnFlowerToStart();
            FlowerDropFailed.Invoke();
            return;
        }

        if (SnapToDropPoint && DropSnapPoint != null && FlowerObject != null)
            FlowerObject.transform.position = DropSnapPoint.position;

        mState = InteractionState.FlowerPlaced;
        FlowerPlaced.Invoke();
    }

    bool IsFlowerInDropZone()
    {
        if (FlowerObject == null || DropZoneCollider == null)
            return false;

        Vector3 flowerPosition = FlowerObject.transform.position;
        if (DropZoneCollider.bounds.Contains(flowerPosition))
            return true;

        Vector3 closestPoint = DropZoneCollider.ClosestPoint(flowerPosition);
        return Vector3.Distance(flowerPosition, closestPoint) <= DropAcceptDistance;
    }

    IEnumerator PlayCompletedOrbDissolveRoutine(TaskStep task)
    {
        if (task == null || task.OrbObject == null)
            yield break;

        if (OrbDissolveDelaySeconds > 0f)
            yield return new WaitForSeconds(OrbDissolveDelaySeconds);

        GameObject orbObject = task.OrbObject;
        Transform orbTransform = orbObject.transform;
        Vector3 startScale = orbTransform.localScale;
        GameObject effect = GetOrbDissolveEffect(task);
        Transform originalEffectParent = null;
        bool detachedEffect = false;

        SetOrbVisible(orbObject, true);

        if (effect != null)
        {
            if (DetachDissolveEffectDuringPlay && effect != orbObject && effect.transform.IsChildOf(orbTransform))
            {
                originalEffectParent = effect.transform.parent;
                effect.transform.SetParent(orbTransform.parent, true);
                detachedEffect = true;
            }

            if (MoveDissolveEffectToOrb)
            {
                effect.transform.position = orbTransform.position;
                effect.transform.rotation = orbTransform.rotation;
            }

            effect.SetActive(true);
            PlayDissolveParticles(effect);
        }

        PlayOrbDissolveAudio(task);

        float elapsed = 0f;
        while (elapsed < OrbShrinkSeconds)
        {
            elapsed += Time.deltaTime;
            float t = OrbShrinkSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / OrbShrinkSeconds);
            float smoothT = t * t * (3f - 2f * t);
            orbTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, smoothT);
            yield return null;
        }

        orbTransform.localScale = Vector3.zero;
        SetOrbVisible(orbObject, false);

        if (effect != null)
        {
            StopDissolveParticles(effect);

            if (OrbDissolveParticleFadeSeconds > 0f)
                yield return new WaitForSeconds(OrbDissolveParticleFadeSeconds);

            effect.SetActive(false);

            if (detachedEffect)
                effect.transform.SetParent(originalEffectParent, true);
        }
    }

    GameObject GetOrbDissolveEffect(TaskStep task)
    {
        if (task != null && task.OrbDissolveEffect != null)
            return task.OrbDissolveEffect;

        return DefaultOrbDissolveEffect;
    }

    AudioClip GetOrbDissolveClip(TaskStep task)
    {
        if (task != null && task.OrbDissolveClip != null)
            return task.OrbDissolveClip;

        return DefaultOrbDissolveClip;
    }

    void PlayOrbDissolveAudio(TaskStep task)
    {
        AudioClip clip = GetOrbDissolveClip(task);
        if (clip == null)
            return;

        if (OrbDissolveAudioSource != null)
        {
            OrbDissolveAudioSource.PlayOneShot(clip);
            return;
        }

        Vector3 position = task != null && task.OrbObject != null ? task.OrbObject.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(clip, position);
    }

    void PlayDissolveParticles(GameObject effect)
    {
        if (effect == null)
            return;

        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null)
                continue;

            particles[i].Clear(true);
            particles[i].Play(true);
        }
    }

    void StopDissolveParticles(GameObject effect)
    {
        if (effect == null)
            return;

        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] != null)
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    void SetOrbDissolveEffectsVisible(bool visible)
    {
        if (DefaultOrbDissolveEffect != null)
            DefaultOrbDissolveEffect.SetActive(visible);

        for (int i = 0; i < TaskCount; i++)
        {
            if (Tasks[i] != null && Tasks[i].OrbDissolveEffect != null)
                Tasks[i].OrbDissolveEffect.SetActive(visible);
        }
    }

    void CacheFlowerStartPose()
    {
        if (mHasFlowerStartPose || FlowerObject == null)
            return;

        mFlowerStartLocalPosition = FlowerObject.transform.localPosition;
        mFlowerStartLocalRotation = FlowerObject.transform.localRotation;
        mHasFlowerStartPose = true;
    }

    void ReturnFlowerToStart()
    {
        if (!mHasFlowerStartPose || FlowerObject == null)
            return;

        FlowerObject.transform.localPosition = mFlowerStartLocalPosition;
        FlowerObject.transform.localRotation = mFlowerStartLocalRotation;
    }

    bool TryGetPrimaryPointer(out PointerInput pointer)
    {
        pointer = new PointerInput();

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            pointer.Position = Touchscreen.current.primaryTouch.position.ReadValue();
            pointer.PointerId = 0;
            pointer.Down = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            pointer.Held = Touchscreen.current.primaryTouch.press.isPressed;
            pointer.Up = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            return pointer.Down || pointer.Held || pointer.Up;
        }

        if (Mouse.current != null)
        {
            pointer.Position = Mouse.current.position.ReadValue();
            pointer.PointerId = -1;
            pointer.Down = Mouse.current.leftButton.wasPressedThisFrame;
            pointer.Held = Mouse.current.leftButton.isPressed;
            pointer.Up = Mouse.current.leftButton.wasReleasedThisFrame;
            return pointer.Down || pointer.Held || pointer.Up;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointer.Position = touch.position;
            pointer.PointerId = touch.fingerId;
            pointer.Down = touch.phase == TouchPhase.Began;
            pointer.Held = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            pointer.Up = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            return pointer.Down || pointer.Held || pointer.Up;
        }

        pointer.Position = Input.mousePosition;
        pointer.PointerId = -1;
        pointer.Down = Input.GetMouseButtonDown(0);
        pointer.Held = Input.GetMouseButton(0);
        pointer.Up = Input.GetMouseButtonUp(0);
        return pointer.Down || pointer.Held || pointer.Up;
#else
        return false;
#endif
    }

    bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
            return false;

        if (pointerId >= 0)
            return EventSystem.current.IsPointerOverGameObject(pointerId);

        return EventSystem.current.IsPointerOverGameObject();
    }

    bool IsObjectHit(Vector2 screenPosition, GameObject target)
    {
        RaycastHit hit;
        if (target == null)
        {
            LogDebug("Raycast target is not assigned.");
            return false;
        }

        if (!TryRaycast(screenPosition, out hit))
        {
            LogDebug("Raycast did not hit anything. Check Camera, Collider, Layer, and RaycastDistance.");
            return false;
        }

        bool hitTarget = IsTransformUnderRoot(hit.transform, target.transform);
        LogDebug("Raycast hit: " + hit.transform.name + ". Expected: " + target.name + ". Match: " + hitTarget);
        return hitTarget;
    }

    int GetHitTaskOrbIndex(Vector2 screenPosition)
    {
        RaycastHit hit;
        if (!TryRaycast(screenPosition, out hit))
            return -1;

        for (int i = 0; i < TaskCount; i++)
        {
            TaskStep task = Tasks[i];
            if (task == null || task.Completed || task.OrbObject == null)
                continue;

            if (IsTransformUnderRoot(hit.transform, task.OrbObject.transform))
                return i;
        }

        return -1;
    }

    bool TryRaycast(Vector2 screenPosition, out RaycastHit hit)
    {
        Ray ray = InteractionCamera.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out hit, RaycastDistance, InteractionLayers, QueryTriggerInteraction.Collide);
    }

    bool IsTaskCompleteButtonScreenHit(Vector2 screenPosition)
    {
        if (mState != InteractionState.RunningTask || TaskCompleteButton == null)
            return false;

        if (!TaskCompleteButton.gameObject.activeInHierarchy || !TaskCompleteButton.interactable)
            return false;

        RectTransform rectTransform = TaskCompleteButton.GetComponent<RectTransform>();
        if (rectTransform == null)
            return false;

        Canvas canvas = TaskCompleteButton.GetComponentInParent<Canvas>();
        Camera eventCamera = GetCanvasEventCamera(canvas);
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera);
    }

    Camera GetCanvasEventCamera(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (canvas.worldCamera != null)
            return canvas.worldCamera;

        return InteractionCamera;
    }

    void ResolveTaskCompleteButton()
    {
        if (!AutoFindTaskCompleteButton || TaskCompleteButton != null || string.IsNullOrEmpty(TaskCompleteButtonName))
            return;

        GameObject buttonObject = GameObject.Find(TaskCompleteButtonName);
        if (buttonObject != null)
            TaskCompleteButton = buttonObject.GetComponent<Button>();

        if (TaskCompleteButton == null)
        {
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null || buttons[i].name != TaskCompleteButtonName)
                    continue;

                if (!buttons[i].gameObject.scene.IsValid())
                    continue;

                TaskCompleteButton = buttons[i];
                break;
            }
        }

        if (TaskCompleteButton != null)
            LogDebug("Found task complete button by name: " + TaskCompleteButtonName);
    }

    void ResolveMainCanvasSidebar()
    {
        if (!AutoFindMainCanvasSidebar || MainCanvasSidebar != null || string.IsNullOrEmpty(MainCanvasSidebarName))
            return;

        MainCanvasSidebar = FindSceneGameObjectByName(MainCanvasSidebarName);

        if (MainCanvasSidebar != null)
            LogDebug("Found main canvas sidebar by name: " + MainCanvasSidebarName);
    }

    GameObject FindSceneGameObjectByName(string objectName)
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

    void RegisterTaskCompleteButtonListener()
    {
        if (TaskCompleteButton == null || mRegisteredTaskCompleteButton == TaskCompleteButton)
            return;

        UnregisterTaskCompleteButtonListener();
        TaskCompleteButton.onClick.AddListener(CompleteCurrentTask);
        mRegisteredTaskCompleteButton = TaskCompleteButton;
    }

    void UnregisterTaskCompleteButtonListener()
    {
        if (mRegisteredTaskCompleteButton == null)
            return;

        mRegisteredTaskCompleteButton.onClick.RemoveListener(CompleteCurrentTask);
        mRegisteredTaskCompleteButton = null;
    }

    void ConfigureUiCanvases()
    {
        if (!AutoConfigureUiCanvases)
            return;

        ConfigureCanvasForObject(FullScreenPanel != null ? FullScreenPanel.gameObject : null);
        ConfigureCanvasForObject(TaskCompleteButton != null ? TaskCompleteButton.gameObject : null);
    }

    void ConfigureCanvasForObject(GameObject uiObject)
    {
        if (uiObject == null)
            return;

        Canvas canvas = uiObject.GetComponentInParent<Canvas>(true);
        if (canvas == null)
            return;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            LogDebug("Added GraphicRaycaster to canvas: " + canvas.name);
        }

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && InteractionCamera != null && canvas.worldCamera != InteractionCamera)
        {
            canvas.worldCamera = InteractionCamera;
            LogDebug("Assigned UI canvas camera: " + canvas.name + " -> " + InteractionCamera.name);
        }
    }

    void ResolveInteractionCamera()
    {
        if (!AutoFindInteractionCamera || InteractionCamera != null)
            return;

        if (!string.IsNullOrEmpty(CameraTag))
        {
            GameObject taggedCamera = null;
            try
            {
                taggedCamera = GameObject.FindGameObjectWithTag(CameraTag);
            }
            catch (UnityException)
            {
                Debug.LogWarning("FlowerTaskInteractionController: CameraTag is not defined: " + CameraTag);
            }

            if (taggedCamera != null)
                InteractionCamera = taggedCamera.GetComponent<Camera>();
        }

        if (InteractionCamera == null)
            InteractionCamera = Camera.main;

        if (InteractionCamera == null)
        {
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].isActiveAndEnabled)
                {
                    InteractionCamera = cameras[i];
                    break;
                }
            }
        }

        if (InteractionCamera != null)
            LogDebug("Using interaction camera: " + InteractionCamera.name);
    }

    void ValidateSetup()
    {
        ResolveInteractionCamera();

        LogDebug("Controller is running. State: " + mState);

        if (InteractionCamera == null)
            Debug.LogWarning("FlowerTaskInteractionController: InteractionCamera is not assigned and Camera.main was not found.");

        if (FlowerObject == null)
            Debug.LogWarning("FlowerTaskInteractionController: FlowerObject is not assigned.");
        else if (FlowerObject.GetComponentInChildren<Collider>() == null)
            Debug.LogWarning("FlowerTaskInteractionController: FlowerObject has no Collider in itself or its children. It cannot be clicked.");

        for (int i = 0; i < TaskCount; i++)
        {
            if (Tasks[i] == null || Tasks[i].OrbObject == null)
                continue;

            if (Tasks[i].OrbObject.GetComponentInChildren<Collider>() == null)
                Debug.LogWarning("FlowerTaskInteractionController: OrbObject has no Collider and cannot be clicked: " + Tasks[i].OrbObject.name);
        }
    }

    void LogDebug(string message)
    {
        mLastDebugMessage = message;

        if (LogTouchDebug)
            Debug.Log("FlowerTaskInteractionController: " + message);
    }

    void PrepareClickableObjects()
    {
        if (!AutoCreateMissingColliders)
            return;

        EnsureCollider(FlowerObject);

        for (int i = 0; i < TaskCount; i++)
        {
            if (Tasks[i] != null)
                EnsureCollider(Tasks[i].OrbObject);
        }
    }

    void EnsureCollider(GameObject target)
    {
        if (target == null || target.GetComponentInChildren<Collider>(true) != null)
            return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning("FlowerTaskInteractionController: Cannot auto-create Collider because no Renderer was found on " + target.name);
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        BoxCollider collider = target.AddComponent<BoxCollider>();
        collider.center = target.transform.InverseTransformPoint(bounds.center);

        Vector3 localSize = target.transform.InverseTransformVector(bounds.size);
        collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

        LogDebug("Auto-created BoxCollider on " + target.name);
    }

    void OnGUI()
    {
        if (!ShowDebugOverlay)
            return;

        string cameraName = InteractionCamera != null ? InteractionCamera.name : "None";
        string flowerName = FlowerObject != null ? FlowerObject.name : "None";
        string debugText =
            "Flower Interaction Debug\n" +
            "State: " + mState + "\n" +
            "Camera: " + cameraName + "\n" +
            "Flower: " + flowerName + "\n" +
            "Last: " + mLastDebugMessage;

        GUI.Box(new Rect(20, 20, 430, 120), debugText);
    }

    bool IsTransformUnderRoot(Transform child, Transform root)
    {
        if (child == null || root == null)
            return false;

        return child == root || child.IsChildOf(root);
    }

    public Transform GetFirstAvailableOrbTransform()
    {
        for (int i = 0; i < TaskCount; i++)
        {
            if (Tasks[i] != null && !Tasks[i].Completed && Tasks[i].OrbObject != null)
                return Tasks[i].OrbObject.transform;
        }

        return null;
    }

    public Transform GetFlowerTransform()
    {
        return FlowerObject != null ? FlowerObject.transform : null;
    }

    void CacheOriginalOrbScales()
    {
        mOriginalOrbScales.Clear();

        for (int i = 0; i < TaskCount; i++)
        {
            if (Tasks[i] == null || Tasks[i].OrbObject == null)
                continue;

            mOriginalOrbScales[Tasks[i].OrbObject] = Tasks[i].OrbObject.transform.localScale;
        }
    }

    void RestoreOriginalOrbScales()
    {
        foreach (KeyValuePair<GameObject, Vector3> pair in mOriginalOrbScales)
        {
            if (pair.Key != null)
                pair.Key.transform.localScale = pair.Value;
        }
    }

    void CacheOriginalModelStates()
    {
        mOriginalModelStates.Clear();

        if (HideFlowerDuringTask && FlowerObject != null)
            AddOriginalModelState(FlowerObject);

        if (ModelsToHideDuringTask == null)
            return;

        for (int i = 0; i < ModelsToHideDuringTask.Length; i++)
            AddOriginalModelState(ModelsToHideDuringTask[i]);
    }

    void AddOriginalModelState(GameObject model)
    {
        if (model != null && !mOriginalModelStates.ContainsKey(model))
            mOriginalModelStates.Add(model, model.activeSelf);
    }

    void SetMainModelsVisible(bool visible)
    {
        foreach (KeyValuePair<GameObject, bool> pair in mOriginalModelStates)
        {
            if (pair.Key == null)
                continue;

            pair.Key.SetActive(visible ? pair.Value : false);
        }
    }

    void CacheMainCanvasSidebarState()
    {
        if (mHasMainCanvasSidebarInitialState || MainCanvasSidebar == null)
            return;

        mMainCanvasSidebarInitialState = MainCanvasSidebar.activeSelf;
        mHasMainCanvasSidebarInitialState = true;
    }

    void SetMainCanvasSidebarVisible(bool visible)
    {
        if (!HideMainCanvasSidebarDuringTask || MainCanvasSidebar == null)
            return;

        CacheMainCanvasSidebarState();
        MainCanvasSidebar.SetActive(visible ? mMainCanvasSidebarInitialState : false);
    }

    void SetAllTaskRootsVisible(bool visible)
    {
        for (int i = 0; i < TaskCount; i++)
        {
            if (Tasks[i] != null && Tasks[i].TaskRoot != null)
                Tasks[i].TaskRoot.SetActive(visible);
        }
    }

    void SetAllOrbsVisible(bool visible)
    {
        for (int i = 0; i < TaskCount; i++)
        {
            if (Tasks[i] != null && Tasks[i].OrbObject != null)
                SetOrbVisible(Tasks[i].OrbObject, visible);
        }
    }

    void ShowAvailableOrbs()
    {
        if (TaskCount == 0)
        {
            Debug.LogWarning("FlowerTaskInteractionController: No Tasks are configured, so no orbs can be shown.");
            return;
        }

        int visibleOrbCount = 0;

        for (int i = 0; i < TaskCount; i++)
        {
            TaskStep task = Tasks[i];
            if (task == null)
            {
                Debug.LogWarning("FlowerTaskInteractionController: Task " + i + " is empty.");
                continue;
            }

            if (task.OrbObject == null)
            {
                Debug.LogWarning("FlowerTaskInteractionController: Task " + i + " has no OrbObject assigned.");
                continue;
            }

            bool shouldShow = !task.Completed || i == mPendingOrbDissolveIndex;
            SetOrbVisible(task.OrbObject, shouldShow);

            if (shouldShow)
                visibleOrbCount++;

            LogDebug(
                "Orb " + i +
                " visible=" + shouldShow +
                ", activeSelf=" + task.OrbObject.activeSelf +
                ", activeInHierarchy=" + task.OrbObject.activeInHierarchy +
                ", enabledRenderers=" + CountEnabledRenderers(task.OrbObject) +
                ", position=" + task.OrbObject.transform.position);
        }

        LogDebug("Available orb count: " + visibleOrbCount);
    }

    bool HasUnfinishedTask()
    {
        for (int i = 0; i < TaskCount; i++)
        {
            if (Tasks[i] != null && !Tasks[i].Completed)
                return true;
        }

        return false;
    }

    void SetTaskCompleteButtonVisible(bool visible)
    {
        if (TaskCompleteButton != null)
            TaskCompleteButton.gameObject.SetActive(visible);
    }

    int CountEnabledRenderers(GameObject root)
    {
        if (root == null)
            return 0;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        int count = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].enabled)
                count++;
        }

        return count;
    }

    void SetOrbVisible(GameObject orbObject, bool visible)
    {
        if (orbObject == null)
            return;

        if (visible && ActivateOrbParentsWhenShown)
            ActivateParentsUpToController(orbObject.transform);

        orbObject.SetActive(visible);
    }

    void ActivateParentsUpToController(Transform child)
    {
        if (child == null || !child.IsChildOf(transform))
        {
            LogDebug("Orb is not under this controller, so parent activation was skipped.");
            return;
        }

        Transform current = child.parent;
        while (current != null && current != transform)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
                LogDebug("Activated orb parent: " + current.name);
            }

            current = current.parent;
        }
    }

    void SetPanelVisibleInstant(bool visible)
    {
        if (FullScreenPanel == null)
            return;

        FullScreenPanel.alpha = visible ? 1f : 0f;
        FullScreenPanel.blocksRaycasts = visible;
        FullScreenPanel.interactable = visible;
        FullScreenPanel.gameObject.SetActive(visible);
    }
}
