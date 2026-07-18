using System.Collections;
using TMPro;
using UnityEngine;

public class Page1DialogueController : MonoBehaviour
{
    private enum DialoguePanel
    {
        Hint,
        Butterfly
    }

    [Header("Dialogue Data")]
    [Tooltip(
        "Contains all Page 1 dialogue text, voice clips, " +
        "and waiting times."
    )]
    [SerializeField]
    private Page1DialogueData dialogueData;

    [Header("Game Hint")]
    [Tooltip(
        "The GameHint panel used for narration " +
        "and interaction instructions."
    )]
    [SerializeField]
    private GameObject gameHintPanel;

    [Tooltip(
        "The TextMeshPro text inside GameHint."
    )]
    [SerializeField]
    private TMP_Text gameHintText;

    [Header("Butterfly Story Dialogue")]
    [Tooltip(
        "The StoryBG panel used when the butterfly speaks."
    )]
    [SerializeField]
    private GameObject storyPanel;

    [Tooltip(
        "The TextMeshPro text inside StoryBG."
    )]
    [SerializeField]
    private TMP_Text storyText;

    [Header("Other UI")]
    [Tooltip(
        "Optional PropsBar. It remains hidden on Page 1."
    )]
    [SerializeField]
    private GameObject propsBar;

    [Header("Dialogue Voice")]
    [Tooltip(
        "Plays narration and butterfly voice clips."
    )]
    [SerializeField]
    private AudioSource dialogueVoiceSource;

    [Header("Typewriter")]
    [Tooltip(
        "How many characters appear each second."
    )]
    [Min(1f)]
    [SerializeField]
    private float charactersPerSecond = 35f;

    private Coroutine activeDialogueRoutine;

    public bool IsTyping
    {
        get;
        private set;
    }

    private void Awake()
    {
        InitialisePanels();
        InitialiseAudioSource();
    }

    private void OnDisable()
    {
        StopActiveDialogue();
    }

    private void InitialisePanels()
    {
        SetObjectActive(
            gameHintPanel,
            false
        );

        SetObjectActive(
            storyPanel,
            false
        );

        SetObjectActive(
            propsBar,
            false
        );

        if (gameHintText != null)
        {
            gameHintText.text = string.Empty;
            gameHintText.maxVisibleCharacters = 0;
        }

        if (storyText != null)
        {
            storyText.text = string.Empty;
            storyText.maxVisibleCharacters = 0;
        }
    }

    private void InitialiseAudioSource()
    {
        if (dialogueVoiceSource == null)
        {
            return;
        }

        dialogueVoiceSource.playOnAwake = false;
        dialogueVoiceSource.loop = false;
        dialogueVoiceSource.spatialBlend = 0f;
    }

    /*
     * Opening narration.
     *
     * Page1MemoryManager should use:
     *
     * yield return
     *     dialogueController.PlayIntroSequence();
     */
    public IEnumerator PlayIntroSequence()
    {
        if (dialogueData == null)
        {
            Debug.LogError(
                "Page1DialogueData has not been assigned."
            );

            yield break;
        }

        yield return PlaySequenceAndWait(
            dialogueData.introLines,
            DialoguePanel.Hint
        );
    }

    /*
     * This second name is kept as an optional alias.
     */
    public IEnumerator PlayIntroNarration()
    {
        yield return PlayIntroSequence();
    }

    /*
     * Display the correct narration for a memory.
     *
     * This version starts the text and voice,
     * but does not make the calling Manager wait.
     */
    public void ShowMemoryHint(
        MemoryType memoryType)
    {
        Page1DialogueLine line =
            GetMemoryHint(
                memoryType
            );

        StartSingleLine(
            line,
            DialoguePanel.Hint
        );
    }

    /*
     * Use this version when the Manager must wait
     * until both the voice and text are complete.
     */
    public IEnumerator PlayMemoryHint(
        MemoryType memoryType)
    {
        Page1DialogueLine line =
            GetMemoryHint(
                memoryType
            );

        yield return PlaySingleLineAndWait(
            line,
            DialoguePanel.Hint
        );
    }

    public void ShowBallMemoryHint()
    {
        ShowMemoryHint(
            MemoryType.Ball
        );
    }

    public void ShowBedMemoryHint()
    {
        ShowMemoryHint(
            MemoryType.Bed
        );
    }

    public void ShowCollarMemoryHint()
    {
        ShowMemoryHint(
            MemoryType.Collar
        );
    }

    public void ShowFoodMemoryHint()
    {
        ShowMemoryHint(
            MemoryType.Food
        );
    }

    public void ShowAllMemoriesCompletedHint()
    {
        if (dialogueData == null)
        {
            return;
        }

        StartSingleLine(
            dialogueData.allMemoriesCompletedHint,
            DialoguePanel.Hint
        );
    }

    public IEnumerator PlayAllMemoriesCompletedHint()
    {
        if (dialogueData == null)
        {
            yield break;
        }

        yield return PlaySingleLineAndWait(
            dialogueData.allMemoriesCompletedHint,
            DialoguePanel.Hint
        );
    }

    public void ShowSeedRevealHint()
    {
        if (dialogueData == null)
        {
            return;
        }

        StartSingleLine(
            dialogueData.seedRevealHint,
            DialoguePanel.Hint
        );
    }

    public IEnumerator PlaySeedRevealHint()
    {
        if (dialogueData == null)
        {
            yield break;
        }

        yield return PlaySingleLineAndWait(
            dialogueData.seedRevealHint,
            DialoguePanel.Hint
        );
    }

    public void ShowSeedFirstTapHint()
    {
        if (dialogueData == null)
        {
            return;
        }

        StartSingleLine(
            dialogueData.seedFirstTapHint,
            DialoguePanel.Hint
        );
    }

    public IEnumerator PlaySeedFirstTapHint()
    {
        if (dialogueData == null)
        {
            yield break;
        }

        yield return PlaySingleLineAndWait(
            dialogueData.seedFirstTapHint,
            DialoguePanel.Hint
        );
    }

    /*
     * The butterfly dialogue uses StoryBG.
     *
     * SeedButterflyManager already waits for this:
     *
     * yield return
     *     dialogueController.PlayButterflyDialogue();
     */
    public IEnumerator PlayButterflyDialogue()
    {
        if (dialogueData == null)
        {
            Debug.LogError(
                "Page1DialogueData has not been assigned."
            );

            yield break;
        }

        yield return PlaySequenceAndWait(
            dialogueData.butterflyLines,
            DialoguePanel.Butterfly
        );
    }

    public void ShowCollectSeedHint()
    {
        if (dialogueData == null)
        {
            return;
        }

        StartSingleLine(
            dialogueData.collectSeedHint,
            DialoguePanel.Hint
        );
    }

    public IEnumerator PlayCollectSeedHint()
    {
        if (dialogueData == null)
        {
            yield break;
        }

        yield return PlaySingleLineAndWait(
            dialogueData.collectSeedHint,
            DialoguePanel.Hint
        );
    }

    public void ShowPageCompletedHint()
    {
        if (dialogueData == null)
        {
            return;
        }

        StartSingleLine(
            dialogueData.pageCompletedHint,
            DialoguePanel.Hint
        );
    }

    public IEnumerator PlayPageCompletedHint()
    {
        if (dialogueData == null)
        {
            yield break;
        }

        yield return PlaySingleLineAndWait(
            dialogueData.pageCompletedHint,
            DialoguePanel.Hint
        );
    }

    private Page1DialogueLine GetMemoryHint(
        MemoryType memoryType)
    {
        if (dialogueData == null)
        {
            return null;
        }

        switch (memoryType)
        {
            case MemoryType.Ball:
                return dialogueData.ballMemoryHint;

            case MemoryType.Bed:
                return dialogueData.bedMemoryHint;

            case MemoryType.Collar:
                return dialogueData.collarMemoryHint;

            case MemoryType.Food:
                return dialogueData.foodMemoryHint;

            default:
                return null;
        }
    }

    private void StartSingleLine(
        Page1DialogueLine line,
        DialoguePanel panel)
    {
        StopActiveDialogue();

        activeDialogueRoutine =
            StartCoroutine(
                PlaySingleLineRoutine(
                    line,
                    panel
                )
            );
    }

    private IEnumerator PlaySingleLineAndWait(
        Page1DialogueLine line,
        DialoguePanel panel)
    {
        StopActiveDialogue();

        activeDialogueRoutine =
            StartCoroutine(
                PlaySingleLineRoutine(
                    line,
                    panel
                )
            );

        yield return activeDialogueRoutine;
    }

    private IEnumerator PlaySequenceAndWait(
        Page1DialogueLine[] lines,
        DialoguePanel panel)
    {
        StopActiveDialogue();

        activeDialogueRoutine =
            StartCoroutine(
                PlaySequenceRoutine(
                    lines,
                    panel
                )
            );

        yield return activeDialogueRoutine;
    }

    private IEnumerator PlaySingleLineRoutine(
        Page1DialogueLine line,
        DialoguePanel panel)
    {
        IsTyping = true;

        ShowPanel(
            panel
        );

        TMP_Text targetText =
            GetTargetText(
                panel
            );

        if (line == null)
        {
            ClearTargetText(
                targetText
            );

            IsTyping = false;
            activeDialogueRoutine = null;

            yield break;
        }

        yield return PlayDialogueLineRoutine(
            targetText,
            line
        );

        IsTyping = false;
        activeDialogueRoutine = null;
    }

    private IEnumerator PlaySequenceRoutine(
        Page1DialogueLine[] lines,
        DialoguePanel panel)
    {
        IsTyping = true;

        ShowPanel(
            panel
        );

        TMP_Text targetText =
            GetTargetText(
                panel
            );

        if (lines == null ||
            lines.Length == 0)
        {
            ClearTargetText(
                targetText
            );

            IsTyping = false;
            activeDialogueRoutine = null;

            yield break;
        }

        for (int i = 0;
             i < lines.Length;
             i++)
        {
            Page1DialogueLine line =
                lines[i];

            if (line == null)
            {
                continue;
            }

            /*
             * Each new line starts only after
             * the previous text and voice are complete.
             */
            yield return PlayDialogueLineRoutine(
                targetText,
                line
            );
        }

        IsTyping = false;
        activeDialogueRoutine = null;
    }

    private IEnumerator PlayDialogueLineRoutine(
        TMP_Text targetText,
        Page1DialogueLine line)
    {
        if (line == null)
        {
            yield break;
        }

        bool voiceStarted = false;

        /*
         * The voice and typewriter text begin
         * during the same frame.
         */
        if (dialogueVoiceSource != null &&
            line.voiceClip != null)
        {
            dialogueVoiceSource.Stop();

            dialogueVoiceSource.clip =
                line.voiceClip;

            dialogueVoiceSource.loop =
                false;

            dialogueVoiceSource.Play();

            voiceStarted = true;
        }

        /*
         * Wait until all text has appeared.
         * The voice continues playing at the same time.
         */
        yield return TypeTextRoutine(
            targetText,
            line.text
        );

        /*
         * If the text finishes first, wait until
         * the voice clip also finishes.
         *
         * If the voice finishes first, the typewriter
         * routine has already waited for the text.
         */
        if (voiceStarted)
        {
            yield return new WaitWhile(
                () =>
                    dialogueVoiceSource != null &&
                    dialogueVoiceSource.isPlaying
            );
        }

        if (line.holdAfter > 0f)
        {
            yield return new WaitForSeconds(
                line.holdAfter
            );
        }
    }

    private IEnumerator TypeTextRoutine(
        TMP_Text targetText,
        string message)
    {
        if (targetText == null)
        {
            Debug.LogError(
                "The dialogue TMP_Text has not been assigned."
            );

            yield break;
        }

        targetText.text =
            message ?? string.Empty;

        targetText.maxVisibleCharacters =
            0;

        targetText.ForceMeshUpdate();

        int totalCharacters =
            targetText.textInfo.characterCount;

        if (totalCharacters <= 0)
        {
            yield break;
        }

        float safeCharactersPerSecond =
            Mathf.Max(
                1f,
                charactersPerSecond
            );

        float visibleCharacterProgress =
            0f;

        while (
            targetText.maxVisibleCharacters <
            totalCharacters)
        {
            visibleCharacterProgress +=
                safeCharactersPerSecond *
                Time.deltaTime;

            int visibleCharacters =
                Mathf.FloorToInt(
                    visibleCharacterProgress
                );

            targetText.maxVisibleCharacters =
                Mathf.Min(
                    visibleCharacters,
                    totalCharacters
                );

            yield return null;
        }

        targetText.maxVisibleCharacters =
            totalCharacters;
    }

    private void ShowPanel(
        DialoguePanel panel)
    {
        bool showHint =
            panel ==
            DialoguePanel.Hint;

        SetObjectActive(
            gameHintPanel,
            showHint
        );

        SetObjectActive(
            storyPanel,
            !showHint
        );

        SetObjectActive(
            propsBar,
            false
        );
    }

    private TMP_Text GetTargetText(
        DialoguePanel panel)
    {
        if (panel ==
            DialoguePanel.Hint)
        {
            return gameHintText;
        }

        return storyText;
    }

    private void ClearTargetText(
        TMP_Text targetText)
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text =
            string.Empty;

        targetText.maxVisibleCharacters =
            0;
    }

    private void StopActiveDialogue()
    {
        if (activeDialogueRoutine != null)
        {
            StopCoroutine(
                activeDialogueRoutine
            );

            activeDialogueRoutine = null;
        }

        if (dialogueVoiceSource != null)
        {
            dialogueVoiceSource.Stop();
            dialogueVoiceSource.clip = null;
        }

        IsTyping = false;
    }

    public void HideAllDialogue()
    {
        StopActiveDialogue();

        SetObjectActive(
            gameHintPanel,
            false
        );

        SetObjectActive(
            storyPanel,
            false
        );
    }

    private void SetObjectActive(
        GameObject targetObject,
        bool isActive)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(
                isActive
            );
        }
    }
}