using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ShadowAreaPainter : MonoBehaviour
{
    [Header("Alanı oluşturan noktalar")]
    public Transform pointA;
    public Transform pointB;
    public Transform pointC;
    public Transform pointD;

    [Header("Renk")]
    public Color areaColor = new Color(0f, 0f, 0f, 0.35f);

    [Header("Kameraya doğru hafif öne alma")]
    public Vector3 offset = new Vector3(0f, 0f, -0.02f);

    private Mesh mesh;
    private MeshRenderer meshRenderer;

    private void OnEnable()
    {
        Setup();
        DrawArea();
    }

    private void Update()
    {
        DrawArea();
    }

    private void OnValidate()
    {
        Setup();
        DrawArea();
    }

    private void Setup()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Shadow Area Mesh";
        }

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer.sharedMaterial == null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

            if (mat.shader == null)
                mat = new Material(Shader.Find("Sprites/Default"));

            meshRenderer.sharedMaterial = mat;
        }

        ApplyMaterialSettings();
    }

    private void ApplyMaterialSettings()
    {
        if (meshRenderer == null || meshRenderer.sharedMaterial == null)
            return;

        Material mat = meshRenderer.sharedMaterial;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", areaColor);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", areaColor);

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1);

        if (mat.HasProperty("_ZWrite"))
            mat.SetFloat("_ZWrite", 0);

        if (mat.HasProperty("_Cull"))
            mat.SetFloat("_Cull", 0);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
    }

    private void DrawArea()
    {
        if (pointA == null || pointB == null || pointC == null || pointD == null)
            return;

        Vector3[] vertices = new Vector3[4];

        vertices[0] = transform.InverseTransformPoint(pointA.position + offset);
        vertices[1] = transform.InverseTransformPoint(pointB.position + offset);
        vertices[2] = transform.InverseTransformPoint(pointC.position + offset);
        vertices[3] = transform.InverseTransformPoint(pointD.position + offset);

        int[] triangles =
        {
            0, 1, 2,
            0, 2, 3,

            // Ters yüzey de görünsün diye
            2, 1, 0,
            3, 2, 0
        };

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        ApplyMaterialSettings();
    }
}