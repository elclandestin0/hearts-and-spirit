using UnityEngine;

public class WingFollower : MonoBehaviour
{
    public Transform target; // controller (left/right hand)
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + target.rotation * positionOffset;
            transform.rotation = target.rotation * Quaternion.Euler(rotationOffset);
        }
    }
}
