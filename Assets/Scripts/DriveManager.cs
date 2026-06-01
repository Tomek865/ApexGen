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
    
    // Prywatne referencje
    private RectTransform gForceDot;
    private TextMeshProUGUI tractionLossText;

    void Start()
    {
        // 1. AUTOMATYCZNE WYSZUKIWANIE UI (BEZ PRZECIĄGANIA W INSPEKTORZE)
        
        // Zmień "GForce_Dot" na dokładną nazwę Twojego zielonego kwadracika z okna Hierarchy
        GameObject foundGForce = GameObject.Find("GForce_Dot"); 
        if (foundGForce != null)
        {
            gForceDot = foundGForce.GetComponent<RectTransform>();
            Debug.Log("Telemetria: Znaleziono G-Force Dot!");
        }
        else
        {
            Debug.LogWarning("Telemetria: Nie znaleziono obiektu GForce_Dot. Sprawdź nazwę w Hierarchy.");
        }

        // Zmień "TractionLossText" na dokładną nazwę Twojego czerwonego napisu z okna Hierarchy
        GameObject foundTractionText = GameObject.Find("WarningText"); 
        if (foundTractionText != null)
        {
            tractionLossText = foundTractionText.GetComponent<TextMeshProUGUI>();
            foundTractionText.SetActive(false); // Ukrywamy na start
            Debug.Log("Telemetria: Znaleziono Traction Loss Text!");
        }
        else
        {
            Debug.LogWarning("Telemetria: Nie znaleziono obiektu TractionLossText. Sprawdź nazwę w Hierarchy.");
        }

        // 2. STOPER
        if (timerText != null)
        {
            timerText.text = "00:00.00";
            timerText.gameObject.SetActive(false);
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

        // Wywołanie naszej nowej funkcji w każdej klatce!
        UpdateTelemetry(); 
    }

    // --- NOWA METODA TELEMETRYCZNA ---
    private void UpdateTelemetry()
    {
        // Jeśli auto jeszcze nie zostało zrespione, nie robimy nic
        if (carRb == null || carController == null) return;

        // 1. Obsługa Traction Loss (czyta zmienną z Prometeo)
        if (tractionLossText != null)
        {
            tractionLossText.gameObject.SetActive(carController.isDrifting);
        }

        // 2. Obsługa G-Force Meter oparta na Input (AWSD) i Prędkości
        if (gForceDot != null)
        {
            // Prędkość do mnożnika (zakładamy użyteczny zakres 0-50 km/h dla wychylenia)
            float speedFactor = Mathf.Clamp(carController.carSpeed / 50f, 0f, 2f);

            // Odwrócone wciśnięcie klawiszy dla efektu bezwładności
            float inputZ = -Input.GetAxis("Vertical"); 
            float inputX = -Input.GetAxis("Horizontal");

            float targetX = inputX * maxDotRange * speedFactor;
            float targetY = inputZ * maxDotRange * (speedFactor + 0.2f); // +0.2 żeby było widać ruch przy starcie

            targetX = Mathf.Clamp(targetX, -maxDotRange, maxDotRange);
            targetY = Mathf.Clamp(targetY, -maxDotRange, maxDotRange);

            // Płynny ruch kropki (Lerp)
            gForceDot.anchoredPosition = Vector2.Lerp(gForceDot.anchoredPosition, new Vector2(targetX, targetY), Time.deltaTime * 6f);
        }
    }
    // ---------------------------------

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

	    public void ResetDriveManager()
    {
        isTimerRunning = false;
        currentTime = 0f;
        currentLap = 0;
        lapTimes.Clear();
        
        if (timerText != null)
        {
            timerText.text = "00:00.00";
            timerText.gameObject.SetActive(false);
        }
    }

    public void ShowButton(List<Vector2> drawnPoints)
    {
        if (drawnPoints.Count < 2) return;
        startPoint = drawnPoints[0];
        nextPoint = drawnPoints[1];
    }

    public void StartDriving()
    {
        // Tutaj usunęliśmy gaszenie przycisku, więc EXECUTE_APEXGEN nie zniknie!

        transform.rotation = Quaternion.Euler(90, 0, 0);

        Vector3 spawnPos = new Vector3(startPoint.x, 3.0f, startPoint.y);
        Vector3 forwardVector = new Vector3(nextPoint.x - startPoint.x, 0f, nextPoint.y - startPoint.y).normalized;

        GameObject spawnedCar = Instantiate(carPrefab, spawnPos, Quaternion.LookRotation(forwardVector));

        // Podpinamy komponenty wygenerowanego auta do telemetrii
        carRb = spawnedCar.GetComponent<Rigidbody>();
        carController = spawnedCar.GetComponent<PrometeoCarController>();

        if (mainCamera != null)
        {
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 60f;

            mainCamera.transform.SetParent(spawnedCar.transform);
            mainCamera.transform.localPosition = new Vector3(0f, 2.5f, -5f);
            mainCamera.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        }

        currentTime = 0f;
        isTimerRunning = true;
        
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.color = Color.white;
        }
    }
}