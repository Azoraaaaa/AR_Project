using UnityEngine;
using UnityEngine.EventSystems;

public class SeedClickable :
    MonoBehaviour,
    IPointerClickHandler
{
    [SerializeField]
    private SeedButterflyManager seedButterflyManager;

    private bool hasBeenUsed;

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (hasBeenUsed ||
            seedButterflyManager == null)
        {
            return;
        }

        bool accepted =
            seedButterflyManager.TrySelectSeed();

        if (accepted)
        {
            hasBeenUsed = true;
        }
    }
}