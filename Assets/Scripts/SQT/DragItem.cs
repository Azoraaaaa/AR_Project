using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragItem : MonoBehaviour,
IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int cardID;

    private Vector2 startPos;

    void Start()
    {
        startPos = GetComponent<RectTransform>().anchoredPosition;
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        // 拖动时允许穿透检测
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }


    public void OnDrag(PointerEventData eventData)
    {
        transform.position += (Vector3)eventData.delta;
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        // 如果没有成功放置，回原位置
        GetComponent<RectTransform>().anchoredPosition = startPos;
    }
}