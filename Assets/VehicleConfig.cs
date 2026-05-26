using UnityEngine;

[CreateAssetMenu(fileName = "NewVehicle", menuName = "ApexGen/Vehicle Config")]
public class VehicleConfig : ScriptableObject
{
    [Header("Dane Podstawowe")]
    public string vehicleName = "Domyślne Auto";

    [Header("Gabaryty [m]")]
    public float length_L = 4.5f;
    public float width_W = 1.8f;

    [Header("Fizyka i Osiągi")]
    public float frictionCoefficient_Mu = 1.0f;
    public float max_speed = 60.0f;
    public float max_brake = 9.0f;
    public float max_acceleration = 5.0f;
    public float turn_radius = 5.0f;

    private const float g = 9.81f;

    public float CalculateLimitSpeed_v(float radius_R)
    {
        return Mathf.Sqrt(frictionCoefficient_Mu * g * radius_R);
    }

    public bool CheckDimensionsClearance(float distanceToEdge)
    {
        return distanceToEdge >= (width_W / 2f);
    }
}