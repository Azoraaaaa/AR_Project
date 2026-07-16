using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FlowerTaskInteractionController : MonoBehaviour
{
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
    public LayerMask InteractionLayers = ~0;
    public float RaycastDistance = 100f;

    [Header("Flower")]
    public GameObject FlowerObject;
    public bool HideFlowerDuringTask = true;

    [Header("Drop Zone")]
    public Collider DropZoneCollider;
    public Transform DropSnapPoint;
    public float DropAcceptDistance = 0.05f;
    public bool SnapToDropPoint = true;

    [Header("Scene Models")]
    public GameObject[] ModelsToHideDuringTask = new GameObject[0];

    [Header("Tasks")]
    public TaskStep[] Tasks = new TaskStep[0];

    [Header("Full Screen Panel")]
    public CanvasGroup FullScreenPanel;
    public Image PanelImage;
    public TMP_Text PanelText;
    public float PanelFadeSeconds = 0.5f;

    [Header("Testing")]
    public Button TaskCompleteButton;

    InteractionState mState = InteractionState.WaitingForFirstFlowerTouch;
    readonly Dictionary<GameObject, bool> mOriginalModelStates = new Dictionary<GameObject, bool>();
    Plane mDragPlane;
    Vector3 mDragOffset;
    int mCurrentTaskIndex = -1;
    bool mIsBusy;
    bool mIsDraggingFlower;

    int TaskCount
    {
        get { return Tasks == null ? 0 : Tasks.Length; }
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
        if (InteractionCamera == null)
            InteractionCamera = Camera.main;

        CacheOriginalModelStates();

        if (TaskCompleteButton != null)
            TaskCompleteButton.onClick.AddListener(CompleteCurrentTask);
    }

    void Start()
    {
        ResetInteraction();
    }

    void OnDestroy()
    {
        if (TaskCompleteButton != null)
            TaskCompleteButton.onClick.RemoveListener(CompleteCurrentTask);
    }

    void Update()
    {
        if (mIsBusy || InteractionCamera == null)
            return;

        PointerInput pointer;
        if (!TryGetPrimaryPointer(out pointer))
            return;

        if (pointer.Down && IsPointerOverUi(pointer.PointerId))
            return;

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
        mState = InteractionState.WaitingForFirstFlowerTouch;

        for (int i = 0; i < TaskCount; i++)
        {
            if (Tasks[i] == null)
                continue;

            Tasks[i].Completed = false;
        }

        SetMainModelsVisible(true);
        SetAllTaskRootsVisible(false);
        SetAllOrbsVisible(false);
        SetTaskCompleteButtonVisible(false);
        SetPanelVisibleInstant(false);
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
        if (mState == InteractionState.WaitingForFirstFlowerTouch)
        {
            if (IsObjectHit(screenPosition, FlowerObject))
            {
                ShowAvailableOrbs();
                mState = HasUnfinishedTask() ? InteractionState.ChoosingOrb : InteractionState.FlowerDraggable;
            }

            return;
        }

        if (mState == InteractionState.ChoosingOrb)
        {
            int taskIndex = GetHitTaskOrbIndex(screenPosition);
            if (taskIndex >= 0)
                StartCoroutine(StartTaskRoutine(taskIndex));

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

        SetAllOrbsVisible(false);
        yield return ShowTimedPanel(task.IntroImage, task.IntroText, task.IntroPanelSeconds);

        SetMainModelsVisible(false);
        SetAllTaskRootsVisible(false);

        if (task.TaskRoot != null)
            task.TaskRoot.SetActive(true);

        SetTaskCompleteButtonVisible(true);
        mState = InteractionState.RunningTask;
        mIsBusy = false;
    }

    IEnumerator CompleteCurrentTaskRoutine()
    {
        mIsBusy = true;
        mState = InteractionState.ShowingPanel;
        SetTaskCompleteButtonVisible(false);

        TaskStep task = mCurrentTaskIndex >= 0 && mCurrentTaskIndex < TaskCount ? Tasks[mCurrentTaskIndex] : null;

        if (task != null && task.TaskRoot != null)
            task.TaskRoot.SetActive(false);

        if (task != null)
            yield return ShowTimedPanel(task.CompletionImage, task.CompletionText, task.CompletionPanelSeconds);

        if (task != null)
        {
            task.Completed = true;

            if (task.OrbObject != null)
                task.OrbObject.SetActive(false);
        }

        mCurrentTaskIndex = -1;
        SetMainModelsVisible(true);

        if (HasUnfinishedTask())
        {
            ShowAvailableOrbs();
            mState = InteractionState.ChoosingOrb;
        }
        else
        {
            SetAllOrbsVisible(false);
            mState = InteractionState.FlowerDraggable;
        }

        mIsBusy = false;
    }

    IEnumerator ShowTimedPanel(Sprite image, string message, float seconds)
    {
        if (FullScreenPanel == null)
        {
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
            return;

        if (SnapToDropPoint && DropSnapPoint != null && FlowerObject != null)
            FlowerObject.transform.position = DropSnapPoint.position;

        mState = InteractionState.FlowerPlaced;
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

    bool TryGetPrimaryPointer(out PointerInput pointer)
    {
        pointer = new PointerInput();

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
        return target != null && TryRaycast(screenPosition, out hit) && IsTransformUnderRoot(hit.transform, target.transform);
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

    bool IsTransformUnderRoot(Transform child, Transform root)
    {
        if (child == null || root == null)
            return false;

        return child == root || child.IsChildOf(root);
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
                Tasks[i].OrbObject.SetActive(visible);
        }
    }

    void ShowAvailableOrbs()
    {
        for (int i = 0; i < TaskCount; i++)
        {
            TaskStep task = Tasks[i];
            if (task != null && task.OrbObject != null)
                task.OrbObject.SetActive(!task.Completed);
        }
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
