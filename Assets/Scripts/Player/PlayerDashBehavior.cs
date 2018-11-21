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

        GameManager.instance.notificationManager.CallOnMessageIfSameObject(
            Message.PlayerPressedDash, DashPressed, this.gameObject);
        GameManager.instance.notificationManager.CallOnMessageIfSameObject(
            Message.PlayerReleasedDash, ChargeReleased, this.gameObject);
    }

    private void Awake()
    {
        player = this.EnsureComponent<Player>();
    }

    public void SetPrefabColors()
    {
        if (player.team != null)
        {
            EffectSpawner chargeEffectSpawner = this.FindEffect(EffectType.DashCharge);
            dashEffectPrefab = player.team.resources.dashEffectPrefab;
            chargeEffectSpawner.effectPrefab = player.team.resources.dashChargeEffectPrefab;
            dashAimerPrefab = player.team.resources.dashAimerPrefab;
        }
    }

    private void DashPressed()
    {
        if (Time.time - lastDashTime >= cooldown)
        {
            stateManager.AttemptDashCharge(StartChargeDash, StopChargeDash);
        }
    }

    private void StartChargeDash()
    {
        chargeCoroutine = StartCoroutine(Charge());

        // Lock Player at current position when charging.
        playerMovement.FreezePlayer();

        dashAimer = Instantiate(dashAimerPrefab, transform.position, transform.rotation, transform);
    }

    private void StopChargeDash()
    {
        if (chargeCoroutine != null)
        {
            StopCoroutine(chargeCoroutine);
            chargeCoroutine = null;
            playerMovement.UnFreezePlayer();

            Destroy(dashAimer);
        }
    }

    private void ChargeReleased()
    {
        if (stateManager.IsInState(State.ChargeDash))
        {
            stateManager.AttemptDash(() => StartDash(chargeAmount), StopDash);
        }
    }

    private IEnumerator Charge()
    {
        float startChargeTime = Time.time;
        chargeAmount = 0.0f;

        while (true)
        {
            chargeAmount += chargeRate * Time.deltaTime;

            // Continue updating direction to indicate charge direction.
            playerMovement.RotatePlayer();

            yield return null;
        }
    }

    private void StartDash(float chargeAmount)
    {
        dashCoroutine = StartCoroutine(Dash(chargeAmount));
        lastDashTime = Time.time;
        if (tronMechanic.layWallOnDash)
        {
            tronMechanic.PlaceWallAnchor();
        }
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

    private IEnumerator Dash(float chargeAmount)
    {
        float dashDuration = Mathf.Min(chargeAmount, 0.5f);
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

        while (Time.time - startTime <= dashDuration)
        {
            rb.velocity = direction * dashSpeed * (1.0f + chargeAmount);

            yield return null;
        }

        foreach (ParticleSystem ps in dashEffect.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Stop();
        }

        stateManager.CurrentStateHasFinished();
    }

    private void Stun(Player otherPlayer)
    {
        PlayerStun otherStun = otherPlayer.GetComponent<PlayerStun>();
        PlayerStateManager otherStateManager = otherPlayer.GetComponent<PlayerStateManager>();
        if (otherStun != null && otherStateManager != null)
        {
            cameraShake.shakeAmount = stealShakeAmount;
            cameraShake.shakeDuration = stealShakeDuration;
            otherStateManager.AttemptStun(
                                          () => otherStun.StartStun(rb.velocity.normalized * stealKnockbackAmount, stealKnockbackLength),
                otherStun.StopStunned);
        }
    }

    private void StunAndSteal(GameObject otherGameObject)
    {
        bool hitBall = otherGameObject.GetComponent<Ball>() != null;
        Player otherPlayer = GetAssociatedPlayer(otherGameObject);
        if (otherPlayer != null &&
            (otherPlayer.team?.teamColor != player.team?.teamColor
             || otherPlayer.team == null || player.team == null))
        {
            Ball ball = otherPlayer.gameObject.GetComponent<BallCarrier>()?.Ball;

            bool shouldSteal = ball != null && (!onlyStealOnBallHit || hitBall);
            if (shouldSteal || (ball == null && !onlyStunBallCarriers))
            {
                Stun(otherPlayer);
            }

            if (shouldSteal)
            {
                GameManager.instance.notificationManager.NotifyMessage(Message.StolenFrom, otherPlayer.gameObject);
                AudioManager.instance.StealSound.Play(.5f);
                stateManager.AttemptPossession(
                    () => carrier.StartCarryingBall(ball), carrier.DropBall);
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
        if (!stateManager.IsInState(State.Dash))
        {
            return;
        }

        int layerMask = LayerMask.GetMask(stopDashOnCollisionWith);
        if (layerMask == (layerMask | 1 << other.layer))
        {
            this.TimeDelayCall(() =>
            {
                if (stateManager.IsInState(State.Dash))
                {
                    stateManager.CurrentStateHasFinished();
                }
            }, 0.1f);
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
