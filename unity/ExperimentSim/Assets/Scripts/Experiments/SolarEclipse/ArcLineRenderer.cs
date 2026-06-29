using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ArcLineRenderer : MonoBehaviour
{
    public enum ArcPlane
    {
        XY,
        XZ,
        YZ
    }

    [Header("Arc Settings")]
    public float radius = 3f;
    public float startAngle = 0f;
    public float endAngle = 120f;
    public int segments = 40;

    [Header("Position")]
    public ArcPlane plane = ArcPlane.XY;
    public bool useWorldSpace = false;

    [Header("Line Settings")]
    public float lineWidth = 0.05f;
    public Color lineColor = Color.white;

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
        DrawArc();
    }

    void OnValidate()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        SetupLineRenderer();
        DrawArc();
    }

    void SetupLineRenderer()
    {
        lineRenderer.useWorldSpace = useWorldSpace;
        lineRenderer.loop = false;

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = lineColor;

        lineRenderer.material = mat;
    }

    void DrawArc()
    {
        if (segments < 2)
            segments = 2;

        lineRenderer.positionCount = segments + 1;

        float angleStep = (endAngle - startAngle) / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 point = Vector3.zero;

            if (plane == ArcPlane.XY)
            {
                point = new Vector3(
                    Mathf.Cos(rad) * radius,
                    Mathf.Sin(rad) * radius,
                    0f
                );
            }
            else if (plane == ArcPlane.XZ)
            {
                point = new Vector3(
                    Mathf.Cos(rad) * radius,
                    0f,
                    Mathf.Sin(rad) * radius
                );
            }
            else if (plane == ArcPlane.YZ)
            {
                point = new Vector3(
                    0f,
                    Mathf.Cos(rad) * radius,
                    Mathf.Sin(rad) * radius
                );
            }

            lineRenderer.SetPosition(i, point);
        }
    }
}