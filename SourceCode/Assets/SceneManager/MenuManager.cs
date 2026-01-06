using UnityEngine;


public class MenuManager : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject playMenu;
    public GameObject rulesMenu;
    public GameObject optionsMenu;

    void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainMenu.SetActive(true);
        playMenu.SetActive(false);
        rulesMenu.SetActive(false);
        optionsMenu.SetActive(false);
    }

    public void ShowPlayMenu()
    {
        mainMenu.SetActive(false);
        playMenu.SetActive(true);
        rulesMenu.SetActive(false);
        optionsMenu.SetActive(false);
    }

    public void ShowRulesMenu()
    {
        mainMenu.SetActive(false);
        playMenu.SetActive(false);
        rulesMenu.SetActive(true);
        optionsMenu.SetActive(false);
    }

    public void ShowOptionsMenu()
    {
        mainMenu.SetActive(false);
        playMenu.SetActive(false);
        rulesMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }
}
