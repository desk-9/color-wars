using UnityEngine;
using UtilityExtensions;

public class SlowMoCameraEffects : MonoBehaviour
{
    public ChromaticAberrationController chromaticAberrationController;
    public float aberationMagnitude = 0.5f;
    public float transitionTime = 0.3f;

    private void Start()
    {
        chromaticAberrationController = this.EnsureComponent<ChromaticAberrationController>();
        GameModel.instance.notificationCenter.CallOnMessage(Message.SlowMoEntered, StartSlowMoEffects);
        GameModel.instance.notificationCenter.CallOnMessage(Message.SlowMoExited, StopSlowMoEffects);
    }

    private void StartSlowMoEffects()
    {
        chromaticAberrationController.SetIntensitySmooth(
            aberationMagnitude, transitionTime);
    }

    private void StopSlowMoEffects()
    {
        chromaticAberrationController.SetIntensitySmooth(0, transitionTime);
    }
}
