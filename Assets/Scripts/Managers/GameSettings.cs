using UnityEngine;

/// <summary>
/// Class which contains all of the "tweakables" for the game
/// </summary>
[System.Serializable]
public class GameSettings
{
    public bool PushAwayOtherPlayers = true;
    public float SlowMoFactor = 0.4f;
    public float PitchShiftTime = 0.3f;
    public float SlowedPitch = 0.5f;
    public bool RespectSoundEffectSlowMo = true;
    public int WinningScore = 4;
    public int RequiredWinMargin = 1;
    [Tooltip("The amount of time that the goal scoring team is allotted to move around after they scored")]
    public float PauseAfterGoalScore = 3f;
    [Tooltip("The length of the ball implosion animation")]
    public float LengthOfBallSpawnAnimation = 2f;
    public Color NeutralColor = Color.white;
}
