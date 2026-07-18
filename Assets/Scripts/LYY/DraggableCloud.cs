using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class DraggableCloud : MonoBehaviour
{
    [Header("Page Flow")]
    [SerializeField]
    private FlowController2 flowController;

    [Header("Camera")]
    [Tooltip("Prefab 中可以留空，运行时自动寻找 MainCamera")]
    [SerializeField]
    private Camera arCamera;

    [Header("Drag Plane")]
    [Tooltip("通常拖入 Page11Prefab 或 ContentRoot")]
    [SerializeField]
    private Transform dragPlaneReference;

    [Header("Raycast")]
    [Tooltip("建议先设为 Everything 测试")]
    [SerializeField]
    private LayerMask raycastLayers = ~0;

    [SerializeField]
    private float rayDistance = 100f;

    [Header("UI Blocking")]
    [Tooltip(
        "关闭后，即使透明 UI 覆盖屏幕也可以拖云。" +
        "Page11 建议保持关闭。"
    )]
    [SerializeField]
    private bool blockWhenPointerOverUI = false;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = false;

    private bool interactable;
    private bool isDragging;

    private Plane dragPlane;
    private Vector3 dragOffset;

    // =========================================================
    // Unity Lifecycle
    // =========================================================

    private void Awake()
    {
        FindFlowController();
        FindCamera();

        if (flowController == null)
        {
            Debug.LogError(
                $"DraggableCloud [{name}]: " +
                "FlowController2 could not be found."
            );
        }

        Collider cloudCollider =
            GetComponent<Collider>();

        if (cloudCollider == null)
        {
            Debug.LogError(
                $"DraggableCloud [{name}]: " +
                "Collider is missing."
            );
        }
        else if (!cloudCollider.enabled)
        {
            Debug.LogWarning(
                $"DraggableCloud [{name}]: " +
                "Collider is disabled."
            );
        }
    }

    private void Update()
    {
        /*
         * FlowController2 会在开场对白结束后，
         * 调用 SetInteractable(true)。
         */
        if (!interactable)
            return;

        if (flowController == null)
        {
            FindFlowController();

            if (flowController == null)
                return;
        }

        if (!flowController.CanDragCloud)
            return;

        // -------------------------
        // 尚未开始拖动
        // -------------------------

        if (!isDragging)
        {
            if (PointerDown(
                out Vector2 pointerPosition))
            {
                TryBeginDragging(
                    pointerPosition
                );
            }

            return;
        }

        // -------------------------
        // 正在拖动
        // -------------------------

        if (PointerHeld(
            out Vector2 heldPosition))
        {
            ContinueDragging(
                heldPosition
            );
        }

        if (PointerUp())
        {
            EndDragging();
        }
    }

    // =========================================================
    // Interaction
    // =========================================================

    public void SetInteractable(bool value)
    {
        interactable = value;

        if (!value)
        {
            isDragging = false;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"DraggableCloud [{name}]: " +
                $"Interactable = {value}"
            );
        }
    }

    // =========================================================
    // Begin Drag
    // =========================================================

    private void TryBeginDragging(
        Vector2 screenPosition)
    {
        if (arCamera == null)
        {
            FindCamera();
        }

        if (arCamera == null)
        {
            Debug.LogError(
                $"DraggableCloud [{name}]: " +
                "No camera found."
            );

            return;
        }

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        /*
         * 使用 RaycastAll。
         *
         * 合并场景后可能新增：
         * - Background Collider
         * - Plane Collider
         * - ImageTarget Collider
         * - Room Collider
         *
         * 原来的 Physics.Raycast 只检测最前面的 Collider，
         * 所以云可能被其他 Collider 挡住。
         */
        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                rayDistance,
                raycastLayers
            );

        if (hits == null ||
            hits.Length == 0)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    $"DraggableCloud [{name}]: " +
                    "Raycast hit nothing."
                );
            }

            return;
        }

        /*
         * 按距离排序。
         */
        System.Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(
                    b.distance
                )
        );

        /*
         * 找射线碰到的第一朵 Cloud。
         *
         * 普通背景 Collider 会被跳过，
         * 所以它们不会再挡住云。
         */
        DraggableCloud nearestCloud =
            null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (showDebugLogs)
            {
                Debug.Log(
                    $"Cloud Raycast Hit: " +
                    $"{hit.collider.name}, " +
                    $"Layer = " +
                    $"{LayerMask.LayerToName(hit.collider.gameObject.layer)}"
                );
            }

            DraggableCloud hitCloud =
                hit.collider
                    .GetComponentInParent
                    <DraggableCloud>();

            if (hitCloud != null)
            {
                nearestCloud =
                    hitCloud;

                break;
            }
        }

        /*
         * 如果最前面的 Cloud 不是当前这一朵，
         * 当前脚本不响应。
         *
         * 防止多朵云重叠时一起被拖动。
         */
        if (nearestCloud != this)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    $"DraggableCloud [{name}]: " +
                    "This cloud was not the selected cloud."
                );
            }

            return;
        }

        // =====================================================
        // 建立拖动平面
        // =====================================================

        Vector3 planeNormal;

        if (dragPlaneReference != null)
        {
            planeNormal =
                dragPlaneReference.up;
        }
        else
        {
            /*
             * 没有设置 DragPlaneReference 时，
             * 使用当前 Prefab 的方向。
             */
            planeNormal =
                transform.up;
        }

        dragPlane =
            new Plane(
                planeNormal,
                transform.position
            );

        if (!dragPlane.Raycast(
                ray,
                out float enter))
        {
            Debug.LogWarning(
                $"DraggableCloud [{name}]: " +
                "Could not intersect drag plane."
            );

            return;
        }

        Vector3 hitPoint =
            ray.GetPoint(enter);

        /*
         * 保存鼠标点击点与云中心的偏移，
         * 防止开始拖动时云突然跳到鼠标位置。
         */
        dragOffset =
            transform.position -
            hitPoint;

        isDragging = true;

        if (showDebugLogs)
        {
            Debug.Log(
                $"DraggableCloud [{name}]: " +
                "Drag STARTED."
            );
        }

        if (flowController != null)
        {
            flowController.OnCloudGrabbed();
        }
    }

    // =========================================================
    // Continue Drag
    // =========================================================

    private void ContinueDragging(
        Vector2 screenPosition)
    {
        if (arCamera == null)
        {
            FindCamera();

            if (arCamera == null)
                return;
        }

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        if (dragPlane.Raycast(
            ray,
            out float enter))
        {
            Vector3 targetPosition =
                ray.GetPoint(enter)
                + dragOffset;

            transform.position =
                targetPosition;
        }
    }

    // =========================================================
    // End Drag
    // =========================================================

    private void EndDragging()
    {
        if (!isDragging)
            return;

        isDragging = false;

        if (showDebugLogs)
        {
            Debug.Log(
                $"DraggableCloud [{name}]: " +
                "Drag ENDED."
            );
        }

        if (flowController != null)
        {
            flowController.OnCloudReleased();
        }
    }

    // =========================================================
    // Find FlowController2
    // =========================================================

    private void FindFlowController()
    {
        if (flowController != null)
            return;

        /*
         * 先找父级。
         */
        flowController =
            GetComponentInParent
            <FlowController2>(true);

        if (flowController != null)
            return;

        /*
         * FlowController2 很可能在：
         *
         * Page11Prefab
         * ├ Managers
         * │  └ FlowController2
         * └ Clouds
         *    └ Cloud01
         *
         * 它和 Cloud 是兄弟分支，
         * GetComponentInParent 找不到。
         *
         * 所以从当前 Prefab Root 下重新搜索。
         */
        Transform root =
            transform.root;

        if (root != null)
        {
            flowController =
                root.GetComponentInChildren
                <FlowController2>(true);
        }

        if (flowController != null)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    $"DraggableCloud [{name}]: " +
                    $"Found FlowController2 = " +
                    $"{flowController.name}"
                );
            }

            return;
        }

        /*
         * 最后的备用方案。
         */
#if UNITY_2023_1_OR_NEWER
        flowController =
            FindFirstObjectByType
            <FlowController2>(
                FindObjectsInactive.Include
            );
#else
        flowController =
            FindObjectOfType
            <FlowController2>(true);
#endif

        if (flowController == null)
        {
            Debug.LogError(
                $"DraggableCloud [{name}]: " +
                "Unable to find FlowController2."
            );
        }
    }

    // =========================================================
    // Find Camera
    // =========================================================

    private void FindCamera()
    {
        if (arCamera != null &&
            arCamera.isActiveAndEnabled)
        {
            return;
        }

        /*
         * 优先使用 MainCamera。
         * Vuforia ARCamera 必须 Tag = MainCamera。
         */
        Camera mainCamera =
            Camera.main;

        if (mainCamera != null &&
            mainCamera.isActiveAndEnabled)
        {
            arCamera =
                mainCamera;

            if (showDebugLogs)
            {
                Debug.Log(
                    $"DraggableCloud [{name}]: " +
                    $"Using Camera.main = " +
                    $"{arCamera.name}"
                );
            }

            return;
        }

        /*
         * Camera.main 找不到时备用搜索。
         */
#if UNITY_2023_1_OR_NEWER
        arCamera =
            FindFirstObjectByType
            <Camera>();
#else
        arCamera =
            FindObjectOfType<Camera>();
#endif

        if (arCamera != null)
        {
            Debug.LogWarning(
                $"DraggableCloud [{name}]: " +
                $"Camera.main was not found. " +
                $"Using fallback camera: " +
                $"{arCamera.name}"
            );
        }
        else
        {
            Debug.LogError(
                $"DraggableCloud [{name}]: " +
                "No Camera exists in the scene."
            );
        }
    }

    // =========================================================
    // Input Detection
    // =========================================================

    private bool PointerDown(
        out Vector2 position)
    {
        position =
            Vector2.zero;

        // =====================================================
        // Android / Touch
        // =====================================================

        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current
                    .primaryTouch;

            if (touch.press
                .wasPressedThisFrame)
            {
                /*
                 * 默认不检查 UI。
                 *
                 * 合并场景后，全屏透明 Image、
                 * Hint Panel、Canvas 等都可能导致
                 * IsPointerOverGameObject() 永远返回 true。
                 */
                if (blockWhenPointerOverUI &&
                    EventSystem.current != null)
                {
                    int fingerId =
                        touch.touchId
                            .ReadValue();

                    if (EventSystem.current
                        .IsPointerOverGameObject(
                            fingerId))
                    {
                        if (showDebugLogs)
                        {
                            Debug.Log(
                                $"DraggableCloud [{name}]: " +
                                "Touch blocked by UI."
                            );
                        }

                        return false;
                    }
                }

                position =
                    touch.position
                        .ReadValue();

                return true;
            }
        }

        // =====================================================
        // PC Mouse
        // =====================================================

        if (Mouse.current != null &&
            Mouse.current
                .leftButton
                .wasPressedThisFrame)
        {
            if (blockWhenPointerOverUI &&
                EventSystem.current != null &&
                EventSystem.current
                    .IsPointerOverGameObject())
            {
                if (showDebugLogs)
                {
                    Debug.Log(
                        $"DraggableCloud [{name}]: " +
                        "Mouse blocked by UI."
                    );
                }

                return false;
            }

            position =
                Mouse.current
                    .position
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

        // Touch
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

        // Mouse
        if (Mouse.current != null &&
            Mouse.current
                .leftButton
                .isPressed)
        {
            position =
                Mouse.current
                    .position
                    .ReadValue();

            return true;
        }

        return false;
    }

    private bool PointerUp()
    {
        // Touch
        if (Touchscreen.current != null &&
            Touchscreen.current
                .primaryTouch
                .press
                .wasReleasedThisFrame)
        {
            return true;
        }

        // Mouse
        if (Mouse.current != null &&
            Mouse.current
                .leftButton
                .wasReleasedThisFrame)
        {
            return true;
        }

        return false;
    }
}