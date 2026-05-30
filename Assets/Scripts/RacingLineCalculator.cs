using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RacingLinePoint
{
    public Vector2 Position;
    public float TargetSpeedKmh;
}

public class RacingLineCalculator : MonoBehaviour
{
    [Space(10)]
    public int maxSpeed = 90;
    public int maxSteeringAngle = 27;
    public int brakeForce = 350;

    [Space(10)]
    public float carWidth = 2.0f;
    public int optimizationIterations = 150;
    private LineRenderer optimalLineRenderer;

    public List<RacingLinePoint> CalculateOptimalLine(List<Vector2> centerPoints, float trackWidth)
    {
        if (centerPoints == null || centerPoints.Count < 3)
            return new List<RacingLinePoint>();

        int count = centerPoints.Count;
        List<RacingLinePoint> optimalLine = new List<RacingLinePoint>();

        float maxOffset = (trackWidth / 2f) - (carWidth / 2f) - 0.5f;
        if (maxOffset < 0) maxOffset = 0;

        Vector2[] optPositions = centerPoints.ToArray();
        Vector2[] normals = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            int prevIdx = (i - 1 + count) % count;
            int nextIdx = (i + 1) % count;
            Vector2 forward = (centerPoints[nextIdx] - centerPoints[prevIdx]).normalized;
            normals[i] = new Vector2(-forward.y, forward.x);
        }

        for (int iter = 0; iter < optimizationIterations; iter++)
        {
            for (int i = 0; i < count; i++)
            {
                int prev = (i - 1 + count) % count;
                int next = (i + 1) % count;

                Vector2 relaxedPos = (optPositions[prev] + optPositions[next]) * 0.5f;

                Vector2 offsetVec = relaxedPos - centerPoints[i];

                float dotProduct = Vector2.Dot(offsetVec, normals[i]);

                dotProduct = Mathf.Clamp(dotProduct, -maxOffset, maxOffset);

                optPositions[i] = centerPoints[i] + normals[i] * dotProduct;
            }
        }

        float lookAheadDistance = 3.0f;

        for (int i = 0; i < count; i++)
        {
            Vector2 pCurr = optPositions[i];
            Vector2 pPrev = GetPointAtDistanceBackward(optPositions, i, lookAheadDistance);
            Vector2 pNext = GetPointAtDistanceForward(optPositions, i, lookAheadDistance);

            Vector2 dirIn = (pCurr - pPrev).normalized;
            Vector2 dirOut = (pNext - pCurr).normalized;

            float angle = Vector2.Angle(dirIn, dirOut);

            float steeringFactor = Mathf.Clamp01(maxSteeringAngle / 35f);
            float safeSpeed = maxSpeed * steeringFactor * Mathf.Lerp(1f, 0.15f, angle / 45f);

            optimalLine.Add(new RacingLinePoint
            {
                Position = pCurr,
                TargetSpeedKmh = Mathf.Clamp(safeSpeed, 15f, maxSpeed)
            });
        }

        return ApplyBrakingZones(optimalLine);
    }

    private Vector2 GetPointAtDistanceBackward(Vector2[] points, int startIndex, float targetDistance)
    {
        float currentDist = 0;
        int currentIndex = startIndex;

        while (currentDist < targetDistance)
        {
            int prevIndex = (currentIndex - 1 + points.Length) % points.Length;
            currentDist += Vector2.Distance(points[currentIndex], points[prevIndex]);
            currentIndex = prevIndex;
        }
        return points[currentIndex];
    }

    private Vector2 GetPointAtDistanceForward(Vector2[] points, int startIndex, float targetDistance)
    {
        float currentDist = 0;
        int currentIndex = startIndex;

        while (currentDist < targetDistance)
        {
            int nextIndex = (currentIndex + 1) % points.Length;
            currentDist += Vector2.Distance(points[currentIndex], points[nextIndex]);
            currentIndex = nextIndex;
        }
        return points[currentIndex];
    }

    private List<RacingLinePoint> ApplyBrakingZones(List<RacingLinePoint> line)
    {
        float decelerationRate = brakeForce / 75f;

        for (int i = line.Count - 1; i >= 0; i--)
        {
            int prevIdx = (i - 1 + line.Count) % line.Count;
            float distance = Vector2.Distance(line[prevIdx].Position, line[i].Position);

            float maxSpeedGivenBrakes = line[i].TargetSpeedKmh + (distance * decelerationRate);

            if (line[prevIdx].TargetSpeedKmh > maxSpeedGivenBrakes)
            {
                RacingLinePoint modifiedPoint = line[prevIdx];
                modifiedPoint.TargetSpeedKmh = maxSpeedGivenBrakes;
                line[prevIdx] = modifiedPoint;
            }
        }
        return line;
    }
    public void DrawOptimalLine(List<RacingLinePoint> optimalLine)
    {
        if (optimalLine == null || optimalLine.Count == 0) return;

        if (optimalLineRenderer == null)
        {
            GameObject lineObj = new GameObject("CzerwonaLiniaWyscigowa");
            optimalLineRenderer = lineObj.AddComponent<LineRenderer>();

            optimalLineRenderer.startWidth = 1.5f;
            optimalLineRenderer.endWidth = 1.5f;
            optimalLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            optimalLineRenderer.startColor = Color.red;
            optimalLineRenderer.endColor = Color.red;
            optimalLineRenderer.numCornerVertices = 5;

            optimalLineRenderer.loop = true;
        }

        optimalLineRenderer.positionCount = optimalLine.Count;

        for (int i = 0; i < optimalLine.Count; i++)
        {
            optimalLineRenderer.SetPosition(i, new Vector3(optimalLine[i].Position.x, 0.2f, optimalLine[i].Position.y));
        }
    }
}
