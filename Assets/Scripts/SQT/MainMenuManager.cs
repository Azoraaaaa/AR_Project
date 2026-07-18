using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenu;
    public GameObject storySelection;
    public GameObject scanPage;


    [Header("Button Sound")]
    public AudioSource audioSource;
    public AudioClip clickSound;



    // Start Button
    public void StartGame()
    {
        PlayClickSound();


        // Hide Main Menu
        mainMenu.SetActive(false);


        // Show Story Selection
        storySelection.SetActive(true);
    }





    // Story Button Click
    public void SelectStory()
    {
        PlayClickSound();


        // Hide Story Selection
        storySelection.SetActive(false);


        // Show Scan Page
        scanPage.SetActive(true);
    }






    private void PlayClickSound()
    {
        if (audioSource != null &&
           clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}