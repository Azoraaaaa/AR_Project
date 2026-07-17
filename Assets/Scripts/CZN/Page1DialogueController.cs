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
    [SerializeField]
    private Page1DialogueData dialogueData;

    [Header("Game Hint")]
    [SerializeField]
    private GameObject gameHintRoot;

    [SerializeField]
    private TMP_Text gameHintText;

    [Header("Butterfly Story Box")]
    [SerializeField]
    private GameObject storyBackgroundRoot;

    [SerializeField]
    private TMP_Text storyText;

    [Header("Unused UI")]
    [SerializeField]
    private GameObject propsBarRoot;

    [Header("Typewriter")]
    [Tooltip("Number of visible characters per second.")]
    [SerializeField]
    private float charactersPerSecond = 35f;

    private Coroutine activeDialogueRoutine;

    public bool IsTyping { get; private set; }

    private void Awake()
    {
        if (propsBarRoot != null)
        {
            propsBarRoot.SetActive(false);
        }

        ShowPanel(
            DialoguePanel.Hint
        );

        ClearText();
    }

    public Coroutine PlayIntroSequence()
    {
        Page1DialogueLine[] lines = null;

        if (dialogueData != null)
        {
            lines =
                dialogueData.introLines;
        }

        return BeginSequence(
            lines,
            DialoguePanel.Hint
        );
    }

    public void ShowMemoryHint(
        MemoryType memoryType)
    {
        if (dialogueData == null)
        {
            return;
        }

        string message = "";

        switch (memoryType)
        {
            case MemoryType.Ball:
                message =
                    dialogueData.ballMemoryHint;
                break;

            case MemoryType.Bed:
                message =
                    dialogueData.bedMemoryHint;
                break;

            case MemoryType.Collar:
                message =
                    dialogueData.collarMemoryHint;
                break;

            case MemoryType.Food:
                message =
                    dialogueData.foodMemoryHint;
                break;
        }

        ShowHint(
            message
        );
    }

    public void ShowAllMemoriesCompletedHint()
    {
        if (dialogueData == null)
        {
            return;
        }

        ShowHint(
            dialogueData.allMemoriesCompletedHint
        );
    }

    public void ShowSeedRevealHint()
    {
        if (dialogueData == null)
        {
            return;
        }

        ShowHint(
            dialogueData.seedRevealHint
        );
    }

    public void ShowSeedFirstTapHint()
    {
        if (dialogueData == null)
        {
            return;
        }

        ShowHint(
            dialogueData.seedFirstTapHint
        );
    }

    public Coroutine PlayButterflyDialogue()
    {
        Page1DialogueLine[] lines = null;

        if (dialogueData != null)
        {
            lines =
                dialogueData.butterflyLines;
        }

        return BeginSequence(
            lines,
            DialoguePanel.Butterfly
        );
    }

    public void ShowCollectSeedHint()
    {
        if (dialogueData == null)
        {
            return;
        }

        ShowHint(
            dialogueData.collectSeedHint
        );
    }

    public void ShowPageCompletedHint()
    {
        if (dialogueData == null)
        {
            return;
        }

        ShowHint(
            dialogueData.pageCompletedHint
        );
    }

    public void ShowHint(
        string message)
    {
        StopActiveDialogue();

        ShowPanel(
            DialoguePanel.Hint
        );

        activeDialogueRoutine =
            StartCoroutine(
                TypeSingleLineRoutine(
                    gameHintText,
                    message
                )
            );
    }

    public void HideAllDialogue()
    {
        StopActiveDialogue();

        if (gameHintRoot != null)
        {
            gameHintRoot.SetActive(false);
        }

        if (storyBackgroundRoot != null)
        {
            storyBackgroundRoot.SetActive(false);
        }
    }

    private Coroutine BeginSequence(
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

        return activeDialogueRoutine;
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
            panel == DialoguePanel.Hint
            ? gameHintText
            : storyText;

        if (lines == null ||
            lines.Length == 0)
        {
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

            yield return TypeTextRoutine(
                targetText,
                line.text
            );

            if (line.holdAfter > 0f)
            {
                yield return new WaitForSeconds(
                    line.holdAfter
                );
            }
        }

        IsTyping = false;
        activeDialogueRoutine = null;
    }

    private IEnumerator TypeSingleLineRoutine(
        TMP_Text targetText,
        string message)
    {
        IsTyping = true;

        yield return TypeTextRoutine(
            targetText,
            message
        );

        IsTyping = false;
        activeDialogueRoutine = null;
    }

    private IEnumerator TypeTextRoutine(
        TMP_Text targetText,
        string message)
    {
        if (targetText == null)
        {
            yield break;
        }

        targetText.text =
            message ?? "";

        targetText.maxVisibleCharacters =
            0;

        targetText.ForceMeshUpdate();

        int totalCharacters =
            targetText.textInfo.characterCount;

        float safeSpeed =
            Mathf.Max(
                1f,
                charactersPerSecond
            );

        float visibleCharacterCount =
            0f;

        while (
            targetText.maxVisibleCharacters <
            totalCharacters)
        {
            visibleCharacterCount +=
                safeSpeed *
                Time.deltaTime;

            targetText.maxVisibleCharacters =
                Mathf.Min(
                    totalCharacters,
                    Mathf.FloorToInt(
                        visibleCharacterCount
                    )
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
            panel == DialoguePanel.Hint;

        if (gameHintRoot != null)
        {
            gameHintRoot.SetActive(
                showHint
            );
        }

        if (storyBackgroundRoot != null)
        {
            storyBackgroundRoot.SetActive(
                !showHint
            );
        }
    }

    private void StopActiveDialogue()
    {
        if (activeDialogueRoutine != null)
        {
            StopCoroutine(
                activeDialogueRoutine
            );

            activeDialogueRoutine =
                null;
        }

        IsTyping = false;
    }

    private void ClearText()
    {
        if (gameHintText != null)
        {
            gameHintText.text = "";
        }

        if (storyText != null)
        {
            storyText.text = "";
        }
    }
}