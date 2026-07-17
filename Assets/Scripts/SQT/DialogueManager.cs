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

    // Leaf出现音效
    public AudioClip leafAppearSound;


    [Header("Typing")]
    public float typingSpeed = 0.05f;


    [Header("After Dialogue")]
    public GameObject[] objectsToHide;
    public GameObject gameUI;


    [Header("Props Settings")]
    public GameObject propsBar;


    // 最后一句话播完后需要出现的 Leaf Object
    public GameObject leafObject;



    private int currentDialogueIndex = 0;

    private bool isPlaying = false;



    void Start()
    {
        foreach (TMP_Text text in dialogueTexts)
        {
            if (text != null)
            {
                text.gameObject.SetActive(false);
            }
        }


        if (gameUI != null)
        {
            gameUI.SetActive(false);
        }


        if (propsBar != null)
        {
            propsBar.SetActive(false);
        }


        // 开始隐藏Leaf
        if (leafObject != null)
        {
            leafObject.SetActive(false);
        }


        StartCoroutine(StartDialogue());
    }



    IEnumerator StartDialogue()
    {
        if (isPlaying)
            yield break;


        isPlaying = true;


        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }


        if (propsBar != null)
        {
            propsBar.SetActive(false);
        }



        while (currentDialogueIndex < dialogueTexts.Length)
        {
            yield return StartCoroutine(
                PlayLine(currentDialogueIndex)
            );


            currentDialogueIndex++;



            // Element 4结束
            if (currentDialogueIndex == 5)
            {
                isPlaying = false;


                if (dialoguePanel != null)
                {
                    dialoguePanel.SetActive(false);
                }


                if (gameUI != null)
                {
                    gameUI.SetActive(true);
                }


                if (propsBar != null)
                {
                    propsBar.SetActive(true);
                }


                HideTargetObjects();


                Debug.Log("【第一阶段 0-4 播完】已隐藏对话框并开启交互。");


                yield break;
            }
        }



        EndDialogueFreeze();


        isPlaying = false;
    }



    IEnumerator PlayLine(int index)
    {
        foreach (TMP_Text text in dialogueTexts)
        {
            if (text != null)
            {
                text.gameObject.SetActive(false);
            }
        }



        TMP_Text currentText = dialogueTexts[index];


        currentText.gameObject.SetActive(true);



        string content = currentText.text;


        currentText.text = "";



        // 播放对白音效
        if (dialogueAudios.Length > index &&
            dialogueAudios[index] != null)
        {
            audioSource.clip = dialogueAudios[index];
            audioSource.Play();
        }



        yield return StartCoroutine(
            TypeText(currentText, content)
        );



        if (dialogueAudios.Length > index &&
            dialogueAudios[index] != null)
        {
            while (audioSource.isPlaying)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }



        if (index == dialogueTexts.Length - 1)
        {
            Debug.Log("最后一句播放完毕，保持文字显示状态。");
        }
        else
        {
            currentText.gameObject.SetActive(false);
        }
    }




    IEnumerator TypeText(TMP_Text text, string content)
    {
        foreach (char c in content)
        {
            text.text += c;

            yield return new WaitForSeconds(typingSpeed);
        }
    }





    void HideTargetObjects()
    {
        if (objectsToHide != null)
        {
            foreach (GameObject obj in objectsToHide)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
    }





    void EndDialogueFreeze()
    {
        HideTargetObjects();



        // 显示PropsBar
        if (propsBar != null)
        {
            propsBar.SetActive(true);
        }



        // 显示Leaf
        if (leafObject != null)
        {
            leafObject.SetActive(true);



            // 播放Leaf出现音效
            if (audioSource != null &&
               leafAppearSound != null)
            {
                audioSource.PlayOneShot(leafAppearSound);

                Debug.Log("🍃 Leaf appear sound played");
            }



            Debug.Log(
                "【全剧终定格】PropsBar 与 Leaf Object 已成功显现！"
            );
        }



        Debug.Log(
            "Dialogue finished and frozen. Final index: "
            + (currentDialogueIndex - 1)
        );
    }





    public void ResumeDialogue()
    {
        Debug.Log(
            "Resume Dialogue from element : "
            + currentDialogueIndex
        );


        if (currentDialogueIndex >= dialogueTexts.Length)
        {
            Debug.Log("No more dialogue.");
            return;
        }


        StartCoroutine(StartDialogue());
    }
}