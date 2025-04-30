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
        }

        if (rightHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
        {
            transform.localRotation = rot;
        }
    }
}
