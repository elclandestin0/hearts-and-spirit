using UnityEngine;
using UnityEngine.Splines;

public class DestroyAfterSplineEnd : MonoBehaviour
{
    private SplineAnimate animate;

    void Start()
    {
        animate = GetComponent<SplineAnimate>();
    }

    void Update()
    {
        if (animate.NormalizedTime >= 1f)
            Destroy(this);
    }
}
