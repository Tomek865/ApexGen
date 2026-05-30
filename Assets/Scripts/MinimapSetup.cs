using System.Collections.Generic;
using UnityEngine;

public class MinimapSetup : MonoBehaviour
{
    [Header("Ustawienia")]
    public float cameraHeight = 50f;
    public float paddingMultiplier = 1.15f;

    public void ConfigureMinimap(List<Vector2> points)
    {
        if (points == null || points.Count == 0) return;

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        foreach (Vector2 p in points)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minZ) minZ = p.y;
            if (p.y > maxZ) maxZ = p.y;
        }

        float centerX = (minX + maxX) / 2f;
        float centerZ = (minZ + maxZ) / 2f;

        transform.position = new Vector3(centerX, cameraHeight, centerZ);

        float trackWidth = maxX - minX;
        float trackHeight = maxZ - minZ;

        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            float largestDimension = Mathf.Max(trackWidth, trackHeight);

            cam.orthographicSize = (largestDimension / 2f) * paddingMultiplier;
        }
    }
}