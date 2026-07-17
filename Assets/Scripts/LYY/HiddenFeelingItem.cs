using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class HiddenFeelingItem : MonoBehaviour
{
    [Header("Feeling")]
    [SerializeField] private FeelingType feelingType;

    [TextArea(2, 4)]
    [SerializeField] private string displayText;

    [Header("Page Flow")]
    [SerializeField] private FlowController2 flowController;

    [Header("Camera")]
    [SerializeField] private Camera arCamera;

    [Header("Click Detection")]
    [SerializeField] private LayerMask raycastLayers = ~0;
    [SerializeField] private float rayDistance = 100f;

    private bool interactable;
    private bool selected;

    public FeelingType FeelingType =>
        feelingType;

    public string DisplayText =>
        displayText;

    private void Awake()
    {
        if (flowController == null)
        {
            flowController =
                GetComponentInParent
                <FlowController2>();
        }

        FindCamera();
    }

    private void Update()
    {
        if (!interactable ||
            selected ||
            flowController == null ||
            !flowController.CanSelectFeelingItem)
        {
            return;
        }

        if (PointerDown(
                out Vector2 screenPosition))
        {
            TrySelect(screenPosition);
        }
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
    }

    private void TrySelect(
        Vector2 screenPosition)
    {
        if (arCamera == null)
            FindCamera();

        if (arCamera == null)
            return;

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance,
                raycastLayers))
        {
            return;
        }

        HiddenFeelingItem hitItem =
            hit.collider.GetComponentInParent
            <HiddenFeelingItem>();

        if (hitItem != this)
            return;

        bool accepted =
            flowController
                .TrySelectFeelingItem(this);

        if (accepted)
            selected = true;
    }

    private void FindCamera()
    {
        if (arCamera == null)
            arCamera = Camera.main;

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
    }

    private bool PointerDown(
        out Vector2 position)
    {
        position = Vector2.zero;

        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                position =
                    touch.position.ReadValue();

                return true;
            }
        }

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
}