using System;
using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class BallColorManager : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer outerRingRenderer;
    [SerializeField]
    private SpriteRenderer innerFillRenderer;
    [SerializeField]
    private Color neutralColor = Color.white;

    private TrailRenderer trailRenderer;
    private Coroutine delayedTrailEnable;

    // Use this for initialization
    private void Start()
    {
        outerRingRenderer.ThrowIfNull("No ring renderer on ball assigned");
        innerFillRenderer.ThrowIfNull("No fill renderer on ball assigned");

        trailRenderer = this.EnsureComponent<TrailRenderer>();

        GameManager.Instance.NotificationManager.CallOnMessage(
            Message.ChargeChanged, HandleChargeChanged
        );
        GameManager.Instance.NotificationManager.CallOnMessage(
            Message.BallWentOutOfBounds, ResetToNeutral
        );
        GameManager.Instance.NotificationManager.CallOnMessage(
            Message.ResetAfterGoal, ResetToNeutral
        );
        GameManager.Instance.NotificationManager.CallOnMessage(
            Message.GoalScored, HandleGoalScore
        );
    }

    private void HandleGoalScore()
    {
        trailRenderer.enabled = false;
    }

    private void ResetToNeutral()
    {
        trailRenderer.enabled = false;
        outerRingRenderer.color = neutralColor;
        innerFillRenderer.enabled = false;
    }

    private void HandleChargeChanged()
    {
        // TODO dkonik: This was in the old adjustSpriteToCurrentTeam function...
        // is it still needed?
        //// Happens if player shoots a frame after pickup
        //if (Owner == null)
        //{
        //    Debug.Assert(LastOwner != null);
        //    Color lastOwnerColor = ColorFromBallCarrier(LastOwner);
        //    bool fill = goal?.currentTeam != null && goal?.currentTeam.teamColor == lastOwnerColor;
        //    SetColor(lastOwnerColor, fill);
        //    return;
        //}

        TeamManager newTeam = GameManager.Instance.PossessionManager.CurrentTeam;
        if (newTeam == null)
        {
            throw new Exception("Would not expect the current team to be null in charge changed");
        }

        SetColor(
            GameManager.Instance.PossessionManager.CurrentTeam.TeamColor, 
            GameManager.Instance.PossessionManager.IsCharged
        );
    }

    private void SetColor(Color newColor, bool fill)
    {
        SetTrailRendererColor(newColor);

        if (fill)
        {
            outerRingRenderer.color = newColor;
            innerFillRenderer.enabled = true;
            innerFillRenderer.color = newColor;
        }
        else
        {
            outerRingRenderer.color = Color.Lerp(newColor, Color.white, .6f);
            innerFillRenderer.enabled = false;
        }
    }

    private void SetTrailRendererColor(Color newColor)
    {
        trailRenderer.Clear();
        Gradient gradient = trailRenderer.colorGradient;
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(newColor, 0.0f),
                new GradientColorKey(newColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0.0f),
                new GradientAlphaKey(0f, 1.0f)
            });

        trailRenderer.colorGradient = gradient;
    }
}
