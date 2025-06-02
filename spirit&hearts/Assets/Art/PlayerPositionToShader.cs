using UnityEngine;

public class PlayerPositionToShader : MonoBehaviour
{
    public Color playerColor = Color.white;
    public float playerLightRange = 5f;

    void Update()
    {
        Shader.SetGlobalVector("_PlayerPosition", transform.position);
        Shader.SetGlobalColor("_PlayerColor", playerColor);
        Shader.SetGlobalFloat("_PlayerLightRange", playerLightRange);
    }
}
