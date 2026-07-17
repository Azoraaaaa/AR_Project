using UnityEngine;
using UnityEngine.EventSystems;

public class GardenKeyClickable :
    MonoBehaviour,
    IPointerClickHandler
{
    [SerializeField]
    private MemoryGardenGateManager gateManager;

    private bool hasBeenUsed;
    private Collider keyCollider;

    private void Awake()
    {
        keyCollider =
            GetComponent<Collider>();
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (hasBeenUsed ||
            gateManager == null)
        {
            return;
        }

        bool accepted =
            gateManager.TrySelectKey();

        if (!accepted)
        {
            return;
        }

        hasBeenUsed = true;

        if (keyCollider != null)
        {
            keyCollider.enabled = false;
        }
    }
}