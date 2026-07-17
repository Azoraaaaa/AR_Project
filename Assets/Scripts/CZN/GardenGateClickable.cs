using UnityEngine;
using UnityEngine.EventSystems;

public class GardenGateClickable :
    MonoBehaviour,
    IPointerClickHandler
{
    [SerializeField]
    private MemoryGardenGateManager gateManager;

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (gateManager == null)
        {
            Debug.LogError(
                $"{gameObject.name}: " +
                "MemoryGardenGateManager has not been assigned."
            );

            return;
        }

        gateManager.TryCheckGate();
    }
}