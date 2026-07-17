using UnityEngine;
using System.Collections;

public class SequentialChildrenAnimation : MonoBehaviour
{
    [Header("Animator Trigger Name")]
    public string triggerName = "Grow";


    void OnEnable()
    {
        StartCoroutine(PlaySequence());
    }


    IEnumerator PlaySequence()
    {
        // 先隐藏所有子物体
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }


        // 等一帧，确保Parent已经激活
        yield return null;


        // 依次播放
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;


            // 当前物体出现
            child.SetActive(true);


            Animator animator = child.GetComponent<Animator>();

            if (animator != null)
            {
                animator.SetTrigger(triggerName);

                yield return StartCoroutine(
                    WaitAnimation(animator)
                );
            }



            // 如果不是最后一个物体，则隐藏
            // 最后一个物体保持显示
            if (i < transform.childCount - 1)
            {
                child.SetActive(false);
            }
        }
    }



    IEnumerator WaitAnimation(Animator animator)
    {
        // 等Animator进入动画
        yield return null;


        while (true)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);


            if (state.normalizedTime >= 1f &&
                !animator.IsInTransition(0))
            {
                break;
            }


            yield return null;
        }
    }
}