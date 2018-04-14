using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class ShotChargeIndicator : CircularIndicator {
    public override void Start() {
        base.Start();
        this.FrameDelayCall(Initialize);
    }

    // Find the Player component on the parent gameObject
    // Use the Player component to set
    // - the image fill color
    // - maxFillAmount == maxShotSpeed
    void Initialize() {
        var player = GetComponentInParent(typeof(Player)) as Player;
        player.CallAsSoonAsTeamAssigned(MatchTeamColor);
    }

    public void MatchTeamColor(TeamManager team) {
        fillColor = team.teamColor.color;
    }
}
