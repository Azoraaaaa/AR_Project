using UnityEngine;
using UnityEngine.EventSystems;

public enum MemoryType
{
    Ball,
    Bed,
    Collar,
    Food
}

public class MemoryClickable :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("Memory Settings")]
    [SerializeField]
    private Page1MemoryManager memoryManager;

    [SerializeField]
    private MemoryType memoryType;

    private bool hasBeenUsed;
    private Collider clickCollider;

    private void Awake()
    {
        clickCollider =
            GetComponent<Collider>();

        if (clickCollider == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: No Collider was found. " +
                "This ClickArea may not receive clicks."
            );
        }
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (hasBeenUsed)
        {
            return;
        }

        if (memoryManager == null)
        {
            Debug.LogError(
                $"{gameObject.name}: " +
                "Page1MemoryManager has not been assigned."
            );

            return;
        }

        bool accepted =
            memoryManager.TryPlayMemory(
                memoryType
            );

        if (!accepted)
        {
            return;
        }

        hasBeenUsed = true;

        if (clickCollider != null)
        {
            clickCollider.enabled = false;
        }

        Debug.Log(
            $"{gameObject.name} activated " +
            $"{memoryType} memory."
        );
    }
}