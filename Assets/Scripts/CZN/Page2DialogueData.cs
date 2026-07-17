using System;
using UnityEngine;

[Serializable]
public class Page2DialogueLine
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
    fileName = "Page2DialogueData",
    menuName = "Memory Garden/Page 2 Dialogue Data"
)]
public class Page2DialogueData : ScriptableObject
{
    [Header("Opening Butterfly Dialogue")]
    public Page2DialogueLine[] introButterflyLines;

    [Header("Locked Gate Hint")]
    public Page2DialogueLine[] lockedGateHintLines;

    [Header("Paw Trail")]
    [TextArea(2, 6)]
    public string pawTrailHint;

    [Header("Key Reveal Hint")]
    public Page2DialogueLine[] keyRevealHintLines;

    [Header("Gate Opened Butterfly Dialogue")]
    public Page2DialogueLine[] gateOpenedButterflyLines;
}