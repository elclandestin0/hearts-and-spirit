using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class SplineWindZone : MonoBehaviour
{
    public SplineContainer spline;
    public float windStrength = 100f;
    public float influenceRadius = 25f;

    public float maxStrength = 1000f;

    public Renderer targetRenderer;

    static readonly int WindStrengthID = Shader.PropertyToID("_WindStrength");
    MaterialPropertyBlock _mpb;


    void Awake()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        // Auto-find sibling "SplineMesh" if none was assigned
        if (!targetRenderer)
        {
            var parent = transform.parent;
            if (parent)
            {
                var t = parent.Find("SplineMesh");
                if (t) targetRenderer = t.GetComponent<Renderer>();
                if (!targetRenderer)
                    targetRenderer = parent.GetComponentInChildren<Renderer>(true);
            }
        }
    }

    void OnEnable()
    {
        PushWindStrengthToTarget();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        PushWindStrengthToTarget();
    }
#endif

    public void PushWindStrengthToTarget()
    {
        if (!targetRenderer) return;

        float v = Mathf.Clamp(windStrength, 0f, maxStrength);
        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(WindStrengthID, v);     // Material reference is _WindStrength
        targetRenderer.SetPropertyBlock(_mpb);
    }

    public Vector3 GetWindForceAtPosition(Vector3 position)
    {
        if (spline == null || spline.Spline == null)
            return Vector3.zero;

        Vector3 localPos = transform.InverseTransformPoint(position);

        float3 closestPoint = float3.zero;
        float t;
        float3 tangent;

        SplineUtility.GetNearestPoint(
            spline.Spline,
            localPos,
            out closestPoint,
            out t
        );

        tangent = SplineUtility.EvaluateTangent(spline.Spline, t);
        Vector3 worldPoint = transform.TransformPoint(closestPoint);
        Vector3 worldTangent = transform.TransformDirection((Vector3)tangent);

        float distance = Vector3.Distance(position, worldPoint);
        if (distance > influenceRadius)
            return Vector3.zero;

        // Debug.Log("Now being affected by " + gameObject.transform.parent.name);
        float falloff = 1f - Mathf.Clamp01(distance / influenceRadius);
        return worldTangent.normalized * windStrength * falloff;
    }
}
