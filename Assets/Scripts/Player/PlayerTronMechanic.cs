using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IC = InControl;
using UtilityExtensions;
using System.Linq;

public class PlayerTronMechanic : MonoBehaviour
{
    [SerializeField]
    private float wallLifeLength = 4f;
    [SerializeField]
	private GameObject tronWallPrefab;
    [SerializeField]
	private IC.InputControlType tronButton = IC.InputControlType.Action2;
    [SerializeField]
	private float lengthStunWhileLaying = 1.5f;
    [SerializeField]
	private float layingSpeedMovementSpeedRatio = .79f;
    [SerializeField]
	private float tronWallOffset = 2.5f;
    [SerializeField]
	private int wallLimit = 2;
    [SerializeField]
	private float wallLayingDurationCap = 1f;
    [SerializeField]
    private float wallBreakSoundVolume = .35f;

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
        GameManager.instance.NotificationManager.CallOnMessageIfSameObject(
            Message.PlayerPressedWall, WallPressed, gameObject);
        GameManager.instance.NotificationManager.CallOnMessageIfSameObject(
            Message.PlayerReleasedWall, WallEnd, gameObject);

    }

    private void WallPressed()
    {
        if (player.Team != null)
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

        GameObject newWall = GameObject.Instantiate(tronWallPrefab,
                                             transform.position - transform.right * tronWallOffset,
                                             transform.rotation);
        TronWall tronWallComponent = newWall.EnsureComponent<TronWall>();
        walls.Add(tronWallComponent);
        tronWallComponent.Initialize(this, wallLifeLength,
                                     player.Team, tronWallOffset);
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
        while (elapsedTime < wallLayingDurationCap)
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
        AudioManager.instance.StunPlayerWallBreak.Play(wallBreakSoundVolume);
        stateManager.StunNetworked(playerMovement.CurrentPosition, Vector2.zero, lengthStunWhileLaying);
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
