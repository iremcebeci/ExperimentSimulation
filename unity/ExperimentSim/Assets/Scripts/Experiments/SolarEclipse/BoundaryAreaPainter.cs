using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class BoundaryAreaPainter : MonoBehaviour
{
    [Header("Sınır Noktaları")]
    public Transform[] points;

    [Header("Alan Rengi")]
    public Color areaColor = new Color(1f, 0.7f, 0f, 0.25f);

    [Header("Sürekli Güncellensin mi?")]
    public bool updateEveryFrame = true;

    private Mesh mesh;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        mesh = new Mesh();
        mesh.name = "Painted Area Mesh";

        GetComponent<MeshFilter>().mesh = mesh;

        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = CreateTransparentMaterial(areaColor);
    }

    void Start()
    {
        DrawArea();
    }

    void Update()
    {
        if (updateEveryFrame)
        {
            DrawArea();
        }
    }

    public void DrawArea()
    {
        if (points == null || points.Length < 3)
            return;

        Vector3[] vertices = new Vector3[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null)
                return;

            vertices[i] = transform.InverseTransformPoint(points[i].position);
        }

        int[] triangles = CreateTriangles(points.Length);

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    private int[] CreateTriangles(int pointCount)
    {
        int triangleCount = pointCount - 2;
        int[] triangles = new int[triangleCount * 3];

        int index = 0;

        for (int i = 0; i < triangleCount; i++)
        {
            triangles[index++] = 0;
            triangles[index++] = i + 1;
            triangles[index++] = i + 2;
        }

        return triangles;
    }

    private Material CreateTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material mat = new Material(shader);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetFloat("_AlphaClip", 0);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;

        return mat;
    }
}