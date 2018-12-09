using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class PlayerStun : MonoBehaviour
{
    public float stunTime = 5f;
    private Coroutine stunned;
    private PlayerStateManager playerStateManager;

    private void Start()
    {
        playerStateManager = this.EnsureComponent<PlayerStateManager>();
    }

    public void StartStun(Vector2? knockbackVelocity = null, float? length = null)
    {
        if (knockbackVelocity != null)
        {
            Rigidbody2D rigidbody = GetComponent<Rigidbody2D>();
            if (rigidbody != null)
            {
                this.FrameDelayCall(() => rigidbody.velocity = knockbackVelocity.Value);
            }
        }
        stunned = StartCoroutine(Stun(length));
    }

    private IEnumerator Stun(float? length = null)
    {
        if (length == null)
        {
            length = stunTime;
        }
        float endTime = Time.time + length.Value;
        while (Time.time < endTime)
        {
            yield return null;
        }
        playerStateManager.TransitionToState(State.NormalMovement);
    }

    public void StopStunned()
    {
        if (stunned != null)
        {
            StopCoroutine(stunned);
            stunned = null;
        }
    }
}
