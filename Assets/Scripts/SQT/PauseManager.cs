using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("Pause UI")]
    public GameObject pausePage;


    [Header("Pause Audio Effect")]
    public AudioSource buttonAudioSource;
    public AudioClip pauseSound;
    public AudioClip resumeSound;


    [Header("All Game Audio Sources")]
    public AudioSource[] allAudioSources;


    private bool isPaused = false;


    void Start()
    {
        pausePage.SetActive(false);

        Time.timeScale = 1;
    }


    public void PauseGame()
    {
        if (isPaused)
            return;


        isPaused = true;


        // Button sound
        if (buttonAudioSource != null && pauseSound != null)
        {
            buttonAudioSource.PlayOneShot(pauseSound);
        }


        // Pause all audio
        foreach (AudioSource audio in allAudioSources)
        {
            if (audio != null && audio.isPlaying)
            {
                audio.Pause();
            }
        }


        // Pause animation and movement
        Time.timeScale = 0;


        pausePage.SetActive(true);


        Debug.Log("Game Paused");
    }



    public void ResumeGame()
    {
        if (!isPaused)
            return;


        // Resume sound
        if (buttonAudioSource != null && resumeSound != null)
        {
            buttonAudioSource.PlayOneShot(resumeSound);
        }


        // Resume all audio
        foreach (AudioSource audio in allAudioSources)
        {
            if (audio != null)
            {
                audio.UnPause();
            }
        }


        Time.timeScale = 1;


        isPaused = false;


        pausePage.SetActive(false);


        Debug.Log("Game Resumed");
    }



    void OnDestroy()
    {
        Time.timeScale = 1;
    }
}