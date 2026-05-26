using System.Collections.Generic;
using UnityEngine;

public class TrackGenerator : MonoBehaviour
{
    [Header("Parametry Toru")]
    public float trackWidth = 0.5f;
    public int splineResolution = 5;

    [Header("Wygląd Toru")]
    public float edgeWidth = 0.2f;
    public Color edgeColor = Color.white; // Zmieniłem na biały (jak linie na drodze)
    public Color roadColor = new Color(0.2f, 0.2f, 0.2f); // Ciemnoszary asfalt

    private TrackMesh currentTrackMesh;
    private LineRenderer leftEdgeRenderer;
    private LineRenderer rightEdgeRenderer;

    void Start()
    {
        leftEdgeRenderer = CreateEdgeRenderer("LeftEdge");
        rightEdgeRenderer = CreateEdgeRenderer("RightEdge");
    }

    private LineRenderer CreateEdgeRenderer(string edgeName)
    {
        GameObject edgeObj = new GameObject(edgeName);
        edgeObj.transform.SetParent(this.transform);

        LineRenderer lr = edgeObj.AddComponent<LineRenderer>();
        lr.startWidth = edgeWidth;
        lr.endWidth = edgeWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = edgeColor;
        lr.endColor = edgeColor;
        lr.loop = true;
        lr.positionCount = 0;
        // Odsuwamy linie lekko do przodu w osi Z, żeby były ZAWSZE nad asfaltem
        lr.transform.position = new Vector3(0, 0, -0.1f);

        return lr;
    }

    public void BuildTrackBoundaries(List<Vector2> controlPoints)
    {
        currentTrackMesh = new TrackMesh();
        currentTrackMesh.centerLine = CreateSpline(controlPoints);

        List<Vector2> normals = ComputeNormals(currentTrackMesh.centerLine);

        for (int i = 0; i < currentTrackMesh.centerLine.Count; i++)
        {
            Vector2 center = currentTrackMesh.centerLine[i];
            Vector2 normal = normals[i];

            Vector2 leftPoint = center + normal * (trackWidth / 2f);
            Vector2 rightPoint = center - normal * (trackWidth / 2f);

            currentTrackMesh.leftEdge.Add(leftPoint);
            currentTrackMesh.rightEdge.Add(rightPoint);
        }

        // 1. Rysujemy białe linie brzegowe
        DrawEdge(leftEdgeRenderer, currentTrackMesh.leftEdge);
        DrawEdge(rightEdgeRenderer, currentTrackMesh.rightEdge);

        // 2. GENERUJEMY WYPEŁNIENIE (ASFALT)
        BuildRoadMesh(currentTrackMesh);

        Debug.Log("TrackGenerator: Tor 3D wygenerowany!");
    }

    // --- NOWA METODA: TWORZENIE ASFALTU ---
    private void BuildRoadMesh(TrackMesh track)
    {
        // Tworzymy nowy obiekt na scenie, który będzie trzymał nasz model 3D drogi
        GameObject roadObj = new GameObject("RoadSurface");
        roadObj.transform.SetParent(this.transform);

        MeshFilter meshFilter = roadObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = roadObj.AddComponent<MeshRenderer>();

        // Ustawiamy szary kolor
        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.material.color = roadColor;

        Mesh mesh = new Mesh();

        int numPoints = track.centerLine.Count;

        // Tablica wierzchołków (2 razy więcej niż punktów środkowych, bo lewa i prawa strona)
        Vector3[] vertices = new Vector3[numPoints * 2];
        // Tablica indeksów trójkątów (6 punktów na każdy segment drogi)
        int[] triangles = new int[numPoints * 6];

        for (int i = 0; i < numPoints; i++)
        {
            // Przypisanie pozycji wierzchołków
            vertices[i * 2] = new Vector3(track.leftEdge[i].x, track.leftEdge[i].y, 0);
            vertices[i * 2 + 1] = new Vector3(track.rightEdge[i].x, track.rightEdge[i].y, 0);

            // Wyliczanie indeksów do połączenia ich w trójkąty
            int currLeft = i * 2;
            int currRight = i * 2 + 1;
            int nextLeft = ((i + 1) % numPoints) * 2;
            int nextRight = (((i + 1) % numPoints) * 2) + 1;

            int triIndex = i * 6;

            // Pierwszy trójkąt segmentu
            triangles[triIndex] = currLeft;
            triangles[triIndex + 1] = nextLeft;
            triangles[triIndex + 2] = currRight;

            // Drugi trójkąt segmentu
            triangles[triIndex + 3] = currRight;
            triangles[triIndex + 4] = nextLeft;
            triangles[triIndex + 5] = nextRight;
        }

        // Wrzucamy dane do silnika Unity
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        meshFilter.mesh = mesh;
    }

    private void DrawEdge(LineRenderer lr, List<Vector2> points)
    {
        lr.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            lr.SetPosition(i, new Vector3(points[i].x, points[i].y, 0f));
        }
    }

    private List<Vector2> CreateSpline(List<Vector2> points)
    {
        List<Vector2> splinePoints = new List<Vector2>();
        if (points.Count < 3) return points;

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 p0 = points[(i - 1 + points.Count) % points.Count];
            Vector2 p1 = points[i];
            Vector2 p2 = points[(i + 1) % points.Count];
            Vector2 p3 = points[(i + 2) % points.Count];

            for (int j = 0; j < splineResolution; j++)
            {
                float t = j / (float)splineResolution;
                Vector2 a = 2f * p1;
                Vector2 b = p2 - p0;
                Vector2 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
                Vector2 d = -p0 + 3f * p1 - 3f * p2 + p3;
                Vector2 pos = 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));
                splinePoints.Add(pos);
            }
        }
        return splinePoints;
    }

    private List<Vector2> ComputeNormals(List<Vector2> curve)
    {
        List<Vector2> normals = new List<Vector2>();
        for (int i = 0; i < curve.Count; i++)
        {
            Vector2 current = curve[i];
            Vector2 next = curve[(i + 1) % curve.Count];
            Vector2 tangent = (next - current).normalized;
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            normals.Add(normal);
        }
        return normals;
    }
}