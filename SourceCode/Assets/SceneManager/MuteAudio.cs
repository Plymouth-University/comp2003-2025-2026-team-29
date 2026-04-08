using UnityEngine;

public class MuteAudio : MonoBehaviour {
    private bool muteOn;
    void Start(){
        muteOn = false; 
    }
    public void PressMute(){
        muteOn = !muteOn;
        AudioListener.pause = muteOn;
    }
    
}
