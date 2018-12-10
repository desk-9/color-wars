using UtilityExtensions;

public class ShotChargeIndicator : CircularIndicator
{
    public override void Start()
    {
        base.Start();
        this.FrameDelayCall(Initialize);
    }

    // Find the Player component on the parent gameObject
    // Use the Player component to set
    // - the image fill color
    // - maxFillAmount == maxShotSpeed
    private void Initialize()
    {
        Player player = GetComponentInParent(typeof(Player)) as Player;
        player.CallAsSoonAsTeamAssigned(MatchTeamColor);
    }

    public void MatchTeamColor(TeamManager team)
    {
        fillColor = team.TeamColor.color;
    }
}
