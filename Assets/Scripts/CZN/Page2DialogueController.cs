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
    [SerializeField]
    private Page2DialogueData dialogueData;

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

        HideAllDialogue();
        ClearText();
    }

    public Coroutine PlayIntroButterflySequence()
    {
        Page2DialogueLine[] lines = null;

        if (dialogueData != null)
        {
            lines =
                dialogueData.introButterflyLines;
        }

        return BeginSequence(
            lines,
            DialoguePanel.Butterfly
        );
    }

    public Coroutine PlayLockedGateHintSequence()
    {
        Page2DialogueLine[] lines = null;

        if (dialogueData != null)
        {
            lines =
                dialogueData.lockedGateHintLines;
        }

        return BeginSequence(
            lines,
            DialoguePanel.Hint
        );
    }

    public void ShowPawTrailHint()
    {
        if (dialogueData == null)
        {
            return;
        }

        ShowHint(
            dialogueData.pawTrailHint
        );
    }

    public Coroutine PlayKeyRevealHintSequence()
    {
        Page2DialogueLine[] lines = null;

        if (dialogueData != null)
        {
            lines =
                dialogueData.keyRevealHintLines;
        }

        return BeginSequence(
            lines,
            DialoguePanel.Hint
        );
    }

    public Coroutine PlayGateOpenedButterflySequence()
    {
        Page2DialogueLine[] lines = null;

        if (dialogueData != null)
        {
            lines =
                dialogueData.gateOpenedButterflyLines;
        }

        return BeginSequence(
            lines,
            DialoguePanel.Butterfly
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

        return activeDialogueRoutine;
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
            Page2DialogueLine line =
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

            activeDialogueRoutine = null;
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