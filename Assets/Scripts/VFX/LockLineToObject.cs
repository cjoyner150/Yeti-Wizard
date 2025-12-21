using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LockLineToObject : MonoBehaviour
{
    public Transform startTarget;
    public Transform endTarget;

    public float radius = 0.25f;
    public float speed = 2f;

    private LineRenderer line;
    private float time;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 3;
        line.useWorldSpace = true;
    }

    void LateUpdate()
    {
        if (!startTarget || !endTarget) return;

        Vector3 start = startTarget.position;
        Vector3 end = endTarget.position;

        Vector3 center = (start + end) * 0.5f;
        Vector3 lineDir = (end - start).normalized;

        // Build an orthonormal basis perpendicular to the line
        Vector3 tangentA = Vector3.Cross(lineDir, Vector3.up);
        if (tangentA.sqrMagnitude < 0.001f)
            tangentA = Vector3.Cross(lineDir, Vector3.right);

        tangentA.Normalize();
        Vector3 tangentB = Vector3.Cross(lineDir, tangentA);

        time += Time.deltaTime * speed;

        Vector3 circularOffset =
            (Mathf.Cos(time) * tangentA +
             Mathf.Sin(time) * tangentB) * radius;

        Vector3 midpoint = center + circularOffset;

        line.SetPosition(0, start);
        line.SetPosition(1, midpoint);
        line.SetPosition(2, end);
    }
}
