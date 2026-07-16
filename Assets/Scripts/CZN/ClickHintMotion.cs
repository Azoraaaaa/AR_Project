using UnityEngine;

public class ClickHintMotion : MonoBehaviour
{
    [Header("Floating Motion")]
    [Tooltip("The maximum vertical distance of the floating movement.")]
    [SerializeField] private float floatingHeight = 0.08f;

    [Tooltip("The speed of the floating movement.")]
    [SerializeField] private float floatingSpeed = 2.5f;

    [Header("Camera Facing")]
    [Tooltip("Keep the hint facing the AR camera.")]
    [SerializeField] private bool faceCamera = true;

    [Tooltip("Assign the AR Camera. The script will use Camera.main if left empty.")]
    [SerializeField] private Camera targetCamera;

    private Vector3 originalLocalPosition;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        originalLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        float verticalOffset =
            Mathf.Sin(Time.time * floatingSpeed) * floatingHeight;

        transform.localPosition =
            originalLocalPosition +
            Vector3.up * verticalOffset;

        if (faceCamera && targetCamera != null)
        {
            Vector3 direction =
                transform.position -
                targetCamera.transform.position;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(direction);
            }
        }
    }
}