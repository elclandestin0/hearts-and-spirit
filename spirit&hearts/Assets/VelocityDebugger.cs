using UnityEngine;
using TMPro;

public class VelocityDebugger : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 0.3f, 0);
    public TextMeshProUGUI textMesh;
    public Movement movement; // Your updated script reference

    void LateUpdate()
    {
        if (textMesh == null)
            return;

         string output = "";

        if (movement != null)
        {
            Vector3 liveVel = movement.CurrentVelocity;
            output += $"\nLive: {liveVel.magnitude:F2} m/s\nVec: {liveVel.ToString("F2")}";
        }

        textMesh.text = output;
    }
}

