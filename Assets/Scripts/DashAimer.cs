using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DashAimer : MonoBehaviour {
    public float lookahead = 4.0f;
    public float maxAhead = 6;
    LineRenderer line;

    void Awake() {
        line = GetComponent<LineRenderer>();
    }

    void Update() {
        var inc = new Vector3(Time.deltaTime * lookahead, 0, 0);
        var newPosition = line.GetPosition(1) + inc;
        if ((line.GetPosition(0) - newPosition).magnitude < maxAhead) {
            line.SetPosition(1, newPosition);
        }
    }
}
