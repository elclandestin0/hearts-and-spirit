using UnityEngine;
using System.Collections.Generic;

public class HandVelocityTracker : MonoBehaviour
{
    [SerializeField] private int frameMemory = 20;

    private Queue<Vector3> positionHistory = new Queue<Vector3>();
    private Vector3 previousPosition;
    private bool hasInitialized = false;
    [SerializeField] private Transform headReference; // usually the camera or head


    public Vector3 SmoothedVelocity { get; private set; }

    void LateUpdate()
    {
        if (headReference == null) return;

        Vector3 currentRelativePos = headReference.InverseTransformPoint(transform.position);

        if (!hasInitialized)
        {
            previousPosition = currentRelativePos;
            hasInitialized = true;
            return;
        }

        Vector3 frameVelocity = (currentRelativePos - previousPosition) / Time.deltaTime;
        previousPosition = currentRelativePos;

        positionHistory.Enqueue(frameVelocity);
        if (positionHistory.Count > frameMemory)
            positionHistory.Dequeue();

        SmoothedVelocity = Average(positionHistory);
    }


    private Vector3 Average(Queue<Vector3> values)
    {
        Vector3 sum = Vector3.zero;
        foreach (var v in values) sum += v;
        return sum / values.Count;
    }
}
