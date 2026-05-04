using UnityEngine;
using UnityEngine.UI;


public class MuteAudio : MonoBehaviour {
    public Button muteButton;
    private bool muteOn = false;
    void Start(){
        LoadPlayerPreferences();
        SetButtonColor(muteButton, muteOn);
        
    }

    public void PressMute(){
        muteOn = !muteOn;
        AudioListener.pause = muteOn;
        SetButtonColor(muteButton, muteOn);
        SavePlayerPreferences();
    }

    public void SavePlayerPreferences()
    {
        int muteInt = muteOn ? 1 : 0;
        PlayerPrefs.SetInt("Mute Data", muteInt);
        PlayerPrefs.Save();
    }

    public void LoadPlayerPreferences(){
    if (PlayerPrefs.HasKey("Mute Data"))
    {
        int savedMuteInt = PlayerPrefs.GetInt("Mute Data");
        muteOn = savedMuteInt == 1;
        AudioListener.pause = muteOn;
    }
}
    void SetButtonColor(Button button, bool isActive)
    {
        Color color = isActive ? Color.green : Color.white;

        ColorBlock cb = button.colors;
        cb.normalColor = color;
        cb.highlightedColor = color;
        cb.pressedColor = color;
        cb.selectedColor = color;

        button.colors = cb;
    }

}
