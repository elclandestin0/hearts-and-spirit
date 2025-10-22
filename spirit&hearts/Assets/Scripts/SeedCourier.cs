using UnityEngine;
using System;

public class SeedCourier : MonoBehaviour
{
    private Transform _target;
    private Action _onArrived, _onBegin, _onDestroyed;

    [Header("Motion")]
    public float minSpeed = 10f;
    public float maxSpeed = 60f;
    public float stopDistance = 10f; // match your old "<= 10f" reach

    public void Init(Transform target, Action onArrived, Action onBegin = null, Action onDestroyed = null)
    {
        _target = target;
        _onArrived = onArrived;
        _onBegin = onBegin;
        _onDestroyed = onDestroyed;
        _onBegin?.Invoke();
    }

    void Update()
    {
        if (_target == null)
        {
            _onDestroyed?.Invoke();
            Destroy(gameObject);
            return;
        }

        float distance = Vector3.Distance(transform.position, _target.position);
        if (distance <= stopDistance)
        {
            _onArrived?.Invoke();
            Destroy(gameObject);
            return;
        }

        float t = Mathf.InverseLerp(120f, 0f, distance); // faster when close
        float speed = Mathf.Lerp(minSpeed, maxSpeed, t);
        Vector3 dir = (_target.position - transform.position).normalized;

        transform.position += dir * speed * Time.deltaTime;

        // face movement
        if (dir.sqrMagnitude > 0.0001f)
        {
            var rot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 6f);
        }
    }

    void OnDestroy() => _onDestroyed?.Invoke();
}
