using UnityEngine;
using System.Collections;

public class SequentialChildrenAnimation : MonoBehaviour
{
    [Header("Animator Trigger Name")]
    public string triggerName = "Grow";


    [Header("Always Active Objects")]
    public GameObject[] alwaysActiveObjects;



    void OnEnable()
    {
        StartCoroutine(PlaySequence());
    }



    IEnumerator PlaySequence()
    {

        // ① 关闭所有参与生长的child
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;


            // 如果是永久显示物体，跳过
            if (IsAlwaysActive(child))
                continue;


            child.SetActive(false);
        }



        // ② 开启永久显示物体
        foreach (GameObject obj in alwaysActiveObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }



        // 等一帧，确保Parent激活
        yield return null;



        // ③ 按顺序播放生长动画
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;


            // 跳过Particle等永久物体
            if (IsAlwaysActive(child))
                continue;



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



            // 非最后一个隐藏
            if (IsNotLastGrowthObject(child))
            {
                child.SetActive(false);
            }
        }
    }



    bool IsAlwaysActive(GameObject obj)
    {
        foreach (GameObject always in alwaysActiveObjects)
        {
            if (obj == always)
                return true;
        }

        return false;
    }



    bool IsNotLastGrowthObject(GameObject obj)
    {
        int count = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;


            if (!IsAlwaysActive(child))
            {
                count++;
            }


            if (child == obj)
            {
                return count < GetGrowthObjectCount();
            }
        }


        return false;
    }



    int GetGrowthObjectCount()
    {
        int count = 0;


        for (int i = 0; i < transform.childCount; i++)
        {
            if (!IsAlwaysActive(transform.GetChild(i).gameObject))
            {
                count++;
            }
        }


        return count;
    }



    IEnumerator WaitAnimation(Animator animator)
    {
        yield return null;


        while (true)
        {
            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(0);


            if (state.normalizedTime >= 1f &&
               !animator.IsInTransition(0))
            {
                break;
            }


            yield return null;
        }
    }
}