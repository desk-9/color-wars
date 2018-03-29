using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DashAimer : MonoBehaviour {
    public float lookahead = 4.0f;

    LineRenderer line;

    void Awake() {
        line = GetComponent<LineRenderer>();
    }

    void Update() {
        var inc = new Vector3(Time.deltaTime * lookahead, 0, 0);

        line.SetPosition(1, line.GetPosition(1) + inc);
    }
}
