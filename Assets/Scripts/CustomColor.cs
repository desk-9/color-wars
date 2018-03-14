using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CustomColor {
    public Color color;
    public string name;

    CustomColor(Color color, string name) {
        this.color = color;
        this.name = name;
    }

    public static implicit operator Color(CustomColor custom) {
        return custom.color;
    }
}
