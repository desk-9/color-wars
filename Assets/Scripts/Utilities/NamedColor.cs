using UnityEngine;

// TODO: Remove this component!
// IIRC, NamedColor predated the TeamResourceManager, and hasn't aged well since
// -krista
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
