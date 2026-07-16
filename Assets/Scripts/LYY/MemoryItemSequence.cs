using System.Collections;
using UnityEngine;

public class MemoryItemSequence : MonoBehaviour
{
    [System.Serializable]
    public class MemoryItemStage
    {
        [Header("道具类型")]
        public MemoryItemType itemType;

        [Header("桌面上的可拖动原始道具")]
        public GameObject draggableItem;

        [Header("当前阶段的 Trigger 槽位")]
        public GameObject slotObject;

        [Header("槽位上的轮廓提示图案")]
        public GameObject outlineHint;

        [Header("正确放入后显示的固定模型")]
        public GameObject placedItemModel;

        [Header("蝴蝶反馈文本")]
        [TextArea(2, 5)]
        public string feedbackText;

        [Header("可选：反馈配音")]
        public AudioClip feedbackVoice;
    }

    [Header("阶段顺序")]
    [Tooltip("建议顺序：Leaf → Petal → Star")]
    [SerializeField] private MemoryItemStage[] stages;

    [Header("Page 10 总流程")]
    [SerializeField] private FlowController1 flowController;

    [Header("Audio Sources")]
    [Tooltip("播放 Grab 和 Success 音效")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Tooltip("播放蝴蝶反馈配音")]
    [SerializeField] private AudioSource narrationAudioSource;

    [Header("Sound Effects")]
    [Tooltip("拿起和放下物品时使用")]
    [SerializeField] private AudioClip grabClip;

    [Tooltip("正确放入槽位时使用")]
    [SerializeField] private AudioClip successClip;

    [Range(0f, 1f)]
    [SerializeField] private float grabVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float successVolume = 1f;

    [Tooltip("正确放下后，Success 音效相对 Grab 音效的延迟")]
    [SerializeField] private float successSoundDelay = 0.12f;

    [Header("Feedback Timing")]
    [Tooltip("没有配音时，反馈文本显示时间")]
    [SerializeField] private float fallbackFeedbackDuration = 2.5f;

    [Tooltip("反馈结束后，下一个槽位出现前的等待时间")]
    [SerializeField] private float nextSlotDelay = 0.3f;

    private int currentStageIndex = -1;
    private bool sequenceStarted;
    private bool isChangingStage;
    private bool sequenceCompleted;

    public bool SequenceStarted => sequenceStarted;
    public bool SequenceCompleted => sequenceCompleted;
    public int CurrentStageIndex => currentStageIndex;

    private void Awake()
    {
        PrepareInitialState();

        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
        }
    }

    /// <summary>
    /// 初始化 Page 10 道具状态。
    /// 三个原始道具显示；
    /// 所有槽位、轮廓、固定模型隐藏。
    /// </summary>
    private void PrepareInitialState()
    {
        sequenceStarted = false;
        sequenceCompleted = false;
        isChangingStage = false;
        currentStageIndex = -1;

        if (stages == null)
            return;

        foreach (MemoryItemStage stage in stages)
        {
            if (stage == null)
                continue;

            if (stage.draggableItem != null)
            {
                stage.draggableItem.SetActive(true);
            }

            if (stage.slotObject != null)
            {
                stage.slotObject.SetActive(false);
            }

            if (stage.outlineHint != null)
            {
                stage.outlineHint.SetActive(false);
            }

            if (stage.placedItemModel != null)
            {
                stage.placedItemModel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 种子成功放入花盆并完成 Ready Hint 后调用。
    /// </summary>
    public void BeginSequence()
    {
        if (sequenceStarted || sequenceCompleted)
            return;

        if (stages == null || stages.Length == 0)
        {
            Debug.LogError(
                "MemoryItemSequence: No stages have been assigned."
            );

            CompleteEntireSequence();
            return;
        }

        sequenceStarted = true;
        currentStageIndex = 0;

        ShowCurrentSlot();
    }

    /// <summary>
    /// 只显示当前阶段对应的槽位和轮廓。
    /// </summary>
    private void ShowCurrentSlot()
    {
        if (!sequenceStarted || sequenceCompleted)
            return;

        if (stages == null ||
            currentStageIndex < 0 ||
            currentStageIndex >= stages.Length)
        {
            CompleteEntireSequence();
            return;
        }

        for (int i = 0; i < stages.Length; i++)
        {
            MemoryItemStage stage = stages[i];

            if (stage == null)
                continue;

            bool isCurrent = i == currentStageIndex;

            if (stage.slotObject != null)
            {
                stage.slotObject.SetActive(isCurrent);
            }

            if (stage.outlineHint != null)
            {
                stage.outlineHint.SetActive(isCurrent);
            }
        }
    }

    /// <summary>
    /// 由 DraggableMemoryItem 在松手时调用。
    /// 返回 true 表示放置正确；
    /// 返回 false 后，道具应回到原位。
    /// </summary>
    public bool TryPlaceItem(
    DraggableMemoryItem item,
    MemoryItemSlot slot)
    {
        if (!sequenceStarted ||
            sequenceCompleted ||
            isChangingStage)
        {
            Debug.LogWarning(
                $"Placement blocked. " +
                $"Started: {sequenceStarted}, " +
                $"Completed: {sequenceCompleted}, " +
                $"Changing: {isChangingStage}"
            );

            return false;
        }

        if (item == null || slot == null)
        {
            Debug.LogWarning("Item or slot is null.");
            return false;
        }

        if (stages == null ||
            currentStageIndex < 0 ||
            currentStageIndex >= stages.Length)
        {
            Debug.LogWarning(
                $"Invalid stage index: {currentStageIndex}"
            );

            return false;
        }

        MemoryItemStage currentStage =
            stages[currentStageIndex];

        bool correctItemType =
            item.ItemType == currentStage.itemType;

        bool correctSlotType =
            slot.AcceptedType == currentStage.itemType;

        Debug.Log(
            $"Trying placement: " +
            $"Item={item.ItemType}, " +
            $"Slot={slot.AcceptedType}, " +
            $"Expected={currentStage.itemType}"
        );

        if (!correctItemType || !correctSlotType)
        {
            Debug.LogWarning("Wrong item or wrong slot.");
            return false;
        }

        StartCoroutine(
            CompleteCurrentStageRoutine(
                currentStage,
                item
            )
        );

        return true;
    }

    /// <summary>
    /// 处理单个道具正确放入后的流程。
    /// </summary>
    private IEnumerator CompleteCurrentStageRoutine(
        MemoryItemStage stage,
        DraggableMemoryItem item)
    {
        isChangingStage = true;

        // 正确放下，同样先播放 Grab 音效
        PlayGrabSound();

        if (successSoundDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                successSoundDelay
            );
        }

        // 再播放 Success 音效
        PlaySuccessSound();

        // 原始可拖动物品消失
        if (item != null)
        {
            item.gameObject.SetActive(false);
        }

        // 当前 Trigger 槽位消失
        if (stage.slotObject != null)
        {
            stage.slotObject.SetActive(false);
        }

        // 当前轮廓提示消失
        if (stage.outlineHint != null)
        {
            stage.outlineHint.SetActive(false);
        }

        // 显示正确放置后的固定模型
        if (stage.placedItemModel != null)
        {
            stage.placedItemModel.SetActive(true);
        }

        // 播放这一阶段的反馈配音
        if (stage.feedbackVoice != null &&
            narrationAudioSource != null)
        {
            narrationAudioSource.Stop();
            narrationAudioSource.clip =
                stage.feedbackVoice;
            narrationAudioSource.Play();
        }

        float feedbackDuration =
            fallbackFeedbackDuration;

        if (stage.feedbackVoice != null)
        {
            feedbackDuration = Mathf.Max(
                fallbackFeedbackDuration,
                stage.feedbackVoice.length
            );
        }

        // 使用蝴蝶 Story UI 显示反馈
        if (flowController != null)
        {
            flowController.ShowStoryFeedback(
                stage.feedbackText,
                feedbackDuration
            );
        }
        else
        {
            Debug.LogWarning(
                "MemoryItemSequence: FlowController is not assigned."
            );
        }

        // 等待反馈文本及配音完成
        yield return new WaitForSecondsRealtime(
            feedbackDuration
        );

        if (nextSlotDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                nextSlotDelay
            );
        }

        currentStageIndex++;
        isChangingStage = false;

        if (currentStageIndex < stages.Length)
        {
            ShowCurrentSlot();
        }
        else
        {
            CompleteEntireSequence();
        }
    }

    /// <summary>
    /// Leaf、Petal、Star 全部放置完成。
    /// </summary>
    private void CompleteEntireSequence()
    {
        if (sequenceCompleted)
            return;

        sequenceCompleted = true;
        sequenceStarted = false;
        isChangingStage = false;

        HideAllSlotsAndOutlines();

        if (flowController != null)
        {
            flowController.OnAllItemsPlaced();
        }
        else
        {
            Debug.LogError(
                "MemoryItemSequence: FlowController is missing."
            );
        }

        Debug.Log(
            "MemoryItemSequence: All items have been placed."
        );
    }

    private void HideAllSlotsAndOutlines()
    {
        if (stages == null)
            return;

        foreach (MemoryItemStage stage in stages)
        {
            if (stage == null)
                continue;

            if (stage.slotObject != null)
            {
                stage.slotObject.SetActive(false);
            }

            if (stage.outlineHint != null)
            {
                stage.outlineHint.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 拿起或普通放下时播放。
    /// DraggableMemoryItem 可以调用这个方法。
    /// </summary>
    public void PlayGrabSound()
    {
        if (sfxAudioSource != null &&
            grabClip != null)
        {
            sfxAudioSource.PlayOneShot(
                grabClip,
                grabVolume
            );
        }
    }

    private void PlaySuccessSound()
    {
        if (sfxAudioSource != null &&
            successClip != null)
        {
            sfxAudioSource.PlayOneShot(
                successClip,
                successVolume
            );
        }
    }

    /// <summary>
    /// 调试或重新开始 Page 10 时可以调用。
    /// </summary>
    public void ResetSequence()
    {
        StopAllCoroutines();

        if (narrationAudioSource != null)
        {
            narrationAudioSource.Stop();
        }

        PrepareInitialState();
    }
}