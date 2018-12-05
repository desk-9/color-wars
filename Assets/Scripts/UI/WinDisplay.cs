using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class WinDisplay : MonoBehaviour
{
    private Text winnerText = null;
    private Text restartText = null;
    private Text restartCount = null;
    private Text mainMenuInstructions = null;

    public AnimationCurve restartCountSize;
    public int SecondsBeforeReset = 10;
    private float minRestartCountSize = 40;
    private float maxRestartCountSize = 125;
    private float restartCountDuration = 1.0f;
    private float epsilon = 0.05f;
    private TransitionUtility.Panel winDisplayPanel;
    private float gameOverTransitionDuration = 1.0f;
    private float delayBeforeResetCountdown = 0.25f;

    private void Awake()
    {
        FindTextObjects();

        winDisplayPanel = new TransitionUtility.Panel(
            this.gameObject, gameOverTransitionDuration);
        winDisplayPanel.MakeTransparent();
    }

    private void FindTextObjects()
    {
        winnerText = winnerText ?? transform.FindComponent<Text>("WinnerText");
        restartText = restartText ?? transform.FindComponent<Text>("RestartText");
        restartCount = restartCount ?? transform.FindComponent<Text>("RestartCount");
        mainMenuInstructions = mainMenuInstructions ??
            transform.FindComponent<Text>("MainMenuInstructions");
    }

    public void GameOverFunction()
    {
        this.gameObject.SetActive(true);
        SetGameOverText();
        StartCoroutine(CoroutineUtility.RunThenCallback(
                           winDisplayPanel.FadeIn(),
                           () => this.TimeDelayCall(
                               StartCountdown, delayBeforeResetCountdown)));
    }

    private void SetGameOverText()
    {
        FindTextObjects();

        TeamManager winner = GameManager.instance.winner;
        if (winner == null)
        {
            winnerText.text = "Tie!";
            winnerText.color = Color.black;
        }
        else
        {
            winnerText.text = string.Format("{0} Team won!", winner.color.name);
            winnerText.color = winner.color;
        }
    }

    private void StartCountdown()
    {
        GameManager.instance.notificationManager.CallOnMessage(
            Message.PlayerPressedX, () => SceneStateManager.instance.ReloadScene());
        GameManager.instance.notificationManager.CallOnMessage(
            Message.PlayerPressedY, () => SceneStateManager.instance.Load(Scene.Selection));
        StartCoroutine(ResetCountdown());
    }

    private IEnumerator ResetCountdown()
    {
        restartText.text = "Resetting in: ";
        for (int i = SecondsBeforeReset; i > 0; --i)
        {
            restartCount.text = i.ToString();
            StartCoroutine(
                TransitionUtility.LerpFloat(
                    (float value) =>
                    {
                        float scaledProgress = restartCountSize.Evaluate(value);
                        restartCount.fontSize = (int)Mathf.Lerp(
                            minRestartCountSize, maxRestartCountSize, scaledProgress);
                    },
                    0.0f, 1.0f,
                    restartCountDuration));
            yield return new WaitForSecondsRealtime(restartCountDuration + epsilon);
        }
        SceneStateManager.instance.Load(Scene.Court);
    }

}
