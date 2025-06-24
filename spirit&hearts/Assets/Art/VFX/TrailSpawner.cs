using UnityEngine;
using UnityEngine.Splines;
using System.Collections;

public class TrailSpawner : MonoBehaviour
{
    public SplineContainer spline;
    public GameObject trailPrefab;

    public float spawnInterval = 0.5f;

    [Header("Visual offset area (local)")]
    public Vector3 visualOffsetArea = new Vector3(1f, 0.5f, 0f);

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnTrail();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnTrail()
    {
        GameObject instance = Instantiate(trailPrefab, transform.position, Quaternion.identity, transform);
        instance.SetActive(true);

        var animate = instance.GetComponent<SplineAnimate>();
        animate.Container = spline;
        animate.Restart(true);
        animate.Play();

        // Busca hijo y aplica offset
        Transform visualChild = instance.transform.Find("TrailVisual");
        if (visualChild != null)
        {
            Vector3 offset = new Vector3(
                Random.Range(-visualOffsetArea.x * 0.5f, visualOffsetArea.x * 0.5f),
                Random.Range(-visualOffsetArea.y * 0.5f, visualOffsetArea.y * 0.5f),
                Random.Range(-visualOffsetArea.z * 0.5f, visualOffsetArea.z * 0.5f)
            );

            visualChild.localPosition = offset;
        }
        else
        {
            Debug.LogWarning($"No se encontró el hijo 'TrailVisual' en {instance.name}");
        }
    }
}
