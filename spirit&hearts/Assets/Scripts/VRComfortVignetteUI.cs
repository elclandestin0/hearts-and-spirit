using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(RawImage))]
public class VRComfortVignetteUI : MonoBehaviour
{
    [Header("References")]
    public Movement playerMovement;
    public Transform head;
                   
    [Header("Speed Mapping")]
    public float minSpeed;
    public float maxSpeed;
    [Range(0f,1f)] public float maxIntensity = 0.8f;
    public float minRadius = 0.85f;          // 0.85 = wide open; 0.4 = tight tunnel
    public float maxRadius = 0.45f;
    public float smooth = 8f;
    [Header("Angular Comfort")]
    public float angMin = 45f;
    public float angMax = 140f;
    [Range(0f,1f)] public float angWeight = 0.35f;

    [Header("Shape")]
    public Color color = Color.black;
    [Range(0.001f,0.5f)] public float softness = 0.22f;
    public Vector2 center = new Vector2(0.5f, 0.5f);
    public float ellipticity = 1.0f;

    public RawImage img;
    public Material mat;
    public Vector3 prevFwd;
    public Volume volume;             
    public Vignette vig;
    void Awake()
    {
        img = GetComponent<RawImage>();

        if (img.material != null)
            mat = new Material(img.material);
        else
            mat = new Material(Shader.Find("ComfortVignetteUnlit"));

        img.material = mat;

        if (!head && Camera.main) head = Camera.main.transform;
        if (head) prevFwd = head.forward;
    }


    void Start() 
    { 
        if (volume && volume.profile) volume.profile.TryGet(out vig); 
    }

    void Update()
    {
        if (playerMovement == null || mat == null) return;

        // --- speed term
        float speed = playerMovement.CurrentVelocity.magnitude;
        float tSpeed = Mathf.InverseLerp(minSpeed, maxSpeed, speed);

        // --- angular term
        float tAng = 0f;
        if (head)
        {
            float angle = Vector3.Angle(prevFwd, head.forward);
            float degPerSec = angle / Mathf.Max(Time.deltaTime, 1e-4f);
            tAng = Mathf.InverseLerp(angMin, angMax, degPerSec);
            prevFwd = head.forward;
        }

        float t = Mathf.Clamp01(Mathf.Lerp(tSpeed, Mathf.Max(tSpeed, tAng), angWeight));

        // Map t → radius (smaller radius = stronger tunnel) and intensity
        float targetRadius = Mathf.Lerp(minRadius, maxRadius, t);
        float targetIntensity = Mathf.Lerp(0f, maxIntensity, t);

        // Smooth
        float curRadius = Mathf.Lerp(mat.GetFloat("_Radius"), targetRadius, Time.deltaTime * smooth);
        float curIntensity = Mathf.Lerp(mat.GetFloat("_Intensity"), targetIntensity, Time.deltaTime * smooth);

        // Push to material
        mat.SetColor("_Color", color);
        mat.SetFloat("_Softness", softness);
        mat.SetFloat("_Radius", curRadius);
        mat.SetFloat("_Intensity", curIntensity);

        // aspect correction & center
        var cam = head ? head.GetComponent<Camera>() : Camera.main;
        float aspect = cam ? (float)cam.pixelWidth / cam.pixelHeight : (float)Screen.width / Screen.height;
        mat.SetFloat("_Aspect", aspect);
        mat.SetFloat("_Ellipticity", Mathf.Max(0.01f, ellipticity));
        mat.SetVector("_Center", new Vector4(center.x, center.y, 0, 0));

        if (vig == null || playerMovement == null) return;
        vig.intensity.value = Mathf.Lerp(vig.intensity.value, Mathf.Lerp(0f, maxIntensity, t), Time.deltaTime * smooth);
    }
}