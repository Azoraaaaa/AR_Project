using System.Collections;
using TMPro;
using UnityEngine;

public class FlowController1 : MonoBehaviour
{
    [Header("Hint UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;

    [Header("Story UI")]
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private TMP_Text storyText;

    [Header("Main Interaction")]
    [SerializeField] private MemoryItemSequence memoryItemSequence;
    [SerializeField] private SeedTapController seedTapController;

    [Header("Timing")]
    [SerializeField] private float readyHintDuration = 1.5f;
    [SerializeField] private float itemInstructionDuration = 2f;

    [Header("Final Something Missing Dialogue")]

    [TextArea(2, 4)]
    [SerializeField]
    private string somethingMissingText =
    "Something is still missing.";

    [Tooltip("用于播放最终故事配音")]
    [SerializeField] private AudioSource narrationAudioSource;

    [Tooltip("最终一句 Something is still missing 的配音")]
    [SerializeField] private AudioClip somethingMissingVoice;

    [Range(0f, 1f)]
    [SerializeField] private float somethingMissingVolume = 1f;

    [Range(0.5f, 1f)]
    [SerializeField] private float finalRevealDurationRatio = 0.9f;

    [SerializeField] private float finalFallbackCharactersPerSecond = 25f;

    private Coroutine mainFlowRoutine;
    private Coroutine storyFeedbackRoutine;

    private void Awake()
    {
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
        }

        /*
         * 不要在这里强制隐藏 StoryPanel。
         * 开场的 DialogueController 还需要使用它。
         */

        if (seedTapController != null)
        {
            seedTapController.SetInteractable(false);
        }
    }

    /// <summary>
    /// 种子成功放进花盆后调用。
    /// </summary>
    public void OnSeedPlanted()
    {
        if (mainFlowRoutine != null)
        {
            StopCoroutine(mainFlowRoutine);
        }

        mainFlowRoutine =
            StartCoroutine(SeedPlantedRoutine());
    }

    private IEnumerator SeedPlantedRoutine()
    {
        HideStory();

        ShowHint("The Memory Seed is ready");

        yield return new WaitForSecondsRealtime(
            readyHintDuration
        );

        ShowHint(
            "Place the three gifts around the Memory Seed"
        );

        yield return new WaitForSecondsRealtime(
            itemInstructionDuration
        );

        HideHint();

        if (memoryItemSequence != null)
        {
            memoryItemSequence.BeginSequence();
        }
        else
        {
            Debug.LogError(
                "Page10FlowController: MemoryItemSequence is missing."
            );
        }

        mainFlowRoutine = null;
    }

    /// <summary>
    /// Leaf、Petal、Star 放置成功后的蝴蝶反馈。
    /// </summary>
    public void ShowStoryFeedback(
        string message,
        float duration)
    {
        if (storyFeedbackRoutine != null)
        {
            StopCoroutine(storyFeedbackRoutine);
        }

        storyFeedbackRoutine =
            StartCoroutine(
                StoryFeedbackRoutine(message, duration)
            );
    }

    private IEnumerator StoryFeedbackRoutine(
        string message,
        float duration)
    {
        HideHint();

        if (storyPanel == null)
        {
            Debug.LogError(
                "Page10FlowController: StoryPanel is not assigned."
            );

            yield break;
        }

        if (storyText == null)
        {
            Debug.LogError(
                "Page10FlowController: StoryText is not assigned."
            );

            yield break;
        }

        storyPanel.SetActive(true);
        storyText.gameObject.SetActive(true);

        storyText.text = message;

        /*
         * 防止前面的 Typewriter 脚本把可见字符数量留在 0。
         */
        storyText.maxVisibleCharacters = int.MaxValue;
        storyText.ForceMeshUpdate();

        Debug.Log(
            "Showing story feedback: " +
            message +
            " | Panel active: " +
            storyPanel.activeInHierarchy
        );

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.1f, duration)
        );

        HideStory();

        storyFeedbackRoutine = null;
    }

    /// <summary>
    /// 三个物品全部放置完成后调用。
    /// </summary>
    public void OnAllItemsPlaced()
    {
        if (mainFlowRoutine != null)
        {
            StopCoroutine(mainFlowRoutine);
        }

        mainFlowRoutine =
            StartCoroutine(AllItemsPlacedRoutine());
    }

    private IEnumerator AllItemsPlacedRoutine()
    {
        /*
         * 如果 Star 的反馈还在显示，
         * 等待它自己结束，不立即覆盖。
         */
        while (storyFeedbackRoutine != null)
        {
            yield return null;
        }

        HideStory();

        ShowHint("Tap the Memory Seed");

        if (seedTapController != null)
        {
            seedTapController.SetInteractable(true);
        }
        else
        {
            Debug.LogError(
                "Page10FlowController: SeedTapController is missing."
            );
        }

        mainFlowRoutine = null;
    }

    /// <summary>
    /// 最后点击种子，无事发生后调用。
    /// </summary>
    public void ShowFinalSomethingMissing()
    {
        if (storyFeedbackRoutine != null)
        {
            StopCoroutine(storyFeedbackRoutine);
            storyFeedbackRoutine = null;
        }

        if (mainFlowRoutine != null)
        {
            StopCoroutine(mainFlowRoutine);
            mainFlowRoutine = null;
        }

        storyFeedbackRoutine =
            StartCoroutine(FinalSomethingMissingRoutine());
    }
    private IEnumerator FinalSomethingMissingRoutine()
    {
        HideHint();

        if (storyPanel == null)
        {
            Debug.LogError(
                "Page10FlowController: Story Panel is not assigned."
            );

            yield break;
        }

        if (storyText == null)
        {
            Debug.LogError(
                "Page10FlowController: Story Text is not assigned."
            );

            yield break;
        }

        storyPanel.SetActive(true);
        storyText.gameObject.SetActive(true);

        storyText.text = somethingMissingText;
        storyText.ForceMeshUpdate();

        int characterCount =
            storyText.textInfo.characterCount;

        storyText.maxVisibleCharacters = 0;

        float dialogueDuration;

        if (somethingMissingVoice != null)
        {
            dialogueDuration =
                somethingMissingVoice.length;

            if (narrationAudioSource != null)
            {
                narrationAudioSource.Stop();
                narrationAudioSource.clip =
                    somethingMissingVoice;

                narrationAudioSource.volume =
                    somethingMissingVolume;

                narrationAudioSource.Play();
            }
            else
            {
                Debug.LogWarning(
                    "Page10FlowController: " +
                    "Narration AudioSource is not assigned."
                );
            }
        }
        else
        {
            dialogueDuration =
                characterCount /
                Mathf.Max(
                    1f,
                    finalFallbackCharactersPerSecond
                );
        }

        float revealDuration =
            dialogueDuration *
            finalRevealDurationRatio;

        float characterInterval =
            characterCount > 0
                ? revealDuration / characterCount
                : 0f;

        for (int i = 0; i <= characterCount; i++)
        {
            storyText.maxVisibleCharacters = i;

            if (characterInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    characterInterval
                );
            }
        }

        /*
         * 如果有配音，等待配音完整播放结束。
         */
        if (somethingMissingVoice != null &&
            narrationAudioSource != null)
        {
            while (narrationAudioSource.isPlaying)
            {
                yield return null;
            }
        }

        storyText.maxVisibleCharacters =
            int.MaxValue;

        /*
         * 不隐藏 Story Panel。
         * 最终文本和蝴蝶 UI 永久保留，
         * 代表 Page 10 结束。
         */
        storyFeedbackRoutine = null;
    }

    public void HideAllUI()
    {
        HideHint();
        HideStory();
    }

    private void ShowHint(string message)
    {
        HideStory();

        if (hintPanel != null)
        {
            hintPanel.SetActive(true);
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = message;
            hintText.maxVisibleCharacters = int.MaxValue;
        }
    }

    private void HideHint()
    {
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
        }
    }

    private void HideStory()
    {
        if (storyPanel != null)
        {
            storyPanel.SetActive(false);
        }
    }
}