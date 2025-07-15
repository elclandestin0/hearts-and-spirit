using UnityEngine;
using UnityEngine.Splines;

public class DestroyAfterSplineEnd : MonoBehaviour
{
    void Start()
    {
        SplineAnimate animate = GetComponent<SplineAnimate>();
        if (animate != null)
        {
            StartCoroutine(DestroyAfter(animate.Duration));
        }
    }

    private System.Collections.IEnumerator DestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
