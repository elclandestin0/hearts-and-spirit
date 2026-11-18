// A minimal stand-in for the parts of UnityEngine that the flight code touches.
// The maths mirrors Unity's documented behaviour (clamped Lerp, zero-safe normalized, and so on)
// because the results of the tests depend on it. Everything else is an inert stub.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);

        public float sqrMagnitude => x * x + y * y + z * z;
        public float magnitude => MathF.Sqrt(sqrMagnitude);
        public Vector3 normalized
        {
            get { float m = magnitude; return m > 1e-5f ? this / m : zero; }
        }
        public void Normalize() { this = normalized; }

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => a * d;
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public static bool operator ==(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 9.99999944E-11f;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
        public override bool Equals(object o) => o is Vector3 v && this == v;
        public override int GetHashCode() => HashCode.Combine(x, y, z);
        public override string ToString() => $"({x:F3}, {y:F3}, {z:F3})";

        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) =>
            new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);

        public static float Angle(Vector3 from, Vector3 to)
        {
            float denominator = MathF.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (denominator < 1e-15f) return 0f;
            return MathF.Acos(Mathf.Clamp(Dot(from, to) / denominator, -1f, 1f)) * Mathf.Rad2Deg;
        }

        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 normal)
        {
            float sqr = normal.sqrMagnitude;
            if (sqr < 1e-15f) return v;
            return v - normal * (Dot(v, normal) / sqr);
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * Mathf.Clamp01(t);

        // Same case split as Unity's native Slerp: interpolates direction and magnitude separately.
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            float aMag = a.magnitude, bMag = b.magnitude;
            if (aMag < 1e-5f || bMag < 1e-5f) return Lerp(a, b, t);

            float mag = Mathf.Lerp(aMag, bMag, t);
            float dot = Dot(a, b) / (aMag * bMag);
            if (dot > 1f - 1e-5f) return Lerp(a, b, t);

            Vector3 aNorm = a / aMag;
            Vector3 axis = dot < -1f + 1e-5f
                ? (MathF.Abs(aNorm.y) < 0.9f ? Cross(aNorm, up) : Cross(aNorm, forward)).normalized
                : Cross(a, b).normalized;
            float angle = MathF.Acos(Mathf.Clamp(dot, -1f, 1f)) * t;

            // Rodrigues' rotation; the k(k.v) term vanishes because the axis is orthogonal to aNorm.
            return (aNorm * MathF.Cos(angle) + Cross(axis, aNorm) * MathF.Sin(angle)) * mag;
        }
    }

    public struct Vector2
    {
        public float x, y;
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);

        public static Quaternion AngleAxis(float degrees, Vector3 axis)
        {
            float half = degrees * Mathf.Deg2Rad * 0.5f;
            Vector3 n = axis.normalized * MathF.Sin(half);
            return new Quaternion(n.x, n.y, n.z, MathF.Cos(half));
        }

        public static Quaternion Inverse(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);

        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
            a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);

        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            Vector3 u = new Vector3(q.x, q.y, q.z);
            Vector3 t = Vector3.Cross(u, v) * 2f;
            return v + t * q.w + Vector3.Cross(u, t);
        }
    }

    public static class Mathf
    {
        public const float Rad2Deg = 57.29578f;
        public const float Deg2Rad = 0.017453292f;

        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float InverseLerp(float a, float b, float v) => a != b ? Clamp01((v - a) / (b - a)) : 0f;
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Pow(float a, float b) => MathF.Pow(a, b);
        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = -2f * t * t * t + 3f * t * t;
            return to * t + from * (1f - t);
        }
    }

    public static class Time
    {
        public static float time;
        public static float deltaTime;
    }

    public static class Random
    {
        public static float value => 0.5f; // Deterministic: only used to pick voice lines.
    }

    public static class Application
    {
        public static bool isPlaying => true;
    }

    public struct Color
    {
        public static Color red => default;
        public static Color green => default;
        public static Color blue => default;
        public static Color cyan => default;
        public static Color yellow => default;
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void DrawLine(Vector3 a, Vector3 b, Color c, float duration = 0f, bool depthTest = true) { }
        public static void DrawRay(Vector3 origin, Vector3 dir, Color c) { }
    }

    public static class Gizmos
    {
        public static Color color;
        public static void DrawWireSphere(Vector3 center, float radius) { }
    }

    public enum KeyCode { None, F, M, N }

    // No keyboard in the tests: flaps can only come from hand movement.
    public static class Input
    {
        public static bool GetKey(KeyCode k) => false;
        public static bool GetKeyDown(KeyCode k) => false;
        public static bool GetKeyUp(KeyCode k) => false;
    }

    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }

    public struct LayerMask
    {
        public int value;
        public static implicit operator int(LayerMask m) => m.value;
    }

    public struct RaycastHit
    {
        public Vector3 normal, point;
    }

    // Open sky: nothing to bounce off.
    public static class Physics
    {
        public static bool SphereCast(Vector3 origin, float radius, Vector3 dir, out RaycastHit hit,
            float distance, int mask, QueryTriggerInteraction q)
        {
            hit = default;
            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int o) { } }

    public class AudioClip { }
    public class WaitForSeconds { public WaitForSeconds(float s) { } }

    public class AudioSource
    {
        public AudioClip clip;
        public float time, volume;
        public bool isPlaying;
        public void Play() { }
        public void Stop() { }
    }

    public class Transform
    {
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 forward => rotation * Vector3.forward;
        public Vector3 up => rotation * Vector3.up;
        public void Rotate(Vector3 axis, float degrees) => rotation = rotation * Quaternion.AngleAxis(degrees, axis);
        public Vector3 InverseTransformPoint(Vector3 p) => Quaternion.Inverse(rotation) * (p - position);
    }

    public class GameObject
    {
        public readonly Transform transform = new Transform();
        internal readonly List<MonoBehaviour> components = new List<MonoBehaviour>();

        public T AddComponent<T>() where T : MonoBehaviour, new()
        {
            var c = new T { gameObject = this };
            components.Add(c);
            return c;
        }
    }

    public class MonoBehaviour
    {
        public GameObject gameObject;
        public bool enabled = true;
        public Transform transform => gameObject.transform;

        public T GetComponent<T>()
        {
            foreach (var c in gameObject.components)
                if (c is T match) return match;
            return default;
        }

        public T GetComponentInParent<T>() => GetComponent<T>();
        public Coroutine StartCoroutine(IEnumerator routine) => null;
        public static T[] FindObjectsOfType<T>() => Array.Empty<T>(); // No wind zones in the test sky.
    }

    public class Coroutine { }
}

namespace UnityEngine.Events
{
    public class UnityEvent { public void Invoke() { } }
    public class UnityEvent<T> { public void Invoke(T value) { } }
}

namespace UnityEngine.InputSystem
{
    public class InputAction { public T ReadValue<T>() where T : struct => default; }
    public class InputActionReference { public InputAction action => new InputAction(); }
}
