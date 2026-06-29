using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class OrbitCircleRenderer : MonoBehaviour
{
    [Header("Orbit Shape")]
    public float radius = 5f;
    [Range(24, 360)] public int segments = 120;
    public bool drawOnXZPlane = true;

    [Header("Line Style")]
    public float lineWidth = 0.08f;
    public Color startColor = new Color(0.2f, 0.7f, 1f, 0.9f);
    public Color endColor = new Color(0.6f, 0.9f, 1f, 0.9f);

    [Header("Material")]
    public Material orbitMaterial;

    private LineRenderer lineRenderer;

    void Awake()
    {
        Setup();
        DrawOrbit();
    }

    void Start()
    {
        Setup();
        DrawOrbit();
    }

    void OnValidate()
    {
        Setup();
        DrawOrbit();
    }

    void Setup()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.positionCount = segments;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        if (orbitMaterial != null)
        {
            lineRenderer.sharedMaterial = orbitMaterial;
        }

        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(endColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(startColor.a, 0f),
                new GradientAlphaKey(endColor.a, 1f)
            }
        );

        lineRenderer.colorGradient = gradient;
    }

    void DrawOrbit()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (segments < 3)
            segments = 3;

        lineRenderer.positionCount = segments;

        float angleStep = 2f * Mathf.PI / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            Vector3 point = drawOnXZPlane
                ? new Vector3(x, 0f, z)
                : new Vector3(x, z, 0f);

            lineRenderer.SetPosition(i, point);
        }
    }
}