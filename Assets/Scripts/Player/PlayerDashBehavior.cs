using System.Collections;
using UnityEngine;
using IC = InControl;
using UtilityExtensions;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDashBehavior : MonoBehaviour
{
    public GameObject dashEffectPrefab;
    public GameObject dashAimerPrefab;
    public IC.InputControlType dashButton = IC.InputControlType.Action2;
    public bool onlyStunBallCarriers = true;
    public bool onlyStealOnBallHit = false;
    public string[] stopDashOnCollisionWith;
    public float maxChargeTime = 1.0f;
    public float chargeRate = 1.0f;
    public float dashSpeed = 50.0f;
    public float cooldown = 0.5f;
    public float stealShakeAmount = .7f;
    public float stealShakeDuration = .05f;
    public float stealKnockbackAmount = 100f;
    public float stealKnockbackLength = .5f;
    public float wallHitStunTime = 0.05f;
    private PlayerStateManager stateManager;
    private PlayerMovement playerMovement;
    private Player player;
    private Rigidbody2D rb;
    private Coroutine chargeCoroutine;
    private Coroutine dashCoroutine;
    private PlayerTronMechanic tronMechanic;
    private BallCarrier carrier;
    private GameObject dashEffect;
    private GameObject dashAimer;
    private float lastDashTime;
    private CameraShake cameraShake;
    private float chargeAmount = 0;

    private void Start()
    {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        rb = this.EnsureComponent<Rigidbody2D>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        carrier = this.EnsureComponent<BallCarrier>();
        tronMechanic = this.EnsureComponent<PlayerTronMechanic>();
        cameraShake = GameObject.FindObjectOfType<CameraShake>();

        stateManager.OnStateChange += HandleNewPlayerState;

        GameManager.instance.NotificationManager.CallOnMessageIfSameObject(
            Message.PlayerPressedDash, DashButtonPressed, this.gameObject);
        GameManager.instance.NotificationManager.CallOnMessageIfSameObject(
            Message.PlayerReleasedDash, DeshButtonReleased, this.gameObject);
    }

    private void HandleNewPlayerState(State oldState, State newState)
    {
        // Cleanup for old state
        if (oldState == State.ChargeDash)
        {
            StopChargeDash();
        } else if (oldState == State.Dash)
        {
            StopDash();
        }

        // Handle new state
        if (newState == State.Dash)
        {
            if (oldState != State.ChargeDash)
            {
                Debug.LogError("Entered Dash state but previous state was not ChargeDash. How?!");
            }
            StartDash();
        } else if (newState == State.ChargeDash)
        {
            StartChargeDash();
        }
    }

    private void Awake()
    {
        player = this.EnsureComponent<Player>();
    }

    public void SetPrefabColors()
    {
        if (player.Team != null)
        {
            EffectSpawner chargeEffectSpawner = this.FindEffect(EffectType.DashCharge);
            dashEffectPrefab = player.Team.resources.dashEffectPrefab;
            chargeEffectSpawner.effectPrefab = player.Team.resources.dashChargeEffectPrefab;
            dashAimerPrefab = player.Team.resources.dashAimerPrefab;
        }
    }

    private void StartChargeDash()
    {
        chargeCoroutine = StartCoroutine(Charge());
        dashAimer = Instantiate(dashAimerPrefab, transform.position, transform.rotation, transform);
    }

    private void StopChargeDash()
    {
        if (chargeCoroutine != null)
        {
            StopCoroutine(chargeCoroutine);
            chargeCoroutine = null;
            Destroy(dashAimer);
        }
    }

    private void DashButtonPressed()
    {
        if (Time.time - lastDashTime >= cooldown && stateManager.CurrentState == State.NormalMovement)
        {
            stateManager.TransitionToState(State.ChargeDash);
        }
    }

    private void DeshButtonReleased()
    {
        if (stateManager.CurrentState == State.ChargeDash)
        {
            DashInformation info = stateManager.GetStateInformationForWriting<DashInformation>(State.Dash);
            info.StartPosition = playerMovement.CurrentPosition;
            info.Direction = (Quaternion.AngleAxis(rb.rotation, Vector3.forward) * Vector3.right);
            info.Strength = chargeAmount;
            stateManager.TransitionToState(State.Dash, info);
        }
    }

    private IEnumerator Charge()
    {
        float startChargeTime = Time.time;
        chargeAmount = 0.0f;

        while (true)
        {
            chargeAmount += chargeRate * Time.deltaTime;
            yield return null;
        }
    }

    private void StartDash()
    {
        dashCoroutine = StartCoroutine(Dash());
        lastDashTime = Time.time;
    }

    private void StopDash()
    {
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
            Destroy(dashEffect, 1.0f);
        }
    }

    private IEnumerator Dash()
    {
        // TODO anyone: This is where we could do something like handling turning off of the 
        // photon transform view component, since we know which way the ball will be heading for
        // a little bit.
        // TODO dkonik: Handle this stuff

        DashInformation information = stateManager.CurrentStateInformation as DashInformation;
        float dashDuration = Mathf.Min(information.Strength, 0.5f);
        AudioManager.instance.DashSound.Play();


        // Set duration of particle system for each dash trail.
        dashEffect = Instantiate(dashEffectPrefab, transform.position, transform.rotation, transform);

        foreach (ParticleSystem ps in dashEffect.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Stop();
            ParticleSystem.MainModule main = ps.main;
            main.duration = dashDuration;
            ps.Play();
        }

        Vector2 direction = (Vector2)(Quaternion.AngleAxis(rb.rotation, Vector3.forward) * Vector3.right);
        float startTime = Time.time;

        // TODO dkonik: Make sure to handle this properly with respect to PlayerMovement component
        while (Time.time - startTime <= dashDuration)
        {
            rb.velocity = direction * dashSpeed * (1.0f + chargeAmount);

            yield return null;
        }

        foreach (ParticleSystem ps in dashEffect.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Stop();
        }

        stateManager.TransitionToState(State.NormalMovement);
    }

    private Ball TrySteal(Player otherPlayer)
    {
        BallCarrier otherCarrier = otherPlayer.gameObject.GetComponent<BallCarrier>();
        return otherCarrier?.Ball;
    }

    private void Stun(Player otherPlayer)
    {
        cameraShake.shakeAmount = stealShakeAmount;
        cameraShake.shakeDuration = stealShakeDuration;
        otherPlayer.StateManager.StunNetworked(
            otherPlayer.PlayerMovement.CurrentPosition,
            playerMovement.CurrentVelocity.normalized * stealKnockbackAmount,
            stealKnockbackLength);
    }

    private void StunAndSteal(GameObject otherGameObject)
    {
        bool hitBall = otherGameObject.GetComponent<Ball>() != null;
        Player otherPlayer = GetAssociatedPlayer(otherGameObject);
        if (otherPlayer != null &&
            (otherPlayer.Team?.TeamColor != player.Team?.TeamColor
             || otherPlayer.Team == null || player.Team == null))
        {
            Ball ball = TrySteal(otherPlayer);

            bool shouldSteal = ball != null && (!onlyStealOnBallHit || hitBall);
            if (shouldSteal || (ball == null && !onlyStunBallCarriers))
            {
                Stun(otherPlayer);
            }

            if (shouldSteal)
            {
                // TODO dkonik: This stuff. COmmented out to make it compile
                //GameManager.instance.notificationManager.NotifyMessage(Message.StolenFrom, otherPlayer.gameObject);
                //AudioManager.instance.StealSound.Play(.5f);
                //StealBallInformation info = stateManager.GetStateInformationForWriting<StealBallInformation>();
                //stateManager.AttemptPossession(
                //    () => carrier.StartCarryingBall(ball), carrier.DropBall);
            }
        }
    }

    private Player GetAssociatedPlayer(GameObject gameObject)
    {
        Ball ball = gameObject.GetComponent<Ball>();
        if (ball != null)
        {
            return (ball.Owner == null) ? null : ball.Owner.GetComponent<Player>();
        }
        return gameObject.GetComponent<Player>();
    }

    public void OnTriggerEnter2D(Collider2D collider)
    {
        StunAndSteal(collider.gameObject);
    }

    public void OnTriggerStay2D(Collider2D collider)
    {
        StunAndSteal(collider.gameObject);
    }

    private void HandleCollision(GameObject other)
    {
        if (stateManager.CurrentState != State.Dash)
        {
            return;
        }

        int layerMask = LayerMask.GetMask(stopDashOnCollisionWith);
        if (layerMask == (layerMask | 1 << other.layer))
        {
            // TODO dkonik: We used to have a TimeDelayCall here...I'm not sure why
            // but make sure this works without it
            stateManager.TransitionToState(State.NormalMovement);
        }
        else
        {
            StunAndSteal(other);
        }

    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    public void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

}
