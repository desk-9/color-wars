using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IC = InControl;

[RequireComponent(typeof(GameModel))]
public class MenuController : MonoBehaviour {
    public IC.InputControlType StartButton = IC.InputControlType.Start;
    public IC.InputControlType ResetButton = IC.InputControlType.DPadDown;
    public PauseMenu pauseMenu = null;

    GameModel gameModel = null;

    void Start() {
        gameModel = GetComponent<GameModel>();
    }

    void Update () {
        var device = IC.InputManager.ActiveDevice;

        if (device.GetControl(StartButton).WasPressed) {
            pauseMenu.TogglePause();
        }

        if (device.GetControl(ResetButton).WasPressed && pauseMenu.IsPaused()) {
            gameModel.ResetScene();
        }
    }
}
