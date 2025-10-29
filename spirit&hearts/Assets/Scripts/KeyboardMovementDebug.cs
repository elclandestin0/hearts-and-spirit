using UnityEngine;

public class KeyboardMovementDebug : MonoBehaviour
{
    public Movement movement;

    [Header("Settings")]
    public float pitchSpeed = 45f;
    public float yawSpeed = 60f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Mouse / Touchpad")]
    public bool enableMouseLook = true;
    public float mouseSensitivity = 3f;
    [Tooltip("How quickly mouse input smooths (lower = snappier).")]
    public float mouseSmoothTime = 0.05f; // 0.05–0.1 good range

    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private MainMenu mainMenu;

    private float smoothMouseX;
    private float smoothMouseY;
    private float targetYaw;
    private float targetPitch;

    void Start()
    {
        if (movement && movement.head)
        {
            Vector3 euler = movement.head.localEulerAngles;
            targetPitch = euler.x > 180f ? euler.x - 360f : euler.x;
            targetYaw = euler.y > 180f ? euler.y - 360f : euler.y;
        }
    }

    void Update()
    {
        if (!tutorialManager.IsRunning && !mainMenu.gameBegun) return;
        if (PauseState.IsPaused) return;
        if (movement == null || movement.head == null) return;

        Transform head = movement.head;

        // Keyboard input
        if (Input.GetKey(KeyCode.W)) targetPitch -= pitchSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) targetPitch += pitchSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) targetYaw -= yawSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) targetYaw += yawSpeed * Time.deltaTime;

        // Mouse input (smooth)
        if (enableMouseLook)
        {
            float rawMouseX = Input.GetAxis("Mouse X");
            float rawMouseY = Input.GetAxis("Mouse Y");

            // Smooth mouse deltas
            smoothMouseX = Mathf.Lerp(smoothMouseX, rawMouseX, 1f - Mathf.Exp(-Time.deltaTime / mouseSmoothTime));
            smoothMouseY = Mathf.Lerp(smoothMouseY, rawMouseY, 1f - Mathf.Exp(-Time.deltaTime / mouseSmoothTime));

            targetYaw += smoothMouseX * yawSpeed * mouseSensitivity * Time.deltaTime;
            targetPitch -= smoothMouseY * pitchSpeed * mouseSensitivity * Time.deltaTime;
        }

        // Clamp pitch
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        // Smooth final rotation
        Vector3 current = head.localEulerAngles;
        float currentPitch = current.x > 180f ? current.x - 360f : current.x;
        float currentYaw = current.y > 180f ? current.y - 360f : current.y;

        float smoothedPitch = Mathf.Lerp(currentPitch, targetPitch, 1f - Mathf.Exp(-Time.deltaTime / 0.08f));
        float smoothedYaw = Mathf.Lerp(currentYaw, targetYaw, 1f - Mathf.Exp(-Time.deltaTime / 0.08f));

        head.localEulerAngles = new Vector3(smoothedPitch, smoothedYaw, 0f);
    }
}
