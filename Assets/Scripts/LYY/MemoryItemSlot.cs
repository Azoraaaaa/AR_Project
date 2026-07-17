using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MemoryItemSlot : MonoBehaviour
{
    [SerializeField] private MemoryItemType acceptedType;

    public MemoryItemType AcceptedType => acceptedType;

    private void Reset()
    {
        Collider slotCollider = GetComponent<Collider>();

        if (slotCollider != null)
            slotCollider.isTrigger = true;
    }
}