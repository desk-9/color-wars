using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using System.Linq;

using Photon.Pun;
using Photon.Realtime;

public class PlayerTronMechanic : MonoBehaviourPunCallbacks
{
    public GameObject tronWall; // tronWallPrefab

    #region Stun times
    public float wallLayerStunTime = 2.5f;
    public float wallBreakerStunTime = 0.35f;
    #endregion

    #region Tweakables
    public float tronWallOffset = 2f;
    public float wallLifespan = 10f;
    public int maxNumWalls = 3;
    // Note: the maximum length of a tron wall is determined by
    // `maxLayingDurationPerWall` and `velocityWhileLaying`
    // (b/c velocity * time = distance)
    public float maxLayingDurationPerWall = 1f;
    public float layingSpeedMovementSpeedRatio;
    private float velocityWhileLaying
    {
        get
        {
            return playerMovement.movementSpeed * layingSpeedMovementSpeedRatio;
        }
        set
        {
            Debug.LogError("Someone tried to set derived property `velocityWhileLaying`!");
        }
    }
    #endregion

    #region Private references to other components on the Player
    private PlayerStateManager stateManager;
    private PlayerMovement playerMovement;
    private PlayerStun playerStun;
    private Player player;
    private Rigidbody2D rb;
    #endregion

    #region Private member vars
    private List<TronWall> walls = new List<TronWall>();
    private Coroutine layWallCoroutine;
    #endregion


    private void Start()
    {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        rb = this.EnsureComponent<Rigidbody2D>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        player = this.EnsureComponent<Player>();
        playerStun = this.EnsureComponent<PlayerStun>();
        GameManager.instance.notificationCenter.CallOnMessageIfSameObject(
            Message.PlayerPressedWall, WallPressed, gameObject);
        GameManager.instance.notificationCenter.CallOnMessageIfSameObject(
            Message.PlayerReleasedWall, StopLayingWallAndUpdateState, gameObject);

    }

    private void WallPressed()
    {
        if (player.team != null)
        {
            stateManager.AttemptLayTronWall(
                () => layWallCoroutine = StartCoroutine(LayTronWall()), StopLayingWall);
        }
    }

    private IEnumerator LayTronWall()
    {
        InitializeTronWall();

        rb.velocity = transform.right * velocityWhileLaying;
        rb.rotation = Vector2.SignedAngle(Vector2.right, transform.right);

        yield return null;
        float elapsedTime = 0f;
        while (elapsedTime < maxLayingDurationPerWall)
        {
            rb.velocity = transform.right * velocityWhileLaying;
            rb.rotation = Vector2.SignedAngle(Vector2.right, transform.right);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        StopLayingWallAndUpdateState();
    }

    public void InitializeTronWall()
    {
        GameObject newWall = GameObject.Instantiate(tronWall,
                                             transform.position - transform.right * tronWallOffset,
                                             transform.rotation);
        TronWall tronWallComponent = newWall.EnsureComponent<TronWall>();
        walls.Add(tronWallComponent);
        tronWallComponent.BeginConstruction(this, wallLifespan,
                                            player.team, tronWallOffset);
        EnforceMaxNumWalls();
    }

    private void EnforceMaxNumWalls() {
        if (walls.Count > maxNumWalls)
        {
            if (walls[0] != null)
            {
                // NOTE: TronWall's `Remove` function(s) take care of
                // deregistering the wall with the PlayerTronMechanic (i.e. this
                // component). So we're *not* removing the wall from `walls`
                // here.
                walls[0].LruRemove();
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (layWallCoroutine == null)
        {
            return;
        }
        TronWall currentWall = walls.Last(); // This shouldn't ever be null
        int layerMask = LayerMask.GetMask("Wall", "TronWall", "PlayerBlocker", "Goal", "PlayerBlocker");
        if (collision.gameObject != currentWall &&
            Utility.IsInLayer(collision.gameObject, layerMask))
        {
            StopLayingWall();
            stateManager.CurrentStateHasFinished();
        }
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

    private void StopLayingWallAndUpdateState()
    {
        if (stateManager.IsInState(State.LayTronWall))
        {
            if (layWallCoroutine != null)
            {
                StopCoroutine(layWallCoroutine);
            }
            layWallCoroutine = null;
            PlaceCurrentWall();
            stateManager.CurrentStateHasFinished();
        }
    }

    public void PlaceCurrentWall()
    {
        walls.Last().FinishConstruction();
    }

    #region Public methods called by TronWall
    public void StopWatching(TronWall wall)
    {
        if (!walls.Remove(wall)) {
            // wall not in `walls` => nothing to do
            return;
        }
        // I don't think this should actually ever run
        // if (!walls.Any() && layWallCoroutine != null)
        // {
        //     StopCoroutine(layWallCoroutine);
        //     layWallCoroutine = null;
        // }
    }

    // Collision while laying that stuns player
    public void HandleWallCollisionWhileLaying()
    {
        AudioManager.instance.StunPlayerWallBreak.Play(volume: 0.35f);
        stateManager.AttemptStun(() => playerStun.StartStun(Vector2.zero, wallLayerStunTime),
                                 () => playerStun.StopStunned());
    }
    #endregion

    private void OnDestroy()
    {
        KillAllWalls();
    }

    public void KillAllWalls()
    {
        foreach (TronWall wall in new List<TronWall>(walls))
        {
            wall.BreakWallAndRemove();
        }
    }

}
