using UnityEngine;
using UnityEngine.XR;

public class HandDebug : MonoBehaviour
{
    public XRNode node;
    void Update()
    {
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(node);

        if (rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
        {
            transform.localPosition = pos;
            Debug.Log("Right Hand Position: " + pos);
        }

        if (rightHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
        {
            transform.localRotation = rot;
            Debug.Log("Right Hand Rotation: " + rot);
        }
    }
}
