using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue")]
    public GameObject dialoguePanel;
    public TMP_Text[] dialogueTexts;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] dialogueAudios;

    [Header("Typing")]
    public float typingSpeed = 0.05f;

    [Header("After Dialogue")]
    public GameObject[] objectsToHide;
    public GameObject gameUI;

    void Start()
    {
        foreach (TMP_Text text in dialogueTexts)
        {
            text.gameObject.SetActive(false);
        }

        gameUI.SetActive(false);

        StartCoroutine(StartDialogue());
    }

    IEnumerator StartDialogue()
    {
        dialoguePanel.SetActive(true);

        for (int i = 0; i < dialogueTexts.Length; i++)
        {
            yield return StartCoroutine(PlayLine(i));
        }

        EndDialogue();
    }

    IEnumerator PlayLine(int index)
    {
        TMP_Text currentText = dialogueTexts[index];

        currentText.gameObject.SetActive(true);

        // 保存原文字
        string content = currentText.text;

        // 清空准备打字
        currentText.text = "";

        // 同时播放音频
        if (dialogueAudios.Length > index &&
            dialogueAudios[index] != null)
        {
            audioSource.clip = dialogueAudios[index];
            audioSource.Play();
        }

        // 开始打字
        yield return StartCoroutine(TypeText(currentText, content));

        // 有音频
        if (dialogueAudios.Length > index &&
            dialogueAudios[index] != null)
        {
            // 等待声音播放结束
            while (audioSource.isPlaying)
            {
                yield return null;
            }
        }
        else
        {
            // 没有音频
            yield return new WaitForSeconds(2f);
        }

        currentText.gameObject.SetActive(false);
    }

    IEnumerator TypeText(TMP_Text text, string content)
    {
        foreach (char c in content)
        {
            text.text += c;

            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);

        foreach (GameObject obj in objectsToHide)
        {
            obj.SetActive(false);
        }

        gameUI.SetActive(true);
    }
}