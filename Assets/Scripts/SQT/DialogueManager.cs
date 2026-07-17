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

    [Header("Props Settings")]
    public GameObject propsBar;

    // 最后一句话播完后需要出现的 Leaf Object
    public GameObject leafObject;


    // 当前播放位置
    private int currentDialogueIndex = 0;


    // 防止重复播放
    private bool isPlaying = false;


    void Start()
    {
        // 开始时关闭所有文字
        foreach (TMP_Text text in dialogueTexts)
        {
            if (text != null)
            {
                text.gameObject.SetActive(false);
            }
        }


        // 游戏UI关闭
        if (gameUI != null)
        {
            gameUI.SetActive(false);
        }

        // 一开始播放对话，把 PropsBar 隐藏掉
        if (propsBar != null)
        {
            propsBar.SetActive(false);
        }

        // 游戏刚开始时，确保叶子物体也是隐藏的
        if (leafObject != null)
        {
            leafObject.SetActive(false);
        }


        // 播放前面的故事 element0开始
        StartCoroutine(StartDialogue());
    }


    IEnumerator StartDialogue()
    {
        if (isPlaying)
            yield break;

        isPlaying = true;

        // 打开Dialogue Panel
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        // 确保只要进入对话状态，PropsBar 就是隐藏的
        if (propsBar != null)
        {
            propsBar.SetActive(false);
        }


        // 从当前index继续
        while (currentDialogueIndex < dialogueTexts.Length)
        {
            yield return StartCoroutine(
                PlayLine(currentDialogueIndex)
            );

            currentDialogueIndex++;

            // =========================================================
            // 当播完 Element 4 时（此时 index 被自增成了 5）主动截断
            // =========================================================
            if (currentDialogueIndex == 5)
            {
                isPlaying = false;

                // 1. 隐藏自己（Dialogue Panel）
                if (dialoguePanel != null)
                {
                    dialoguePanel.SetActive(false);
                }

                // 2. 显示游戏UI（Game Canvas），让玩家开始进行卡牌交互
                if (gameUI != null)
                {
                    gameUI.SetActive(true);
                }

                // 3. 把 PropsBar 重新显示出来供玩家交互
                if (propsBar != null)
                {
                    propsBar.SetActive(true);
                }

                // 触发第一阶段的隐藏物体
                HideTargetObjects();

                Debug.Log("【第一阶段 0-4 播完】已隐藏对话框并开启交互。");

                yield break;
            }
        }


        // =========================================================
        // ✨【核心改动】所有对话播完（包括 5-7 阶段），进入定格收尾
        // =========================================================
        EndDialogueFreeze();

        isPlaying = false;
    }


    IEnumerator PlayLine(int index)
    {
        // 防止之前文字残留
        foreach (TMP_Text text in dialogueTexts)
        {
            if (text != null)
            {
                text.gameObject.SetActive(false);
            }
        }

        TMP_Text currentText = dialogueTexts[index];

        // 激活当前element
        currentText.gameObject.SetActive(true);

        // 保存原文本
        string content = currentText.text;

        // 清空文字准备打字
        currentText.text = "";

        // 播放声音
        if (dialogueAudios.Length > index && dialogueAudios[index] != null)
        {
            audioSource.clip = dialogueAudios[index];
            audioSource.Play();
        }

        // 打字效果
        yield return StartCoroutine(TypeText(currentText, content));

        // 等待语音结束
        if (dialogueAudios.Length > index && dialogueAudios[index] != null)
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

        // =========================================================
        // ✨【核心修复】如果是整套对话的最后一句（Element 7），跳过隐藏逻辑！
        // 让最后一句文字继续钉在屏幕上不消失
        // =========================================================
        if (index == dialogueTexts.Length - 1)
        {
            Debug.Log("最后一句播放完毕，保持文字显示状态。");
        }
        else
        {
            // 不是最后一句时，正常隐藏当前文字，准备下一句
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


    // =========================================================
    // ✨【新版收尾函数】最后一句话（Element 7）播完后，UI停止不动并定格
    // =========================================================
    void EndDialogueFreeze()
    {
        // 【保持不动】注销了原本关闭 dialoguePanel 的代码，使其继续保留在屏幕上
        // if (dialoguePanel != null) { dialoguePanel.SetActive(false); }

        // 1. 彻底完结时确保隐藏指定需要消失的物体
        HideTargetObjects();

    

        // 3. 最后一句话显示完毕后，让 PropsBar 显现
        if (propsBar != null)
        {
            propsBar.SetActive(true);
        }

        // 4. 最后一句话显示完毕后，让特定的 Leaf Object 显现
        if (leafObject != null)
        {
            leafObject.SetActive(true);
            Debug.Log("【全剧终定格】UI保持原样，PropsBar 与 Leaf Object 已成功显现！");
        }

        Debug.Log("Dialogue finished and frozen. Final index: " + (currentDialogueIndex - 1));
    }


    // =====================================
    // 外部调用（LifeCycleManager调用这里）
    // =====================================
    public void ResumeDialogue()
    {
        Debug.Log("Resume Dialogue from element : " + currentDialogueIndex);

        if (currentDialogueIndex >= dialogueTexts.Length)
        {
            Debug.Log("No more dialogue.");
            return;
        }

        StartCoroutine(StartDialogue());
    }
}