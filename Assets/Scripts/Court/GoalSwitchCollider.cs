using UnityEngine;

public class GoalSwitchCollider : MonoBehaviour
{
    public static string EventId { get; set; } = Utility.UniqueString();

    private void GoalSwitch(GameObject thing)
    {
        GameManager.NotificationManager.NotifyStringEvent(EventId, thing);
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
