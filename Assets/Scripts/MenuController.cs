using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using UnityEngine.UI;

using IC = InControl;

public class MenuController : MonoBehaviour {
    IC.InputControlType StartButton = IC.InputControlType.Command;
    public IC.InputControlType ResetButton = IC.InputControlType.DPadDown;
    public IC.InputControlType MainMenuButton = IC.InputControlType.DPadUp;

    public GameObject pauseMenu;
    TransitionUtility.Panel pauseMenuPanel;
    float pauseBeforeWinDisplay = 3;
    float pauseTransitionDuration = 0.25f;

    public WinDisplay winDisplay;

    void Start() {
        if (winDisplay != null) {
            GameModel.instance.OnGameOver += () => {
                this.RealtimeDelayCall(winDisplay.GameOverFunction, pauseBeforeWinDisplay);
            };
        }
        if (pauseMenu != null) {
            pauseMenuPanel = new TransitionUtility.Panel(
                pauseMenu, pauseTransitionDuration);
        }
    }

    void Update () {
        if (TutorialLiveClips.runningLiveClips) {
            return;
        }
        var paused = SceneStateController.instance?.paused ?? false;
        var devicePressed = PlayerInputManager.instance.Any((device) => device.GetControl(ResetButton).WasPressed);
        if (paused && devicePressed) {
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
            AudioManager.instance.PauseSound.Play(1.0f);
            StartCoroutine(pauseMenuPanel.FadeIn());
            SceneStateController.instance.PauseTime();
        }
        else {
            SceneStateController.instance.UnPauseTime();
            AudioManager.instance.UnPauseSound.Play(2.5f);
            StartCoroutine(pauseMenuPanel.FadeOut());
        }
    }

}
