using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IC = InControl;
using UtilityExtensions;
using System.Linq;

public class PlayerTronMechanic : MonoBehaviour {

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

    PlayerStateManager stateManager;
    PlayerMovement     playerMovement;
    PlayerStun playerStun;
    Player player;
    Rigidbody2D        rb;
    List<TronWall> walls = new List<TronWall>();
    IC.InputDevice inputDevice;
    float velocityWhileLaying;
    Coroutine layWallCoroutine;


    // Use this for initialization
    void Start () {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        rb             = this.EnsureComponent<Rigidbody2D>();
        stateManager   = this.EnsureComponent<PlayerStateManager>();
        player         = this.EnsureComponent<Player>();
        playerStun = this.EnsureComponent<PlayerStun>();
        velocityWhileLaying = playerMovement.movementSpeed * layingSpeedMovementSpeedRatio;
        GameModel.instance.nc.CallOnMessageIfSameObject(
            Message.PlayerPressedWall, WallPressed, gameObject);
        GameModel.instance.nc.CallOnMessageIfSameObject(
            Message.PlayerReleasedWall, WallEnd, gameObject);

    }

    void WallPressed() {
        if (player.team != null) {
            stateManager.AttemptLayTronWall(
                () => layWallCoroutine = StartCoroutine(LayTronWall()), StopLayingWall);
        }
    }

    public void PlaceWallAnchor() {
        if (walls.Count >= wallLimit) {
            if (walls[0] != null) {
                walls[0].PlayDestroyedParticleEffect();
                Destroy(walls[0].gameObject);
            }
            walls.RemoveAt(0);
        }

        var newWall = GameObject.Instantiate(tronWall,
                                             transform.position - transform.right * tronWallOffset,
                                             transform.rotation);
        var tronWallComponent = newWall.EnsureComponent<TronWall>();
        walls.Add(tronWallComponent);
        tronWallComponent.Initialize(this, wallLifeLength,
                                     player.team, tronWallOffset);
    }

    public void PlaceCurrentWall() {
        walls.Last().PlaceWall();
        Utility.TutEvent("MakeWalls", this);
    }

    void WallEnd() {
        if (stateManager.IsInState(State.LayTronWall)) {
            if (layWallCoroutine != null) {
                StopCoroutine(layWallCoroutine);
            }
            layWallCoroutine = null;
            PlaceCurrentWall();
            stateManager.CurrentStateHasFinished();
        }
    }

    IEnumerator LayTronWall() {
        PlaceWallAnchor();

        if (!layWallOnDash) {
            rb.velocity = transform.right * velocityWhileLaying;
            rb.rotation = Vector2.SignedAngle(Vector2.right, transform.right);
        }

        yield return null;
        var elapsedTime = 0f;
        while (elapsedTime < tronWallLayingLimit) {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        WallEnd();
    }

    public void StopWatching(TronWall wall) {
        walls.Remove(wall);
        if (!walls.Any() && layWallCoroutine != null) {
            StopCoroutine(layWallCoroutine);
            layWallCoroutine = null;
        }
    }

    public void HandleWallCollision() {
        AudioManager.instance.StunPlayerWallBreak.Play(.35f);
        stateManager.AttemptStun(() => playerStun.StartStun(Vector2.zero, lengthStunWhileLaying),
                                 () => playerStun.StopStunned());
    }

    void StopLayingWall() {
        if (layWallCoroutine != null) {
            StopCoroutine(layWallCoroutine);
            layWallCoroutine = null;
            PlaceCurrentWall();
        }
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if (layWallCoroutine != null && collision.gameObject != gameObject) {
            StopLayingWall();
            stateManager.CurrentStateHasFinished();
        }
    }
}
