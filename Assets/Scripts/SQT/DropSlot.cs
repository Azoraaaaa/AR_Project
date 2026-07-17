using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropSlot : MonoBehaviour, IDropHandler
{
    [Header("Slot Settings")]
    public int slotID;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;

    private Image slotImage;
    private LifeCycleManager manager;

    void Awake()
    {
        slotImage = GetComponent<Image>();

        manager = FindObjectOfType<LifeCycleManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        DragItem item = eventData.pointerDrag.GetComponent<DragItem>();

        if (item == null)
            return;

        // ¢Ù ¼ì²éË³Ðò
        if (!manager.CanPlace(item.cardID))
        {
            Debug.Log("Wrong order!");

            if (audioSource != null && wrongSound != null)
            {
                audioSource.PlayOneShot(wrongSound);
            }

            return;
        }

        // ¢Ú ¼ì²éÎ»ÖÃ
        if (item.cardID == slotID)
        {
            Image cardImage = item.GetComponent<Image>();

            slotImage.sprite = cardImage.sprite;
            slotImage.color = Color.white;

            item.gameObject.SetActive(false);

            Debug.Log("Correct!");

            if (audioSource != null && correctSound != null)
            {
                audioSource.PlayOneShot(correctSound);
            }

            manager.CardPlaced();
        }
        else
        {
            Debug.Log("Wrong slot!");

            if (audioSource != null && wrongSound != null)
            {
                audioSource.PlayOneShot(wrongSound);
            }
        }
    }
}