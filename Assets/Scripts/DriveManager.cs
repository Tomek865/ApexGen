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


    [Header("Pojazd Ghost Car")]
    public GameObject ghostCarPrefab;
    private List<RacingLinePoint> optimalLineToFollow;
    private GameObject spawnedGhostCar;

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
    private TextMeshProUGUI rpmText; // <--- NOWY WSKAŹNIK

    // Płynność wskazówki obrotomierza
    private float currentRPM = 800f;
    void Start()
    {
        // 1. AUTOMATYCZNE WYSZUKIWANIE ELEMENTÓW HUD

        GameObject foundGForce = GameObject.Find("GForce_Dot");
        if (foundGForce != null) gForceDot = foundGForce.GetComponent<RectTransform>();

        GameObject foundTractionText = GameObject.Find("TractionLossText");
        if (foundTractionText != null)
        {
            tractionLossText = foundTractionText.GetComponent<TextMeshProUGUI>();
            foundTractionText.SetActive(false);
        }

        GameObject foundSpeedText = GameObject.Find("SpeedText");
        if (foundSpeedText != null)
        {
            speedText = foundSpeedText.GetComponent<TextMeshProUGUI>();
            speedText.text = "Speed : 000 km/h";
            speedText.gameObject.SetActive(false);
        }

        // --- Wyszukiwanie płaskiego obrotomierza (RPMText) ---
        GameObject foundRPMText = GameObject.Find("RPMText");
        if (foundRPMText != null)
        {
            rpmText = foundRPMText.GetComponent<TextMeshProUGUI>();
            rpmText.text = "RPM   : [..........] 0800";
            rpmText.gameObject.SetActive(false); // Ukrywamy przy szkicowaniu
            Debug.Log("Telemetria: Znaleziono RPMText!");
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
        if (isTimerRunning && timerText != null)
        {
            currentTime += Time.deltaTime;
            int minutes = Mathf.FloorToInt(currentTime / 60F);
            int seconds = Mathf.FloorToInt(currentTime % 60F);
            int fraction = Mathf.FloorToInt((currentTime * 100F) % 100F);
            timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, fraction);
        }

        UpdateTelemetry();
    }

    private void UpdateTelemetry()
    {
        if (carRb == null || carController == null) return;

        // 1. Traction Loss
        if (tractionLossText != null)
        {
            tractionLossText.gameObject.SetActive(carController.isDrifting);
        }

        // 2. Speedometer
        if (speedText != null)
        {
            int currentSpeed = Mathf.FloorToInt(Mathf.Abs(carController.carSpeed));
            speedText.text = string.Format("Speed : {0:D3} km/h", currentSpeed);
        }

        // 3. FAKE RPM (Płaski obrotomierz)
        if (rpmText != null)
        {
            float speed = Mathf.Abs(carController.carSpeed);
            float inputGas = Mathf.Abs(Input.GetAxis("Vertical"));

            // Symulacja biegów (zakładamy zmianę co ok. 35 km/h)
            float speedPerGear = 35f;
            float speedInCurrentGear = speed % speedPerGear;

            // Procent wkręcenia na obroty (od 0.0 do 1.0)
            float rpmPercent = speedInCurrentGear / speedPerGear;

            // Sprzęgło (gazowanie w miejscu przy zerowej prędkości)
            if (speed < 2f && inputGas > 0.1f)
            {
                // Pulsowanie obrotów przy wciśniętym gazie na postoju
                rpmPercent = (Mathf.Sin(Time.time * 15f) * 0.1f) + 0.9f;
            }
            else if (speed < 2f && inputGas < 0.1f)
            {
                rpmPercent = 0f; // Bieg jałowy
            }

            // Ustawienie zakresu obrotów
            float targetRPM = 800f + (rpmPercent * 6000f);

            // Płynny ruch obrotomierza (Lerp)
            currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * 8f);

            // Rysowanie płaskiego paska ASCII (15 znaków szerokości)
            int totalBars = 15;
            int activeBars = Mathf.Clamp(Mathf.FloorToInt((currentRPM / 6800f) * totalBars), 0, totalBars);
            string barString = new string('|', activeBars) + new string('.', totalBars - activeBars);
            rpmText.text = string.Format("RPM   : [{0}] {1:0000}", barString, Mathf.FloorToInt(currentRPM));
        }

        // 4. G-Force Meter
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

    public void ReceiveOptimalLine(List<RacingLinePoint> calculatedLine)
    {
        optimalLineToFollow = calculatedLine;
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

        if (speedText != null)
        {
            speedText.text = "Speed : 000 km/h";
            speedText.gameObject.SetActive(false);
        }

        // Resetowanie RPM
        if (rpmText != null)
        {
            rpmText.text = "RPM   : [..........] 0800";
            rpmText.gameObject.SetActive(false);
        }


        if (spawnedGhostCar != null)
        {
            Destroy(spawnedGhostCar);
            spawnedGhostCar = null;
        }

        optimalLineToFollow = null;
    }

    public void LapCompleted()
    {
        currentLap++;
        lapTimes.Add(currentTime);
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
        transform.rotation = Quaternion.Euler(90, 0, 0);

        Vector3 spawnPos = new Vector3(startPoint.x, 3.0f, startPoint.y);
        Vector3 forwardVector = new Vector3(nextPoint.x - startPoint.x, 0f, nextPoint.y - startPoint.y).normalized;

        GameObject spawnedCar = Instantiate(carPrefab, spawnPos, Quaternion.LookRotation(forwardVector));

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

        if (speedText != null) speedText.gameObject.SetActive(true);
        if (rpmText != null) rpmText.gameObject.SetActive(true); // Aktywacja po starcie


        if (ghostCarPrefab != null && optimalLineToFollow != null && optimalLineToFollow.Count > 0)
        {
            spawnedGhostCar = Instantiate(ghostCarPrefab);
            GhostCar ghostScript = spawnedGhostCar.GetComponent<GhostCar>();

            if (ghostScript != null)
            {
                ghostScript.playerCar = carController;
                ghostScript.SetPathAndStart(optimalLineToFollow);
            }
        }
    }
}
