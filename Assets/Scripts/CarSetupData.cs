using UnityEngine;

// Ta linijka pozwoli Ci stworzyć ten plik klikając prawym przyciskiem myszy w folderze w Unity
[CreateAssetMenu(fileName = "NewCarSetup", menuName = "Racing/Car Setup Data")]
public class CarSetupData : ScriptableObject
{
    [Header("CAR SETUP")]
    [Range(20, 190)] public int maxSpeed = 90;
    [Range(10, 120)] public int maxReverseSpeed = 45;
    [Range(1, 10)] public int accelerationMultiplier = 2;
    
    [Space(10)]
    [Range(10, 45)] public int maxSteeringAngle = 27;
    [Range(0.1f, 1f)] public float steeringSpeed = 0.5f;
    
    [Space(10)]
    [Range(100, 600)] public int brakeForce = 350;
    [Range(1, 10)] public int decelerationMultiplier = 2;
    [Range(1, 10)] public int handbrakeDriftMultiplier = 5;
    
    [Space(10)]
    public Vector3 bodyMassCenter;
    
    [Header("RACING LINE SPECIFIC")]
    public float carWidth = 2.0f;
}
