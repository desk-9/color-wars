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

    GameObject activeDisplay = null;
    PlayerInputManager playerInput;

    void Start() {
        SceneStateController.instance.OnEnter[Scene.Court] += ResetDisplays;
        SceneStateController.instance.OnEnter[Scene.Tutorial] += ResetDisplays;
        playerInput = GameModel.instance.EnsureComponent<PlayerInputManager>();
    }

    public void ResetDisplays() {
        pauseMenu.active = false;
        // winDisplay.SetEnabled(false);
        activeDisplay = null;
    }

    void Update () {

        if (SceneStateController.instance.paused
            && playerInput.Any((device)
                               => device.GetControl(ResetButton).WasPressed)) {
            SceneStateController.instance.Load(Scene.Court);
            return;
        }
        
        if (playerInput.Any((device)
                            => device.GetControl(StartButton).WasPressed)) {
            TogglePause();
            return;
        }
    }

    
    public void TogglePause() {
        Utility.Toggle(pauseMenu);
        // activeDisplay = pauseMenu;
        SceneStateController.instance.TogglePauseTime();
    }

}
