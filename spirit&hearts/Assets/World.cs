using UnityEngine;

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }

    [Header("World Gravity Settings")]
    public float gravityStrength = 9.8f;

    public Vector3 Center => transform.position;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    // Returns the gravity vector pointing toward the center of the sphere
    public Vector3 GetGravityDirection(Vector3 position)
    {
        return (Center - position).normalized;
    }

    public Vector3 GetGravityForce(Vector3 position)
    {
        return GetGravityDirection(position) * gravityStrength;
    }
}
