using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IC = InControl;
using UtilityExtensions;
using System.Linq;

public class PlayerTronMechanic : MonoBehaviour
{

    public GameObject tronWall;
    public IC.InputControlType tronButton = IC.InputControlType.Action2;
    public float wallLifeLength = 10f;
    public float lengthStunWhileLaying = 2.5f;
    public float layingSpeedMovementSpeedRatio;
    public float tronWallOffset = 2f;
    public int wallLimit = 3;
    public bool layWallOnDash = false;
    public float wallBreakerStunTime = .35f;
    public float tronWallLayingLimit = 1f;
    private PlayerStateManager stateManager;
    private PlayerMovement playerMovement;
    private PlayerStun playerStun;
    private Player player;
    private Rigidbody2D rb;
    private List<TronWall> walls = new List<TronWall>();
    private IC.InputDevice inputDevice;
    private float velocityWhileLaying;
    private Coroutine layWallCoroutine;


    // Use this for initialization
    private void Start()
    {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        rb = this.EnsureComponent<Rigidbody2D>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        player = this.EnsureComponent<Player>();
        playerStun = this.EnsureComponent<PlayerStun>();
        velocityWhileLaying = playerMovement.movementSpeed * layingSpeedMovementSpeedRatio;
        GameManager.instance.notificationManager.CallOnMessageIfSameObject(
            Message.PlayerPressedWall, WallPressed, gameObject);
        GameManager.instance.notificationManager.CallOnMessageIfSameObject(
            Message.PlayerReleasedWall, WallEnd, gameObject);

    }

    private void WallPressed()
    {
        if (player.team != null)
        {
            stateManager.AttemptLayTronWall(
                () => layWallCoroutine = StartCoroutine(LayTronWall()), StopLayingWall);
        }
    }

    public void PlaceWallAnchor()
    {
        if (walls.Count >= wallLimit)
        {
            if (walls[0] != null)
            {
                walls[0].PlayDestroyedParticleEffect();
                Destroy(walls[0].gameObject);
            }
            walls.RemoveAt(0);
        }

        GameObject newWall = GameObject.Instantiate(tronWall,
                                             transform.position - transform.right * tronWallOffset,
                                             transform.rotation);
        TronWall tronWallComponent = newWall.EnsureComponent<TronWall>();
        walls.Add(tronWallComponent);
        tronWallComponent.Initialize(this, wallLifeLength,
                                     player.team, tronWallOffset);
    }

    public void PlaceCurrentWall()
    {
        walls.Last().PlaceWall();
    }

    private void WallEnd()
    {
        if (stateManager.IsInState(OldState.LayTronWall))
        {
            if (layWallCoroutine != null)
            {
                StopCoroutine(layWallCoroutine);
            }
            layWallCoroutine = null;
            PlaceCurrentWall();
            // TODO dkonik: If another player destroys the tron wall that this player is laying,
            // this player may ignore that message if this fires before that destruction gets to their
            // client. So, that message should include the photon timestamp so that the player can check,
            // and even if they have transitioned to a new state (normal movement, or they even started charging
            // a dash or whatever, they'll properly transition to stun state.
            stateManager.TransitionToState(State.NormalMovement);
        }
    }

    private IEnumerator LayTronWall()
    {
        PlaceWallAnchor();

        rb.velocity = transform.right * velocityWhileLaying;
        rb.rotation = Vector2.SignedAngle(Vector2.right, transform.right);

        yield return null;
        float elapsedTime = 0f;
        while (elapsedTime < tronWallLayingLimit)
        {
            rb.velocity = transform.right * velocityWhileLaying;
            rb.rotation = Vector2.SignedAngle(Vector2.right, transform.right);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        WallEnd();
    }

    public void StopWatching(TronWall wall)
    {
        walls.Remove(wall);
        if (!walls.Any() && layWallCoroutine != null)
        {
            StopCoroutine(layWallCoroutine);
            layWallCoroutine = null;
        }
    }

    public void HandleWallCollision()
    {
        AudioManager.instance.StunPlayerWallBreak.Play(.35f);
        stateManager.AttemptStun(() => playerStun.StartStun(Vector2.zero, lengthStunWhileLaying),
                                 () => playerStun.StopStunned());
    }

    private void StopLayingWall()
    {
        if (layWallCoroutine != null)
        {
            StopCoroutine(layWallCoroutine);
            layWallCoroutine = null;
            PlaceCurrentWall();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (layWallCoroutine == null)
        {
            return;
        }
        TronWall currentWall = walls.Last(); // This shouldn't ever be null
        int layerMask = LayerMask.GetMask("Wall", "TronWall", "PlayerBlocker", "Goal");
        if (collision.gameObject != currentWall &&
            layerMask == (layerMask | (1 << collision.gameObject.layer)))
        {
            StopLayingWall();
            stateManager.TransitionToState(State.NormalMovement);
        }
    }

    private void OnDestroy()
    {
        foreach (TronWall wall in new List<TronWall>(walls))
        {
            wall.KillSelf();
        }
    }
}
