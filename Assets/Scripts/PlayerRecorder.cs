using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;
using UtilityExtensions;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

[System.Serializable]
struct InputFrame {
    public float leftStickX, leftStickY;
    public bool APressed;
    public bool AReleased;
    public bool BPressed;
    public bool BReleased;
    public InputFrame(float stickX, float stickY, bool APressed = false, bool AReleased = false,
                      bool BPressed = false, bool BReleased = false) {
        this.leftStickX = stickX;
        this.leftStickY = stickY;
        this.APressed = APressed;
        this.AReleased = AReleased;
        this.BPressed = BPressed;
        this.BReleased = BReleased;
    }
}

public class PlayerRecorder : MonoBehaviour {
    public bool respondToRecordings = true;
    PlayerMovement movement;
    void Start() {
        movement = this.EnsureComponent<PlayerMovement>();
        StartCoroutine(RecordCheck());
        Directory.CreateDirectory(recordingFolder);
    }

    IEnumerator RecordCheck() {
        while (true) {
            var input = movement.GetInputDevice();
            if (input != null && respondToRecordings && input.RightBumper.WasPressed) {
                yield return Record();
            }
            yield return null;
        }
    }

    IEnumerator Record() {
        Debug.Log("Recording started");
        yield return null;
        List<InputFrame> record = new List<InputFrame>();
        while (true) {
            var input = movement.GetInputDevice();
            if (input != null) {
                if (input.RightBumper.WasPressed) {
                    SaveRecording(record);
                    yield break;
                }
                record.Add(
                    new InputFrame(
                        input.LeftStickX, input.LeftStickY,
                        input.Action1.WasPressed,
                        input.Action1.WasReleased,
                        input.Action2.WasPressed,
                        input.Action2.WasReleased));
            } else {
                record.Add(new InputFrame(0, 0));
            }
            yield return null;
        }
    }

    string recordingFolder {
        get {
            return string.Format("{0}/recordings", Application.persistentDataPath);
        }
    }

    const string extension = "inputRecord";

    string GetPlayerName() {
        return gameObject.name;
    }

    List<string> RecordingsForPlayer() {
        var pattern = string.Format("{0}*.{1}", GetPlayerName(), extension);
        foreach (var path in Directory.GetFiles(recordingFolder, pattern)) {
            Utility.Print("Rec path ", path);
        }
        return (from path in Directory.GetFiles(recordingFolder, pattern)
                select Path.GetFileName(path)).ToList();
    }

    string filePattern {
        get {
            return string.Format("{0}-recording-{{0}}.{1}", GetPlayerName(), extension);
        }
    }

    int NextRecordingNumber() {
        foreach (var path in RecordingsForPlayer()) {
            Utility.Print("Processed Rec path ", path);
        }
        var result = RecordingsForPlayer().Select<string, int?>((string filename) => {
                var prefixLength = string.Format("{0}-recording-", GetPlayerName()).Length;
                var num = Path.GetFileNameWithoutExtension(filename).Substring(prefixLength);
                int possibleInt;
                bool worked = int.TryParse(num, out possibleInt);
                if (worked) {
                    return possibleInt;
                } else {
                    return null;
                }
            }).Where(x => x != null).OfType<int>().DefaultIfEmpty(0).Max();
        Utility.Print("Next number: ", result + 1);
        return result + 1;
    }

    void SaveRecording(List<InputFrame> record) {
        var name = string.Format(filePattern, NextRecordingNumber());
        WriteRecording(record, name);
    }

    void WriteRecording(List<InputFrame> record, string name) {
        BinaryFormatter formatter = new BinaryFormatter();
        Directory.CreateDirectory(recordingFolder);
        var path = string.Format("{0}/recordings/{1}",
                                 Application.persistentDataPath, name);
        Debug.LogFormat("Recording finished, saving to {0}", path);
        FileStream file = File.Create(path);
        formatter.Serialize(file, record);
        file.Close();
    }

}
