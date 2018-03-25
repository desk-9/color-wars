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
    public float wallBreakerStunTime = .5f;

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
        this.EnsureComponent<PlayerDashBehavior>().layWallOnDash = layWallOnDash;
    }

    // Update is called once per frame
    void Update () {
        if (inputDevice == null) {
            inputDevice = playerMovement.GetInputDevice();
            return;
        }

        if (inputDevice.GetControl(tronButton).WasPressed) {
            stateManager.AttemptLayTronWall(() => layWallCoroutine = StartCoroutine(LayTronWall())
                                            , StopLayingWall);
        }
    }

    public void PlaceWallAnchor() {
        if (walls.Count >= wallLimit) {
            Destroy(walls[0].gameObject);
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
    }

    IEnumerator LayTronWall() {
        PlaceWallAnchor();

        if (!layWallOnDash) {
            rb.velocity = transform.right * velocityWhileLaying;
        }

        yield return null;
        while (!inputDevice.GetControl(tronButton).WasReleased) {
            yield return null;
        }

        PlaceCurrentWall();
        layWallCoroutine = null;
        stateManager.CurrentStateHasFinished();
    }

    public void StopWatching(TronWall wall) {
        walls.Remove(wall);
    }

    public void HandleWallCollision() {
        stateManager.AttemptStun(() => playerStun.StartStun(Vector2.zero, lengthStunWhileLaying),
                                 () => playerStun.StopStunned());
    }

    void StopLayingWall() {
        if (layWallCoroutine != null) {
            StopCoroutine(layWallCoroutine);
            layWallCoroutine = null;
            Destroy(walls.Last().gameObject);
            walls.RemoveAt(walls.Count - 1);
        }
    }
}
