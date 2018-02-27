using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour {
    bool paused = false;

    void Start() {
        gameObject.SetActive(paused);
    }

    public bool IsPaused() { return paused; }

    public void TogglePause() {
        // Pause the game.
        if (!paused) {
            paused = true;

            gameObject.SetActive(paused);
            Time.timeScale = 0.0f;
        }

        // Unpause the game.
        else {
            paused = false;

            gameObject.SetActive(paused);
            Time.timeScale = 1.0f;
        }
    }
}
