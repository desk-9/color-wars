using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using UtilityExtensions;

public delegate IEnumerator Routine();


public class PlayerPuppet : MonoBehaviour {

    public enum PuppetProgram {
        ShortForward,
        PlayRecording
    }

    public PuppetProgram program;
    public float defaultMovementSpeed = 30;
    public string recordingName = "test";
    public bool doPuppeting = true;

    const string recordingDirectory = "Recordings";
    Coroutine puppetRoutine;
    Coroutine inputControlRoutine;
    new Rigidbody2D rigidbody;
    PlayerStateManager stateManager;

    Vector2 movementInput = Vector2.zero;

    void Start() {
        rigidbody = this.EnsureComponent<Rigidbody2D>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        if (!doPuppeting) {
            return;
        }
        this.FrameDelayCall(() => StartProgram(program), 1);
    }


    List<InputFrame> LoadRecording(string name) {
        var path = Path.Combine(
            Application.streamingAssetsPath, recordingDirectory, name + ".inputRecord");
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream file = File.Open(path, FileMode.Open);
        var record = formatter.Deserialize(file) as List<InputFrame>;
        if (record == null) {
            Debug.LogError(string.Format("No such recording at {0}", path));
        }
        return record;
    }

    IEnumerator PlayRecording(List<InputFrame> record, bool loop = false) {
        while (true) {
            foreach (var frame in record) {
                PlayerMovement.SendInputEvents(
                    frame.leftStickX, frame.leftStickY, frame.APressed, frame.AReleased,
                    frame.BPressed, frame.BReleased, this.gameObject);
                yield return null;
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
                puppetRoutine = StartCoroutine(routine);
                // stateManager.AttemptNormalMovement(StartMovementControl, StopMovementControl);
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
        Debug.Log("Playing recording");
        var record = LoadRecording(recordingName);
        Utility.Print("Record length", record.Count);
        yield return PlayRecording(record, true);
    }
}
