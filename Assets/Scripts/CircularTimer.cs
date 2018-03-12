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

    Image fillImage;
    Callback endTimerCallback;
    Coroutine timer;

    public virtual void Start () {
        fillImage = transform.FindComponent<Image>("CircularTimerImage");
        fillImage.enabled = false;
    }

    public virtual void Update() {
        transform.rotation = Quaternion.identity;
    }

    public void StartTimer(float secondsUntilTimeout, Callback endTimerCallback) {
        Debug.Log("Starting circular timer");
        fillImage.enabled = true;
        timer = StartCoroutine(Timer(secondsUntilTimeout));
        FillAmount = 0;
        this.endTimerCallback = endTimerCallback;
    }

    public void StopTimer() {
        if (timer != null) {
            StopCoroutine(timer);
            timer = null;
        }
        endTimerCallback();
        fillImage.enabled = false;
    }

    IEnumerator Timer(float secondsUntilTimeout) {
        float elapsedTime = 0.0f;
        while (elapsedTime < secondsUntilTimeout) {
            elapsedTime += Time.deltaTime;
            FillAmount = elapsedTime/secondsUntilTimeout;
            yield return null;
        }
        StopTimer();
    }

}
