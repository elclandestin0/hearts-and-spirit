using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tags a seed courier that a REMOTE player fired, so the local player can be
/// hit by it. The seed itself flies with the normal local simulation.
/// </summary>
public class NetSeedMarker : MonoBehaviour
{
    public static readonly List<NetSeedMarker> Active = new List<NetSeedMarker>();

    public ulong shooterClientId;
    [HideInInspector] public bool consumed;

    SeedMotion _motion;
    Vector3 _lastPos;
    Vector3 _fallbackVel;

    public Vector3 Velocity => _motion != null ? _motion.Velocity : _fallbackVel;

    void Awake()
    {
        Active.Add(this);
        _motion = GetComponent<SeedMotion>();
        _lastPos = transform.position;
    }

    void Update()
    {
        if (Time.deltaTime > 0f)
            _fallbackVel = (transform.position - _lastPos) / Time.deltaTime;
        _lastPos = transform.position;
    }

    void OnDestroy() => Active.Remove(this);
}
