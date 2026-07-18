using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;


[RequireComponent(typeof(Collider))]
public class DraggableCloud : MonoBehaviour
{
    [Header("Page Flow")]
    [SerializeField] private FlowController2 flowController;


    [Header("Camera")]
    [SerializeField] private Camera arCamera;


    [Header("Drag Plane")]
    [Tooltip("通常拖入 Page11Prefab 或 ContentRoot")]
    [SerializeField] private Transform dragPlaneReference;


    [Header("Raycast")]
    [SerializeField] private LayerMask raycastLayers = ~0;
    [SerializeField] private float rayDistance = 100f;


    private bool interactable;
    private bool isDragging;


    private Plane dragPlane;
    private Vector3 dragOffset;



    private void Awake()
    {
        if (flowController == null)
        {
            flowController =
                GetComponentInParent<FlowController2>();
        }

        FindCamera();
    }



    private void Update()
    {
        if (!interactable ||
            flowController == null ||
            !flowController.CanDragCloud)
        {
            return;
        }



        if (!isDragging)
        {
            if (PointerDown(
                out Vector2 pointerPosition))
            {
                TryBeginDragging(pointerPosition);
            }

            return;
        }



        if (PointerHeld(
            out Vector2 heldPosition))
        {
            ContinueDragging(heldPosition);
        }



        if (PointerUp())
        {
            EndDragging();
        }
    }




    public void SetInteractable(bool value)
    {
        interactable = value;

        if (!value)
            isDragging = false;
    }





    private void TryBeginDragging(
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



        DraggableCloud hitCloud =
            hit.collider.GetComponentInParent
            <DraggableCloud>();


        if (hitCloud != this)
            return;




        Vector3 planeNormal =
            dragPlaneReference != null
            ? dragPlaneReference.up
            : transform.up;



        dragPlane =
            new Plane(
                planeNormal,
                transform.position
            );



        if (!dragPlane.Raycast(
            ray,
            out float enter))
        {
            return;
        }



        Vector3 hitPoint =
            ray.GetPoint(enter);



        dragOffset =
            transform.position - hitPoint;



        isDragging = true;


        flowController.OnCloudGrabbed();
    }





    private void ContinueDragging(
        Vector2 screenPosition)
    {
        if (arCamera == null)
            return;



        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );



        if (dragPlane.Raycast(
            ray,
            out float enter))
        {
            transform.position =
                ray.GetPoint(enter)
                + dragOffset;
        }
    }






    private void EndDragging()
    {
        if (!isDragging)
            return;



        isDragging = false;



        if (flowController != null)
            flowController.OnCloudReleased();
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






    // ================================
    // Input Detection
    // ================================



    private bool PointerDown(
        out Vector2 position)
    {
        position = Vector2.zero;



        // Android Touch
        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current.primaryTouch;



            if (touch.press.wasPressedThisFrame)
            {

                int fingerId =
                    touch.touchId.ReadValue();



                // 如果点击的是UI，不执行云朵互动
                if (EventSystem.current != null &&
                    EventSystem.current
                    .IsPointerOverGameObject(fingerId))
                {
                    return false;
                }



                position =
                    touch.position.ReadValue();



                return true;
            }
        }






        // PC Mouse
        if (Mouse.current != null &&
            Mouse.current.leftButton
            .wasPressedThisFrame)
        {


            // 如果点击的是UI，不执行云朵互动
            if (EventSystem.current != null &&
                EventSystem.current
                .IsPointerOverGameObject())
            {
                return false;
            }



            position =
                Mouse.current.position.ReadValue();



            return true;
        }



        return false;
    }







    private bool PointerHeld(
        out Vector2 position)
    {
        position = Vector2.zero;



        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current.primaryTouch;



            if (touch.press.isPressed)
            {
                position =
                    touch.position.ReadValue();


                return true;
            }
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







    private bool PointerUp()
    {

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch
            .press.wasReleasedThisFrame)
        {
            return true;
        }



        if (Mouse.current != null &&
            Mouse.current.leftButton
            .wasReleasedThisFrame)
        {
            return true;
        }



        return false;
    }
}