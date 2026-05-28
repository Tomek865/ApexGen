using System.Collections.Generic;
using UnityEngine;

public class TrackGenerator : MonoBehaviour
{
    [Header("Ustawienia Fizyki Toru")]
    public float trackWidth = 8f;
    public float wallHeight = 3f;
    public float overlap = 1.5f;

    private GameObject trackColliders;

    public void BuildTrackBoundaries(List<Vector2> points)
    {
        if (points.Count < 2) return;
        if (trackColliders != null) Destroy(trackColliders);

        trackColliders = new GameObject("WidocznyTor3D");

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 current = points[i];
            Vector2 next = points[(i + 1) % points.Count];

            Vector3 p1 = new Vector3(current.x, 0, current.y);
            Vector3 p2 = new Vector3(next.x, 0, next.y);

            Vector3 center = (p1 + p2) / 2f;
            float distance = Vector3.Distance(p1, p2) + overlap;

            Vector3 direction = (p2 - p1).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Podloga_" + i;
            floor.transform.SetParent(trackColliders.transform);
            floor.transform.position = center + new Vector3(0, -0.25f, 0);
            floor.transform.rotation = rotation;
            floor.transform.localScale = new Vector3(trackWidth, 0.5f, distance);

            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LewaSciana_" + i;
            leftWall.transform.SetParent(trackColliders.transform);
            leftWall.transform.position =
                center +
                (rotation * Vector3.left * (trackWidth / 2f)) +
                new Vector3(0, wallHeight / 2f, 0);

            leftWall.transform.rotation = rotation;
            leftWall.transform.localScale = new Vector3(1f, wallHeight, distance);

            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "PrawaSciana_" + i;
            rightWall.transform.SetParent(trackColliders.transform);
            rightWall.transform.position =
                center +
                (rotation * Vector3.right * (trackWidth / 2f)) +
                new Vector3(0, wallHeight / 2f, 0);

            rightWall.transform.rotation = rotation;
            rightWall.transform.localScale = new Vector3(1f, wallHeight, distance);
        }

        Vector2 lastPoint = points[points.Count - 1];
        Vector2 prevToLast = points[points.Count - 2];
        Vector3 metaCenter = new Vector3(lastPoint.x, 1f, lastPoint.y);
        Vector3 metaDir = (new Vector3(lastPoint.x, 0, lastPoint.y) - new Vector3(prevToLast.x, 0, prevToLast.y)).normalized;

        GameObject finishLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finishLine.name = "LiniaMety";
        finishLine.transform.SetParent(trackColliders.transform);
        finishLine.transform.position = metaCenter;
        finishLine.transform.rotation = Quaternion.LookRotation(metaDir);
        finishLine.transform.localScale = new Vector3(trackWidth, 4f, 1f);

        finishLine.GetComponent<MeshRenderer>().enabled = false;
        finishLine.GetComponent<BoxCollider>().isTrigger = true;
        finishLine.AddComponent<FinishLineTrigger>();
    }
}