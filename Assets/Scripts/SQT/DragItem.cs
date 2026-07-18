using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragItem : MonoBehaviour,
IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Card ID")]
    public int cardID;


    [Header("Drag Sound")]
    public AudioSource audioSource;
    public AudioClip dragSound;



    private Vector2 startPos;



    void Start()
    {
        startPos =
            GetComponent<RectTransform>()
            .anchoredPosition;
    }





    public void OnBeginDrag(PointerEventData eventData)
    {
        // 播放拖动音效
        if (audioSource != null &&
           dragSound != null)
        {
            audioSource.PlayOneShot(dragSound);
        }



        // 拖动时允许穿透检测
        CanvasGroup canvasGroup =
            GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }
    }






    public void OnDrag(PointerEventData eventData)
    {
        transform.position +=
            (Vector3)eventData.delta;
    }






    public void OnEndDrag(PointerEventData eventData)
    {
        CanvasGroup canvasGroup =
            GetComponent<CanvasGroup>();


        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }



        // 如果没有成功放置，回原位置
        GetComponent<RectTransform>()
            .anchoredPosition = startPos;
    }
}