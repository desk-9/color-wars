using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using IC = InControl;

public class ShootBallMechanic : MonoBehaviour {

    public IC.InputControlType shootButton = IC.InputControlType.Action1;
    public float forcedShotTime;
    public float baseShotSpeed = 1.0f;
    public float chargeRate = 1.0f;
    public float shotPower = 1.1f;
    public GameObject circularTimerPrefab;
    public Vector2 circleTimerScale;
    CircularTimer circularTimer;
    public bool seperatePassAndShoot = false;
    public float chargedBallPercent = 0.4f;
    public float chargedBallMassFactor = 1;
    public bool chargedBallStuns = false;

    // The below params are only relevant when [seperatePassAndShoot] is true
    public IC.InputControlType scoreGoalButton = IC.InputControlType.Action2;
    public IC.InputControlType juggleButton = IC.InputControlType.RightBumper;
    public float rotationLerpTime = .2f;
    // How much to lead your teammate by when you pass.
    public float passLeadMultiplier = 1f;
    public float juggleSpeed = 12f;
    public bool separateScoreGoalButton = true;
    public float aimAssistAngle = 7.5f;
    public float adaptiveAimAssist = 200f;

    public GameObject chargeEffect;

    PlayerMovement playerMovement;
    PlayerStateManager stateManager;
    Coroutine shootTimer;
    BallCarrier ballCarrier;
    GameObject effect;
    TeamManager team;
    Player player;
    Goal goal;
    Player teamMate;


    float shotSpeed = 1.0f;
    float elapsedTime = 0.0f;

    float maxShotSpeed;

    void Start() {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        ballCarrier = this.EnsureComponent<BallCarrier>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        player = this.EnsureComponent<Player>();
        goal = GameObject.FindObjectOfType<Goal>();
        stateManager.CallOnStateEnter(
                                      State.Posession, StartTimer);
        stateManager.CallOnStateExit(
            State.Posession, () => StopChargeShot());

        if (chargeEffect != null) {
            var ps = chargeEffect.GetComponent<ParticleSystem>();
            var main = ps.main;
            if (main.duration != forcedShotTime) {
                main.duration = forcedShotTime;
            }
        }
        circularTimer = Instantiate(
            circularTimerPrefab, transform).GetComponent<CircularTimer>();
        circularTimer.transform.localScale = circleTimerScale;

        ballCarrier.chargedBallStuns = chargedBallStuns;
        var ball = GameObject.FindObjectOfType<Ball>();
        if (ball != null) {
            ball.chargedMassFactor = chargedBallMassFactor;
        }
        maxShotSpeed = baseShotSpeed + Mathf.Pow((1 + forcedShotTime * chargeRate), shotPower);
    }

    Vector2 GetPositionInFrontOfTeammate(Rigidbody2D rb2d) {
        return (Vector2)rb2d.transform.position + rb2d.velocity * passLeadMultiplier;
    }

    void StartTimer() {
        if (seperatePassAndShoot) {
            shootTimer = StartCoroutine(ShootTimerSeperatePassShoot());
        } else {
            shootTimer = StartCoroutine(ShootTimer());
        }
    }

    GameObject GetTeammate() {
        var team = player.team;
        if (team == null) {
            return null;
        }
        foreach (var teammate in team.teamMembers) {
            if (teammate != player) {
                return teammate.gameObject;
            }
        }
        return null;
    }

    IEnumerator ShootTimerSeperatePassShoot() {
        // circularTimer?.StartTimer(forcedShotTime, delegate{});


        elapsedTime = 0.0f;
        shotSpeed = baseShotSpeed;

        while (elapsedTime < forcedShotTime) {
            elapsedTime += Time.deltaTime;
            var inputDevice = playerMovement.GetInputDevice();
            if (separateScoreGoalButton) {
                if (inputDevice.GetControl(scoreGoalButton).WasPressed) {
                    var goal = GameObject.FindObjectOfType<Goal>();
                    shootTimer = StartCoroutine(ChargeShot(scoreGoalButton,
                                                           goal.gameObject));
                    yield break;
                }
                if (inputDevice.GetControl(shootButton).WasPressed) {
                    shootTimer = StartCoroutine(ChargeShot(shootButton,
                                                           GetTeammate()));
                    yield break;
                }
                if (inputDevice.GetControl(juggleButton).WasPressed) {
                    shotSpeed = juggleSpeed;
                    Shoot();
                    yield break;
                }
            } else {
                if (inputDevice.GetControl(shootButton).WasPressed) {
                    shootTimer = StartCoroutine(ChargeShot(shootButton));
                    yield break;
                }
                if (inputDevice.GetControl(scoreGoalButton).WasPressed) {
                    shootTimer = StartCoroutine(ChargeShot(scoreGoalButton,
                                                           GetTeammate()));
                    yield break;
                }
            }
            yield return null;
        }
        Shoot();
    }

    IEnumerator ShootTimer() {
        // circularTimer?.StartTimer(forcedShotTime, delegate{});

        elapsedTime = 0.0f;
        shotSpeed = baseShotSpeed;
        while (elapsedTime < forcedShotTime) {
            elapsedTime += Time.deltaTime;
            var inputDevice = playerMovement.GetInputDevice();
            if (inputDevice.GetControl(shootButton).WasPressed) {
                shootTimer = StartCoroutine(ChargeShot(shootButton));
                yield break;
            }
            yield return null;
        }
        Utility.TutEvent("BallPickupTimeout", this);
        Shoot();
    }

    IEnumerator ChargeShot(IC.InputControlType button,
                           GameObject target = null) {
        effect = Instantiate(chargeEffect, transform.position, transform.rotation, transform);

        while (elapsedTime < forcedShotTime) {
            elapsedTime += Time.deltaTime;
            shotSpeed += chargeRate * Time.deltaTime;
            var inputDevice = playerMovement.GetInputDevice();
            if (inputDevice.GetControl(button).WasReleased) {
                shotSpeed = baseShotSpeed + Mathf.Pow(shotSpeed, shotPower);
                if (target == null) {
                    Shoot();
                    yield break;
                } else {
                    shootTimer = StartCoroutine(RotateTowardsTarget(target));
                    yield break;
                }
            }

            yield return null;
        }
        shotSpeed = baseShotSpeed + Mathf.Pow(shotSpeed, shotPower);
        Shoot();
    }

    void Update() {
        if (teamMate == null) {
            teamMate = GetTeammate()?.EnsureComponent<Player>();
        }
    }

    IEnumerator RotateTowardsTarget(GameObject target) {
        playerMovement.freezeRotation = true;
        Vector2 targetVector = Vector2.zero;
        if (target.GetComponent<Goal>() != null) {
            targetVector = (target.transform.position - transform.position).normalized;
        } else {
            var teammateRB2D = target.GetComponent<Rigidbody2D>();
            targetVector = (GetPositionInFrontOfTeammate(teammateRB2D) -
                            (Vector2)transform.position).normalized;
        }
        var rotationElapsedTime = 0f;
        var rb2d = this.EnsureComponent<Rigidbody2D>();
        while (rotationElapsedTime < rotationLerpTime) {
            var lerpedVector = Vector2.Lerp(transform.right, targetVector,
                                            rotationElapsedTime / rotationLerpTime);
            rb2d.rotation = Vector2.SignedAngle(Vector2.right, lerpedVector);
            rotationElapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        Shoot();
    }

    Vector3 GetShotDirection(Vector3 defaultAngle, out GameObject target) {
        var goalAngle = Mathf.Abs(Vector3.Angle(transform.right, goal.transform.position - transform.position));
        float teamMateAngle = 360f;
        if (teamMate != null) {
            teamMateAngle = Mathf.Abs(
                Vector3.Angle(transform.right,
                              teamMate.transform.position - transform.position));
        }
        if (Mathf.Min(goalAngle, teamMateAngle) < aimAssistAngle) {
            target = goalAngle < teamMateAngle ? goal.gameObject : teamMate.gameObject;
            return target.transform.position - transform.position;
        } else {
            target = null;
            return defaultAngle;
        }
    }

    void Shoot() {
        Utility.TutEvent("Shoot", this);
        shootTimer = null;
        playerMovement.freezeRotation = false;
        var ball = ballCarrier.ball;
        var shotDirection = ball.transform.position - transform.position;
        GameObject target;
        shotDirection = GetShotDirection(shotDirection, out target);

        if (target != null && adaptiveAimAssist > 0f) {
            ball.target = target;
            ball.adjustmentCoefficient = adaptiveAimAssist;
        }

        shootTimer = null;
        playerMovement.freezeRotation = false;
        var ballRigidBody = ball.EnsureComponent<Rigidbody2D>();
        if (shotSpeed / maxShotSpeed >= 0.45f) {
            Utility.TutEvent("ShootCharge", this);
        }
        if (shotSpeed / maxShotSpeed >= chargedBallPercent) {
            ball.charged = true;
        }
        ballRigidBody.velocity = shotDirection.normalized * shotSpeed;
        stateManager.CurrentStateHasFinished();
    }

    void StopChargeShot() {
        // circularTimer?.StopTimer();
        if (shootTimer != null) {
            StopCoroutine(shootTimer);
            shootTimer = null;
        }
        if (effect != null) {
            Destroy(effect);
        }
        playerMovement.freezeRotation = false;
    }

}
