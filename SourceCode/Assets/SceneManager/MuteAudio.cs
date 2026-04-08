using UnityEngine;
using UnityEngine.UI;

public class MuteAudio : MonoBehaviour {
    public Button muteButton;
    private bool muteOn = false;
    void Start(){
        if (AudioListener.pause == true)
        {
            muteOn = true;
            SetButtonColor(muteButton, muteOn);
        }
    }

    public void PressMute(){
        muteOn = !muteOn;
        AudioListener.pause = muteOn;
        SetButtonColor(muteButton, muteOn);
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
