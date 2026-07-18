using System.Collections;
using TMPro;
using UnityEngine;

public static class LwyTypewriterText
{
    public const float DefaultCharacterSeconds = 0.05f;

    public static IEnumerator TypeText(TMP_Text text, string content, float characterSeconds)
    {
        if (text == null)
            yield break;

        content = content ?? "";
        text.text = "";

        if (characterSeconds <= 0f)
        {
            text.text = content;
            yield break;
        }

        for (int i = 0; i < content.Length; i++)
        {
            text.text += content[i];
            yield return new WaitForSeconds(characterSeconds);
        }
    }

    public static float GetTypingDuration(string content, float characterSeconds)
    {
        if (string.IsNullOrEmpty(content) || characterSeconds <= 0f)
            return 0f;

        return content.Length * characterSeconds;
    }
}
