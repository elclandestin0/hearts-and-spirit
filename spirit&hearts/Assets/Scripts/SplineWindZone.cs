using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class SplineWindZone : MonoBehaviour
{
    public float windStrength = 10f;
    public float influenceRadius = 5f;

    private SplineContainer splineContainer;

    void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
    }

    public Vector3 GetWindForceAtPosition(Vector3 playerPosition)
    {
        SplineUtility.GetNearestPoint(splineContainer.Spline, playerPosition, out float3 closestPoint, out float t);

        float distance = Vector3.Distance(playerPosition, (Vector3)closestPoint);
        if (distance > influenceRadius)
            return Vector3.zero;

        float3 tangent = SplineUtility.EvaluateTangent(splineContainer.Spline, t);
        Vector3 direction = ((Vector3)tangent).normalized;

        float strengthFactor = 1f - (distance / influenceRadius);
        return direction * windStrength * strengthFactor;
    }
}
