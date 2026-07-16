using UnityEngine;
using System.Collections;

public class LifeCycleManager : MonoBehaviour
{
    public int currentStep = 0;

    public GameObject seedObject;

    public float seedAppearDelay = 0.5f;


    public void CardPlaced()
    {
        currentStep++;

        Debug.Log("Current Step: " + currentStep);


        // 第一张卡片完成
        if (currentStep == 1)
        {
            StartCoroutine(ShowSeedDelay());
        }


        if (currentStep == 4)
        {
            Debug.Log("全部完成！");
        }
    }



    public bool CanPlace(int cardID)
    {
        return cardID == currentStep;
    }



    IEnumerator ShowSeedDelay()
    {
        yield return new WaitForSeconds(seedAppearDelay);


        if (seedObject != null)
        {
            seedObject.SetActive(true);
        }
    }
}