using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

[RequireComponent(typeof(RawImage))]
public class VRComfortVignetteUI : MonoBehaviour
{
    [Header("References")]
    public Movement playerMovement;
    public Transform head;

    [Header("Speed Mapping")]
    public float minSpeed;
    public float maxSpeed;

    [Range(0f,1f)] public float maxIntensity;
    public float minRadius = 0.85f;
    public float maxRadius = 0.35f;
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
    private bool comfortableMode = false;
    [SerializeField] private TextMeshProUGUI comfortableModeToggle;
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

        // speed term
        float speed = playerMovement.CurrentVelocity.magnitude;
        float tSpeed = Mathf.InverseLerp(minSpeed, maxSpeed, speed);

        // angular term
        float tAng = 0f;
        float t = Mathf.Clamp01(Mathf.Lerp(tSpeed, Mathf.Max(tSpeed, tAng), angWeight));

        float targetIntensity = Mathf.Lerp(0f, maxIntensity, t);

        if (comfortableMode)
        {
            if (vig == null || playerMovement == null) return;
            vig.intensity.value = Mathf.Lerp(vig.intensity.value, Mathf.Lerp(0f, maxIntensity, t), Time.deltaTime * smooth);
        }
    }
    public void ToggleComfortableMode()
    {
        comfortableMode = !comfortableMode;
        comfortableModeToggle.text = comfortableMode ? "X" : "";
    }
}