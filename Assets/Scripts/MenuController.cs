using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

using IC = InControl;

public class MenuController : MonoBehaviour {
    public IC.InputControlType StartButton = IC.InputControlType.Start;
    public IC.InputControlType ResetButton = IC.InputControlType.DPadDown;
    public IC.InputControlType MainMenuButton = IC.InputControlType.DPadUp;

    public GameObject pauseMenu;
    public WinDisplay winDisplay;

    void Start() {
        if (winDisplay != null) {
            GameModel.instance.OnGameOver += () => winDisplay.gameObject.SetActive(true);
            GameModel.instance.OnGameOver += winDisplay.GameOverFunction;
        }
    }
    
    void Update () {

        if (SceneStateController.instance.paused
            && PlayerInputManager.instance.Any((device)
                               => device.GetControl(ResetButton).WasPressed)) {
            SceneStateController.instance.Load(Scene.Court);
            return;
        }
        
        if (PlayerInputManager.instance.Any((device)
                            => device.GetControl(StartButton).WasPressed)) {
            TogglePause();
            return;
        }

        if (PlayerInputManager.instance.Any((device)
                            => device.GetControl(MainMenuButton).WasPressed)) {
            SceneStateController.instance.Load(Scene.MainMenu);
            return;
        }
    }

    
    public void TogglePause() {
        SceneStateController.instance.TogglePauseTime();
        pauseMenu.SetActive(SceneStateController.instance.paused);
    }

}
