using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DriveManager : MonoBehaviour
{
    [Header("UI i Kamera")]
    public GameObject startButton;
    public Camera mainCamera;
    public TextMeshProUGUI timerText;

    [Header("Pojazd")]
    public GameObject carPrefab;

    private Vector2 startPoint;
    private Vector2 nextPoint;

    private bool isTimerRunning = false;
    private float currentTime = 0f;

    [Header("Okrążenia")]
    public int currentLap = 0;
    public List<float> lapTimes = new List<float>();

    void Start()
    {
        if (startButton != null) startButton.SetActive(false);

        if (timerText != null)
        {
            timerText.text = "00:00.00";
            timerText.gameObject.SetActive(false);
            RectTransform rect = timerText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(20f, -20f);
            }
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

        if (startButton != null) startButton.SetActive(true);
    }

    public void StartDriving()
    {

        if (startButton != null) startButton.SetActive(false);

        LineRenderer drawingLine = GetComponent<LineRenderer>();
        if (drawingLine != null) drawingLine.enabled = false;

        LineRenderer[] lines = GetComponentsInChildren<LineRenderer>();
        foreach (LineRenderer lr in lines) lr.useWorldSpace = false;

        transform.rotation = Quaternion.Euler(90, 0, 0);

        Vector3 spawnPos = new Vector3(startPoint.x, 3.0f, startPoint.y);
        Vector3 forwardVector = new Vector3(nextPoint.x - startPoint.x, 0f, nextPoint.y - startPoint.y).normalized;

        GameObject spawnedCar = Instantiate(carPrefab, spawnPos, Quaternion.LookRotation(forwardVector));

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