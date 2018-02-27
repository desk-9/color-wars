using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour {
    bool paused = false;

    void Start() {
        UnPause();
    }

    public bool IsPaused() { return paused; }

    public void Pause() {
        paused         = true;
        Time.timeScale = 0.0f;
        gameObject.SetActive(paused);
    }

    public void UnPause() {
        paused         = false;
        Time.timeScale = 1.0f;
        gameObject.SetActive(paused);
    }

    public void TogglePause() {
        if (!paused) Pause();
        else         UnPause();
    }
}
