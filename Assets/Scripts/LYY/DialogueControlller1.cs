using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueController1 : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string text;

        [Tooltip("这一句对应的旁白配音")]
        public AudioClip voiceClip;
    }

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Narration Audio")]
    [SerializeField] private AudioSource narrationAudioSource;

    [Header("Dialogue Content")]
    [SerializeField] private DialogueLine[] dialogueLines;

    [Header("Typewriter Settings")]
    [Range(0.5f, 1f)]
    [SerializeField] private float revealDurationRatio = 0.9f;

    [SerializeField] private float fallbackCharactersPerSecond = 25f;

    [SerializeField] private float pauseAfterLine = 0.35f;

    [SerializeField] private float pauseAfterFinalLine = 0.6f;

    [Header("Seed Interaction")]
    [SerializeField] private PlantingZone plantingZone;
    [SerializeField] private DraggableSeed draggableSeed;

    private bool dialogueStarted;
    private bool dialogueFinished;

    private void Awake()
    {
        dialogueStarted = false;
        dialogueFinished = false;

        if (draggableSeed != null)
        {
            draggableSeed.enabled = false;
        }

        if (plantingZone != null)
        {
            plantingZone.PrepareForDialogue();
        }
    }

    private void Start()
    {
        if (dialoguePanel == null)
        {
            Debug.LogError(
                "Page10DialogueController: Dialogue Panel is not assigned."
            );
            return;
        }

        if (dialogueText == null)
        {
            Debug.LogError(
                "Page10DialogueController: Dialogue Text is not assigned."
            );
            return;
        }

        dialoguePanel.SetActive(true);
        dialogueText.text = "";
        dialogueText.maxVisibleCharacters = 0;

        StartCoroutine(PlayDialogueSequence());
    }

    private void PrepareDialogueUI()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 0;
        }
    }


    private void PrepareDialogueState()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 0;
        }

        if (draggableSeed != null)
            draggableSeed.enabled = false;

        if (plantingZone != null)
            plantingZone.PrepareForDialogue();
    }

    private IEnumerator PlayDialogueSequence()
    {
        dialogueStarted = true;

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            FinishDialogue();
            yield break;
        }

        for (int i = 0; i < dialogueLines.Length; i++)
        {
            DialogueLine line = dialogueLines[i];

            if (line == null || string.IsNullOrWhiteSpace(line.text))
                continue;

            yield return StartCoroutine(PlaySingleLine(line));

            bool isFinalLine = i == dialogueLines.Length - 1;

            if (!isFinalLine && pauseAfterLine > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    pauseAfterLine
                );
            }
        }

        if (pauseAfterFinalLine > 0f)
        {
            yield return new WaitForSecondsRealtime(
                pauseAfterFinalLine
            );
        }

        FinishDialogue();
    }

    private IEnumerator PlaySingleLine(DialogueLine line)
    {
        if (dialogueText == null)
            yield break;

        dialogueText.text = line.text;
        dialogueText.ForceMeshUpdate();

        int visibleCharacterCount =
            dialogueText.textInfo.characterCount;

        dialogueText.maxVisibleCharacters = 0;

        float revealDuration;

        if (line.voiceClip != null)
        {
            revealDuration =
                line.voiceClip.length * revealDurationRatio;

            if (narrationAudioSource != null)
            {
                narrationAudioSource.Stop();
                narrationAudioSource.clip = line.voiceClip;
                narrationAudioSource.Play();
            }
        }
        else
        {
            revealDuration =
                visibleCharacterCount /
                Mathf.Max(1f, fallbackCharactersPerSecond);
        }

        float characterInterval =
            visibleCharacterCount > 0
                ? revealDuration / visibleCharacterCount
                : 0f;

        for (int i = 0; i <= visibleCharacterCount; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            if (characterInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    characterInterval
                );
            }
        }

        if (line.voiceClip != null &&
            narrationAudioSource != null)
        {
            while (narrationAudioSource.isPlaying)
            {
                yield return null;
            }
        }
    }

    private void FinishDialogue()
    {
        dialogueFinished = true;

        if (dialogueText != null)
        {
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (plantingZone != null)
        {
            plantingZone.BeginPlantingInteraction();
        }

        if (draggableSeed != null)
        {
            draggableSeed.enabled = true;
        }
    }
}