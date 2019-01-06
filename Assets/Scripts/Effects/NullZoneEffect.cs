using UnityEngine;
using UtilityExtensions;

public class NullZoneBlink : MonoBehaviour
{

    public float flashOpacity = .6f;
    public float stayedFlashDuration = 0f;
    public float flashTransitionDuration = .1f;
    public Color flashColor;
    new SpriteRenderer renderer;
    Color startingColor;

    // Use this for initialization
    private void Start()
    {
        renderer = this.EnsureComponent<SpriteRenderer>();
        startingColor = renderer.color;
        GameManager.NotificationManager.CallOnMessage(Message.NullChargePrevention, FlashNullZone);
    }

    private void FlashNullZone()
    {
        AudioManager.instance.PassToNullZone.Play(.1f);
        
        StartCoroutine(TransitionUtility.LerpColor(color => renderer.color = color,
                                                   renderer.color, flashColor, flashTransitionDuration));
        this.RealtimeDelayCall(() => FlashBackToNormal(), stayedFlashDuration + flashTransitionDuration);
    }

    private void FlashBackToNormal()
    {
        StartCoroutine(TransitionUtility.LerpColor(color => renderer.color = color, renderer.color, startingColor, flashTransitionDuration));
    }
}
