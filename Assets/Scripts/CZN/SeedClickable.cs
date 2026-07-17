using UnityEngine;
using UnityEngine.EventSystems;

public class SeedClickable :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("Manager")]
    [SerializeField]
    private SeedButterflyManager seedButterflyManager;

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (seedButterflyManager == null)
        {
            Debug.LogError(
                $"{gameObject.name}: " +
                "SeedButterflyManager has not been assigned."
            );

            return;
        }

        /*
         * Do not use hasBeenUsed here.
         *
         * The seed needs two valid taps:
         * 1. Begin the butterfly sequence.
         * 2. Collect the seed.
         *
         * SeedButterflyManager decides whether
         * the current tap is valid.
         */
        bool accepted =
            seedButterflyManager.TrySelectSeed();

        if (accepted)
        {
            Debug.Log(
                $"{gameObject.name}: Seed tap accepted."
            );
        }
        else
        {
            Debug.Log(
                $"{gameObject.name}: Seed tap ignored " +
                "because the sequence is not ready."
            );
        }
    }
}