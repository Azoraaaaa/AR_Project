using UnityEngine;

public class LifeCycleManager : MonoBehaviour
{
    public int finished = 0;

    public void CardPlaced()
    {
        finished++;

        if (finished == 4)
        {
            Debug.Log("全部完成！");
            // 这里播放树长大动画 / 掉发光叶子 / 下一剧情
        }
    }
}