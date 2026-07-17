using System.Collections;
using TMPro;
using UnityEngine;

public class PlantingZone : MonoBehaviour
{
    [Header("Page Flow")]
    [SerializeField] private FlowController1 flowController;

    [Header("种子模型")]
    [Tooltip("花盆里预先摆好的种子，游戏开始时隐藏")]
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
    [Tooltip("拿起种子前，指向种子的手指图片")]
    [SerializeField] private GameObject fingerHint;

    [Tooltip("花盆里的虚线圆圈图片")]
    [SerializeField] private GameObject dashedCircleHint;

    [Tooltip("指向花盆放置位置的箭头图片")]
    [SerializeField] private GameObject potArrowHint;

    [Header("Sound Effects")]
    [Tooltip("必须放在不会被隐藏的对象上，例如 PlantingTrigger")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Tooltip("拿起和放下共用的音效")]
    [SerializeField] private AudioClip grabClip;

    [Tooltip("正确放入花盆后的成功音效")]
    [SerializeField] private AudioClip successClip;

    [Range(0f, 1f)]
    [SerializeField] private float grabVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float successVolume = 1f;

    [Tooltip("Grab 音效开始后，延迟多久播放 Success")]
    [SerializeField] private float successDelay = 0.12f;

    private bool hasPlanted;

    public bool HasPlanted => hasPlanted;

    private void Awake()
    {
        hasPlanted = false;

        if (plantedSeedModel != null)
            plantedSeedModel.SetActive(false);

        if (sfxAudioSource == null)
            sfxAudioSource = GetComponent<AudioSource>();

        /*
         * 不要在这里直接显示交互提示，
         * 由 Page10DialogueController 在对白结束后开启。
         */
        PrepareForDialogue();
    }

    /// <summary>
    /// 玩家开始拿起种子。
    /// </summary>
    public void OnSeedPickedUp()
    {
        if (hasPlanted)
            return;

        PlayGrabSound();

        // 玩家已经知道种子在哪里，隐藏手指提示
        SetObjectActive(fingerHint, false);

        // 花盆里的目标提示继续显示
        SetObjectActive(dashedCircleHint, true);
        SetObjectActive(potArrowHint, true);

        SetPrompt(beforePlantText);
    }

    /// <summary>
    /// 玩家在错误位置放下种子，种子返回原位。
    /// </summary>
    public void OnSeedReturned()
    {
        if (hasPlanted)
            return;

        // 放下也播放同一个 Grab 音效
        PlayGrabSound();

        // 返回原位后，重新显示手指提示
        SetObjectActive(fingerHint, true);

        SetObjectActive(dashedCircleHint, true);
        SetObjectActive(potArrowHint, true);

        SetPrompt(beforePlantText);
    }

    /// <summary>
    /// 玩家在正确 Trigger 中放下种子。
    /// </summary>
    public bool PlantSeed(GameObject draggedSeed)
    {
        if (hasPlanted)
            return false;

        hasPlanted = true;

        /*
         * 音效必须先由 PlantingZone 播放。
         * PlantingZone 不会被隐藏，所以 draggedSeed 消失后音效仍然继续。
         */
        PlayGrabSound();
        StartCoroutine(PlaySuccessAfterDelay());

        // 显示花盆里预先摆好的固定种子
        if (plantedSeedModel != null)
        {
            plantedSeedModel.SetActive(true);
        }

        // 隐藏原本可以拖动的种子
        if (draggedSeed != null)
        {
            draggedSeed.SetActive(false);
        }

        // 成功后隐藏所有引导图
        SetObjectActive(fingerHint, false);
        SetObjectActive(dashedCircleHint, false);
        SetObjectActive(potArrowHint, false);

        if (promptPanel != null)
            promptPanel.SetActive(false);

        if (flowController != null)
        {
            flowController.OnSeedPlanted();
        }
        else
        {
            Debug.LogError(
                "PlantingZone: Page10FlowController is not assigned."
            );
        }

        return true;
    }

    public void PlayGrabSound()
    {
        if (sfxAudioSource != null && grabClip != null)
        {
            sfxAudioSource.PlayOneShot(grabClip, grabVolume);
        }
    }

    private IEnumerator PlaySuccessAfterDelay()
    {
        if (successDelay > 0f)
        {
            yield return new WaitForSeconds(successDelay);
        }

        if (sfxAudioSource != null && successClip != null)
        {
            sfxAudioSource.PlayOneShot(
                successClip,
                successVolume
            );
        }
    }

    private void SetPrompt(string message)
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(true);
        }

        if (promptText != null)
        {
            promptText.text = message;
        }
    }

    private void SetObjectActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
    public void PrepareForDialogue()
    {
        hasPlanted = false;

        if (plantedSeedModel != null)
            plantedSeedModel.SetActive(false);

        // 故事对白期间不显示种植提示
        if (promptPanel != null)
            promptPanel.SetActive(false);

        SetObjectActive(fingerHint, false);
        SetObjectActive(dashedCircleHint, false);
        SetObjectActive(potArrowHint, false);
    }

    public void BeginPlantingInteraction()
    {
        if (hasPlanted)
            return;

        SetPrompt(beforePlantText);

        // 指向种子的手指
        SetObjectActive(fingerHint, true);

        // 指示花盆目标位置
        SetObjectActive(dashedCircleHint, true);
        SetObjectActive(potArrowHint, true);
    }
}