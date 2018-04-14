using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UtilityExtensions;
using UnityEngine.UI;

public class CircularTimer : CircularIndicator {
    Callback endTimerCallback = delegate{};
    Coroutine timer;

    public void StartTimer(float secondsUntilTimeout, Callback endTimerCallback) {
        if (this == null) {
            return;
        }
        base.Show();
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
        base.Hide();
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
