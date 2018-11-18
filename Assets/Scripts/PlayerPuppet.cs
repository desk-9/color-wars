using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using UtilityExtensions;

public delegate IEnumerator Routine();


public class PlayerPuppet : MonoBehaviour {
    public static bool puppetsPause = false;
    public enum PuppetProgram {
        ShortForward,
        PlayRecording
    }

    public PuppetProgram program;
    public float defaultMovementSpeed = 30;
    public string recordingName = "test";
    public bool doPuppeting = true;
    public bool doLoop = false;

    const string recordingDirectory = "Recordings";
    Coroutine inputControlRoutine;
    new Rigidbody2D rigidbody;

    bool recordingFinishedThisFrame = false;

    void Start() {
        rigidbody = this.EnsureComponent<Rigidbody2D>();
        if (!doPuppeting) {
            return;
        }
        GameModel.instance.notificationCenter.CallOnMessage(
            Message.RecordingFinished,
            () => {
                recordingFinishedThisFrame = true;
                if (this != null) {
                    this.FrameDelayCall(() => {
                            if (this != null) {
                                this.recordingFinishedThisFrame = false;
                            }
                        });
                }
            });
        this.FrameDelayCall(() => StartProgram(program));
    }


    InputRecording LoadRecording(string name) {
        var path = Path.Combine(
            Application.streamingAssetsPath, recordingDirectory, name + ".inputRecord");
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream file = File.Open(path, FileMode.Open);
        var record = (InputRecording) formatter.Deserialize(file);
        file.Close();
        return record;
    }

    IEnumerator PlayRecording(InputRecording record, bool loop = false) {
        while (true) {
            transform.position = new Vector2(record.startX, record.startY);
            rigidbody.rotation = record.startRotation;
            foreach (var frame in record.recording) {
                if (this.gameObject == null) {
                    yield break;
                }

                while (puppetsPause) {
                    PlayerControls.SendInputEvents(
                        0, 0, false, false, false, false, this.gameObject);
                    yield return new WaitForFixedUpdate();
                }
                rigidbody.position = new Vector2(frame.positionX, frame.positionY);
                rigidbody.rotation = frame.rotation;
                PlayerControls.SendInputEvents(
                    0, 0, frame.APressed, frame.AReleased,
                    frame.BPressed, frame.BReleased, this.gameObject);
                if (frame.Interrupt) {
                    Debug.LogWarning("Interrupt! Text will change");

                        GameModel.instance.notificationCenter.NotifyMessage(
                            Message.RecordingInterrupt, this.gameObject);

                }
                yield return null;

            }
            if (!recordingFinishedThisFrame) {
                GameModel.instance.notificationCenter.NotifyMessage(
                    Message.RecordingFinished, this.gameObject);
            }
            if (!loop) {
                break;
            }
        }
    }

    void StartProgram(PuppetProgram program) {
        string programName = program.ToString();
        var methodInfo = this.GetType().GetMethod(programName);
        if (methodInfo != null) {
            var routine = methodInfo.Invoke(this, new object[0]) as IEnumerator;
            if (routine != null) {
                StartCoroutine(routine);
                return;
            }
        }
        Debug.LogError(string.Format("No such puppet program: '{0}'", programName));
    }

    void StartMovementControl() {
        inputControlRoutine = StartCoroutine(MovementControl());
    }

    void StopMovementControl() {
        if (inputControlRoutine != null) {
            StopCoroutine(inputControlRoutine);
        }
    }

    IEnumerator MovementControl() {
        while (true) {
            yield return null;
        }
    }

    public IEnumerator MoveInDirection(Vector2 direction, float speed = 1,
                                       float? length = null) {
        if (length == null) {
            length = Mathf.Infinity;
        }
        direction = direction.normalized;
        var endTime = Time.time + length.Value;
        while (Time.time < endTime) {
            rigidbody.velocity = direction * speed;
            yield return new WaitForFixedUpdate();
        }
    }

    public IEnumerator MoveForward(float speed = 1, float? length = null) {
        yield return MoveInDirection(transform.right, speed, length);
    }

    public IEnumerator ShortForward() {
        yield return MoveForward(10, 2);
    }

    public IEnumerator PlayRecording() {
        yield return new WaitForFixedUpdate();
        var record = LoadRecording(recordingName);
        yield return PlayRecording(record, doLoop);
    }
}
