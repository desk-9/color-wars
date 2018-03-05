using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IC = InControl;

public class GameEndController : MonoBehaviour {
    public WinDisplay winDisplay;
    public float winDisplayTime = 5f;

    public void GameOver(TeamManager winner) {
        GameModel.instance.scene_controller.PauseTime();
        winDisplay.gameObject.SetActive(true);
        winDisplay.SetWinner(winner);
        StartCoroutine(GameEndRoutine());
    }

    IEnumerator GameEndRoutine() {
        var end_time = Time.unscaledTime + winDisplayTime;
        while (Time.unscaledTime < end_time) {
            yield return null;
            winDisplay.SetRestartTime(end_time - Time.unscaledTime);
        }
        GameModel.instance.scene_controller.UnPauseTime();
        GameModel.instance.scene_controller.ResetScene();
    }
}
