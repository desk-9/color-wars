using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UtilityExtensions;
using UnityEngine.UI;

public class CircularTimerGameObject : CircularTimer {
    public Vector2 offset;
    public Transform target;
    RectTransform rt;
    
    public override void Start () {
        base.Start();
        rt = GetComponent<RectTransform>();
    }

    public override void Update() {
        base.Update();
        // Vector2 targetScreenPosition = Camera.main.WorldToScreenPoint(
        //     new Vector3(target.position.x + offset.x,
        //                 target.position.y + offset.y,
        //                 Camera.main.nearClipPlane));
        // rt.anchoredPosition = targetScreenPosition;
        // rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition + offset, screenPosition, 0.1f);
    }

}
