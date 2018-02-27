using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IC = InControl;

public class MenuController : MonoBehaviour {
    public IC.InputControlType StartButton = IC.InputControlType.Start;
    public IC.InputControlType ResetButton = IC.InputControlType.DPadDown;

    GameModel gameModel = null;
    PauseMenu pauseMenu = null;

    void Start() {
        var menu = GameObject.Find("PauseMenu");

        if (!menu) {
            Debug.LogWarningFormat(this, "{0}: Could not find 'PauseMenu' object! Disabling component.", name);
            enabled = false;
            return;
        }

        pauseMenu = menu.GetComponent<PauseMenu>();

        if (!pauseMenu) {
            Debug.LogErrorFormat(this, "{0}: PauseMenu is missing the PauseMenu component! Disabling component.", name);
            enabled = false;
            return;
        }

        var game = GameObject.Find("GameModel");

        if (!game) {
            Debug.LogWarningFormat(this, "{0}: Could not find 'GameModel' object! Disabling component.", name);
            enabled = false;
            return;
        }

        gameModel = game.GetComponent<GameModel>();

        if (!gameModel) {
            Debug.LogErrorFormat(this, "{0}: GameModel is missing the GameModel component! Disabling component.", name);
            enabled = false;
            return;
        }
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
