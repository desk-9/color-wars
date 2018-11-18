using UnityEngine;
using UtilityExtensions;

public class SelfDestruct : MonoBehaviour
{
    private ParticleSystem ps;
    public bool withChildren = true;

    // Use this for initialization
    private void Start()
    {
        ps = this.EnsureComponent<ParticleSystem>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (!ps.IsAlive(withChildren))
        {
            Destroy(gameObject);
        }
    }
}
