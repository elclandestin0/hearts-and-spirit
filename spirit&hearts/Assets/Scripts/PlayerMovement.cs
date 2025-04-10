using UnityEngine;

public class HandFlapMovement : MonoBehaviour
{
    [Header("XR References")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform head;

    [SerializeField] private float gravity = 4f; // m/s²

    // 🔒 Script-controlled flight values
    private readonly float flapStrength = 0.35f;
    private readonly float forwardPropulsionStrength = 0.5f;
    private readonly float glideStrength = 2.5f;
    private readonly float maxSpeed = 7f;
    private readonly float minHandSpread = 1.0f;
    private readonly float glideRotationSpeed = 40f; // kept for future UX toggles

    private Vector3 velocity = Vector3.zero;
    private Vector3 prevLeftPos, prevRightPos;
    private bool isGliding = false;
    private bool isFlapping = false;
    private bool isGrounded = false;

    void Start()
    {
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }

    void Update()
    {
        // ───── Motion + Posture Setup ─────
        Vector3 leftHandDelta = (leftHand.position - prevLeftPos) / Time.deltaTime;
        Vector3 rightHandDelta = (rightHand.position - prevRightPos) / Time.deltaTime;
        Vector3 handDirection = (rightHand.position - leftHand.position).normalized;
        float handDistance = Vector3.Distance(leftHand.position, rightHand.position);

        isFlapping = (leftHandDelta.y < -1f && rightHandDelta.y < -1f);
        isGliding = !isFlapping && !isGrounded && velocity.magnitude > 0.1f;

        if (isFlapping)
        {
            // 🐦 Flap = Lift + Thrust from posture
            Vector3 headToLeft = leftHand.position - head.position;
            Vector3 headToRight = rightHand.position - head.position;
            Vector3 postureNormal = Vector3.Cross(headToRight, headToLeft).normalized;

            Vector3 upwardLift = Vector3.up * flapStrength;
            Vector3 forwardThrust = head.forward * forwardPropulsionStrength;

            velocity += upwardLift;
            velocity += forwardThrust;

            Debug.DrawLine(head.position, head.position + postureNormal * 2f, Color.green, 0f, false); // Flap direction
        }
        else if (isGliding)
        {
            if (handDistance > minHandSpread)
            {
                // 🪂 Simulate lift from forward velocity
                float forwardSpeed = Vector3.Dot(velocity, head.forward);
                float liftForce = Mathf.Clamp01(forwardSpeed / maxSpeed) * flapStrength * 0.8f;
                velocity += Vector3.up * liftForce * Time.deltaTime;

                // Glide push forward
                Vector3 forwardGlide = head.forward * glideStrength;
                velocity += forwardGlide * Time.deltaTime;

                // 🦅 Stronger eagle dive based on look angle
                float diveAngle = Vector3.Angle(head.forward, Vector3.down);
                if (diveAngle < 60f) // triggers earlier
                {
                    float diveIntensity = Mathf.InverseLerp(75f, 15f, diveAngle); // more aggressive curve
                    float diveSpeed = diveIntensity * 20f; // was 10f — now much stronger
                    float diveForward = diveIntensity * 12f; // stronger forward force too

                    velocity += Vector3.down * diveSpeed * Time.deltaTime;
                    velocity += head.forward * diveForward * Time.deltaTime;

                    Debug.Log($"[DIVE] Angle: {diveAngle:F1}°, Intensity: {diveIntensity:F2}, Down: {diveSpeed:F2}, Forward: {diveForward:F2}");
                }
                // Descent limit based on forward speed
                float verticalDescentCap = Mathf.Lerp(0f, -0.5f, 1f - (forwardSpeed / maxSpeed));
                velocity.y = Mathf.Max(velocity.y, verticalDescentCap);

                // 🔄 [Future Feature] Glide turning by hand tilt
                /*
                Vector3 localHandDirection = transform.InverseTransformDirection(handDirection);
                float tiltAngle = Vector3.SignedAngle(localHandDirection, Vector3.right, Vector3.forward);

                if (Mathf.Abs(tiltAngle) > 25f)
                {
                    float rotationAmount = Mathf.Sign(tiltAngle) * glideRotationSpeed * Time.deltaTime;
                    transform.Rotate(Vector3.up, rotationAmount);
                    Debug.Log($"[GLIDE TURN] Tilt: {tiltAngle:F2}°, Rotated: {rotationAmount:F2}°");
                }
                */
            }
        }

        // 🧭 Gradually steer toward head.forward
        Vector3 currentHorizontal = new Vector3(velocity.x, 0f, velocity.z);
        float currentSpeed = currentHorizontal.magnitude;
        Vector3 desiredDirection = head.forward.normalized;
        Vector3 smoothForward = Vector3.Lerp(currentHorizontal.normalized, desiredDirection, 0.05f);
        velocity = smoothForward * currentSpeed + Vector3.up * velocity.y;

        // ✈️ Apply movement
        transform.position += velocity * Time.deltaTime;

        // 🌍 Apply constant gravity
        velocity += Vector3.down * gravity * Time.deltaTime;

        // 🌬️ Drag
        velocity *= isGliding ? 0.99f : 0.98f;

        // 🧪 Debug Lines
        Debug.DrawLine(transform.position, transform.position + velocity.normalized * 2f, Color.cyan, 0f, false); // Velocity
        Debug.DrawLine(head.position, head.position + head.forward * 2f, Color.white, 0f, false); // Look direction
        Debug.DrawLine(transform.position, transform.position + Vector3.up * 2f, Color.blue, 0f, false); // Lift dir

        // 🔁 Update hands
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }
}
