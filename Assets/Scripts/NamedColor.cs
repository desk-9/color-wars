using UnityEngine;

[System.Serializable]
public class NamedColor
{
    public Color color;
    public string name;

    private NamedColor(Color color, string name)
    {
        this.color = color;
        this.name = name;
    }

    public static implicit operator Color(NamedColor custom)
    {
        return custom.color;
    }
}
