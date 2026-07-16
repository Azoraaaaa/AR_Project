using TMPro;
using UnityEngine;

public class PlantingZone : MonoBehaviour
{
    [Header("种子模型")]
    [Tooltip("花盆里预先摆放好的种子，开始时隐藏")]
    [SerializeField] private GameObject plantedSeedModel;

    [Header("提示文字 UI")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;

    [TextArea]
    [SerializeField]
    private string beforePlantText =
        "Drag the Memory Seed into the pot";

    [TextArea]
    [SerializeField]
    private string afterPlantText =
        "The Memory Seed is ready";

    [Header("引导 UI")]
    [Tooltip("指向外部种子的手指箭头")]
    [SerializeField] private GameObject fingerHint;

    [Tooltip("花盆里的虚线圆圈")]
    [SerializeField] private GameObject dashedCircleHint;

    [Tooltip("花盆里指向放置位置的箭头")]
    [SerializeField] private GameObject potArrowHint;

    [Header("种子高亮")]
    [Tooltip("种子外圈发光对象，例如 Glow Sprite 或稍大的发光模型")]
    [SerializeField] private GameObject seedGlowObject;

    [Header("放置音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip placeSeedClip;
    [Range(0f, 1f)]
    [SerializeField] private float placeVolume = 1f;

    private bool hasPlanted;

    public bool HasPlanted => hasPlanted;

    private void Awake()
    {
        hasPlanted = false;

        if (plantedSeedModel != null)
            plantedSeedModel.SetActive(false);

        SetPrompt(beforePlantText);

        SetObjectActive(fingerHint, true);
        SetObjectActive(dashedCircleHint, true);
        SetObjectActive(potArrowHint, true);
        SetObjectActive(seedGlowObject, true);
    }

    /// <summary>
    /// 玩家成功点中并拿起种子时调用。
    /// </summary>
    public void OnSeedPickedUp()
    {
        if (hasPlanted)
            return;

        // 拿起来之后，不再需要手指提示和发光提示
        SetObjectActive(fingerHint, false);
        SetObjectActive(seedGlowObject, false);

        // 花盆位置提示继续保留
        SetObjectActive(dashedCircleHint, true);
        SetObjectActive(potArrowHint, true);

        SetPrompt(beforePlantText);
    }

    /// <summary>
    /// 玩家没有放进花盆，种子返回原位时调用。
    /// </summary>
    public void OnSeedReturned()
    {
        if (hasPlanted)
            return;

        SetObjectActive(fingerHint, true);
        SetObjectActive(seedGlowObject, true);

        SetObjectActive(dashedCircleHint, true);
        SetObjectActive(potArrowHint, true);

        SetPrompt(beforePlantText);
    }

    /// <summary>
    /// 种子在 Trigger 中松手时调用。
    /// </summary>
    public bool PlantSeed(GameObject draggedSeed)
    {
        if (hasPlanted)
            return false;

        hasPlanted = true;

        // 播放放下种子的音效
        if (audioSource != null && placeSeedClip != null)
        {
            audioSource.PlayOneShot(placeSeedClip, placeVolume);
        }

        // 显示花盆里预先放好的种子
        if (plantedSeedModel != null)
        {
            plantedSeedModel.SetActive(true);
        }

        // 隐藏原本被拖动的种子
        if (draggedSeed != null)
        {
            draggedSeed.SetActive(false);
        }

        // 放置完成，隐藏所有引导图
        SetObjectActive(fingerHint, false);
        SetObjectActive(seedGlowObject, false);
        SetObjectActive(dashedCircleHint, false);
        SetObjectActive(potArrowHint, false);

        SetPrompt(afterPlantText);

        return true;
    }

    private void SetPrompt(string message)
    {
        if (promptPanel != null)
            promptPanel.SetActive(true);

        if (promptText != null)
            promptText.text = message;
    }

    private void SetObjectActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}