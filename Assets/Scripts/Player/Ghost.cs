using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class Ghost : MonoBehaviour
{

    public void Initialize(SpriteRenderer rendererIn, float lifeLength, float startingAlpha)
    {
        StartCoroutine(DimOverLifetime(rendererIn, lifeLength, startingAlpha));
    }

    private IEnumerator DimOverLifetime(SpriteRenderer rendererIn, float lifeLength, float startingAlpha)
    {
        ;
        SpriteRenderer ghostRenderer = this.EnsureComponent<SpriteRenderer>();
        Color dimmedColor = rendererIn.color;
        ghostRenderer.sprite = rendererIn.sprite;

        float elapsedTime = 0f;
        while (elapsedTime < lifeLength)
        {
            dimmedColor.a = Mathf.Lerp(startingAlpha, 0f, elapsedTime / lifeLength);
            ghostRenderer.color = dimmedColor;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
