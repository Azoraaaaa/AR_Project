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
    // 这里对应 Dialogue Manager 里的 dialoguePanel (StoryBG)
    public GameObject storyCanvas;


    [Header("Dialogue")]
    public DialogueManager dialogueManager;


    private GameObject currentObject;


    public void CardPlaced()
    {
        currentStep++;
        Debug.Log("Current Step: " + currentStep);

        if (currentStep == 1)
        {
            StartCoroutine(ShowObject(seedObject, seedAppearDelay));
        }

        if (currentStep == 2)
        {
            StartCoroutine(ShowObject(sproutObject, sproutAppearDelay));
        }

        if (currentStep == 3)
        {
            StartCoroutine(ShowObject(thirdObject, thirdAppearDelay));
        }

        if (currentStep == 4)
        {
            StartCoroutine(ShowObject(fourthObject, fourthAppearDelay));
        }

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

        if (currentObject != null)
        {
            currentObject.SetActive(false);
        }

        if (newObject != null)
        {
            newObject.SetActive(true);

            // Falling阶段
            if (newObject == finalObject)
            {
                for (int i = 0; i < newObject.transform.childCount; i++)
                {
                    newObject.transform.GetChild(i).gameObject.SetActive(true);
                }

                // 激活第二阶段对话流程
                StartCoroutine(ShowStoryCanvasDelay());
            }

            currentObject = newObject;
        }
    }


    IEnumerator ShowStoryCanvasDelay()
    {
        yield return new WaitForSeconds(2f);

        // 关闭操作界面的 Canvas
        if (gameCanvas != null)
        {
            gameCanvas.SetActive(false);
        }

        // 重新开启对话界面的 Canvas
        if (storyCanvas != null)
        {
            storyCanvas.SetActive(true);
        }

        yield return null;

        // 让对话管理器从 Element 5 继续按顺序往后播放
        if (dialogueManager != null)
        {
            dialogueManager.ResumeDialogue();
        }
    }
}