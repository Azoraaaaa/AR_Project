using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenu;
    public GameObject storySelection;
    public GameObject scanPage;


    // Start Button
    public void StartGame()
    {
        // Hide Main Menu
        mainMenu.SetActive(false);

        // Show Story Selection
        storySelection.SetActive(true);
    }


    // Story Button Click
    public void SelectStory()
    {
        // Hide Story Selection
        storySelection.SetActive(false);

        // Show Scan Page
        scanPage.SetActive(true);
    }
}