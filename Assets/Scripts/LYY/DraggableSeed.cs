using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class DraggableSeed : MonoBehaviour
{
    [Header("拖动设置")]
    [SerializeField] private Camera arCamera;

    [Tooltip("拖动平面的参考物。建议拖入 Prefab 内的 ContentRoot")]
    [SerializeField] private Transform dragPlaneReference;

    [SerializeField] private float liftHeight = 0.01f;

    [Tooltip("放置失败后是否回到原位")]
    [SerializeField] private bool returnToStartPosition = true;

    [Header("目标花盆")]
    [Tooltip("拖入同一 Prefab 内的 PlantingTrigger")]
    [SerializeField] private PlantingZone targetPlantingZone;

    [Header("拿起种子音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickUpSeedClip;

    [Range(0f, 1f)]
    [SerializeField] private float pickUpVolume = 1f;

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

        // Prefab 中不要写死 Scene 的 AR Camera
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
            Debug.LogError("DraggableSeed: Cannot find AR Camera.");
            return;
        }

        // 鼠标点在普通 Screen Space UI 上时，不拿起种子
        if (EventSystem.current != null &&
            Mouse.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        bool clickedThisSeed =
            hit.transform == transform ||
            hit.transform.IsChildOf(transform);

        if (!clickedThisSeed)
            return;

        isDragging = true;

        // 播放拿起种子的音效
        if (audioSource != null && pickUpSeedClip != null)
        {
            audioSource.PlayOneShot(
                pickUpSeedClip,
                pickUpVolume
            );
        }

        // 通知 UI：种子已经拿起来
        if (targetPlantingZone != null)
        {
            targetPlantingZone.OnSeedPickedUp();
        }

        Vector3 planeNormal;

        if (dragPlaneReference != null)
        {
            planeNormal = dragPlaneReference.up;
        }
        else
        {
            planeNormal = arCamera.transform.forward;
        }

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

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

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

        // 确保 Transform 与物理系统同步
        Physics.SyncTransforms();

        bool isInsideCorrectZone =
            currentPlantingZone != null &&
            currentPlantingZone == targetPlantingZone;

        if (isInsideCorrectZone)
        {
            bool planted =
                currentPlantingZone.PlantSeed(gameObject);

            if (planted)
                return;
        }

        // 没放进花盆，返回原位
        if (returnToStartPosition)
        {
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }

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

        // Android / 手机触屏
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

        // Unity Editor 鼠标
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
            position =
                Mouse.current.position.ReadValue();

            return true;
        }

        return false;
    }
}