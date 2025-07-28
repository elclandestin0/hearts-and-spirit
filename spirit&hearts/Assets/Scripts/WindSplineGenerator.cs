using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(SplineContainer))]
public class WindSplineGenerator : MonoBehaviour
{
    [Header("Wind Spline Settings")]
    public Transform peakTransform;
    public int numberOfSplines = 5;
    public float splineLength = 50f;
    public float angleSpread = 360f;
    public float stepDistance = 2f;
    public float terrainHeightOffset = 2f;
    public float avoidPeakRadius = 5f;
    public LayerMask terrainMask;

    private SplineContainer splineContainer;

    public void GenerateSplines()
    {
        if (peakTransform == null)
        {
            Debug.LogError("No peakTransform assigned.");
            return;
        }

        splineContainer = GetComponent<SplineContainer>();
        splineContainer.Spline.Clear();

        Vector3 origin = peakTransform.position;
        Vector3 localUp = origin.normalized;
        Quaternion baseRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, localUp), localUp);

        for (int i = 0; i < numberOfSplines; i++)
        {
            float angle = i * (angleSpread / numberOfSplines);
            Quaternion rotation = baseRotation * Quaternion.Euler(0f, angle, 0f);
            Vector3 dir = rotation * Vector3.forward;

            List<BezierKnot> knots = new List<BezierKnot>();
            Vector3 currentPos = origin + dir * 2f;

            for (float dist = 0; dist < splineLength; dist += stepDistance)
            {
                Vector3 step = currentPos + dir * stepDistance;

                Ray ray = new Ray(step + localUp * 100f, -localUp);
                if (Physics.Raycast(ray, out RaycastHit hit, 200f, terrainMask))
                {
                    Vector3 terrainPoint = hit.point + localUp * terrainHeightOffset;

                    if (Vector3.Distance(terrainPoint, origin) > avoidPeakRadius)
                    {
                        knots.Add(new BezierKnot(terrainPoint));
                        currentPos = terrainPoint;
                    }
                }
            }

            if (knots.Count >= 2)
            {
                foreach (var knot in knots)
                {
                    splineContainer.Spline.Add(knot);
                }
            }
        }

        Debug.Log($"[WindSplineGenerator] Generated {numberOfSplines} wind splines from peak.");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(WindSplineGenerator))]
public class WindSplineGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WindSplineGenerator generator = (WindSplineGenerator)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Generate Wind Splines"))
        {
            generator.GenerateSplines();
        }
    }
}
#endif