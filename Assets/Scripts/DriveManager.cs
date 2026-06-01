using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DriveManager : MonoBehaviour
{
    [Header("UI i Kamera")]
    public GameObject startButton;
    public Camera mainCamera;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI bestLapText;

    [Header("Telemetria")]
    public float maxDotRange = 50f;
    public GameObject carPrefab;

    private Vector2 startPoint;
    private Vector2 nextPoint;
    private bool isTimerRunning = false;
    private float currentTime = 0f;
    private float bestLapTime = Mathf.Infinity; 
    public int currentLap = 0;
    public List<float> lapTimes = new List<float>();

    private Rigidbody carRb;
    private PrometeoCarController carController;

    private RectTransform gForceDot;
    private TextMeshProUGUI tractionLossText;
    private TextMeshProUGUI speedText;
    private TextMeshProUGUI rpmText;

    private float currentRPM = 800f;

    void Start()
    {
        GameObject foundGForce = GameObject.Find("GForce_Dot");
        if (foundGForce != null) gForceDot = foundGForce.GetComponent<RectTransform>();

        GameObject foundTractionText = GameObject.Find("WarningText");
        if (foundTractionText != null)
        {
            tractionLossText = foundTractionText.GetComponent<TextMeshProUGUI>();
            tractionLossText.gameObject.SetActive(false);
        }

        GameObject foundSpeedText = GameObject.Find("SpeedText");
        if (foundSpeedText != null)
        {
            speedText = foundSpeedText.GetComponent<TextMeshProUGUI>();
            speedText.text = "000 km/h";
            speedText.gameObject.SetActive(false);
        }

        GameObject foundRPMText = GameObject.Find("RPMText");
        if (foundRPMText != null)
        {
            rpmText = foundRPMText.GetComponent<TextMeshProUGUI>();
            rpmText.text = "[..........] 0800";
            rpmText.gameObject.SetActive(false);
        }

        if (timerText != null)
        {
            timerText.text = "00:00.00";
            timerText.gameObject.SetActive(false);
        }

        if (bestLapText != null)
        {
            bestLapText.text = "BEST: --:--.--";
            bestLapText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isTimerRunning && timerText != null)
        {
            currentTime += Time.deltaTime;
            timerText.text = FormatTime(currentTime);
        }

        UpdateTelemetry();
    }

    private void UpdateTelemetry()
    {
        if (carRb == null || carController == null) return;

        if (tractionLossText != null)
        {
            float lateralSpeed = Vector3.Dot(carRb.linearVelocity, carRb.transform.right);
            bool isLosingTraction = Mathf.Abs(lateralSpeed) > 2.5f && carRb.linearVelocity.magnitude > 2f;

            if (isLosingTraction)
            {
                bool isBlinking = Mathf.Sin(Time.time * 30f) > 0f;
                tractionLossText.gameObject.SetActive(isBlinking);
            }
            else
            {
                tractionLossText.gameObject.SetActive(false);
            }
        }

        if (speedText != null)
        {
            int currentSpeed = Mathf.FloorToInt(Mathf.Abs(carController.carSpeed));
            speedText.text = string.Format("{0:D3} km/h", currentSpeed);
        }

        if (rpmText != null)
        {
            float speed = Mathf.Abs(carController.carSpeed);
            float inputGas = Mathf.Abs(Input.GetAxis("Vertical"));
            float speedPerGear = 35f;
            float speedInCurrentGear = speed % speedPerGear;
            float rpmPercent = speedInCurrentGear / speedPerGear;

            if (speed < 2f && inputGas > 0.1f)
            {
                rpmPercent = (Mathf.Sin(Time.time * 15f) * 0.1f) + 0.9f;
            }
            else if (speed < 2f && inputGas < 0.1f)
            {
                rpmPercent = 0f;
            }

            float targetRPM = 800f + (rpmPercent * 6000f);
            currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * 8f);

            int totalBars = 15;
            int activeBars = Mathf.Clamp(Mathf.FloorToInt((currentRPM / 6800f) * totalBars), 0, totalBars);
            string barString = new string('|', activeBars) + new string('.', totalBars - activeBars);
            
            rpmText.text = string.Format("[{0}] {1:0000}", barString, Mathf.FloorToInt(currentRPM));
        }

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
        isTimerRunning = false;
        currentTime = 0f;
        currentLap = 0;
        lapTimes.Clear();
        bestLapTime = Mathf.Infinity;

        if (timerText != null)
        {
            timerText.text = "00:00.00";
            timerText.gameObject.SetActive(false);
        }

        if (bestLapText != null)
        {
            bestLapText.text = "BEST: --:--.--";
            bestLapText.gameObject.SetActive(false);
        }

        if (speedText != null)
        {
            speedText.text = "000 km/h";
            speedText.gameObject.SetActive(false);
        }

        if (rpmText != null)
        {
            rpmText.text = "[..........] 0800";
            rpmText.gameObject.SetActive(false);
        }

        if (tractionLossText != null)
        {
            tractionLossText.gameObject.SetActive(false);
        }
    }

    public void LapCompleted()
    {
        currentLap++;
        lapTimes.Add(currentTime);

        if (currentTime < bestLapTime)
        {
            bestLapTime = currentTime;
            if (bestLapText != null) bestLapText.text = "BEST: " + FormatTime(bestLapTime);
        }

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

        if (bestLapText != null) bestLapText.gameObject.SetActive(true);
        if (speedText != null) speedText.gameObject.SetActive(true);
        if (rpmText != null) rpmText.gameObject.SetActive(true);
    }

    private string FormatTime(float timeToFormat)
    {
        int minutes = Mathf.FloorToInt(timeToFormat / 60F);
        int seconds = Mathf.FloorToInt(timeToFormat % 60F);
        int fraction = Mathf.FloorToInt((timeToFormat * 100F) % 100F);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, fraction);
    }
}