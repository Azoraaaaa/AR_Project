using UnityEngine;

public class TapHandMotion : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Assign the object that the hand should point towards.")]
    [SerializeField] private Transform target;

    [Header("Forward and Backward Motion")]
    [Tooltip("How far the hand moves towards the target.")]
    [SerializeField] private float moveDistance = 0.08f;

    [Tooltip("How fast the hand moves forward and backward.")]
    [SerializeField] private float moveSpeed = 3f;

    private Vector3 startLocalPosition;
    private Transform parentTransform;

    private void Awake()
    {
        parentTransform = transform.parent;
        startLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        startLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        if (target == null || parentTransform == null)
        {
            return;
        }

        Vector3 targetLocalPosition =
            parentTransform.InverseTransformPoint(
                target.position
            );

        Vector3 direction =
            targetLocalPosition - startLocalPosition;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        direction.Normalize();

        float movement =
            (Mathf.Sin(Time.time * moveSpeed) + 1f)
            * 0.5f
            * moveDistance;

        transform.localPosition =
            startLocalPosition +
            direction * movement;
    }
}