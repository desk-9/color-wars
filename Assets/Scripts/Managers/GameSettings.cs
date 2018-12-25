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
}
