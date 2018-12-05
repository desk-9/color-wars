using System.Collections;
using UnityEngine;
using UtilityExtensions;

using EM = EventsManager;
public class PlayerStun : MonoBehaviour
{
    private const float defaultStunTime = 5.0f;
    public float stunTime;
    private Coroutine stunned;
    private Player player;

    void Start() {
        player = GetComponent<Player>();
        stunTime = defaultStunTime;
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
        EM.RaiseOnPlayerStunned(player, new EM.PlayerArgs{});
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
        this.StopStunned();
    }

    public void StopStunned()
    {
        if (stunned != null)
        {
            StopCoroutine(stunned);
            stunned = null;
        }
        EM.RaiseOnPlayerUnstunned(player, new EM.PlayerArgs{});
    }
}
