using System.Collections.Generic;
using UnityEngine;

public class GhostCar : MonoBehaviour
{
    [Header("CAR SETUP")]
    public CarSetupData carData;
    /*[Range(20, 190)] public int maxSpeed = 90;
    [Range(1, 10)] public int accelerationMultiplier = 2;
    [Range(1, 10)] public int decelerationMultiplier = 2;
    [Range(0.1f, 1f)] public float steeringSpeed = 0.5f;*/
    [Header("PATH FOLLOWING")]
    public float waypointThreshold = 1.5f; // Jak blisko punktu musi być auto, by uznać go za "zaliczony"
    public int lookAheadPoints = 3;        // W ile punktów do przodu patrzy auto, żeby płynnie skręcać

    private List<RacingLinePoint> optimalPath;
    private int currentTargetIndex = 0;

    // Aktualna prędkość w m/s (metry na sekundę są potrzebne do fizyki w Unity)
    private float currentSpeedMs = 0f;
    private bool isRunning = false;

    /// <summary>
    /// Funkcja inicjująca – wywołaj ją, gdy trasa jest już policzona.
    /// </summary>
    public void SetPathAndStart(List<RacingLinePoint> path)
    {
        if (path == null || path.Count < 2) return;

        optimalPath = path;
        currentTargetIndex = 0;
        currentSpeedMs = 0f;

        // Ustawiamy auto na pierwszym punkcie trasy
        Vector3 startPos = new Vector3(optimalPath[0].Position.x, transform.position.y, optimalPath[0].Position.y);
        transform.position = startPos;

        // Ustawiamy rotację na drugi punkt
        Vector3 nextPos = new Vector3(optimalPath[1].Position.x, transform.position.y, optimalPath[1].Position.y);
        transform.rotation = Quaternion.LookRotation(nextPos - startPos);

        isRunning = true;
    }

    private void Update()
    {
        if (!isRunning || optimalPath == null) return;

        // --- 1. ODNALEZIENIE CELU ---
        RacingLinePoint targetPoint = optimalPath[currentTargetIndex];
        Vector3 targetPos3D = new Vector3(targetPoint.Position.x, transform.position.y, targetPoint.Position.y);

        // Jeśli jesteśmy wystarczająco blisko punktu, przełączamy się na następny
        if (Vector3.Distance(transform.position, targetPos3D) < waypointThreshold)
        {
            currentTargetIndex = (currentTargetIndex + 1) % optimalPath.Count;
        }

        // --- 2. KONTROLA PRĘDKOŚCI (Gaz i Hamulec) ---
        // Przeliczamy docelową prędkość z km/h na m/s (dzieląc przez 3.6)
        float targetSpeedMs = targetPoint.TargetSpeedKmh / 3.6f;

        // Jeśli auto jedzie wolniej niż cel, przyspieszamy
        if (currentSpeedMs < targetSpeedMs)
        {
            // Mnożnik * 5f to wartość bazowa, by accelerationMultiplier = 2 miał odczuwalną moc
            currentSpeedMs += carData.accelerationMultiplier * 5f * Time.deltaTime;
            currentSpeedMs = Mathf.Min(currentSpeedMs, targetSpeedMs); // Nie przekraczamy celu
        }
        // Jeśli auto jedzie szybciej niż cel (weszło w strefę hamowania), zwalniamy
        else if (currentSpeedMs > targetSpeedMs)
        {
            currentSpeedMs -= carData.decelerationMultiplier * 10f * Time.deltaTime; // Hamowanie zwykle jest mocniejsze niż gaz
            currentSpeedMs = Mathf.Max(currentSpeedMs, targetSpeedMs);
        }

        // --- 3. PORUSZANIE SIĘ (Silnik) ---
        // Popychamy auto do przodu (wzdłuż jego własnej osi Z)
        transform.position += transform.forward * currentSpeedMs * Time.deltaTime;

        // --- 4. STEROWANIE (Kierownica) ---
        // Aby auto nie "trzęsło się" jadąc od punktu do punktu, patrzymy trochę dalej w przód na trasie
        int steeringTargetIndex = (currentTargetIndex + lookAheadPoints) % optimalPath.Count;
        Vector3 steeringTargetPos = new Vector3(optimalPath[steeringTargetIndex].Position.x, transform.position.y, optimalPath[steeringTargetIndex].Position.y);
        Vector3 directionToTarget = (steeringTargetPos - transform.position).normalized;
        if (directionToTarget != Vector3.zero)
        {
            // Obliczamy idealny kąt obrotu
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // Płynnie obracamy kierownicę (Slerp). Używamy steeringSpeed pomnożonego przez czas i bazową wartość
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, steeringSpeed * 10f * Time.deltaTime);
        }
    }
}
