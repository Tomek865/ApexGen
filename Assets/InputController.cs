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

    private LineRenderer lineRenderer;

    private bool isTrackDrawn = false;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;

        lineRenderer.loop = true;
    }

    void Update()
    {
        if (isTrackDrawn) return;

        if (Input.GetMouseButton(0))
        {
            Vector3 mouseScreenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f);
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            Vector2 currentPoint = new Vector2(mousePos.x, mousePos.y);

            if (rawPoints.Count == 0 || Vector2.Distance(rawPoints[rawPoints.Count - 1], currentPoint) > minPointDistance)
            {
                RecordPoint(currentPoint.x, currentPoint.y);
                UpdateLine(rawPoints);
            }
        }

        if (Input.GetMouseButtonUp(0) && rawPoints.Count > 0)
        {
            ApplyMovingAverage();

            UpdateLine(filteredPoints);

            isTrackDrawn = true;
            Debug.Log("Zapisano! Rysowanie zablokowane. Punkty po filtracji: " + filteredPoints.Count);

            TrackGenerator generator = GetComponent<TrackGenerator>();
            if (generator != null)
            {
                generator.BuildTrackBoundaries(filteredPoints);
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
            lineRenderer.SetPosition(i, new Vector3(points[i].x, points[i].y, 0f));
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