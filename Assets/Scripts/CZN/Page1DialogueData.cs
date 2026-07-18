using System;
using UnityEngine;

[Serializable]
public class Page1DialogueLine
{
    [TextArea(2, 6)]
    [Tooltip("The dialogue or narration text.")]
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
    fileName = "Page1DialogueData",
    menuName = "Memory Garden/Page 1 Dialogue Data"
)]
public class Page1DialogueData : ScriptableObject
{
    [Header("Opening Narration")]
    [Tooltip(
        "Opening narration lines played at the beginning of Page 1."
    )]
    public Page1DialogueLine[] introLines;

    [Header("Memory Hints")]
    [Tooltip(
        "Text and optional voice used for the ball memory."
    )]
    public Page1DialogueLine ballMemoryHint;

    [Tooltip(
        "Text and optional voice used for the dog bed memory."
    )]
    public Page1DialogueLine bedMemoryHint;

    [Tooltip(
        "Text and optional voice used for the collar memory."
    )]
    public Page1DialogueLine collarMemoryHint;

    [Tooltip(
        "Text and optional voice used for the food bowl memory."
    )]
    public Page1DialogueLine foodMemoryHint;

    [Header("After All Memories")]
    [Tooltip(
        "Narration played after all four memories are completed."
    )]
    public Page1DialogueLine allMemoriesCompletedHint;

    [Header("Seed Narration")]
    [Tooltip(
        "Narration played when the Memory Seed appears."
    )]
    public Page1DialogueLine seedRevealHint;

    [Tooltip(
        "Narration played after the seed is selected " +
        "for the first time."
    )]
    public Page1DialogueLine seedFirstTapHint;

    [Header("Butterfly Dialogue")]
    [Tooltip(
        "Butterfly dialogue lines explaining the Memory Seed."
    )]
    public Page1DialogueLine[] butterflyLines;

    [Header("Seed Collection")]
    [Tooltip(
        "Instruction telling the player to collect the Memory Seed."
    )]
    public Page1DialogueLine collectSeedHint;

    [Tooltip(
        "Final Page 1 message shown after the seed is collected."
    )]
    public Page1DialogueLine pageCompletedHint;
}