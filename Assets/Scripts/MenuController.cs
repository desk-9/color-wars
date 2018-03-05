﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using IC = InControl;

public class MenuController : MonoBehaviour {
    public IC.InputControlType StartButton = IC.InputControlType.Start;
    public IC.InputControlType ResetButton = IC.InputControlType.DPadDown;
    public GameObject pauseMenu = null;

    SceneStateController scene_controller;
    
    void Start() {
        scene_controller = GameModel.instance.scene_controller;
    }

    void Update () {
        var device = IC.InputManager.ActiveDevice;

        if (device.GetControl(StartButton).WasPressed) {
            scene_controller.TogglePause();
        }

        if (device.GetControl(ResetButton).WasPressed && scene_controller.paused) {
            scene_controller.ResetScene();
        }
    }

    public void TogglePause() {
        if (scene_controller.paused) UnPause();
        else                           Pause();
    }

    public void Pause() {
        scene_controller.PauseTime();
        pauseMenu.SetActive(true);
    }

    public void UnPause() {
        scene_controller.UnPauseTime();
        pauseMenu.SetActive(false);
    }
}
