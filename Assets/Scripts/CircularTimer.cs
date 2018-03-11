using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UtilityExtensions;
using UnityEngine.UI;

public class CircularTimer : MonoBehaviour {
    public float FillAmount {
        get {return fillImage.fillAmount;}
        set {fillImage.fillAmount = value;}
    }

    public bool shouldStart = false;
    Image fillImage;
    Callback timeoutCallback;

    public virtual void Start () {
        fillImage = this.EnsureComponent<Image>();
        fillImage.enabled = false;
    }

    public virtual void Update() {
        if (shouldStart) {
            StartTimer(3, () => Debug.Log("TIMER DONE!!!!!!!!!!!!"));
        }
        transform.rotation = Quaternion.identity;
    }

    public void StartTimer(float secondsUntilTimeout, Callback timeoutCallback) {
        fillImage.enabled = true;
        StartCoroutine(Timer(secondsUntilTimeout));
        FillAmount = 0;
        this.timeoutCallback = timeoutCallback;
    }

    IEnumerator Timer(float secondsUntilTimeout) {
        float elapsedTime = 0.0f;
        while (elapsedTime < secondsUntilTimeout) {
            elapsedTime += Time.deltaTime;
            FillAmount = elapsedTime/secondsUntilTimeout;
            yield return null;
        }
        timeoutCallback();
        fillImage.enabled = false;
        shouldStart = false;
    }

}
