using System;
using UnityEngine;

[Serializable]
public class Page1DialogueLine
{
    [TextArea(2, 6)]
    public string text;

    [Tooltip(
        "How long to wait after this line finishes typing."
    )]
    [Min(0f)]
    public float holdAfter = 2f;
}

[CreateAssetMenu(
    fileName = "Page1DialogueData",
    menuName = "Memory Garden/Page 1 Dialogue Data"
)]
public class Page1DialogueData : ScriptableObject
{
    [Header("Opening Narration")]
    public Page1DialogueLine[] introLines;

    [Header("Memory Hints")]
    [TextArea(2, 6)]
    public string ballMemoryHint;

    [TextArea(2, 6)]
    public string bedMemoryHint;

    [TextArea(2, 6)]
    public string collarMemoryHint;

    [TextArea(2, 6)]
    public string foodMemoryHint;

    [Header("After All Memories")]
    [TextArea(2, 6)]
    public string allMemoriesCompletedHint;

    [Header("Seed Narration")]
    [TextArea(2, 6)]
    public string seedRevealHint;

    [TextArea(2, 6)]
    public string seedFirstTapHint;

    [Header("Butterfly Dialogue")]
    public Page1DialogueLine[] butterflyLines;

    [Header("Seed Collection")]
    [TextArea(2, 6)]
    public string collectSeedHint;

    [TextArea(2, 6)]
    public string pageCompletedHint;
}