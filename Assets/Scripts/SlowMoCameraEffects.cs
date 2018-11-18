using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class SlowMoCameraEffects : MonoBehaviour {
    public ChromaticAberrationController chromaticAberrationController;
    public float aberationMagnitude = 0.5f;
    public float transitionTime = 0.3f;

    void Start() {
        chromaticAberrationController = this.EnsureComponent<ChromaticAberrationController>();
        GameModel.instance.notificationCenter.CallOnMessage(Message.SlowMoEntered, StartSlowMoEffects);
        GameModel.instance.notificationCenter.CallOnMessage(Message.SlowMoExited, StopSlowMoEffects);
    }

    void StartSlowMoEffects() {
        chromaticAberrationController.SetIntensitySmooth(
            aberationMagnitude, transitionTime);
    }

    void StopSlowMoEffects() {
        chromaticAberrationController.SetIntensitySmooth(0, transitionTime);
    }
}
