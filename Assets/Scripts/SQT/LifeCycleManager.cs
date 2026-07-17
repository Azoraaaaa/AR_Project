using UnityEngine;
using System.Collections;

public class LifeCycleManager : MonoBehaviour
{
    public int currentStep = 0;


    [Header("Growth Objects")]
    public GameObject seedObject;
    public GameObject sproutObject;
    public GameObject thirdObject;
    public GameObject fourthObject;
    public GameObject finalObject;


    [Header("Appear Delay")]
    public float seedAppearDelay = 0.5f;
    public float sproutAppearDelay = 0.5f;
    public float thirdAppearDelay = 0.5f;
    public float fourthAppearDelay = 0.5f;
    public float finalAppearDelay = 0.5f;



    [Header("Canvas")]
    public GameObject gameCanvas;
    public GameObject storyCanvas;



    // 当前显示的物体
    private GameObject currentObject;



    public void CardPlaced()
    {
        currentStep++;

        Debug.Log("Current Step: " + currentStep);



        // 第一张卡片完成
        if (currentStep == 1)
        {
            StartCoroutine(ShowObject(seedObject, seedAppearDelay));
        }



        // 第二张卡片完成
        if (currentStep == 2)
        {
            StartCoroutine(ShowObject(sproutObject, sproutAppearDelay));
        }



        // 第三张卡片完成
        if (currentStep == 3)
        {
            StartCoroutine(ShowObject(thirdObject, thirdAppearDelay));
        }



        // 第四张卡片完成
        if (currentStep == 4)
        {
            StartCoroutine(ShowObject(fourthObject, fourthAppearDelay));
        }



        // 第五张卡片完成
        if (currentStep == 5)
        {
            StartCoroutine(ShowObject(finalObject, finalAppearDelay));

            Debug.Log("全部完成！");
        }
    }



    public bool CanPlace(int cardID)
    {
        return cardID == currentStep;
    }





    IEnumerator ShowObject(GameObject newObject, float delay)
    {
        yield return new WaitForSeconds(delay);



        // 隐藏之前的生命阶段物体
        if (currentObject != null)
        {
            currentObject.SetActive(false);
        }



        if (newObject != null)
        {
            // 显示当前物体
            newObject.SetActive(true);



            // 如果是最终阶段 Falling
            // 两个子物体同时显示
            if (newObject == finalObject)
            {
                for (int i = 0; i < newObject.transform.childCount; i++)
                {
                    newObject.transform.GetChild(i).gameObject.SetActive(true);
                }


                // Falling出现后等待2秒切换Canvas
                StartCoroutine(ShowStoryCanvasDelay());
            }



            currentObject = newObject;
        }
    }





    IEnumerator ShowStoryCanvasDelay()
    {
        // 等待玩家看到最终效果
        yield return new WaitForSeconds(2f);



        // 关闭游戏UI
        if (gameCanvas != null)
        {
            gameCanvas.SetActive(false);
        }



        // 打开故事UI
        if (storyCanvas != null)
        {
            storyCanvas.SetActive(true);
        }
    }
}