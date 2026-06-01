public class GhostCar : MonoBehaviour
{
    [Header("REFERENCJE")]
    public PrometeoCarController playerCar; // Przypnij tutaj samochód gracza w edytorze!

    [Header("PATH FOLLOWING")]
    public float waypointThreshold = 1.5f;
    public int lookAheadPoints = 3;

    private List<RacingLinePoint> optimalPath;
    private int currentTargetIndex = 0;
    private float currentSpeedMs = 0f;
    private bool isRunning = false;

    public void SetPathAndStart(List<RacingLinePoint> path)
    {
        if (path == null || path.Count < 2) return;

        optimalPath = path;
        currentTargetIndex = 0;
        currentSpeedMs = 0f;

        Vector3 startPos = new Vector3(optimalPath[0].Position.x, transform.position.y, optimalPath[0].Position.y);
        transform.position = startPos;
        Vector3 nextPos = new Vector3(optimalPath[1].Position.x, transform.position.y, optimalPath[1].Position.y);
        transform.rotation = Quaternion.LookRotation(nextPos - startPos);

        isRunning = true;
    }

    private void Update()
    {
        if (!isRunning || optimalPath == null) return;

        RacingLinePoint targetPoint = optimalPath[currentTargetIndex];
        Vector3 targetPos3D = new Vector3(targetPoint.Position.x, transform.position.y, targetPoint.Position.y);

        if (Vector3.Distance(transform.position, targetPos3D) < waypointThreshold)
        {
            currentTargetIndex = (currentTargetIndex + 1) % optimalPath.Count;
        }

        // --- CZYTAMY DANE BEZPOŚREDNIO Z PROMETEO ---
        float targetSpeedMs = targetPoint.TargetSpeedKmh / 3.6f;

        if (currentSpeedMs < targetSpeedMs)
        {
            currentSpeedMs += playerCar.accelerationMultiplier * 5f * Time.deltaTime;
            currentSpeedMs = Mathf.Min(currentSpeedMs, targetSpeedMs);
        }
        else if (currentSpeedMs > targetSpeedMs)
        {
            currentSpeedMs -= playerCar.decelerationMultiplier * 10f * Time.deltaTime;
            currentSpeedMs = Mathf.Max(currentSpeedMs, targetSpeedMs);
        }

        transform.position += transform.forward * currentSpeedMs * Time.deltaTime;

        int steeringTargetIndex = (currentTargetIndex + lookAheadPoints) % optimalPath.Count;
        Vector3 steeringTargetPos = new Vector3(optimalPath[steeringTargetIndex].Position.x, transform.position.y, optimalPath[steeringTargetIndex].Position.y);
        Vector3 directionToTarget = (steeringTargetPos - transform.position).normalized;
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, playerCar.steeringSpeed * 10f * Time.deltaTime);
        }
    }
}
