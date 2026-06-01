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
    [Header("REFERENCJE")]
    public PrometeoCarController playerCar; // Przypnij samochód gracza!
    [Space(10)]
    public float carWidth = 2.0f; // Szerokość auta zostawiamy tutaj, bo Prometeo jej nie kalkuluje
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

        // 1. Obliczanie wektorów normalnych z odpornością na gęste punkty
        float normalLookAhead = Mathf.Max(3f, trackWidth * 0.5f); // Dynamiczny dystans!

        for (int i = 0; i < count; i++)
        {
            Vector2 pPrev = GetPointAtDistanceBackward(optPositions, i, normalLookAhead);
            Vector2 pNext = GetPointAtDistanceForward(optPositions, i, normalLookAhead);
            Vector2 forward = (pNext - pPrev).normalized;
            normals[i] = new Vector2(-forward.y, forward.x);
        }


        // 2. TWORZENIE OPTYMALNEJ TRASY (Naciąganie struny do wewnątrz zakrętów)
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

        // 3. OBLICZANIE PRĘDKOŚCI NA OPTYMALNEJ TRASIE
        float speedLookAhead = Mathf.Max(5f, trackWidth * 1.2f); // Patrzymy bardzo daleko w przód!

        for (int i = 0; i < count; i++)
        {
            Vector2 pCurr = optPositions[i];
            Vector2 pPrev = GetPointAtDistanceBackward(optPositions, i, speedLookAhead);
            Vector2 pNext = GetPointAtDistanceForward(optPositions, i, speedLookAhead);

            Vector2 dirIn = pCurr - pPrev;
            Vector2 dirOut = pNext - pCurr;

            float angle = 0f;
            if (dirIn.sqrMagnitude > 0.01f && dirOut.sqrMagnitude > 0.01f)
            {
                angle = Vector2.Angle(dirIn.normalized, dirOut.normalized);
            }

            // Zmieniamy dzielnik na 90f. Patrząc tak daleko w przód, kąt 90+ stopni to już brutalny nawrót.
            float curveSeverity = Mathf.Clamp01(angle / 90f);
            float minSpeed = 15f;
            float safeSpeed = Mathf.Lerp(playerCar.maxSpeed, minSpeed, curveSeverity);

            optimalLine.Add(new RacingLinePoint
            {
                Position = pCurr,
                TargetSpeedKmh = Mathf.Clamp(safeSpeed, minSpeed, playerCar.maxSpeed)
            });
        }

        // 4. Nakładanie stref hamowania
        return ApplyBrakingZones(optimalLine);
    }

    private Vector2 GetPointAtDistanceBackward(Vector2[] points, int startIndex, float targetDistance)
    {
        float currentDist = 0;
        int currentIndex = startIndex;
        int failsafe = 0; // Zabezpiecza przed zawieszeniem Unity

        while (currentDist < targetDistance && failsafe < points.Length)
        {
            int prevIndex = (currentIndex - 1 + points.Length) % points.Length;
            currentDist += Vector2.Distance(points[currentIndex], points[prevIndex]);
            currentIndex = prevIndex;
            failsafe++;
        }
        return points[currentIndex];
    }

    private Vector2 GetPointAtDistanceForward(Vector2[] points, int startIndex, float targetDistance)
    {
        float currentDist = 0;
        int currentIndex = startIndex;
        int failsafe = 0;

        while (currentDist < targetDistance && failsafe < points.Length)
        {
            int nextIndex = (currentIndex + 1) % points.Length;
            currentDist += Vector2.Distance(points[currentIndex], points[nextIndex]);
            currentIndex = nextIndex;
            failsafe++;
        }
        return points[currentIndex];
    }

    private List<RacingLinePoint> ApplyBrakingZones(List<RacingLinePoint> line)
    {
        float decelerationRate = playerCar.brakeForce / 75f;

        // Podwójna pętla! Auto będzie poprawnie hamować przed zakrętem, nawet jeśli zakręt jest zaraz za Start/Metą
        for (int pass = 0; pass < 2; pass++)
        {
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
        }
        return line;
    }

    public void DrawOptimalLine(List<RacingLinePoint> optimalLine)
    {
        if (optimalLine == null || optimalLine.Count == 0) return;

        if (optimalLineRenderer == null)
        {
            GameObject lineObj = new GameObject("OptymalnaLiniaWyscigowa_Predkosc");
            optimalLineRenderer = lineObj.AddComponent<LineRenderer>();

            optimalLineRenderer.startWidth = 1.5f;
            optimalLineRenderer.endWidth = 1.5f;
            optimalLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            optimalLineRenderer.colorGradient = new Gradient();
            optimalLineRenderer.numCornerVertices = 5;
            optimalLineRenderer.loop = true;
        }

        optimalLineRenderer.positionCount = optimalLine.Count;

        Vector3[] positions = new Vector3[optimalLine.Count];
        for (int i = 0; i < optimalLine.Count; i++)
        {
            positions[i] = new Vector3(optimalLine[i].Position.x, 0.2f, optimalLine[i].Position.y);
        }
        optimalLineRenderer.SetPositions(positions);

        Gradient colorGradient = new Gradient();
        colorGradient.mode = GradientMode.Blend;

        GradientColorKey[] colorKeys = new GradientColorKey[optimalLine.Count];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[optimalLine.Count];

        for (int i = 0; i < optimalLine.Count; i++)
        {
            float currentSpeed = optimalLine[i].TargetSpeedKmh;
            float minSpeed = 15f;
            float speedFactor = Mathf.InverseLerp(minSpeed, (float)playerCar.maxSpeed, currentSpeed);

            Color pointColor = EvaluateSpeedColor(speedFactor);
            float gradientTime = (float)i / (optimalLine.Count - 1);

            colorKeys[i] = new GradientColorKey(pointColor, gradientTime);
            alphaKeys[i] = new GradientAlphaKey(1.0f, gradientTime);
        }

        if (colorKeys.Length > 8)
        {
            colorGradient = GenerateSimplifiedGradient(colorKeys, alphaKeys);
        }
        else
        {
            colorGradient.SetKeys(colorKeys, alphaKeys);
        }

        optimalLineRenderer.colorGradient = colorGradient;
    }

    private Color EvaluateSpeedColor(float factor)
    {
        if (factor > 0.5f)
        {
            float t = (factor - 0.5f) * 2f;
            return Color.Lerp(Color.yellow, Color.green, t);
        }
        else
        {
            float t = factor * 2f;
            return Color.Lerp(Color.red, Color.yellow, t);
        }
    }

    private Gradient GenerateSimplifiedGradient(GradientColorKey[] allColors, GradientAlphaKey[] allAlphas)
    {
        Gradient grad = new Gradient();
        GradientColorKey[] shortColors = new GradientColorKey[8];
        GradientAlphaKey[] shortAlphas = new GradientAlphaKey[8];

        int count = allColors.Length;
        for (int i = 0; i < 8; i++)
        {
            int index = Mathf.FloorToInt(((float)i / 7f) * (count - 1));
            float time = (float)i / 7f;
            shortColors[i] = new GradientColorKey(allColors[index].color, time);
            shortAlphas[i] = new GradientAlphaKey(1f, time);
        }

        grad.SetKeys(shortColors, shortAlphas);
        return grad;
    }
}
