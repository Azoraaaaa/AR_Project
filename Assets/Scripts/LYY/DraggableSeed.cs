using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(Collider))]
public class DraggableSeed : MonoBehaviour
{
    [Header("拖动设置")]
    [SerializeField] private Camera arCamera;

    [Tooltip("建议拖入 Prefab 内的 ContentRoot")]
    [SerializeField] private Transform dragPlaneReference;

    [SerializeField] private float liftHeight = 0.01f;

    [Tooltip("放错位置后是否返回原位")]
    [SerializeField] private bool returnToStartPosition = true;

    [Header("目标花盆")]
    [Tooltip("拖入同一 Prefab 内的 PlantingTrigger")]
    [SerializeField] private PlantingZone targetPlantingZone;

    private Rigidbody seedRigidbody;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private Plane dragPlane;
    private Vector3 dragOffset;

    private bool isDragging;
    private PlantingZone currentPlantingZone;

    private void Awake()
    {
        seedRigidbody = GetComponent<Rigidbody>();

        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;

        // Prefab 不直接保存 Scene AR Camera 引用
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (arCamera == null)
        {
#if UNITY_2023_1_OR_NEWER
            arCamera = FindFirstObjectByType<Camera>();
#else
            arCamera = FindObjectOfType<Camera>();
#endif
        }

        if (seedRigidbody != null)
        {
            seedRigidbody.useGravity = false;
            seedRigidbody.isKinematic = true;
        }
    }

    private void Update()
    {
        if (targetPlantingZone != null &&
            targetPlantingZone.HasPlanted)
        {
            return;
        }

        if (PointerDown(out Vector2 pointerPosition))
        {
            TryStartDragging(pointerPosition);
        }

        if (isDragging && PointerHeld(out pointerPosition))
        {
            DragSeed(pointerPosition);
        }

        if (isDragging && PointerUp(out pointerPosition))
        {
            StopDragging();
        }
    }

    private void TryStartDragging(Vector2 screenPosition)
    {
        if (arCamera == null)
        {
            Debug.LogError(
                "DraggableSeed: Cannot find AR Camera."
            );
            return;
        }

        if (EventSystem.current != null &&
            Mouse.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Ray ray =
            arCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        bool clickedThisSeed =
            hit.transform == transform ||
            hit.transform.IsChildOf(transform);

        if (!clickedThisSeed)
            return;

        isDragging = true;

        // 拿起音效由不会消失的 PlantingZone 播放
        if (targetPlantingZone != null)
        {
            targetPlantingZone.OnSeedPickedUp();
        }

        Vector3 planeNormal =
            dragPlaneReference != null
                ? dragPlaneReference.up
                : arCamera.transform.forward;

        dragPlane = new Plane(
            planeNormal,
            transform.position
        );

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            dragOffset = transform.position - hitPoint;
        }
    }

    private void DragSeed(Vector2 screenPosition)
    {
        if (arCamera == null)
            return;

        Ray ray =
            arCamera.ScreenPointToRay(screenPosition);

        if (!dragPlane.Raycast(ray, out float enter))
            return;

        Vector3 hitPoint = ray.GetPoint(enter);

        Vector3 planeNormal =
            dragPlaneReference != null
                ? dragPlaneReference.up
                : arCamera.transform.forward;

        Vector3 targetPosition =
            hitPoint +
            dragOffset +
            planeNormal * liftHeight;

        if (seedRigidbody != null)
        {
            seedRigidbody.MovePosition(targetPosition);
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    private void StopDragging()
    {
        isDragging = false;

        Physics.SyncTransforms();

        bool isInsideCorrectZone =
            currentPlantingZone != null &&
            currentPlantingZone == targetPlantingZone;

        if (isInsideCorrectZone)
        {
            /*
             * PlantSeed 内会播放：
             * 1. Grab 音效
             * 2. Success 音效
             */
            bool planted =
                currentPlantingZone.PlantSeed(gameObject);

            if (planted)
                return;
        }

        // 没有放到正确位置，返回原位
        if (returnToStartPosition)
        {
            transform.localPosition =
                originalLocalPosition;

            transform.localRotation =
                originalLocalRotation;
        }

        // 返回原位，同时播放放下的 Grab 音效
        if (targetPlantingZone != null)
        {
            targetPlantingZone.OnSeedReturned();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlantingZone zone =
            other.GetComponent<PlantingZone>();

        if (zone != null)
        {
            currentPlantingZone = zone;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlantingZone zone =
            other.GetComponent<PlantingZone>();

        if (zone != null &&
            zone == currentPlantingZone)
        {
            currentPlantingZone = null;
        }
    }

    private bool PointerDown(out Vector2 position)
    {
        position = Vector2.zero;

        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                position = touch.position.ReadValue();
                return true;
            }
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            position =
                Mouse.current.position.ReadValue();

            return true;
        }

        return false;
    }

    private bool PointerHeld(out Vector2 position)
    {
        position = Vector2.zero;

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            position =
                Touchscreen.current.primaryTouch
                    .position.ReadValue();

            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.isPressed)
        {
            position =
                Mouse.current.position.ReadValue();

            return true;
        }

        return false;
    }

    private bool PointerUp(out Vector2 position)
    {
        position = Vector2.zero;

        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current.primaryTouch;

            if (touch.press.wasReleasedThisFrame)
            {
                position = touch.position.ReadValue();
                return true;
            }
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }
}