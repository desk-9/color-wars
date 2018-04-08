using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class WinDisplay : MonoBehaviour {
    Text winnerText = null;
    Text restartText = null;
    Text restartCount = null;
    Text mainMenuInstructions = null;

    public AnimationCurve restartCountSize;
    float minRestartCountSize = 40;
    float maxRestartCountSize = 125;
    float restartCountDuration = 1.0f;
    float epsilon = 0.05f;

    TransitionUtility.Panel winDisplayPanel;
    float gameOverTransitionDuration = 1.0f;
    float delayBeforeResetCountdown = 0.25f;
    int SecondsBeforeReset = 10;

    void Awake () {
        FindTextObjects();
        
        winDisplayPanel = new TransitionUtility.Panel(
            this.gameObject, gameOverTransitionDuration);
        winDisplayPanel.MakeTransparent();
    }

    void FindTextObjects() {
        winnerText = winnerText ?? transform.FindComponent<Text>("WinnerText");
        restartText = restartText ?? transform.FindComponent<Text>("RestartText");
        restartCount = restartCount ?? transform.FindComponent<Text>("RestartCount");
        mainMenuInstructions = mainMenuInstructions ??
            transform.FindComponent<Text>("MainMenuInstructions");
    }

    public void GameOverFunction() {
        this.gameObject.SetActive(true);
        SetGameOverText();
        StartCoroutine(CoroutineUtility.RunThenCallback(
                           winDisplayPanel.FadeIn(),
                           () => this.TimeDelayCall(
                               StartCountdown, delayBeforeResetCountdown)));
    }

    void SetGameOverText() {
        FindTextObjects();

        var winner = GameModel.instance.winner;
        if (winner == null) {
            winnerText.text = "Tie!";
            winnerText.color = Color.black;
        } else {
            var otherTeam = new List<TeamManager>(GameModel.instance.teams).Find(team => team != winner);
            winnerText.text = string.Format("{0} Team won!", winner.teamColor.name);
            winnerText.color = winner.teamColor;
        }
    }

    void StartCountdown() {
        StartCoroutine(ResetCountdown());
    }

    IEnumerator ResetCountdown() {
        restartText.text = "Resetting in: ";
        for (int i = SecondsBeforeReset; i > 0; --i) {
            restartCount.text = i.ToString();
            StartCoroutine(
                TransitionUtility.LerpFloat(
                    (float value) => {
                        float scaledProgress = restartCountSize.Evaluate(value);
                        restartCount.fontSize = (int) Mathf.Lerp(
                            minRestartCountSize, maxRestartCountSize, scaledProgress);
                    },
                    0.0f, 1.0f,
                    restartCountDuration));
            yield return new WaitForSecondsRealtime(restartCountDuration + epsilon);
        }
        SceneStateController.instance.Load(Scene.Court);
    }

}
