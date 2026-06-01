using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DriveManager : MonoBehaviour
{
    [Header("UI i Kamera")]
    public GameObject startButton; 
    public Camera mainCamera;
    public TextMeshProUGUI timerText;

    [Header("Telemetria (Konfiguracja w kodzie)")]
    public float maxDotRange = 50f; 

    [Header("Pojazd")]
    public GameObject carPrefab;

    private Vector2 startPoint;
    private Vector2 nextPoint;

    private bool isTimerRunning = false;
    private float currentTime = 0f;

    [Header("Okrążenia")]
    public int currentLap = 0;
    public List<float> lapTimes = new List<float>();

    // --- ZMIENNE DO TELEMETRII ---
    private Rigidbody carRb;
    private PrometeoCarController carController;
    
    // Prywatne referencje interfejsu UI
    private RectTransform gForceDot;
    private TextMeshProUGUI tractionLossText;
    private TextMeshProUGUI speedText;

    void Start()
    {
        // 1. AUTOMATYCZNE WYSZUKIWANIE ELEMENTÓW HUD

        // Wyszukiwanie G-Force Dot
        GameObject foundGForce = GameObject.Find("GForce_Dot"); 
        if (foundGForce != null)
        {
            gForceDot = foundGForce.GetComponent<RectTransform>();
            Debug.Log("Telemetria: Znaleziono G-Force Dot!");
        }

        // Wyszukiwanie Traction Loss Text
        GameObject foundTractionText = GameObject.Find("TractionLossText"); 
        if (foundTractionText != null)
        {
            tractionLossText = foundTractionText.GetComponent<TextMeshProUGUI>();
            foundTractionText.SetActive(false); 
            Debug.Log("Telemetria: Znaleziono Traction Loss Text!");
        }

        // Wyszukiwanie wskaźnika prędkości (SpeedText)
        GameObject foundSpeedText = GameObject.Find("SpeedText");
        if (foundSpeedText != null)
        {
            speedText = foundSpeedText.GetComponent<TextMeshProUGUI>();
            speedText.text = "000 km/h"; 
            speedText.gameObject.SetActive(false); // Ukryte podczas rysowania
            Debug.Log("Telemetria: Znaleziono SpeedText!");
        }
        else
        {
            Debug.LogWarning("Telemetria: Nie znaleziono obiektu 'SpeedText' w Hierarchy!");
        }

        // 2. STOPER
        if (timerText != null)
        {
            timerText.text = "00:00.00";
            timerText.gameObject.SetActive(false); // Ukryte podczas rysowania
        }
    }

    void Update()
    {
        // Obsługa stopera
        if (isTimerRunning && timerText != null)
        {
            currentTime += Time.deltaTime;
            int minutes = Mathf.FloorToInt(currentTime / 60F);
            int seconds = Mathf.FloorToInt(currentTime % 60F);
            int fraction = Mathf.FloorToInt((currentTime * 100F) % 100F);
            timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, fraction);
        }

        // Aktualizacja danych na ekranie
        UpdateTelemetry(); 
    }

    private void UpdateTelemetry()
    {
        // Blokada, jeśli auto jeszcze nie zespawnowało się na torze
        if (carRb == null || carController == null) return;

        // 1. Obsługa Traction Loss
        if (tractionLossText != null)
        {
            tractionLossText.gameObject.SetActive(carController.isDrifting);
        }

        // 2. Obsługa cyfrowego prędkościomierza
        if (speedText != null)
        {
            int currentSpeed = Mathf.FloorToInt(carController.carSpeed);
            // Zabezpieczenie przed wyświetlaniem minusowych wartości na wstecznym
            currentSpeed = Mathf.Abs(currentSpeed); 
            speedText.text = string.Format("{0:D3} km/h", currentSpeed);
        }

        // 3. Obsługa G-Force Meter
        if (gForceDot != null)
        {
            float speedFactor = Mathf.Clamp(carController.carSpeed / 50f, 0f, 2f);

            float inputZ = -Input.GetAxis("Vertical"); 
            float inputX = -Input.GetAxis("Horizontal");

            float targetX = inputX * maxDotRange * speedFactor;
            float targetY = inputZ * maxDotRange * (speedFactor + 0.2f); 

            targetX = Mathf.Clamp(targetX, -maxDotRange, maxDotRange);
            targetY = Mathf.Clamp(targetY, -maxDotRange, maxDotRange);

            gForceDot.anchoredPosition = Vector2.Lerp(gForceDot.anchoredPosition, new Vector2(targetX, targetY), Time.deltaTime * 6f);
        }
    }

    public void ResetDriveManager()
    {
        // Pełny reset parametrów
        isTimerRunning = false;
        currentTime = 0f;
        currentLap = 0;
        lapTimes.Clear();
        
        // Ukrywanie UI stopera
        if (timerText != null)
        {
            timerText.text = "00:00.00";
            timerText.gameObject.SetActive(false);
        }

        // Przywrócenie stanu zerowego i ukrycie prędkościomierza
        if (speedText != null)
        {
            speedText.text = "000 km/h";
            speedText.gameObject.SetActive(false);
        }
    }

    public void LapCompleted()
    {
        currentLap++;
        lapTimes.Add(currentTime);

        int minutes = Mathf.FloorToInt(currentTime / 60F);
        int seconds = Mathf.FloorToInt(currentTime % 60F);
        int fraction = Mathf.FloorToInt((currentTime * 100F) % 100F);
        Debug.Log($"Okrążenie {currentLap} ukończone! Czas: {minutes:00}:{seconds:00}:{fraction:00}");
        currentTime = 0f;
    }

    public void StopTimer()
    {
        isTimerRunning = false;
        if (timerText != null) timerText.color = Color.green;
    }

    public void ShowButton(List<Vector2> drawnPoints)
    {
        if (drawnPoints.Count < 2) return;
        startPoint = drawnPoints[0];
        nextPoint = drawnPoints[1];
    }

    public void StartDriving()
    {
        // Ustawienie kamery
        transform.rotation = Quaternion.Euler(90, 0, 0);

        // Spawn pojazdu na początku wygenerowanej optymalnej linii
        Vector3 spawnPos = new Vector3(startPoint.x, 3.0f, startPoint.y);
        Vector3 forwardVector = new Vector3(nextPoint.x - startPoint.x, 0f, nextPoint.y - startPoint.y).normalized;

        GameObject spawnedCar = Instantiate(carPrefab, spawnPos, Quaternion.LookRotation(forwardVector));

        // Podpięcie komponentów nowego pojazdu do naszego systemu telemetrii
        carRb = spawnedCar.GetComponent<Rigidbody>();
        carController = spawnedCar.GetComponent<PrometeoCarController>();

        // Ustawienie kamery w tryb TPP za samochodem
        if (mainCamera != null)
        {
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 60f;

            mainCamera.transform.SetParent(spawnedCar.transform);
            mainCamera.transform.localPosition = new Vector3(0f, 2.5f, -5f);
            mainCamera.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        }

        // Uruchomienie układów czasowych
        currentTime = 0f;
        isTimerRunning = true;
        
        // Aktywacja ukrytych wskaźników UI
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.color = Color.white;
        }

        if (speedText != null)
        {
            speedText.gameObject.SetActive(true);
        }
    }
}