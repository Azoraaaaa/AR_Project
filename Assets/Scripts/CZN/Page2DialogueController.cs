using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Page2DialogueController : MonoBehaviour
{
    private enum DialoguePanel
    {
        Hint,
        Butterfly
    }

    [Header("Dialogue Data")]
    [Tooltip(
        "Contains all Page 2 dialogue text, voice clips, " +
        "and waiting times."
    )]
    [SerializeField]
    private Page2DialogueData dialogueData;

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
        "Optional PropsBar. It remains hidden during dialogue."
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

        ClearTargetText(
            gameHintText
        );

        ClearTargetText(
            storyText
        );
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
     * Opening butterfly dialogue.
     *
     * MemoryGardenGateManager can use:
     *
     * yield return
     *     dialogueController
     *         .PlayIntroButterflySequence();
     */
    public IEnumerator PlayIntroButterflySequence()
    {
        if (dialogueData == null)
        {
            Debug.LogError(
                "Page2DialogueData has not been assigned."
            );

            yield break;
        }

        yield return PlaySequenceAndWait(
            dialogueData.introButterflyLines,
            DialoguePanel.Butterfly
        );
    }

    /*
     * Locked gate dialogue or narration.
     */
    public IEnumerator PlayLockedGateHintSequence()
    {
        if (dialogueData == null)
        {
            Debug.LogError(
                "Page2DialogueData has not been assigned."
            );

            yield break;
        }

        yield return PlaySequenceAndWait(
            dialogueData.lockedGateHintLines,
            DialoguePanel.Hint
        );
    }

    /*
     * Paw trail instruction.
     *
     * This version starts the hint but does not make
     * the calling Manager wait.
     */
    public void ShowPawTrailHint()
    {
        if (dialogueData == null)
        {
            return;
        }

        StartSingleLine(
            dialogueData.pawTrailHint,
            DialoguePanel.Hint
        );
    }

    /*
     * Use this version when the Manager must wait
     * until the text and voice are both complete.
     */
    public IEnumerator PlayPawTrailHint()
    {
        if (dialogueData == null)
        {
            yield break;
        }

        yield return PlaySingleLineAndWait(
            dialogueData.pawTrailHint,
            DialoguePanel.Hint
        );
    }

    /*
     * Dialogue or narration shown after the key appears.
     */
    public IEnumerator PlayKeyRevealHintSequence()
    {
        if (dialogueData == null)
        {
            Debug.LogError(
                "Page2DialogueData has not been assigned."
            );

            yield break;
        }

        yield return PlaySequenceAndWait(
            dialogueData.keyRevealHintLines,
            DialoguePanel.Hint
        );
    }

    /*
     * Butterfly dialogue played after the gate opens.
     */
    public IEnumerator PlayGateOpenedButterflySequence()
    {
        if (dialogueData == null)
        {
            Debug.LogError(
                "Page2DialogueData has not been assigned."
            );

            yield break;
        }

        yield return PlaySequenceAndWait(
            dialogueData.gateOpenedButterflyLines,
            DialoguePanel.Butterfly
        );
    }

    private void StartSingleLine(
        Page2DialogueLine line,
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
        Page2DialogueLine line,
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
        Page2DialogueLine[] lines,
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
        Page2DialogueLine line,
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
        Page2DialogueLine[] lines,
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
            Page2DialogueLine line =
                lines[i];

            if (line == null)
            {
                continue;
            }

            /*
             * The next line only starts after
             * the current text and voice are complete.
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
        Page2DialogueLine line)
    {
        if (line == null)
        {
            yield break;
        }

        bool voiceStarted = false;

        /*
         * Start the voice and typewriter text
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
         * If the text finishes before the voice,
         * continue waiting for the voice.
         */
        if (voiceStarted)
        {
            yield return new WaitWhile(
                () =>
                    dialogueVoiceSource != null &&
                    dialogueVoiceSource.isPlaying
            );
        }

        /*
         * Optional pause after both the text
         * and voice have finished.
         */
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