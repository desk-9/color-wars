using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

using IC = InControl;

public class MenuController : MonoBehaviour {
    public IC.InputControlType StartButton = IC.InputControlType.Start;
    public IC.InputControlType ResetButton = IC.InputControlType.DPadDown;
    public IC.InputControlType MainMenuButton = IC.InputControlType.DPadUp;
    public float delayBeforeWinDisplay = 5.0f;

    public GameObject pauseMenu;
    public WinDisplay winDisplay;

    void Start() {
        if (winDisplay != null) {
            GameModel.instance.OnGameOver += () => {
                this.TimeDelayCall(GameOverFunction, delayBeforeWinDisplay);
            };
        }
    }

    void GameOverFunction() {
        winDisplay.gameObject.SetActive(true);
        winDisplay.GameOverFunction();
    }

    void Update () {
        if (SceneStateController.instance.paused
            && PlayerInputManager.instance.Any((device)
                               => device.GetControl(ResetButton).WasPressed)) {
            SceneStateController.instance.ReloadScene();
            return;
        }

        // note: don't allow pausing if game is over.
        if (!GameModel.instance.gameOver
            && PlayerInputManager.instance.Any((device)
                            => device.GetControl(StartButton).WasPressed)) {
            TogglePause();
            return;
        }

        if ((SceneStateController.instance.paused || GameModel.instance.gameOver)
            && PlayerInputManager.instance.Any((device)
                            => device.GetControl(MainMenuButton).WasPressed)) {
            SceneStateController.instance.Load(Scene.MainMenu);
            return;
        }
    }


    public void TogglePause() {
        // Case: not paused now => toggling will pause
        if (!SceneStateController.instance.paused) {
            Debug.Log("Game paused");
            AudioManager.instance.PauseSound.Play(1.0f);
            pauseMenu.SetActive(true);
            SceneStateController.instance.PauseTime();
        }
        else {
            SceneStateController.instance.UnPauseTime();
            Debug.Log("Game un-paused");
            AudioManager.instance.UnPauseSound.Play(2.5f);
            pauseMenu.SetActive(false);
        }
    }

}
