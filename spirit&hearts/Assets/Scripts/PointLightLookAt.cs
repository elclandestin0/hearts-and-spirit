using UnityEngine;

public class FollowPlayerWithLight : MonoBehaviour
{
    public float rotationSpeed = 2f;

    private Transform target;
    private bool shouldFollow = false;
    private bool returningToStart = false;

    private Quaternion initialRotation;

    private void Start()
    {
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (shouldFollow && target != null)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            // Invertido para que el Z negativo mire hacia el jugador
            Quaternion targetRotation = Quaternion.LookRotation(-directionToTarget, Vector3.up);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
        else if (returningToStart)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, Time.deltaTime * rotationSpeed);

            if (Quaternion.Angle(transform.rotation, initialRotation) < 0.1f)
            {
                transform.rotation = initialRotation;
                returningToStart = false;
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            target = other.transform;
            shouldFollow = true;
            returningToStart = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.transform == target)
        {
            shouldFollow = false;
            target = null;
            returningToStart = true;
        }
    }
}
