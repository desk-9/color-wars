using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using IC = InControl;

public class MenuController : MonoBehaviour {
    public IC.InputControlType StartButton = IC.InputControlType.Start;
    public IC.InputControlType ResetButton = IC.InputControlType.DPadDown;
    public GameObject pauseMenu = null;

    bool paused = false;

    void Start() {
        UnPause();
    }

    void Update () {
        var device = IC.InputManager.ActiveDevice;

        if (device.GetControl(StartButton).WasPressed) {
            TogglePause();
        }

        if (device.GetControl(ResetButton).WasPressed && IsPaused()) {
            ResetScene();
        }
    }

    public bool IsPaused() {
        return paused;
    }

    public void TogglePause() {
        if (paused) UnPause();
        else        Pause();
    }

    public void Pause() {
        paused         = true;
        Time.timeScale = 0.0f;
        pauseMenu.SetActive(true);
    }

    public void UnPause() {
        paused         = false;
        Time.timeScale = 1.0f;
        pauseMenu.SetActive(false);
    }

    public void ResetScene() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
