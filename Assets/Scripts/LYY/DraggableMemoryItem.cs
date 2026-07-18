using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class DraggableMemoryItem : MonoBehaviour
{
    [Header("Item Type")]
    [SerializeField] private MemoryItemType itemType;

    [Header("Camera")]
    [Tooltip("Prefab 中可以留空，运行时自动寻找 MainCamera")]
    [SerializeField] private Camera arCamera;

    [Header("Drag Plane")]
    [Tooltip("拖入 Prefab 内部的 ContentRoot")]
    [SerializeField] private Transform dragPlaneReference;

    [Tooltip("拖动时稍微抬高，避免和桌面重叠")]
    [SerializeField] private float liftHeight = 0.01f;

    [Header("Drag Smoothing")]
    [Tooltip("0 = 完全跟手；20~30 = 稍微平滑")]
    [SerializeField] private float dragSmoothSpeed = 0f;

    [Header("Raycast")]
    [SerializeField] private LayerMask raycastLayers = ~0;

    [SerializeField] private float rayDistance = 100f;

    [Header("Flow")]
    [SerializeField] private MemoryItemSequence sequenceManager;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    public MemoryItemType ItemType => itemType;

    private Rigidbody itemRigidbody;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private Plane dragPlane;
    private Vector3 dragOffset;

    private Vector3 targetDragPosition;

    private bool isDragging;

    private MemoryItemSlot currentSlot;

    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        itemRigidbody =
            GetComponent<Rigidbody>();

        originalLocalPosition =
            transform.localPosition;

        originalLocalRotation =
            transform.localRotation;

        if (sequenceManager == null)
        {
            sequenceManager =
                GetComponentInParent<MemoryItemSequence>();
        }

        FindCamera();

        if (itemRigidbody != null)
        {
            itemRigidbody.useGravity = false;
            itemRigidbody.isKinematic = true;

            /*
             * 不依赖 Rigidbody 插值。
             * 拖动直接由 Transform 控制。
             */
            itemRigidbody.interpolation =
                RigidbodyInterpolation.None;
        }
    }

    private void OnEnable()
    {
        if (arCamera == null)
        {
            FindCamera();
        }
    }

    private void Update()
    {
        /*
         * 种子还没放好、
         * Sequence 还没开始时不能拖。
         */
        if (sequenceManager == null ||
            !sequenceManager.SequenceStarted)
        {
            return;
        }

        /*
         * 还没开始拖动。
         */
        if (!isDragging)
        {
            if (PointerDown(
                out Vector2 pointerPosition))
            {
                TryStartDragging(
                    pointerPosition
                );
            }

            return;
        }

        /*
         * 正在拖动。
         */
        if (PointerHeld(
            out Vector2 heldPosition))
        {
            UpdateDragTarget(
                heldPosition
            );
        }

        /*
         * 每个渲染帧直接更新位置。
         */
        ApplyDragMovement();

        /*
         * 松手。
         */
        if (PointerUp(
            out Vector2 releasePosition))
        {
            StopDragging();
        }
    }

    // =========================================================
    // Start Drag
    // =========================================================

    private void TryStartDragging(
        Vector2 screenPosition)
    {
        if (arCamera == null)
        {
            FindCamera();
        }

        if (arCamera == null)
        {
            Debug.LogError(
                $"{name}: Camera not found."
            );

            return;
        }

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        /*
         * 使用 RaycastAll，
         * 避免合并后其他 Collider 挡住物品。
         */
        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                rayDistance,
                raycastLayers,
                QueryTriggerInteraction.Collide
            );

        if (hits == null ||
            hits.Length == 0)
        {
            return;
        }

        Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(
                    b.distance
                )
        );

        bool clickedThisItem = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            DraggableMemoryItem hitItem =
                hit.collider
                    .GetComponentInParent
                    <DraggableMemoryItem>();

            if (showDebugLogs)
            {
                Debug.Log(
                    $"{name}: Ray hit " +
                    $"{hit.collider.name}"
                );
            }

            if (hitItem == this)
            {
                clickedThisItem = true;
                break;
            }
        }

        if (!clickedThisItem)
        {
            return;
        }

        isDragging = true;

        /*
         * 开始拖时清空旧 Slot，
         * 避免上一次 Trigger 状态残留。
         */
        currentSlot = null;

        if (sequenceManager != null)
        {
            sequenceManager.PlayGrabSound();
        }

        /*
         * 保留你原来的 Drag Plane 逻辑：
         *
         * 有 DragPlaneReference
         * → 使用 dragPlaneReference.up
         *
         * 没有
         * → 使用 Camera.forward
         */
        Vector3 planeNormal =
            dragPlaneReference != null
                ? dragPlaneReference.up
                : arCamera.transform.forward;

        if (planeNormal.sqrMagnitude <
            0.000001f)
        {
            planeNormal =
                arCamera.transform.forward;
        }

        planeNormal.Normalize();

        dragPlane =
            new Plane(
                planeNormal,
                transform.position
            );

        if (!dragPlane.Raycast(
            ray,
            out float enter))
        {
            isDragging = false;

            if (showDebugLogs)
            {
                Debug.LogWarning(
                    $"{name}: Could not hit drag plane."
                );
            }

            return;
        }

        Vector3 hitPoint =
            ray.GetPoint(enter);

        /*
         * 保留点击点与物体中心的偏移，
         * 防止物体突然跳到手指中心。
         */
        dragOffset =
            transform.position -
            hitPoint;

        targetDragPosition =
            transform.position;

        if (showDebugLogs)
        {
            Debug.Log(
                $"{name}: Drag started."
            );
        }
    }

    // =========================================================
    // Drag Target
    // =========================================================

    private void UpdateDragTarget(
        Vector2 screenPosition)
    {
        if (arCamera == null)
            return;

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        if (!dragPlane.Raycast(
            ray,
            out float enter))
        {
            return;
        }

        Vector3 hitPoint =
            ray.GetPoint(enter);

        /*
         * 继续保持你原来的方向逻辑。
         */
        Vector3 planeNormal =
            dragPlaneReference != null
                ? dragPlaneReference.up
                : arCamera.transform.forward;

        if (planeNormal.sqrMagnitude <
            0.000001f)
        {
            planeNormal =
                arCamera.transform.forward;
        }

        planeNormal.Normalize();

        targetDragPosition =
            hitPoint +
            dragOffset +
            planeNormal * liftHeight;
    }

    // =========================================================
    // Apply Movement
    // =========================================================

    private void ApplyDragMovement()
    {
        if (!isDragging)
            return;

        /*
         * 重点优化：
         *
         * 不使用 Rigidbody.MovePosition()
         * 而是直接更新 Transform。
         *
         * 这样拖动会跟随 Update 的屏幕刷新频率，
         * 手感更顺滑。
         */
        if (dragSmoothSpeed <= 0f)
        {
            transform.position =
                targetDragPosition;
        }
        else
        {
            /*
             * 可选平滑。
             */
            float smoothing =
                1f -
                Mathf.Exp(
                    -dragSmoothSpeed *
                    Time.unscaledDeltaTime
                );

            transform.position =
                Vector3.Lerp(
                    transform.position,
                    targetDragPosition,
                    smoothing
                );
        }
    }

    // =========================================================
    // Stop Drag
    // =========================================================

    private void StopDragging()
    {
        if (!isDragging)
            return;

        isDragging = false;

        /*
         * Transform 直接移动后，
         * 松手时同步一次 Physics。
         */
        Physics.SyncTransforms();

        bool placedSuccessfully = false;

        if (currentSlot != null &&
            sequenceManager != null)
        {
            placedSuccessfully =
                sequenceManager.TryPlaceItem(
                    this,
                    currentSlot
                );
        }

        if (placedSuccessfully)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    $"{name}: Placement successful."
                );
            }

            return;
        }

        /*
         * 放错或没有进入正确 Slot。
         */
        if (sequenceManager != null)
        {
            sequenceManager.PlayGrabSound();
        }

        ReturnToOriginalPosition();
    }

    // =========================================================
    // Return
    // =========================================================

    private void ReturnToOriginalPosition()
    {
        transform.localPosition =
            originalLocalPosition;

        transform.localRotation =
            originalLocalRotation;

        targetDragPosition =
            transform.position;

        currentSlot = null;

        if (itemRigidbody != null)
        {
            itemRigidbody.linearVelocity =
                Vector3.zero;

            itemRigidbody.angularVelocity =
                Vector3.zero;
        }
    }

    // =========================================================
    // Slot Detection
    // =========================================================

    private void OnTriggerEnter(
        Collider other)
    {
        MemoryItemSlot slot =
            other.GetComponentInParent
            <MemoryItemSlot>();

        if (slot == null)
            return;

        currentSlot = slot;

        if (showDebugLogs)
        {
            Debug.Log(
                $"{name} entered slot: " +
                $"{slot.name}, " +
                $"AcceptedType: " +
                $"{slot.AcceptedType}"
            );
        }
    }

    private void OnTriggerStay(
        Collider other)
    {
        /*
         * Transform 拖动时，
         * 加 OnTriggerStay 可以减少快速移动时
         * TriggerEnter 漏判的问题。
         */
        MemoryItemSlot slot =
            other.GetComponentInParent
            <MemoryItemSlot>();

        if (slot != null)
        {
            currentSlot = slot;
        }
    }

    private void OnTriggerExit(
        Collider other)
    {
        MemoryItemSlot slot =
            other.GetComponentInParent
            <MemoryItemSlot>();

        if (slot != null &&
            slot == currentSlot)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    $"{name} exited slot: " +
                    $"{slot.name}"
                );
            }

            currentSlot = null;
        }
    }

    // =========================================================
    // Camera
    // =========================================================

    private void FindCamera()
    {
        if (arCamera != null &&
            arCamera.isActiveAndEnabled)
        {
            return;
        }

        arCamera =
            Camera.main;

        if (arCamera == null)
        {
#if UNITY_2023_1_OR_NEWER
            arCamera =
                FindFirstObjectByType<Camera>();
#else
            arCamera =
                FindObjectOfType<Camera>();
#endif
        }

        if (arCamera == null)
        {
            Debug.LogError(
                $"{name}: Camera not found. " +
                "Make sure ARCamera has MainCamera tag."
            );
        }
        else if (showDebugLogs)
        {
            Debug.Log(
                $"{name}: Using camera " +
                $"{arCamera.name}"
            );
        }
    }

    // =========================================================
    // Input
    // =========================================================

    private bool PointerDown(
        out Vector2 position)
    {
        position =
            Vector2.zero;

        /*
         * Touch
         */
        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current
                    .primaryTouch;

            if (touch.press
                .wasPressedThisFrame)
            {
                position =
                    touch.position
                        .ReadValue();

                return true;
            }
        }

        /*
         * Mouse
         */
        if (Mouse.current != null &&
            Mouse.current.leftButton
                .wasPressedThisFrame)
        {
            position =
                Mouse.current.position
                    .ReadValue();

            return true;
        }

        return false;
    }

    private bool PointerHeld(
        out Vector2 position)
    {
        position =
            Vector2.zero;

        /*
         * Touch
         */
        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current
                    .primaryTouch;

            if (touch.press.isPressed)
            {
                position =
                    touch.position
                        .ReadValue();

                return true;
            }
        }

        /*
         * Mouse
         */
        if (Mouse.current != null &&
            Mouse.current.leftButton
                .isPressed)
        {
            position =
                Mouse.current.position
                    .ReadValue();

            return true;
        }

        return false;
    }

    private bool PointerUp(
        out Vector2 position)
    {
        position =
            Vector2.zero;

        /*
         * Touch
         */
        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current
                    .primaryTouch;

            if (touch.press
                .wasReleasedThisFrame)
            {
                position =
                    touch.position
                        .ReadValue();

                return true;
            }
        }

        /*
         * Mouse
         */
        if (Mouse.current != null &&
            Mouse.current.leftButton
                .wasReleasedThisFrame)
        {
            position =
                Mouse.current.position
                    .ReadValue();

            return true;
        }

        return false;
    }
}