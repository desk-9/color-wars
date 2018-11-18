using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class UIVideo : MonoBehaviour
{
    private RawImage image;
    private VideoPlayer player;

    private void Start()
    {
        image = GetComponent<RawImage>();
        player = GetComponent<VideoPlayer>();
    }

    public void StartVideoUpdate()
    {
        StartCoroutine(VideoUpdate());
    }

    private IEnumerator VideoUpdate()
    {
        image.texture = null;
        if (player.clip == null)
        {
            yield break;
        }
        player.Prepare();
        while (!player.isPrepared)
        {
            yield return null;
        }
        image.texture = player.texture;
        player.Play();

        while (player.isPlaying)
        {
            yield return null;
        }
    }

    // void Update () {
    //     image.texture = player.texture;
    // }
}
