using UnityEngine;
using System.Collections;

public class GrowthManager : MonoBehaviour
{
    public GameObject[] buds;

    public Animator[] budAnimators;


    // 最后一个元素是否等待下一阶段
    public bool waitForNextStage = false;


    private int currentIndex = 0;



    void Start()
    {
        for (int i = 0; i < buds.Length; i++)
        {
            buds[i].SetActive(false);
        }

        StartCoroutine(GrowSequence());
    }



    IEnumerator GrowSequence()
    {
        for (int i = 0; i < buds.Length; i++)
        {
            currentIndex = i;


            // 出现当前物体
            buds[i].SetActive(true);


            // 播放动画
            budAnimators[i].SetTrigger("Grow");


            // 等待动画结束
            yield return StartCoroutine(
                WaitForAnimationFinish(budAnimators[i])
            );


            // 如果不是最后一个
            if (i < buds.Length - 1)
            {
                // 下一个出现前隐藏当前
                buds[i].SetActive(false);
            }
            else
            {
                // 最后一个停留
                if (waitForNextStage)
                {
                    Debug.Log("Waiting next stage...");
                    yield break;
                }
            }
        }
    }



    IEnumerator WaitForAnimationFinish(Animator animator)
    {
        yield return null;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);


        while (state.normalizedTime < 1f)
        {
            state = animator.GetCurrentAnimatorStateInfo(0);
            yield return null;
        }


        // 停在最后一帧
        animator.speed = 0;
    }



    // 给LifeCycleManager调用
    public void ContinueToNextStage()
    {
        if (currentIndex >= 0)
        {
            buds[currentIndex].SetActive(false);
        }


        animatorReset();


        StartCoroutine(GrowNextStage());
    }



    void animatorReset()
    {
        for (int i = 0; i < budAnimators.Length; i++)
        {
            budAnimators[i].speed = 1;
        }
    }


    IEnumerator GrowNextStage()
    {
        int next = currentIndex + 1;


        if (next < buds.Length)
        {
            buds[next].SetActive(true);

            budAnimators[next].SetTrigger("Grow");

            yield return StartCoroutine(
                WaitForAnimationFinish(budAnimators[next])
            );
        }
    }
}