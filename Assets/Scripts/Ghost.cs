using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class Ghost : MonoBehaviour {

    public void Initialize(SpriteRenderer rendererIn, float lifeLength, float startingAlpha) {
        StartCoroutine(DimOverLifetime(rendererIn, lifeLength, startingAlpha));
    }

    IEnumerator DimOverLifetime(SpriteRenderer rendererIn, float lifeLength, float startingAlpha) {;
        var ghostRenderer = this.EnsureComponent<SpriteRenderer>();
        var dimmedColor = rendererIn.color;
        ghostRenderer.sprite = rendererIn.sprite;

        float elapsedTime = 0f;
        while(elapsedTime < lifeLength) {
            dimmedColor.a = Mathf.Lerp(startingAlpha, 0f, elapsedTime / lifeLength);
            ghostRenderer.color = dimmedColor;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
