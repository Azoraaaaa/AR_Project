using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropSlot : MonoBehaviour, IDropHandler
{
    public int slotID;

    private Image slotImage;


    void Awake()
    {
        slotImage = GetComponent<Image>();
    }


    public void OnDrop(PointerEventData eventData)
    {
        DragItem item = eventData.pointerDrag.GetComponent<DragItem>();

        if (item == null)
            return;


        // 顺序正确
        if (item.cardID == slotID)
        {

            Image cardImage = item.GetComponent<Image>();

            // 空位换成卡片图片
            slotImage.sprite = cardImage.sprite;


            // 显示
            slotImage.color = Color.white;


            // 原卡片消失
            item.gameObject.SetActive(false);


            Debug.Log("Correct!");
        }
    }
}