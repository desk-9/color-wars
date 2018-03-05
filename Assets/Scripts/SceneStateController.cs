using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneStateController : MonoBehaviour {
    public bool paused {get; private set;} = false;

    public bool IsPaused() {
        return paused;
    }

    public void TogglePauseTime() {
        paused = !paused;
        Time.timeScale = 1 - Time.timeScale;
    }

    public void PauseTime() {
        Time.timeScale = 0.0f;
        paused = true;
    }

    public void UnPauseTime() {
        Time.timeScale = 1.0f;
        paused = false;
    }

    public void ResetScene() {
        UnPauseTime();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
