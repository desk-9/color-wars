// All credit for this script goes to: https://gist.github.com/ftvs
using UnityEngine;
using UtilityExtensions;

public class CameraShakeManager : MonoBehaviour
{
    [SerializeField]
    private float stealShakeAmount = .7f;
    [SerializeField]
    private float stealShakeDuration = .05f;
    [SerializeField]
    private float GoalShakeAmount = 1.5f;
    [SerializeField]
    private float GoalShakeDuration = .4f;

    private Transform cameraTransform;

    /// <summary>
    /// How long the object should shake for.
    /// </summary>
    private float shakeDuration = 0f;

    /// <summary>
    /// Amplitude of the shake. A larger value shakes the camera harder.
    /// </summary>
    private float shakeAmount = 0.7f;

    private float decreaseFactor = 1.0f;
    private Vector3 originalPos;

    private void Awake()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
            cameraTransform.ThrowIfNull("Could not find camera transform in CameraShakeManager");
        }
    }

    private void Start()
    {
        GameManager.Instance.NotificationManager.CallOnMessage(Message.GoalScored, HandleGoalScored);
        GameManager.Instance.NotificationManager.CallOnMessage(Message.BallWasStolen, HandleStun);
    }

    private void HandleStun()
    {
        shakeAmount = stealShakeAmount;
        shakeDuration = stealShakeDuration;
    }

    private void HandleGoalScored()
    {
        shakeAmount = GoalShakeAmount;
        shakeDuration = GoalShakeDuration;
    }

    private void OnEnable()
    {
        originalPos = cameraTransform.localPosition;
    }

    private void Update()
    {
        if (shakeDuration > 0)
        {
            cameraTransform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;

            shakeDuration -= Time.deltaTime * decreaseFactor;
        }
        else
        {
            shakeDuration = 0f;
            cameraTransform.localPosition = originalPos;
        }
    }
}
