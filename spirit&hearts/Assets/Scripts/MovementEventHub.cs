using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class MovementEventHub : MonoBehaviour
{
    // -------- Existing API (kept) --------
    public UnityEvent OnFlap = new();
    public UnityEvent<float> OnGlideTick = new();
    public UnityEvent<float> OnDiveTick = new();
    public UnityEvent<float> OnHoverTick = new();
    public UnityEvent<float> OnLookTick = new();
    public UnityEvent<float> OnGravityTick = new();
    public UnityEvent OnNod = new();

    public void RaiseFlap()
    {
        _flapCount++;
        OnFlap?.Invoke();
        OnFlapCountChanged?.Invoke(_flapCount);
    }

    public void RaiseNod()
    {
        _nodCount++;
        OnNod?.Invoke();
        OnNodCountChanged?.Invoke(_nodCount);
    }

    public void RaiseGlideTick(float dt)
    {
        if (!_isGliding) { _isGliding = true; OnGlideStart?.Invoke(); }
        _glideSec += dt;
        OnGlideTick?.Invoke(dt);
        OnGlideSecondsChanged?.Invoke(_glideSec);
    }

    public void RaiseDiveTick(float dt)
    {
        if (!_isDiving) { _isDiving = true; OnDiveStart?.Invoke(); }
        _diveSec += dt;
        OnDiveTick?.Invoke(dt);
        OnDiveSecondsChanged?.Invoke(_diveSec);
    }

    public void RaiseHoverTick(float dt)
    {
        if (!_isHovering) { _isHovering = true; OnHoverStart?.Invoke(); }
        _hoverSec += dt;
        OnHoverTick?.Invoke(dt);
        OnHoverSecondsChanged?.Invoke(_hoverSec);
    }

    public void RaiseGravityTick(float dt)
    {
        OnGravityTick?.Invoke(dt);
    }

    // -------- New End methods (call once when state turns off) --------
    public void RaiseGlideEnd()
    {
        if (_isGliding) { _isGliding = false; OnGlideEnd?.Invoke(); }
    }

    public void RaiseDiveEnd()
    {
        if (_isDiving) { _isDiving = false; OnDiveEnd?.Invoke(); }
    }

    public void RaiseHoverEnd()
    {
        if (_isHovering) { _isHovering = false; OnHoverEnd?.Invoke(); }
    }

    public void RaiseLookTick(float dt)
    {
        if (!_isLooking) { _isLooking = true; OnLookStart?.Invoke(); }
        _lookSec += dt;
        OnLookTick?.Invoke(dt);
    }

    public void RaiseLookEnd()
    {
        if (_isLooking) { _isLooking = false; OnLookEnd?.Invoke(); }
    }

    public float LookSeconds => _lookSec;

    // -------- New Start/End events --------
    public UnityEvent OnGlideStart = new();
    public UnityEvent OnGlideEnd = new();
    public UnityEvent OnDiveStart = new();
    public UnityEvent OnDiveEnd = new();
    public UnityEvent OnHoverStart = new();
    public UnityEvent OnHoverEnd = new(); public UnityEvent OnLookStart = new();
    public UnityEvent OnLookEnd = new();

    // -------- Optional aggregates (listeners can subscribe directly) --------
    public UnityEvent<int> OnFlapCountChanged = new();
    public UnityEvent<int> OnNodCountChanged = new();
    public UnityEvent<float> OnGlideSecondsChanged = new();
    public UnityEvent<float> OnDiveSecondsChanged = new();
    public UnityEvent<float> OnHoverSecondsChanged = new();

    // Read-only props for UI/debug
    public int FlapCount => _flapCount;
    public float GlideSeconds => _glideSec;
    public float DiveSeconds => _diveSec;
    public float HoverSeconds => _hoverSec;

    // Reset totals and (optionally) force end of active states
    public void ResetAggregates(bool endActiveStates = true)
    {
        if (endActiveStates)
        {
            RaiseGlideEnd();
            RaiseDiveEnd();
            RaiseHoverEnd();
        }

        _flapCount = 0;
        _glideSec = _diveSec = _hoverSec = 0f;

        OnFlapCountChanged?.Invoke(_flapCount);
        OnGlideSecondsChanged?.Invoke(_glideSec);
        OnDiveSecondsChanged?.Invoke(_diveSec);
        OnHoverSecondsChanged?.Invoke(_hoverSec);
    }

    // -------- Internals --------
    int _flapCount, _nodCount;
    float _glideSec, _diveSec, _hoverSec, _lookSec;
    bool _isGliding, _isDiving, _isHovering, _isLooking;
}
