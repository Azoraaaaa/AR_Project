using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class DraggableMemoryItem : MonoBehaviour
{
    [Header("道具类型")]
    [SerializeField] private MemoryItemType itemType;

    [Header("拖动设置")]
    [SerializeField] private Camera arCamera;

    [Tooltip("拖入 Prefab 内部的 ContentRoot")]
    [SerializeField] private Transform dragPlaneReference;

    [SerializeField] private float liftHeight = 0.01f;

    [Header("流程控制")]
    [SerializeField] private MemoryItemSequence sequenceManager;

    public MemoryItemType ItemType => itemType;

    private Rigidbody itemRigidbody;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private Plane dragPlane;
    private Vector3 dragOffset;

    private bool isDragging;
    private MemoryItemSlot currentSlot;

    private void Awake()
    {
        itemRigidbody = GetComponent<Rigidbody>();

        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;

        if (arCamera == null)
            arCamera = Camera.main;

        if (arCamera == null)
        {
#if UNITY_2023_1_OR_NEWER
            arCamera = FindFirstObjectByType<Camera>();
#else
            arCamera = FindObjectOfType<Camera>();
#endif
        }

        if (itemRigidbody != null)
        {
            itemRigidbody.useGravity = false;
            itemRigidbody.isKinematic = true;
        }
    }

    private void Update()
    {
        // 种子还没放好、流程还没开始时，道具不能拖
        if (sequenceManager == null ||
            !sequenceManager.SequenceStarted)
        {
            return;
        }

        if (PointerDown(out Vector2 pointerPosition))
        {
            TryStartDragging(pointerPosition);
        }

        if (isDragging &&
            PointerHeld(out pointerPosition))
        {
            DragItem(pointerPosition);
        }

        if (isDragging &&
            PointerUp(out pointerPosition))
        {
            StopDragging();
        }
    }

    private void TryStartDragging(Vector2 screenPosition)
    {
        if (arCamera == null)
            return;

        Ray ray =
            arCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            return;
        }

        bool clickedThisItem =
            hit.transform == transform ||
            hit.transform.IsChildOf(transform);

        if (!clickedThisItem)
            return;

        isDragging = true;

        if (sequenceManager != null)
            sequenceManager.PlayGrabSound();

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

    private void DragItem(Vector2 screenPosition)
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

        if (itemRigidbody != null)
        {
            itemRigidbody.MovePosition(targetPosition);
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
            return;
        }

        // 放错或没放进槽位
        if (sequenceManager != null)
            sequenceManager.PlayGrabSound();

        ReturnToOriginalPosition();
    }

    private void ReturnToOriginalPosition()
    {
        transform.localPosition =
            originalLocalPosition;

        transform.localRotation =
            originalLocalRotation;

        if (itemRigidbody != null)
        {
            itemRigidbody.linearVelocity =
                Vector3.zero;

            itemRigidbody.angularVelocity =
                Vector3.zero;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        MemoryItemSlot slot =
            other.GetComponentInParent<MemoryItemSlot>();

        if (slot != null)
        {
            currentSlot = slot;

            Debug.Log(
                $"{name} entered slot: {slot.name}, " +
                $"AcceptedType: {slot.AcceptedType}"
            );
        }
    }

    private void OnTriggerExit(Collider other)
    {
        MemoryItemSlot slot =
            other.GetComponentInParent<MemoryItemSlot>();

        if (slot != null && slot == currentSlot)
        {
            Debug.Log($"{name} exited slot: {slot.name}");
            currentSlot = null;
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
            position =
                Mouse.current.position.ReadValue();

            return true;
        }

        return false;
    }
}