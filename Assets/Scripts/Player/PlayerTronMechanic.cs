using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IC = InControl;
using UtilityExtensions;
using System.Linq;

public class PlayerTronMechanic : MonoBehaviour
{
    public static float layingSpeedMovementSpeedRatio = .79f;

    [SerializeField]
    private float wallLifeLength = 4f;
    [SerializeField]
	private GameObject tronWallPrefab;
    [SerializeField]
	private IC.InputControlType tronButton = IC.InputControlType.Action2;
    [SerializeField]
	private float lengthStunWhileLaying = 1.5f;
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
    private Player player;
    private List<TronWall> walls = new List<TronWall>();
    private Coroutine layWallCoroutine;


    // Use this for initialization
    private void Start()
    {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        player = this.EnsureComponent<Player>();
        GameManager.Instance.NotificationManager.CallOnMessageIfSameObject(
            Message.PlayerPressedWall, OnLayWallButtonPressed, gameObject);
        GameManager.Instance.NotificationManager.CallOnMessageIfSameObject(
            Message.PlayerReleasedWall, OnLayWallButtonReleased, gameObject);
        stateManager.OnStateChange += HandleNewPlayerState;
    }

    private void HandleNewPlayerState(State oldState, State newState)
    {
        if (newState == State.LayTronWall)
        {
            layWallCoroutine = StartCoroutine(LayTronWall());
        }

        if (oldState == State.LayTronWall)
        {
            // Only place the wall if we did not get stunned
            StopLayingWall(newState != State.Stun);
        }
    }

    private void OnLayWallButtonReleased()
    {
        if (stateManager.CurrentState == State.LayTronWall)
        {
            stateManager.TransitionToState(State.NormalMovement);
        }
    }

    private void OnLayWallButtonPressed()
    {
        if (player.Team != null && stateManager.CurrentState == State.NormalMovement)
        {
            TronWallInformation info = stateManager.GetStateInformationForWriting<TronWallInformation>(State.LayTronWall);
            info.Direction = playerMovement.Forward;
            info.StartPosition = playerMovement.CurrentPosition;
            stateManager.TransitionToState(State.LayTronWall, info);
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
                                             transform.position - (Vector3)playerMovement.Forward * tronWallOffset,
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

    private IEnumerator LayTronWall()
    {
        PlaceWallAnchor();

        yield return null;
        float elapsedTime = 0f;
        while (elapsedTime < wallLayingDurationCap)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        stateManager.TransitionToState(State.NormalMovement);
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
        stateManager.StunNetworked(playerMovement.CurrentPosition, Vector2.zero, lengthStunWhileLaying, false);
    }

    /// <summary>
    /// Stops laying the wall, if the player is in the process of laying one.
    /// </summary>
    /// <param name="placeWall">Specifies whether or not the player should place the wall (i.e. the wall
    /// laying process came to a natural end)</param>
    private void StopLayingWall(bool placeWall)
    {
        if (layWallCoroutine != null)
        {
            StopCoroutine(layWallCoroutine);
            layWallCoroutine = null;
            if (placeWall)
            {
                PlaceCurrentWall();
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (layWallCoroutine == null || stateManager.CurrentState != State.LayTronWall)
        {
            return;
        }
        TronWall currentWall = walls.Last(); // This shouldn't ever be null
        int layerMask = LayerMask.GetMask("Wall", "TronWall", "PlayerBlocker", "Goal");
        if (collision.gameObject != currentWall &&
            layerMask == (layerMask | (1 << collision.gameObject.layer)))
        {
            StopLayingWall(true);
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
