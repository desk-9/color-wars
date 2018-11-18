using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class Blink : MonoBehaviour
{
    private new SpriteRenderer renderer;

    // Use this for initialization
    private void Start()
    {
        renderer = this.EnsureComponent<SpriteRenderer>();
        StartCoroutine(BlinkRenderer());
    }

    private IEnumerator BlinkRenderer()
    {
        while (true)
        {
            renderer.enabled = !renderer.enabled;
            yield return new WaitForSecondsRealtime(.5f);
        }
    }
}
