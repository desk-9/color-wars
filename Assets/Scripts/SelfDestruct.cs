using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class SelfDestruct : MonoBehaviour {

    ParticleSystem ps;
    public bool withChildren = true;

    // Use this for initialization
    void Start () {
        ps = this.EnsureComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update () {
        if (!ps.IsAlive(withChildren)) {
            Destroy(gameObject);
        }
    }
}
