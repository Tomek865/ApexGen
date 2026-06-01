using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

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

    [Header("UI References")]
    public TextMeshProUGUI modeButtonText;
    private bool isDrawingModeActive = false;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = 8f;
        lineRenderer.endWidth = 8f;
        lineRenderer.numCornerVertices = 8;
        lineRenderer.numCapVertices = 8;

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
        lineRenderer.loop = false;

        if (modeButtonText != null)
        {
            modeButtonText.text = "[ MODE: POINTER ]";
            modeButtonText.color = new Color(0.4f, 0.4f, 0.4f);
        }
    }

    void Update()
    {
        if (isTrackDrawn || !isDrawingModeActive) return;

        if (EventSystem.current.IsPointerOverGameObject())
        {
            if (!IsPointerOverDrawingArea()) return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (rawPoints.Count > 0)
            {
                rawPoints.Clear();
                filteredPoints.Clear();
                lineRenderer.positionCount = 0;
                lineRenderer.loop = false;
            }
			WelcomeTerminal welcome = Object.FindAnyObjectByType<WelcomeTerminal>();
            if (welcome != null) welcome.HideMessage();
            if (modeButtonText != null)
            {
                modeButtonText.text = "[ MODE: DRAWING ]";
                modeButtonText.color = new Color(0f, 1f, 0f);
            }
        }

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
                    Vector2 newPoint = Mathf.Pow(1 - t, 3) * p0 + 3 * Mathf.Pow(1 - t, 2) * t * p1 + 3 * (1 - t) * Mathf.Pow(t, 2) * p2 + Mathf.Pow(t, 3) * p3;
                    rawPoints.Add(newPoint);
                }
            }

            ApplyMovingAverage();
            UpdateLine(filteredPoints);
            lineRenderer.loop = true;
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

    private bool IsPointerOverDrawingArea()
    {
        if (EventSystem.current == null) return false;
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.name == "DrawingPanel") return true;
        }
        return false;
    }

    public void ToggleDrawingMode()
    {
        if (isTrackDrawn) return;
        isDrawingModeActive = !isDrawingModeActive;
        if (modeButtonText != null)
        {
            modeButtonText.text = isDrawingModeActive ? "[ MODE: DRAWING ]" : "[ MODE: DRAWING_OFF ]";
            modeButtonText.color = isDrawingModeActive ? new Color(0f, 1f, 0f) : new Color(0.4f, 0.4f, 0.4f);
        }
    }

    public void GenerateTrackFromUI()
    {
        if (filteredPoints.Count < 2 || isTrackDrawn) return;

        isTrackDrawn = true;
        TrackGenerator generator = GetComponent<TrackGenerator>();
        float safeDistance = (generator != null) ? generator.trackWidth + 1.5f : 9.5f;
        bool hasIntersections = IsTrackIntersecting(filteredPoints);
        bool isTooClose = IsTrackTooClose(filteredPoints, safeDistance);
        bool hasSharpTurns = HasSharpTurns(filteredPoints, 35f);

        if (hasIntersections || isTooClose || hasSharpTurns)
        {
            Debug.LogWarning("Wykryto błędną geometrię toru! Generowanie zablokowane.");

            if (modeButtonText != null)
            {
                if (hasIntersections)
                    modeButtonText.text = "[ BŁĄD: TOR SIĘ PRZECINA! ]";
                else if (hasSharpTurns)
                    modeButtonText.text = "[ BŁĄD: ZBYT OSTRY ZAKRĘT! ]";
                else
                    modeButtonText.text = "[ BŁĄD: TRASY ZBYT BLISKO SIEBIE! ]";

                modeButtonText.color = Color.red;
            }

            rawPoints.Clear();
            filteredPoints.Clear();
            lineRenderer.positionCount = 0;
            lineRenderer.loop = false;

            return;
        }

        isTrackDrawn = true;
        if (generator != null) generator.BuildTrackBoundaries(filteredPoints);

        List<RacingLinePoint> optimalPath = null;

        RacingLineCalculator calculator = GetComponent<RacingLineCalculator>();
        if (calculator != null && generator != null)
        {
            optimalPath = calculator.CalculateOptimalLine(filteredPoints, generator.trackWidth);
            calculator.DrawOptimalLine(optimalPath);
        }

        MinimapSetup minimap = Object.FindAnyObjectByType<MinimapSetup>();
        if (minimap != null) minimap.ConfigureMinimap(filteredPoints);

        if (minimapCamera != null) minimapCamera.ConfigureMinimap(filteredPoints);

        DriveManager dm = Object.FindAnyObjectByType<DriveManager>();
        if (dm != null)
        {
            dm.ShowButton(filteredPoints);

            if (optimalPath != null)
            {
                dm.ReceiveOptimalLine(optimalPath);
            }
            dm.StartDriving();
        }
        lineRenderer.enabled = false;
        CarPanelTerminal terminal = GetComponent<CarPanelTerminal>();
        if (terminal != null) terminal.BootUpPanel();
    }

    public void ClearData()
    {
        rawPoints.Clear();
        filteredPoints.Clear();
        lineRenderer.positionCount = 0;
        lineRenderer.loop = false;
        lineRenderer.enabled = true;
        isTrackDrawn = false;

        isDrawingModeActive = false;
        if (modeButtonText != null)
        {
            modeButtonText.text = "[ MODE: POINTER ]";
            modeButtonText.color = new Color(0.4f, 0.4f, 0.4f);
        }

        if (Camera.main != null)
        {
            Camera.main.transform.SetParent(null);
            Camera.main.transform.position = new Vector3(0, 50, 0);
            Camera.main.transform.rotation = Quaternion.Euler(90, 0, 0);
            Camera.main.orthographic = true;
            Camera.main.orthographicSize = 80f;
        }

        string[] objectsToDestroy = new string[]
        {
            "CiaglyTor3D",
            "Podloga",
            "LewaSciana",
            "PrawaSciana",
            "OptimalRacingLine",
            "CzerwonaLiniaWyscigowa"
        };

        foreach (string objName in objectsToDestroy)
        {
            GameObject obj = GameObject.Find(objName);
            if (obj != null) Destroy(obj);
        }

        GameObject car = GameObject.FindGameObjectWithTag("Player");
        if (car != null) Destroy(car);

        DriveManager dm = Object.FindAnyObjectByType<DriveManager>();
        if (dm != null)
        {
            dm.ResetDriveManager();
        }

        CarPanelTerminal terminal = GetComponent<CarPanelTerminal>();
        if (terminal != null) terminal.ResetPanel();
        // 7. Reset Minimapy (jeśli rysuje własną linię)
        MinimapSetup minimap = Object.FindAnyObjectByType<MinimapSetup>();
        if (minimap != null);
    }
    private bool HasSharpTurns(List<Vector2> points, float maxAngle)
    {
        if (points == null || points.Count < 3) return false;

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 prev = points[(i - 1 + points.Count) % points.Count];
            Vector2 curr = points[i];
            Vector2 next = points[(i + 1) % points.Count];

            Vector2 dirIn = (curr - prev).normalized;
            Vector2 dirOut = (next - curr).normalized;

            float angle = Vector2.Angle(dirIn, dirOut);

            if (angle > maxAngle)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsTrackTooClose(List<Vector2> points, float minSafeDistance)
    {
        if (points == null || points.Count < 10) return false;

        int indexOffsetTolerance = Mathf.CeilToInt((minSafeDistance * 2.5f) / minPointDistance);

        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + indexOffsetTolerance; j < points.Count; j++)
            {
                if (i < indexOffsetTolerance && j > points.Count - indexOffsetTolerance) continue;

                float distance = Vector2.Distance(points[i], points[j]);

                if (distance < minSafeDistance)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsTrackIntersecting(List<Vector2> points)
    {
        if (points == null || points.Count < 4) return false;

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[(i + 1) % points.Count];

            for (int j = i + 2; j < points.Count; j++)
            {
                if (i == 0 && j == points.Count - 1) continue;
                if (j == (i - 1 + points.Count) % points.Count) continue;

                Vector2 p3 = points[j];
                Vector2 p4 = points[(j + 1) % points.Count];

                if (TryGetIntersection(p1, p2, p3, p4))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryGetIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float denominator = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);

        if (denominator == 0) return false;

        float ua = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / denominator;
        float ub = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / denominator;

        if (ua > 0.01f && ua < 0.99f && ub > 0.01f && ub < 0.99f)
        {
            return true;
        }

        return false;
    }
}
