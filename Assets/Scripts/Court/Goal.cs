using UnityEngine;
using UtilityExtensions;
using UnityEngine.UI;

public class Goal : MonoBehaviour
{
    public bool respondToSwitchColliders = false;

    [SerializeField]
    private GameObject playerBlocker;
    [SerializeField]
    private GameObject ballBlocker;
    [SerializeField]
    private SpriteRenderer fillRenderer;
    private Color originalColor;


    private void BlockBalls()
    {
        ballBlocker.SetActive(true);
    }

    private void OnlyBlockPlayers()
    {
        ballBlocker.SetActive(false);
    }

    private void ResetNeutral()
    {
        BlockBalls();
        fillRenderer.color = originalColor;
    }

    private void Start()
    {
        originalColor = fillRenderer.color;
        ResetNeutral();

        NotificationManager notificationManager = GameManager.NotificationManager;
        notificationManager.CallOnMessage(Message.ChargeChanged, HandleChargeChanged);
        notificationManager.CallOnMessage(Message.ResetAfterGoal, ResetNeutral);
    }

    private void HandleChargeChanged()
    {
        if (GameManager.PossessionManager.IsCharged)
        {
            OnlyBlockPlayers();
            if (fillRenderer != null)
            {
                fillRenderer.color = GameManager.PossessionManager.CurrentTeam.TeamColor;
            }
            AudioManager.instance.GoalSwitch.Play();
        } else
        {
            ResetNeutral();
        }
    }

    private void ScoreGoal()
    {
        TeamManager currentTeam = GameManager.PossessionManager.CurrentTeam;
        if (currentTeam != null)
        {
            GameManager.NotificationManager.NotifyMessage(Message.GoalScored, this);
            AudioManager.instance.ScoreGoalSound.Play(0.75f);
        } else
        {
            Debug.LogError("Team scored goal but PossessionManager.CurrentTeam is null");
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        // If we were hit by the ball, and it is charged, score a goal!
        if (collider.gameObject.GetComponent<Ball>() != null && 
            GameManager.PossessionManager.IsCharged)
        {
            ScoreGoal();

        }
    }
}
