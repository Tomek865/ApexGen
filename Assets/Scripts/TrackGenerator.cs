using System.Collections.Generic;
using UnityEngine;

public class TrackGenerator : MonoBehaviour
{
    [Header("Ustawienia Fizyki Toru")]
    public float trackWidth = 8f;
    public float wallHeight = 3f;
    public float wallThickness = 1.5f;

    private GameObject trackContainer;

    public void BuildTrackBoundaries(List<Vector2> points)
    {
        if (points == null || points.Count < 3) return;

        if (trackContainer != null) Destroy(trackContainer);
        trackContainer = new GameObject("CiaglyTor3D");

        int count = points.Count;

        Vector3[] floorVerts = new Vector3[count * 2];
        Vector3[] leftWallVerts = new Vector3[count * 4];
        Vector3[] rightWallVerts = new Vector3[count * 4];

        int[] floorTris = new int[count * 6];
        int[] leftWallTris = new int[count * 18];
        int[] rightWallTris = new int[count * 18];

        Vector3[] leftEdges = new Vector3[count];
        Vector3[] rightEdges = new Vector3[count];
        Vector3[] centers = new Vector3[count];

        // ETAP 1: Środek toru i wyliczenie surowych krawędzi
        for (int i = 0; i < count; i++)
        {
            Vector2 prev = points[(i - 1 + count) % count];
            Vector2 curr = points[i];
            Vector2 next = points[(i + 1) % count];

            Vector2 dirIn = (curr - prev).normalized;
            Vector2 dirOut = (next - curr).normalized;

            Vector2 tangent = (dirIn + dirOut).normalized;
            Vector2 normal = new Vector2(tangent.y, -tangent.x);

            centers[i] = new Vector3(curr.x, 0, curr.y);
            Vector3 offset = new Vector3(normal.x, 0, normal.y) * (trackWidth / 2f);

            leftEdges[i] = centers[i] - offset;
            rightEdges[i] = centers[i] + offset;
        }

        // ETAP 2: ALGORYTM ANTI-BOWTIE (Niszczyciel Pętli)
        // Brutalne wymuszenie progresji: krawędź nie może się cofać względem toru jazdy.
        float minForwardStep = 0.1f;
        for (int pass = 0; pass < 5; pass++)
        {
            for (int i = 0; i < count; i++)
            {
                int prev = (i - 1 + count) % count;
                Vector3 trackDir = (centers[i] - centers[prev]).normalized;

                // Lewa krawędź - jeśli zawraca, ciągniemy punkt na siłę do przodu
                Vector3 lVector = leftEdges[i] - leftEdges[prev];
                if (Vector3.Dot(lVector, trackDir) < minForwardStep)
                {
                    leftEdges[i] = leftEdges[prev] + trackDir * minForwardStep;
                }

                // Prawa krawędź - analogicznie
                Vector3 rVector = rightEdges[i] - rightEdges[prev];
                if (Vector3.Dot(rVector, trackDir) < minForwardStep)
                {
                    rightEdges[i] = rightEdges[prev] + trackDir * minForwardStep;
                }
            }
        }

        // ETAP 3: Relaksacja (miękkie wygładzenie naprawionych wierzchołków)
        for (int pass = 0; pass < 5; pass++)
        {
            Vector3[] tempLeft = new Vector3[count];
            Vector3[] tempRight = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                int prev = (i - 1 + count) % count;
                int next = (i + 1) % count;

                // Wagi 1-2-1 dla ładnego, organicznego zaokrąglenia na wirażach
                tempLeft[i] = (leftEdges[prev] + leftEdges[i] * 2f + leftEdges[next]) / 4f;
                tempRight[i] = (rightEdges[prev] + rightEdges[i] * 2f + rightEdges[next]) / 4f;
            }
            leftEdges = tempLeft;
            rightEdges = tempRight;
        }

        // ETAP 4: Budowa ścian na bezpiecznych i wygładzonych krawędziach
        for (int i = 0; i < count; i++)
        {
            floorVerts[i * 2] = leftEdges[i];
            floorVerts[i * 2 + 1] = rightEdges[i];

            // Grubość ściany liczymy na bieżąco z CZYSTYCH krawędzi
            Vector3 outLeft = (leftEdges[i] - rightEdges[i]).normalized;
            Vector3 outRight = (rightEdges[i] - leftEdges[i]).normalized;

            leftWallVerts[i * 4] = leftEdges[i];
            leftWallVerts[i * 4 + 1] = leftEdges[i] + Vector3.up * wallHeight;
            leftWallVerts[i * 4 + 2] = leftWallVerts[i * 4 + 1] + outLeft * wallThickness;
            leftWallVerts[i * 4 + 3] = leftWallVerts[i * 4] + outLeft * wallThickness;

            rightWallVerts[i * 4] = rightEdges[i];
            rightWallVerts[i * 4 + 1] = rightEdges[i] + Vector3.up * wallHeight;
            rightWallVerts[i * 4 + 2] = rightWallVerts[i * 4 + 1] + outRight * wallThickness;
            rightWallVerts[i * 4 + 3] = rightWallVerts[i * 4] + outRight * wallThickness;
        }

        // ETAP 5: Mapowanie trójkątów
        for (int i = 0; i < count; i++)
        {
            int next_i = (i + 1) % count;

            int f_curr = i * 2;
            int f_next = next_i * 2;
            AddQuad(floorTris, i * 6, f_curr, f_curr + 1, f_next, f_next + 1);

            int wl_curr = i * 4;
            int wl_next = next_i * 4;
            int wr_curr = i * 4;
            int wr_next = next_i * 4;
            int wt = i * 18;

            AddQuad(leftWallTris, wt, wl_curr, wl_next, wl_curr + 1, wl_next + 1);
            AddQuad(leftWallTris, wt + 6, wl_curr + 1, wl_next + 1, wl_curr + 2, wl_next + 2);
            AddQuad(leftWallTris, wt + 12, wl_next + 3, wl_curr + 3, wl_next + 2, wl_curr + 2);

            AddQuad(rightWallTris, wt, wr_next, wr_curr, wr_next + 1, wr_curr + 1);
            AddQuad(rightWallTris, wt + 6, wr_next + 1, wr_curr + 1, wr_next + 2, wr_curr + 2);
            AddQuad(rightWallTris, wt + 12, wr_curr + 3, wr_next + 3, wr_curr + 2, wr_next + 2);
        }

        Color asphaltColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        Color wallColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        CreateMeshObject("Podloga", floorVerts, floorTris, asphaltColor, trackContainer.transform);
        CreateMeshObject("LewaSciana", leftWallVerts, leftWallTris, wallColor, trackContainer.transform);
        CreateMeshObject("PrawaSciana", rightWallVerts, rightWallTris, wallColor, trackContainer.transform);

        // Meta
        Vector2 lastPoint = points[points.Count - 1];
        Vector2 prevToLast = points[points.Count - 2];
        Vector3 metaCenter = new Vector3(lastPoint.x, 1f, lastPoint.y);
        Vector3 metaDir = (new Vector3(lastPoint.x, 0, lastPoint.y) - new Vector3(prevToLast.x, 0, prevToLast.y)).normalized;

        GameObject finishLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finishLine.name = "LiniaMety";
        finishLine.transform.SetParent(trackContainer.transform);
        finishLine.transform.position = metaCenter;
        finishLine.transform.rotation = Quaternion.LookRotation(metaDir);
        finishLine.transform.localScale = new Vector3(trackWidth, 4f, 1f);

        finishLine.GetComponent<MeshRenderer>().enabled = false;
        finishLine.GetComponent<BoxCollider>().isTrigger = true;
        finishLine.AddComponent<FinishLineTrigger>();
    }

    private void AddQuad(int[] tris, int startIndex, int bl, int br, int tl, int tr)
    {
        tris[startIndex] = bl;
        tris[startIndex + 1] = tl;
        tris[startIndex + 2] = tr;

        tris[startIndex + 3] = bl;
        tris[startIndex + 4] = tr;
        tris[startIndex + 5] = br;
    }

    private void CreateMeshObject(string name, Vector3[] verts, int[] tris, Color color, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        MeshFilter filter = obj.AddComponent<MeshFilter>();
        filter.mesh = mesh;

        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader != null)
        {
            renderer.material = new Material(urpShader);
            renderer.material.SetFloat("_Smoothness", 0.05f);
            renderer.material.SetFloat("_Cull", 0);
        }
        else
        {
            renderer.material = new Material(Shader.Find("Standard"));
        }

        renderer.material.color = color;

        MeshCollider collider = obj.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }
}