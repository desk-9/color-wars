using System.Collections;
using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using UtilityExtensions;

public delegate IEnumerator Routine();


public class PlayerPuppet : MonoBehaviour
{
    public static bool puppetsPause = false;
    public enum PuppetProgram
    {
        ShortForward,
        PlayRecording
    }

    public PuppetProgram program;
    public float defaultMovementSpeed = 30;
    public string recordingName = "test";
    public bool doPuppeting = true;
    public bool doLoop = false;
    private const string recordingDirectory = "Recordings";
    private Coroutine inputControlRoutine;
    private new Rigidbody2D rigidbody;
    private bool recordingFinishedThisFrame = false;

    private void Start()
    {
        rigidbody = this.EnsureComponent<Rigidbody2D>();
        if (!doPuppeting)
        {
            return;
        }
        GameManager.instance.notificationManager.CallOnMessage(
            Message.RecordingFinished,
            () =>
            {
                recordingFinishedThisFrame = true;
                if (this != null)
                {
                    this.FrameDelayCall(() =>
                    {
                        if (this != null)
                        {
                            this.recordingFinishedThisFrame = false;
                        }
                    });
                }
            });
        this.FrameDelayCall(() => StartProgram(program));
    }

    private InputRecording LoadRecording(string name)
    {
        string path = Path.Combine(
            Application.streamingAssetsPath, recordingDirectory, name + ".inputRecord");
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream file = File.Open(path, FileMode.Open);
        InputRecording record = (InputRecording)formatter.Deserialize(file);
        file.Close();
        return record;
    }

    private IEnumerator PlayRecording(InputRecording record, bool loop = false)
    {
        while (true)
        {
            transform.position = new Vector2(record.startX, record.startY);
            rigidbody.rotation = record.startRotation;
            foreach (InputFrame frame in record.recording)
            {
                if (this.gameObject == null)
                {
                    yield break;
                }

                while (puppetsPause)
                {
                    PlayerControls.SendInputEvents(
                        0, 0, false, false, false, false, this.gameObject);
                    yield return new WaitForFixedUpdate();
                }
                rigidbody.position = new Vector2(frame.positionX, frame.positionY);
                rigidbody.rotation = frame.rotation;
                PlayerControls.SendInputEvents(
                    0, 0, frame.APressed, frame.AReleased,
                    frame.BPressed, frame.BReleased, this.gameObject);
                if (frame.Interrupt)
                {
                    Debug.LogWarning("Interrupt! Text will change");

                    GameManager.instance.notificationManager.NotifyMessage(
                        Message.RecordingInterrupt, this.gameObject);

                }
                yield return null;

            }
            if (!recordingFinishedThisFrame)
            {
                GameManager.instance.notificationManager.NotifyMessage(
                    Message.RecordingFinished, this.gameObject);
            }
            if (!loop)
            {
                break;
            }
        }
    }

    private void StartProgram(PuppetProgram program)
    {
        string programName = program.ToString();
        System.Reflection.MethodInfo methodInfo = this.GetType().GetMethod(programName);
        if (methodInfo != null)
        {
            IEnumerator routine = methodInfo.Invoke(this, new object[0]) as IEnumerator;
            if (routine != null)
            {
                StartCoroutine(routine);
                return;
            }
        }
        Debug.LogError(string.Format("No such puppet program: '{0}'", programName));
    }

    private void StartMovementControl()
    {
        inputControlRoutine = StartCoroutine(MovementControl());
    }

    private void StopMovementControl()
    {
        if (inputControlRoutine != null)
        {
            StopCoroutine(inputControlRoutine);
        }
    }

    private IEnumerator MovementControl()
    {
        while (true)
        {
            yield return null;
        }
    }

    public IEnumerator MoveInDirection(Vector2 direction, float speed = 1,
                                       float? length = null)
    {
        if (length == null)
        {
            length = Mathf.Infinity;
        }
        direction = direction.normalized;
        float endTime = Time.time + length.Value;
        while (Time.time < endTime)
        {
            rigidbody.velocity = direction * speed;
            yield return new WaitForFixedUpdate();
        }
    }

    public IEnumerator MoveForward(float speed = 1, float? length = null)
    {
        yield return MoveInDirection(transform.right, speed, length);
    }

    public IEnumerator ShortForward()
    {
        yield return MoveForward(10, 2);
    }

    public IEnumerator PlayRecording()
    {
        yield return new WaitForFixedUpdate();
        InputRecording record = LoadRecording(recordingName);
        yield return PlayRecording(record, doLoop);
    }
}
