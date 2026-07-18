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
        text.text = content;

        if (string.IsNullOrEmpty(content) || characterSeconds <= 0f)
        {
            text.maxVisibleCharacters = int.MaxValue;
            yield break;
        }

        text.maxVisibleCharacters = 0;
        text.ForceMeshUpdate();

        int characterCount = text.textInfo.characterCount;
        for (int i = 1; i <= characterCount; i++)
        {
            text.maxVisibleCharacters = i;
            yield return new WaitForSeconds(characterSeconds);
        }

        text.maxVisibleCharacters = int.MaxValue;
    }

    public static float GetTypingDuration(string content, float characterSeconds)
    {
        if (string.IsNullOrEmpty(content) || characterSeconds <= 0f)
            return 0f;

        return content.Length * characterSeconds;
    }

    public static void SetImmediate(TMP_Text text, string content)
    {
        if (text == null)
            return;

        text.text = content ?? "";
        text.maxVisibleCharacters = int.MaxValue;
    }
}
