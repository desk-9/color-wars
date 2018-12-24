using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles doing all the checks for whether or not a team has made a successful pass to
/// eachother, and is also the place to get the truth about who has possession of the ball
/// </summary>
public class PossessionManager : MonoBehaviour
{
    /// <summary>
    /// The player that is currently possessing the ball, if there is one
    /// </summary>
    public Player PossessingPlayer { get; set; }

    /// <summary>
    /// The team that has most recently touched the ball
    /// NOTE: Just because this is not null does not mean that this team
    /// has charged the ball
    /// </summary>
    public TeamManager CurrentTeam
    {
        get
        {
            if (PossessingPlayer != null)
            {
                return PossessingPlayer.Team;
            } else if (lastPlayerToPossessBall != null)
            {
                return lastPlayerToPossessBall.Team;
            } else
            {
                return null;
            }
        }
    }

    /// <summary>
    /// True if [CurrentTeam] has made a successful pass
    /// </summary>
    public bool IsCharged { get; set; }

    /// <summary>
    /// How close the player needs to be from the nullzone in order for them to be considered
    /// in the null zone
    /// </summary>
    private const float radiusForNullZoneCheck = 0.1f;

    private NotificationManager notificationManager;

    /// <summary>
    /// The last player to have possessed the ball. This will only be null at start
    /// </summary>
    private Player lastPlayerToPossessBall;

    void Start()
    {
        // Register callbacks
        notificationManager = GameManager.Instance.NotificationManager;
        notificationManager.CallOnStateStart(State.Possession, HandleNewPlayerPossession, true);
        notificationManager.CallOnStateEnd(State.Possession, HandlePlayerLostPossession, true);
        notificationManager.CallOnStateEnd(State.ChargeShot, HandlePlayerShotBall, true);
        notificationManager.CallOnMessage(Message.ResetAfterGoal, ResetValues);
        ResetValues();
    }

    private void ResetValues()
    {
        IsCharged = false;
        PossessingPlayer = null;
        lastPlayerToPossessBall = null;
    }

    private void HandleNewPlayerPossession(Player player)
    {
        PossessingPlayer = player;

        // At the beginning of the round, the last player is null, so just notify
        if (lastPlayerToPossessBall == null)
        {
            notificationManager.NotifyMessage(Message.ChargeChanged, this);
            notificationManager.NotifyMessage(Message.BallIsPossessed, player);
            return;
        }

        // If this is the same as the last player or we are already 
        // set to this team
        if (PossessingPlayer != lastPlayerToPossessBall &&
            !(lastPlayerToPossessBall.Team == PossessingPlayer.Team && IsCharged))
        {
            // If we passed to our teammate
            if (PossessingPlayer.Team == lastPlayerToPossessBall.Team)
            {
                if (PlayerInNullZone(player))
                {
                    // Blocked by the null zone
                    notificationManager.NotifyMessage(Message.NullChargePrevention, this);
                }
                else
                {
                    // We made a successful pass
                    IsCharged = true;
                    notificationManager.NotifyMessage(Message.ChargeChanged, this);
                }
            }
            else
            {
                // Opposing team got the ball
                bool oldCharged = IsCharged;
                IsCharged = false;
                if (oldCharged)
                {
                    notificationManager.NotifyMessage(Message.ChargeChanged, this);
                }

            }
        }

        notificationManager.NotifyMessage(Message.BallIsPossessed, player);
    }

    private void HandlePlayerLostPossession(Player player)
    {
        // The reason we check for player == PossessingPlayer is because we could
        // theoretically get the notification that a new player has possession before
        // we get notified that the old player lost the ball. This shouldn't happen,
        // but we may need to revisit this
        Debug.Assert(player == PossessingPlayer, "Player was not possessing player in PossessionManager");

        if (player.StateManager.CurrentState != State.ChargeShot)
        {
            DoPossessionLost(player);
        }
    }

    private void HandlePlayerShotBall(Player player)
    {
        // The reason we check for player == PossessingPlayer is because we could
        // theoretically get the notification that a new player has possession before
        // we get notified that the old player lost the ball. This shouldn't happen,
        // but we may need to revisit this
        Debug.Assert(player == PossessingPlayer, "Player was not possessing player in PossessionManager");
        if (player == PossessingPlayer)
        {
            DoPossessionLost(player);
        }
    }

    private void DoPossessionLost(Player player)
    {
        lastPlayerToPossessBall = player;
        PossessingPlayer = null;
        notificationManager.NotifyMessage(Message.BallIsUnpossessed, player);
    }

    private bool PlayerInNullZone(Player player)
    {
        Collider2D collider = Physics2D.OverlapCircle(
            player.transform.position, radiusForNullZoneCheck, LayerMask.GetMask("NullZone"));
        return collider != null;
    }
}
