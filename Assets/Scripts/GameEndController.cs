using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IC = InControl;

public class GameEndController : MonoBehaviour {
    public WinDisplay win_display;
    public float win_display_time = 5f;

    public void GameOver(TeamManager winner) {
        GameModel.instance.scene_controller.PauseTime();
        win_display.gameObject.SetActive(true);
        win_display.SetWinner(winner);
        StartCoroutine(GameEndRoutine());
    }

    IEnumerator GameEndRoutine() {
        var end_time = Time.unscaledTime + win_display_time;
        while (Time.unscaledTime < end_time) {
            yield return null;
            win_display.SetRestartTime(end_time - Time.unscaledTime);
        }
        GameModel.instance.scene_controller.UnPauseTime();
        GameModel.instance.scene_controller.ResetScene();
    }
}
