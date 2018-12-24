using UnityEngine;
using UtilityExtensions;

public class PlayerNullZoneEffect : MonoBehaviour
{
    private bool effectEnabled = false;
    private bool inNullZone = false;
    private Player player;
    private new SpriteRenderer renderer;

    private void Start()
    {
        player = this.EnsureComponent<Player>();
        renderer = this.EnsureComponent<SpriteRenderer>();
        GameManager.Instance.NotificationManager.CallOnMessage(Message.BallIsPossessed, CheckEffect);
    }

    private void CheckEffect()
    {
        NamedColor color = player?.Team?.TeamColor;
        if (color != null)
        {
            if (GameManager.Instance.PossessionManager.CurrentTeam != player.Team && inNullZone)
            {
                effectEnabled = true;
                Utility.HSVColor newColor = new Utility.HSVColor(color);
                newColor.v *= 0.85f;
                renderer.color = newColor.ToColor();
            }
            else
            {
                DisableEffect();
            }
        }
    }

    private void DisableEffect()
    {
        NamedColor color = player?.Team?.TeamColor;
        if (color != null && renderer != null)
        {
            effectEnabled = false;
            renderer.color = color;
        }
    }

    private void HandleEnter(Collider2D collider)
    {
        if (effectEnabled)
        {
            return;
        }
        int? layer = collider.gameObject?.layer;
        if (layer.HasValue && layer.Value == LayerMask.NameToLayer("NullZone"))
        {
            inNullZone = true;
            CheckEffect();
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        HandleEnter(collider);
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        HandleEnter(collider);
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        int? layer = collider.gameObject?.layer;
        if (layer.HasValue && layer.Value == LayerMask.NameToLayer("NullZone"))
        {
            inNullZone = false;
            DisableEffect();
        }
    }
}
