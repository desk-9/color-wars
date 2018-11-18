using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DashAimer : MonoBehaviour
{
    public float lookahead = 4.0f;
    public float maxAhead = 6;
    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        Vector3 inc = new Vector3(Time.deltaTime * lookahead, 0, 0);
        Vector3 newPosition = line.GetPosition(1) + inc;
        if ((line.GetPosition(0) - newPosition).magnitude < maxAhead)
        {
            line.SetPosition(1, newPosition);
        }
    }
}
