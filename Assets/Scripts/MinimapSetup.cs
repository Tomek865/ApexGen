using System.Collections.Generic;
using UnityEngine;

public class MinimapSetup : MonoBehaviour
{
    [Header("Ustawienia Śledzenia")]
    public float cameraHeight = 50f;
    public float orthographicZoom = 30f;

    [Header("Korekcja Pozycji Auta na Mapie")]
    [Tooltip("Jeśli auto jest za bardzo z lewej/prawej, zmień tę wartość (np. na 5 lub -5)")]
    public float rightOffset = 0f;

    [Tooltip("Domyślnie połowa zooma. Zwiększ, by auto było niżej na mapie, zmniejsz, by było wyżej.")]
    public float forwardOffset = 15f;

    private Transform playerTarget;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographicSize = orthographicZoom;

            if (forwardOffset == 0f) forwardOffset = orthographicZoom * 0.5f;
        }
    }

    void LateUpdate()
    {
        if (playerTarget == null)
        {
            GameObject car = GameObject.FindGameObjectWithTag("Player");
            if (car != null) playerTarget = car.transform;
        }

        if (playerTarget != null && cam != null)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.down, playerTarget.forward);

            Vector3 offset = (playerTarget.forward * forwardOffset) + (playerTarget.right * rightOffset);

            Vector3 newPosition = playerTarget.position + offset;
            newPosition.y = cameraHeight;

            transform.position = newPosition;
        }
    }

    public void ConfigureMinimap(List<Vector2> points)
    {
    }
}