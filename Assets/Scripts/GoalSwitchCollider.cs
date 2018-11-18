using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalSwitchCollider : MonoBehaviour {
    public static string EventId {get; set;} = Utility.UniqueString();

    void GoalSwitch(GameObject thing) {
        GameModel.instance.notificationCenter.NotifyStringEvent(EventId, thing);
    }

    void OnTriggerEnter2D(Collider2D collider) {
        GoalSwitch(collider.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision) {
        GoalSwitch(collision.gameObject);
    }
}
