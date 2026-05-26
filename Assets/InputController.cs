using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class InputController : MonoBehaviour
{
    // Listy na punkty z Twojego diagramu klas
    private List<Vector2> rawPoints = new List<Vector2>();
    private List<Vector2> filteredPoints = new List<Vector2>();

    [Header("Ustawienia Filtra")]
    public int filterWindowSize = 3;     // Ile punktów w tył/przód bierze pod uwagę (zgodne z diagramem)
    public int smoothingPasses = 2;      // Ile razy ma wygładzić trasę (im więcej, tym gładsza)
    public float minPointDistance = 0.5f;

    // Referencja do naszego narzędzia rysującego
    private LineRenderer lineRenderer;

    // Ta flaga zablokuje możliwość dalszego rysowania po puszczeniu myszki
    private bool isTrackDrawn = false;

    void Start()
    {
        // Konfiguracja pędzla na starcie gry
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;

        // Zamykamy linię (łączy ostatni punkt z pierwszym)
        lineRenderer.loop = true;
    }

    void Update()
    {
        // Jeśli trasa została już narysowana, ucinamy dalsze działanie (blokada)
        if (isTrackDrawn) return;

        // Sprawdza, czy trzymasz lewy przycisk myszy
        if (Input.GetMouseButton(0))
        {
            // Przeliczamy pozycję myszy z ekranu na świat w grze
            Vector3 mouseScreenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f);
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            Vector2 currentPoint = new Vector2(mousePos.x, mousePos.y);

            // Dodajemy punkt tylko jeśli lista jest pusta ALBO odsunęliśmy mysz od ostatniego punktu
            if (rawPoints.Count == 0 || Vector2.Distance(rawPoints[rawPoints.Count - 1], currentPoint) > minPointDistance)
            {
                RecordPoint(currentPoint.x, currentPoint.y);

                // Aktualizujemy rysunek na bieżąco, pokazując surową, niefiltrowaną linię
                UpdateLine(rawPoints);
            }
        }

        // Kiedy puścisz przycisk myszy, kończymy rysowanie i odpalamy wygładzanie
        if (Input.GetMouseButtonUp(0) && rawPoints.Count > 0)
        {
            ApplyMovingAverage();

            // Po puszczeniu myszki odświeżamy rysunek – podmieniamy go na wygładzoną wersję!
            UpdateLine(filteredPoints);

            // Aktywacja blokady rysowania
            isTrackDrawn = true;
            Debug.Log("Zapisano! Rysowanie zablokowane. Punkty po filtracji: " + filteredPoints.Count);

            // --- INTEGRACJA Z TRACK GENERATOREM ---
            // Przekazanie wygładzonej trasy do silnika geometrii
            TrackGenerator generator = GetComponent<TrackGenerator>();
            if (generator != null)
            {
                generator.BuildTrackBoundaries(filteredPoints);
            }
        }
    }

    // Metoda z diagramu klas: do zapisywania surowych punktów
    public void RecordPoint(float x, float y)
    {
        rawPoints.Add(new Vector2(x, y));
    }

    // Algorytm filtra Moving Average z diagramu klas (wzbogacony o łączenie początku z końcem)
    private void ApplyMovingAverage()
    {
        if (rawPoints.Count == 0) return;

        // Kopiujemy surowe punkty jako punkt startowy do pierwszej pętli wygładzania
        List<Vector2> currentPoints = new List<Vector2>(rawPoints);

        // Nakładamy wygładzanie tyle razy, ile ustawiono w smoothingPasses
        for (int pass = 0; pass < smoothingPasses; pass++)
        {
            filteredPoints.Clear();

            for (int i = 0; i < currentPoints.Count; i++)
            {
                Vector2 sum = Vector2.zero;
                int count = 0;

                for (int j = -filterWindowSize; j <= filterWindowSize; j++)
                {
                    // "Matematyczne" połączenie początku z końcem toru, by na łączeniu też było gładko
                    int neighborIndex = (i + j) % currentPoints.Count;
                    if (neighborIndex < 0) neighborIndex += currentPoints.Count;

                    sum += currentPoints[neighborIndex];
                    count++;
                }

                // Dodajemy uśrednioną pozycję do nowej listy
                filteredPoints.Add(sum / count);
            }

            // Podmieniamy listę do ewentualnej kolejnej iteracji pętli
            currentPoints = new List<Vector2>(filteredPoints);
        }
    }

    // Metoda, która fizycznie układa punkty linii w oknie gry Unity
    private void UpdateLine(List<Vector2> points)
    {
        lineRenderer.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            lineRenderer.SetPosition(i, new Vector3(points[i].x, points[i].y, 0f));
        }
    }

    // Metoda z diagramu klas: pozwalająca innym klasom pobrać gotową trasę
    public List<Vector2> GetProcessedPath()
    {
        return filteredPoints;
    }

    // Metoda z diagramu klas: czyszcząca dane
    public void ClearData()
    {
        rawPoints.Clear();
        filteredPoints.Clear();
        lineRenderer.positionCount = 0;
        isTrackDrawn = false; // Zdejmujemy blokadę, żeby można było narysować nowy tor
    }
}