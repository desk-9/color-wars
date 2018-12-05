﻿// using System.Collections;
// using UnityEngine;
// using UtilityExtensions;

// public class ShootBallMechanic : MonoBehaviour
// {
//     // These control how the charging progresses
//     public AnimationCurve chargeShotCurve;
//     public float shotSpeed { get; private set; }

//     private float baseShotSpeed = 1.0f;
//     private float maxShotSpeed = 56.0f;
//     private float maxChargeShotTime = 1.5f;
//     private float forcedShotTime = 3.75f;

//     private float elapsedTime = 0.0f;
//     private CircularTimer circularTimer;
//     private Coroutine shootTimer;
//     private ShotChargeIndicator shotChargeIndicator;
//     private Coroutine chargeShot;
//     private PlayerMovement playerMovement;
//     private PlayerStateManager stateManager;
//     private BallCarrier ballCarrier;
//     private Player teamMate;
//     private Player player;

//     private void Start()
//     {
//         shotSpeed = baseShotSpeed;
//         playerMovement = this.EnsureComponent<PlayerMovement>();
//         ballCarrier = this.EnsureComponent<BallCarrier>();
//         stateManager = this.EnsureComponent<PlayerStateManager>();
//         player = this.EnsureComponent<Player>();

//         // GameManager.instance.notificationManager.CallOnMessageIfSameObject(
//         //     Message.PlayerPressedShoot, OnShootPressed, gameObject);
//         // GameManager.instance.notificationManager.CallOnMessageIfSameObject(
//         //     Message.PlayerReleasedShoot, OnShootReleased, gameObject);

//         Ball ball = GameObject.FindObjectOfType<Ball>();

//         InitializeCircularIndicators(); // This is for team selection screen
//     }

//     private void HandleNewPlayerState(State oldState, State newState)
//     {
//         if (newState == State.Possession)
//         {
//             StartTimer();
//         }
//         if (oldState == State.Possession)
//         {
//             StopShootBallCoroutines();
//         }
//     }

//     // Initialize the circular timer (forced shot timeout) and the
//     // ShotChargeIndicator -- these are the little circular dials that show
//     // up when a player possesses the ball, and when a player charges their
//     // shot (respectively).
//     private void InitializeCircularIndicators(TeamManager team = null)
//     {
//         // Need to destroy preexisting objects (e.g. if selecting teams, and
//         // then switching team)
//         if (shotChargeIndicator != null)
//         {
//             Destroy(shotChargeIndicator);
//         }
//         if (circularTimer != null)
//         {
//             Destroy(circularTimer);
//         }

//         // Circular timer
//         GameObject circularTimerPrefab = GameManager.instance.neutralResources.circularTimerPrefab;
//         circularTimer = Instantiate(
//             circularTimerPrefab, transform).GetComponent<CircularTimer>();

//         // ShotCharge indicator
//         GameObject shotChargeIndicatorPrefab = GameManager.instance.neutralResources.shotChargeIndicatorPrefab;
//         shotChargeIndicator = Instantiate(
//             shotChargeIndicatorPrefab, transform).GetComponent<ShotChargeIndicator>();

//         shotChargeIndicator.minFillAmount = baseShotSpeed;
//         shotChargeIndicator.maxFillAmount = maxShotSpeed;
//     }

//     private void StartTimer()
//     {
//         bool shootTimerRunning = shootTimer != null;
//         bool alreadyChargingShot = chargeShot != null;
//         if (shootTimerRunning || alreadyChargingShot ||
//             !stateManager.IsInState(DEPRECATED_State.Posession))
//         {
//             return;
//         }
//         shootTimer = StartCoroutine(ShootTimer());
//     }

//     private IEnumerator ShootTimer()
//     {
//         circularTimer?.StartTimer(forcedShotTime, delegate { });
//         shotSpeed = baseShotSpeed;
//         elapsedTime = 0.0f;
//         while (elapsedTime < forcedShotTime)
//         {
//             elapsedTime += Time.deltaTime;
//             yield return null;
//         }
//         Shoot();
//     }

//     // Warning: this function may be called any time the player presses the A
//     // button (or whatever xbox controller button is bound to shoot). This
//     // includes dash
//     private void OnShootPressed()
//     {
//         bool shootTimerRunning = shootTimer != null;
//         bool alreadyChargingShot = chargeShot != null;
//         if (stateManager.IsInState(DEPRECATED_State.Posession)
//             && shootTimerRunning && !alreadyChargingShot)
//         {

//             shotChargeIndicator.Show();
//             chargeShot = StartCoroutine(TransitionUtility.LerpFloat((value) =>
//             {
//                 this.shotSpeed = value;
//                 shotChargeIndicator.FillAmount = value;
//             },
//                     startValue: baseShotSpeed, endValue: maxShotSpeed,
//                     duration: maxChargeShotTime,
//                     useGameTime: true, animationCurve: chargeShotCurve));
//         }
//     }

//     private void OnShootReleased()
//     {
//         bool shootTimerRunning = shootTimer != null;
//         bool alreadyChargingShot = chargeShot != null;
//         if (alreadyChargingShot)
//         {
//             Debug.Assert(shootTimerRunning == true);
//             Shoot();
//         }
//     }

//     public void Shoot()
//     {
//         AudioManager.instance.ShootBallSound.Play(.5f);
//         Ball ball = ballCarrier.ball;
//         if (ball != null)
//         {
//             Vector3 shotDirection = ball.transform.position - transform.position;
//             Rigidbody2D ballRigidBody = ball.EnsureComponent<Rigidbody2D>();
//             ballRigidBody.velocity = shotDirection.normalized * shotSpeed;
//         }
//         StopShootBallCoroutines();
//         // stateManager.CurrentStateHasFinished();
//     }

//     private void StopShootBallCoroutines()
//     {
//         if (shootTimer != null)
//         {
//             circularTimer?.StopTimer();
//             StopCoroutine(shootTimer);
//             shootTimer = null;
//         }
//         if (chargeShot != null)
//         {
//             shotChargeIndicator.Stop();
//             StopCoroutine(chargeShot);
//             chargeShot = null;
//         }

//         playerMovement.freezeRotation = false;
//     }

// }
