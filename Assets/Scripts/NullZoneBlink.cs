using UnityEngine;
using UtilityExtensions;

public class NullZoneBlink : MonoBehaviour
{

    public float flashOpacity = .6f;
    public float stayedFlashDuration = 0f;
    public float flashTransitionDuration = .1f;
    public Color flashColor;

    // Use this for initialization
    private void Start()
    {
        GameModel.instance.notificationCenter.CallOnMessage(Message.NullChargePrevention, FlashNullZone);
    }

    private void FlashNullZone()
    {
        if (this == null)
        {
            return;
        }
        SpriteRenderer renderer = this.EnsureComponent<SpriteRenderer>();
        Color startingColor = renderer.color;
        StartCoroutine(TransitionUtility.LerpColor(color => renderer.color = color,
                                                   renderer.color, flashColor, flashTransitionDuration));
        this.RealtimeDelayCall(() => FlashBackToNormal(startingColor), stayedFlashDuration + flashTransitionDuration);
    }

    private void FlashBackToNormal(Color startingColor)
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        StartCoroutine(TransitionUtility.LerpColor(color => renderer.color = color, renderer.color, startingColor, flashTransitionDuration));

    }
}
