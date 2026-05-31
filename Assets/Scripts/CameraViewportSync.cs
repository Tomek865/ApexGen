using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraViewportSync : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Przeciągnij tutaj obiekt DrawingPanel")]
    public RectTransform targetPanel;
    
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (targetPanel == null) return;

        // Pobieramy koordynaty rogów panelu z interfejsu (w pikselach ekranu)
        Vector3[] corners = new Vector3[4];
        targetPanel.GetWorldCorners(corners);

        // corners[0] to lewy-dolny róg, corners[2] to prawy-górny róg
        float x = corners[0].x / Screen.width;
        float y = corners[0].y / Screen.height;
        float width = (corners[2].x - corners[0].x) / Screen.width;
        float height = (corners[2].y - corners[0].y) / Screen.height;

        // Ograniczamy renderowanie kamery idealnie do naszego okna terminala
        cam.rect = new Rect(x, y, width, height);
    }
}