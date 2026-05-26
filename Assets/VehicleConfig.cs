using UnityEngine;

// Ten nagłówek pozwala łatwo tworzyć nowe auta z poziomu menu Unity
[CreateAssetMenu(fileName = "NewVehicle", menuName = "ApexGen/Vehicle Config")]
public class VehicleConfig : ScriptableObject
{
    [Header("Dane Podstawowe")]
    public string vehicleName = "Domyślne Auto";

    [Header("Gabaryty [m]")]
    public float length_L = 4.5f;
    public float width_W = 1.8f;

    [Header("Fizyka i Osiągi")]
    // Zgodnie z dokumentacją: 1.0 dla cywilnego, 1.5 dla wyścigowego
    public float frictionCoefficient_Mu = 1.0f;
    public float max_speed = 60.0f; // [m/s]
    public float max_brake = 9.0f; // [m/s^2]
    public float max_acceleration = 5.0f; // [m/s^2]
    public float turn_radius = 5.0f; // Minimalny promień skrętu dla danego auta

    // Stała fizyczna - przyspieszenie ziemskie g = 9.81
    private const float g = 9.81f;

    // Metoda z diagramu klas: Wylicza maksymalną prędkość w zakręcie
    public float CalculateLimitSpeed_v(float radius_R)
    {
        // Implementacja wzoru v = sqrt(u * g * R) z dokumentacji
        return Mathf.Sqrt(frictionCoefficient_Mu * g * radius_R);
    }

    // Metoda z diagramu klas: Sprawdza, czy auto mieści się na torze
    public bool CheckDimensionsClearance(float distanceToEdge)
    {
        // Zgodnie z założeniem o zachowaniu bezpiecznego marginesu
        // Odległość do krawędzi musi być większa niż połowa szerokości auta
        return distanceToEdge >= (width_W / 2f);
    }
}