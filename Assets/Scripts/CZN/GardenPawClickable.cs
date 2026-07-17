using UnityEngine;
using UnityEngine.EventSystems;

public class GardenPawClickable :
    MonoBehaviour,
    IPointerClickHandler
{
    [SerializeField]
    private MemoryGardenGateManager gateManager;

    [SerializeField]
    private int pawIndex;

    private bool hasBeenUsed;
    private Collider pawCollider;

    private void Awake()
    {
        pawCollider =
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
            gateManager.TrySelectPaw(
                pawIndex
            );

        if (!accepted)
        {
            return;
        }

        hasBeenUsed = true;

        if (pawCollider != null)
        {
            pawCollider.enabled = false;
        }
    }
}