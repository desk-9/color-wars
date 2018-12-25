using Photon.Pun;
using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class PlayerStun : MonoBehaviour
{
    private Coroutine stunCoroutine;
    private PlayerStateManager playerStateManager;

    private void Start()
    {
        playerStateManager = this.EnsureComponent<PlayerStateManager>();
        playerStateManager.OnStateChange += HandleNewPlayerState;
    }

    private void HandleNewPlayerState(State oldState, State newState)
    {
        if (newState == State.Stun)
        {
            stunCoroutine = StartCoroutine(Stun());
        }

        if (oldState == State.Stun)
        {
            StopStunned();
        }
    }

    /// <summary>
    /// Manages the duration of the stun.
    /// NOTE: Does not handle the movement. PlayerMovement will take care of managing
    /// that while in the stun state
    /// </summary>
    /// <returns></returns>
    private IEnumerator Stun()
    {
        StunInformation info = playerStateManager.CurrentStateInformation as StunInformation;
        if (info == null)
        {
            Debug.LogError("Stun information was null in stun state");
        }

        if (info.StolenFrom)
        {
            GameManager.Instance.NotificationManager.NotifyMessage(Message.BallWasStolen, this);
        }

        float timeSinceCall = (float)(PhotonNetwork.Time - info.EventTimeStamp);

        if (timeSinceCall < info.Duration)
        {
            yield return new WaitForSeconds(info.Duration - timeSinceCall);
        } else
        {
            Debug.LogError("Entered stun after Duration. This shouldn't happen or should happen very rarely. Look into this");
        }
        
        playerStateManager.TransitionToState(State.NormalMovement);
    }

    public void StopStunned()
    {
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }
    }
}
