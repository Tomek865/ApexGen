using UnityEngine;

public class FinishLineTrigger : MonoBehaviour
{
    private float lastLapTime;

    public float minimumLapTime = 5f;

    void Start()
    {
        lastLapTime = Time.time;
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            float driveDirection = Vector3.Dot(transform.forward, rb.linearVelocity.normalized);

            if (driveDirection > 0.2f)
            {
                if (Time.time > lastLapTime + minimumLapTime)
                {
                    DriveManager dm = Object.FindAnyObjectByType<DriveManager>();
                    if (dm != null)
                    {
                        dm.LapCompleted();
                    }

                    lastLapTime = Time.time;
                }
                else
                {
                    Debug.Log("Zbyt wcześnie na mecie! Podejrzane o oszustwo. Okrążenie niezaliczane.");
                }
            }
            else
            {
                Debug.Log("Jazda pod prąd lub cofanie! Okrążenie niezaliczane.");
            }
        }
    }
}