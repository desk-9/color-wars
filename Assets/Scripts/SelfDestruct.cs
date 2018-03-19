using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class SelfDestruct : MonoBehaviour {

    ParticleSystem ps;

    // Use this for initialization
    void Start () {
        ps = this.EnsureComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update () {
        if (!ps.IsAlive()) {
            Destroy(gameObject);
        }
    }
}
