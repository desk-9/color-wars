using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;
using UtilityExtensions;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

[System.Serializable]
public struct InputFrame
{
    public float leftStickX, leftStickY;
    public float positionX, positionY;
    public float rotation;
    public bool APressed;
    public bool AReleased;
    public bool BPressed;
    public bool BReleased;
    public bool Interrupt;
    public float waitTime;
    public InputFrame(float stickX, float stickY, float positionX, float positionY, float rotation = -1f, bool APressed = false, bool AReleased = false,
                      bool BPressed = false, bool BReleased = false, bool Interrupt = false,
                      float waitTime = 0)
    {
        this.leftStickX = stickX;
        this.leftStickY = stickY;
        this.positionX = positionX;
        this.positionY = positionY;
        this.rotation = rotation;
        this.APressed = APressed;
        this.AReleased = AReleased;
        this.BPressed = BPressed;
        this.BReleased = BReleased;
        this.Interrupt = Interrupt;
        this.waitTime = waitTime;
    }
}

[System.Serializable]
public struct InputRecording
{
    public float startX;
    public float startY;
    public float startRotation;
    public List<InputFrame> recording;
    public InputRecording(float startX, float startY, float startRotation,
                          List<InputFrame> recording)
    {
        this.startX = startX;
        this.startY = startY;
        this.startRotation = startRotation;
        this.recording = recording;
    }
}

public class PlayerRecorder : MonoBehaviour
{
    public static bool isRecording = false;
    public bool respondToRecordings = true;
    public bool allRecordAtOnce = false;
    private new Rigidbody2D rigidbody;
    private PlayerControls controls;
    private Coroutine recording;
    private bool endRecording = false;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        controls = this.EnsureComponent<PlayerControls>();
        if (allRecordAtOnce)
        {
            // Have all players start and stop recordings on same button press
            GameManager.NotificationManager.CallOnMessage(
                Message.PlayerPressedRightBumper, () =>
                {
                    if (recording == null)
                    {
                        recording = StartCoroutine(Record());
                    }
                    else
                    {
                        endRecording = true;
                    }
                });
        }
        else
        {
            // Each player recording state is individual
            StartCoroutine(RecordCheck());
        }
        Directory.CreateDirectory(recordingFolder);
    }

    private IEnumerator RecordCheck()
    {
        while (true)
        {
            InputDevice input = controls.GetInputDevice();
            if (input != null && respondToRecordings && input.RightBumper.WasPressed)
            {
                yield return Record();
            }
            yield return null;
        }
    }

    private IEnumerator Record()
    {
        isRecording = true;
        Debug.Log("Recording started");
        yield return null;
        InputRecording record =
            new InputRecording(transform.position.x,
                               transform.position.y,
                               this.EnsureComponent<Rigidbody2D>().rotation,
                               new List<InputFrame>());
        float lastTime = Time.realtimeSinceStartup;
        while (true)
        {
            InputDevice input = controls.GetInputDevice();
            if (input != null)
            {
                if (endRecording)
                {
                    SaveRecording(record);
                    endRecording = false;
                    recording = null;
                    isRecording = false;
                    yield break;
                }
                if (input.LeftBumper.WasPressed)
                {
                    Debug.Log("Recording interrupt");
                }
                float delta = Time.realtimeSinceStartup - lastTime;
                lastTime = Time.realtimeSinceStartup;
                record.recording.Add(
                    new InputFrame(
                        input.LeftStickX, input.LeftStickY,
                        rigidbody.position.x, rigidbody.position.y,
                        rigidbody.rotation,
                        input.Action1.WasPressed,
                        input.Action1.WasReleased,
                        input.Action2.WasPressed,
                        input.Action2.WasReleased,
                        input.LeftBumper.WasPressed,
                        delta));
            }
            else
            {
                record.recording.Add(new InputFrame(0, 0, rigidbody.position.x, rigidbody.position.y, rigidbody.rotation));
            }
            yield return null;
        }
    }

    private string recordingFolder
    {
        get
        {
            return string.Format("{0}/recordings", Application.persistentDataPath);
        }
    }

    private const string extension = "inputRecord";

    private string GetPlayerName()
    {
        return gameObject.name;
    }

    private List<string> RecordingsForPlayer()
    {
        string pattern = string.Format("{0}*.{1}", GetPlayerName(), extension);
        return (from path in Directory.GetFiles(recordingFolder, pattern)
                select Path.GetFileName(path)).ToList();
    }

    private string filePattern
    {
        get
        {
            return string.Format("{0}-recording-{{0}}.{1}", GetPlayerName(), extension);
        }
    }

    private int NextRecordingNumber()
    {
        int result = RecordingsForPlayer().Select<string, int?>((string filename) =>
        {
            int prefixLength = string.Format("{0}-recording-", GetPlayerName()).Length;
            string num = Path.GetFileNameWithoutExtension(filename).Substring(prefixLength);
            int possibleInt;
            bool worked = int.TryParse(num, out possibleInt);
            if (worked)
            {
                return possibleInt;
            }
            else
            {
                return null;
            }
        }).Where(x => x != null).OfType<int>().DefaultIfEmpty(0).Max();
        return result + 1;
    }

    private void SaveRecording(InputRecording record)
    {
        string name = string.Format(filePattern, NextRecordingNumber());
        WriteRecording(record, name);
    }

    private void WriteRecording(InputRecording record, string name)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        Directory.CreateDirectory(recordingFolder);
        string path = string.Format("{0}/recordings/{1}",
                                 Application.persistentDataPath, name);
        Debug.LogFormat("Recording finished, saving to {0}", path);
        FileStream file = File.Create(path);
        formatter.Serialize(file, record);
        file.Close();
    }

}
