using UnityEngine;

public class GoalSwitchCollider : MonoBehaviour
{
    // Can't use Utility.UniqueString since it's not deterministic
    public static string EventId { get; set; } = "$__GOAL_SWITCH_COLLIDER_UNIQUE_EVENT_ID__$";

    private void GoalSwitch(GameObject thing)
    {
        GameManager.instance.notificationManager.NotifyStringEvent(EventId, thing);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        GoalSwitch(collider.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GoalSwitch(collision.gameObject);
    }
}
