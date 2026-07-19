using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class DraggableSeed : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private Camera arCamera;

    [Tooltip("建议拖入 Prefab 内的 ContentRoot")]
    [SerializeField] private Transform dragPlaneReference;

    [Tooltip("拖动时稍微抬高，避免与桌面重叠")]
    [SerializeField] private float liftHeight = 0.01f;

    [Tooltip("0 = 完全跟手；20~30 = 稍微平滑")]
    [SerializeField] private float dragSmoothSpeed = 0f;

    [Tooltip("放错位置后是否返回原位")]
    [SerializeField] private bool returnToStartPosition = true;

    [Header("Raycast")]
    [Tooltip("测试阶段建议 Everything")]
    [SerializeField] private LayerMask raycastLayers = ~0;

    [SerializeField] private float rayDistance = 100f;

    [Header("Target Planting Zone")]
    [Tooltip("拖入同一 Prefab 内的 PlantingTrigger")]
    [SerializeField] private PlantingZone targetPlantingZone;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Rigidbody seedRigidbody;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private Plane dragPlane;
    private Vector3 dragOffset;

    private Vector3 targetDragPosition;

    private bool isDragging;

    private PlantingZone currentPlantingZone;

    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        seedRigidbody =
            GetComponent<Rigidbody>();

        originalLocalPosition =
            transform.localPosition;

        originalLocalRotation =
            transform.localRotation;

        FindCamera();

        if (seedRigidbody != null)
        {
            seedRigidbody.useGravity = false;
            seedRigidbody.isKinematic = true;

            /*
             * 拖动由 Transform 控制，
             * 不需要 Rigidbody 插值。
             */
            seedRigidbody.interpolation =
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
         * 如果已经种植完成，
         * 不再允许拖动。
         */
        if (targetPlantingZone != null &&
            targetPlantingZone.HasPlanted)
        {
            return;
        }

        /*
         * 尚未开始拖动。
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
                "DraggableSeed: Cannot find AR Camera."
            );

            return;
        }

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        /*
         * 使用 RaycastAll，
         * 防止桌子、花盆或其他 Collider
         * 挡在种子前面导致点不到。
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
            if (showDebugLogs)
            {
                Debug.Log(
                    $"{name}: Raycast hit nothing."
                );
            }

            return;
        }

        Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(
                    b.distance
                )
        );

        bool clickedThisSeed = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            DraggableSeed hitSeed =
                hit.collider
                    .GetComponentInParent
                    <DraggableSeed>();

            if (showDebugLogs)
            {
                Debug.Log(
                    $"{name}: Ray hit " +
                    $"{hit.collider.name}"
                );
            }

            if (hitSeed == this)
            {
                clickedThisSeed = true;
                break;
            }
        }

        if (!clickedThisSeed)
        {
            return;
        }

        isDragging = true;

        /*
         * 清除可能残留的 Trigger 状态。
         */
        currentPlantingZone = null;

        /*
         * 拿起音效由 PlantingZone 播放。
         */
        if (targetPlantingZone != null)
        {
            targetPlantingZone.OnSeedPickedUp();
        }

        /*
         * 保留你原来的拖动平面逻辑。
         *
         * 有 DragPlaneReference：
         * 使用 dragPlaneReference.up
         *
         * 没有：
         * 使用 Camera.forward
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
         * 保存点击点和种子中心的偏移，
         * 防止按下时种子突然跳到手指中心。
         */
        dragOffset =
            transform.position -
            hitPoint;

        targetDragPosition =
            transform.position;

        if (showDebugLogs)
        {
            Debug.Log(
                $"{name}: Seed drag started."
            );
        }
    }

    // =========================================================
    // Update Drag Target
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
         * 继续保持你原来的 planeNormal。
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
            planeNormal *
            liftHeight;
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
         * 不再使用：
         * seedRigidbody.MovePosition(...)
         *
         * 直接使用 Transform，
         * 和 Update 渲染帧同步，
         * 拖动会明显更顺滑。
         */
        if (dragSmoothSpeed <= 0f)
        {
            transform.position =
                targetDragPosition;
        }
        else
        {
            /*
             * 可选平滑模式。
             *
             * 建议 20~30。
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

        /*
         * 这里不只依赖 TriggerEnter，
         * 最后再检查一次当前是否与目标区域重叠。
         */
        bool isInsideCorrectZone =
            currentPlantingZone != null &&
            currentPlantingZone ==
            targetPlantingZone;

        if (isInsideCorrectZone)
        {
            /*
             * PlantSeed 内负责：
             * Grab / Success 等对应音效和流程。
             */
            bool planted =
                currentPlantingZone
                    .PlantSeed(
                        gameObject
                    );

            if (planted)
            {
                if (showDebugLogs)
                {
                    Debug.Log(
                        $"{name}: Seed planted successfully."
                    );
                }

                return;
            }
        }

        /*
         * 没放到正确花盆，
         * 返回原位。
         */
        if (returnToStartPosition)
        {
            ReturnToOriginalPosition();
        }

        /*
         * 返回时播放放下 / Grab 音效。
         */
        if (targetPlantingZone != null)
        {
            targetPlantingZone.OnSeedReturned();
        }
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

        currentPlantingZone = null;

        if (seedRigidbody != null)
        {
            seedRigidbody.linearVelocity =
                Vector3.zero;

            seedRigidbody.angularVelocity =
                Vector3.zero;
        }

        Physics.SyncTransforms();

        if (showDebugLogs)
        {
            Debug.Log(
                $"{name}: Seed returned to start."
            );
        }
    }

    // =========================================================
    // Planting Zone Detection
    // =========================================================

    private void OnTriggerEnter(
        Collider other)
    {
        PlantingZone zone =
            other.GetComponentInParent
            <PlantingZone>();

        if (zone == null)
            return;

        currentPlantingZone =
            zone;

        if (showDebugLogs)
        {
            Debug.Log(
                $"{name}: Entered planting zone " +
                $"{zone.name}"
            );
        }
    }

    private void OnTriggerStay(
        Collider other)
    {
        /*
         * 加 OnTriggerStay：
         *
         * Transform 快速拖动时，
         * TriggerEnter 有可能因为 Physics 更新频率
         * 没及时记录。
         */
        PlantingZone zone =
            other.GetComponentInParent
            <PlantingZone>();

        if (zone != null)
        {
            currentPlantingZone =
                zone;
        }
    }

    private void OnTriggerExit(
        Collider other)
    {
        PlantingZone zone =
            other.GetComponentInParent
            <PlantingZone>();

        if (zone != null &&
            zone ==
            currentPlantingZone)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    $"{name}: Exited planting zone " +
                    $"{zone.name}"
                );
            }

            currentPlantingZone =
                null;
        }
    }

    // =========================================================
    // Camera
    // =========================================================

    private void FindCamera()
    {
        /*
         * Prefab 不保存 Scene ARCamera 引用，
         * 运行时优先找 MainCamera。
         */
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
                $"{name}: AR Camera not found. " +
                "Make sure Vuforia ARCamera " +
                "uses the MainCamera tag."
            );
        }
        else if (showDebugLogs)
        {
            Debug.Log(
                $"{name}: Using Camera " +
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