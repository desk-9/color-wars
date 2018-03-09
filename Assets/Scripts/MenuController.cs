using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using IC = InControl;

public class MenuController : MonoBehaviour {
    public IC.InputControlType StartButton = IC.InputControlType.Start;
    public IC.InputControlType ResetButton = IC.InputControlType.DPadDown;
    public GameObject pauseMenu;

    SceneStateController scene_controller;
    
    void Start() {
        scene_controller = GameModel.instance.scene_controller;
    }

    void Update () {
        foreach (var device in IC.InputManager.Devices) {
            if (device.GetControl(StartButton).WasPressed) {
                TogglePause();
                break;
            }

            if (device.GetControl(ResetButton).WasPressed && scene_controller.paused) {
                scene_controller.ResetScene();
                break;
            }
        }
    }

    public void TogglePause() {
        if (scene_controller.paused) UnPause();
        else                           Pause();
    }

    public void Pause() {
        pauseMenu.SetActive(true);
        scene_controller.PauseTime();
    }

    public void UnPause() {
        pauseMenu.SetActive(false);
        scene_controller.UnPauseTime();
    }
}
