using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class InputController : MonoBehaviour
{
    private List<Vector2> rawPoints = new List<Vector2>();
    private List<Vector2> filteredPoints = new List<Vector2>();

    [Header("Ustawienia Filtra")]
    public int filterWindowSize = 3;
    public int smoothingPasses = 2;
    public float minPointDistance = 0.5f;
    public MinimapSetup minimapCamera;
    private LineRenderer lineRenderer;

    private bool isTrackDrawn = false;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = 8f;
        lineRenderer.endWidth = 8f;
        lineRenderer.numCornerVertices = 8;
        lineRenderer.numCapVertices = 8;

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        Color asphaltColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        lineRenderer.startColor = asphaltColor;
        lineRenderer.endColor = asphaltColor;

        lineRenderer.loop = false;
    }

    void Update()
    {
        if (isTrackDrawn) return;
        if (Input.GetMouseButton(0))
        {
            float depth = Mathf.Abs(Camera.main.transform.position.y);
            Vector3 mouseScreenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth);
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            Vector2 currentPoint = new Vector2(mousePos.x, mousePos.z);

            if (rawPoints.Count == 0 || Vector2.Distance(rawPoints[rawPoints.Count - 1], currentPoint) > minPointDistance)
            {
                RecordPoint(currentPoint.x, currentPoint.y);
                UpdateLine(rawPoints);
            }
        }

        if (Input.GetMouseButtonUp(0) && rawPoints.Count > 0)
        {
            Vector2 startPoint = rawPoints[0];
            Vector2 endPoint = rawPoints[rawPoints.Count - 1];

            float dist = Vector2.Distance(startPoint, endPoint);

            if (dist > 2f && rawPoints.Count > 2)
            {
                Vector2 endDir = (rawPoints[rawPoints.Count - 1] - rawPoints[rawPoints.Count - 2]).normalized;
                Vector2 startDir = (rawPoints[1] - rawPoints[0]).normalized;

                float controlDist = dist * 0.5f;

                Vector2 p0 = endPoint;
                Vector2 p1 = endPoint + endDir * controlDist;
                Vector2 p2 = startPoint - startDir * controlDist;
                Vector2 p3 = startPoint;

                int extraPoints = Mathf.CeilToInt(dist * 2f);

                for (int i = 1; i <= extraPoints; i++)
                {
                    float t = (float)i / (extraPoints + 1);

                    Vector2 newPoint = Mathf.Pow(1 - t, 3) * p0 +
                                       3 * Mathf.Pow(1 - t, 2) * t * p1 +
                                       3 * (1 - t) * Mathf.Pow(t, 2) * p2 +
                                       Mathf.Pow(t, 3) * p3;

                    rawPoints.Add(newPoint);
                }
            }

            ApplyMovingAverage();

            UpdateLine(filteredPoints);
            lineRenderer.loop = true;

            isTrackDrawn = true;

            Debug.Log("Zapisano! Rysowanie zablokowane. Punkty po filtracji: " + filteredPoints.Count);

            TrackGenerator generator = GetComponent<TrackGenerator>();
            if (generator != null)
            {
                generator.BuildTrackBoundaries(filteredPoints);
            }

            RacingLineCalculator calculator = GetComponent<RacingLineCalculator>();
            if (calculator != null && generator != null)
            {
                List<RacingLinePoint> optimalPath = calculator.CalculateOptimalLine(filteredPoints, generator.trackWidth);

                calculator.DrawOptimalLine(optimalPath);
            }

            MinimapSetup minimap = FindObjectOfType<MinimapSetup>();
            if (minimap != null)
            {
                minimap.ConfigureMinimap(filteredPoints);
            }

            DriveManager dm = FindObjectOfType<DriveManager>();
            if (dm != null)
            {
                dm.ShowButton(filteredPoints);
            }
            if (minimapCamera != null)
            {
                minimapCamera.ConfigureMinimap(filteredPoints);
            }
            else
            {
                Debug.LogError("Nie przypisałeś MinimapCamera do skryptu InputController w Inspectorze!");
            }
        }
    }

    public void RecordPoint(float x, float y)
    {
        rawPoints.Add(new Vector2(x, y));
    }

    private void ApplyMovingAverage()
    {
        if (rawPoints.Count == 0) return;

        List<Vector2> currentPoints = new List<Vector2>(rawPoints);

        for (int pass = 0; pass < smoothingPasses; pass++)
        {
            filteredPoints.Clear();

            for (int i = 0; i < currentPoints.Count; i++)
            {
                Vector2 sum = Vector2.zero;
                int count = 0;

                for (int j = -filterWindowSize; j <= filterWindowSize; j++)
                {
                    int neighborIndex = (i + j) % currentPoints.Count;
                    if (neighborIndex < 0) neighborIndex += currentPoints.Count;

                    sum += currentPoints[neighborIndex];
                    count++;
                }

                filteredPoints.Add(sum / count);
            }

            currentPoints = new List<Vector2>(filteredPoints);
        }
    }

    private void UpdateLine(List<Vector2> points)
    {
        lineRenderer.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            lineRenderer.SetPosition(i, new Vector3(points[i].x, 0.1f, points[i].y));
        }
    }

    public List<Vector2> GetProcessedPath()
    {
        return filteredPoints;
    }

    public void ClearData()
    {
        rawPoints.Clear();
        filteredPoints.Clear();
        lineRenderer.positionCount = 0;
        isTrackDrawn = false;
    }
}