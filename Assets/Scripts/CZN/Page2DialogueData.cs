using System;
using UnityEngine;

[Serializable]
public class Page2DialogueLine
{
    [TextArea(2, 6)]
    [Tooltip("The dialogue, narration, or instruction text.")]
    public string text;

    [Tooltip(
        "Optional voice clip for this line. " +
        "Leave it empty if this line has no voice."
    )]
    public AudioClip voiceClip;

    [Tooltip(
        "Extra waiting time after both the text " +
        "and voice have finished."
    )]
    [Min(0f)]
    public float holdAfter = 0f;
}

[CreateAssetMenu(
    fileName = "Page2DialogueData",
    menuName = "Memory Garden/Page 2 Dialogue Data"
)]
public class Page2DialogueData : ScriptableObject
{
    [Header("Opening Butterfly Dialogue")]
    [Tooltip(
        "Butterfly dialogue played at the beginning of Page 2."
    )]
    public Page2DialogueLine[] introButterflyLines;

    [Header("Locked Gate Hint")]
    [Tooltip(
        "Lines shown after the player tries to open " +
        "the locked garden gate."
    )]
    public Page2DialogueLine[] lockedGateHintLines;

    [Header("Paw Trail")]
    [Tooltip(
        "Instruction shown before the player begins " +
        "following Lumi's paw prints."
    )]
    public Page2DialogueLine pawTrailHint;

    [Header("Key Reveal Hint")]
    [Tooltip(
        "Lines shown after the hidden Memory Key appears."
    )]
    public Page2DialogueLine[] keyRevealHintLines;

    [Header("Gate Opened Butterfly Dialogue")]
    [Tooltip(
        "Butterfly dialogue played after the garden gate opens."
    )]
    public Page2DialogueLine[] gateOpenedButterflyLines;
}