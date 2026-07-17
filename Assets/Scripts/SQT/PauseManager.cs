using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("Pause UI")]
    public GameObject pausePage;


    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pauseSound;
    public AudioClip resumeSound;


    private bool isPaused = false;


    void Start()
    {
        // 游戏开始时隐藏暂停页面
        pausePage.SetActive(false);

        // 确保时间正常
        Time.timeScale = 1;
    }


    // Pause Button 调用
    public void PauseGame()
    {
        if (isPaused)
            return;


        isPaused = true;


        // 播放暂停音效
        if (audioSource != null && pauseSound != null)
        {
            audioSource.PlayOneShot(pauseSound);
        }


        // 停止游戏时间
        Time.timeScale = 0;


        // 显示暂停页面
        pausePage.SetActive(true);


        Debug.Log("Game Paused");
    }



    // Resume Button 调用
    public void ResumeGame()
    {
        if (!isPaused)
            return;


        // 播放恢复音效
        if (audioSource != null && resumeSound != null)
        {
            audioSource.PlayOneShot(resumeSound);
        }


        // 恢复游戏时间
        Time.timeScale = 1;


        isPaused = false;


        // 隐藏暂停页面
        pausePage.SetActive(false);


        Debug.Log("Game Resumed");
    }



    // 防止切换Scene后保持暂停
    void OnDestroy()
    {
        Time.timeScale = 1;
    }
}