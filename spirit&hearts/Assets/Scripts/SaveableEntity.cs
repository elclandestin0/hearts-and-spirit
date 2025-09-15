using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Attach this to any GameObject you want to be saved. 
/// It will automatically find all components on the object that implement ISaveable.
/// </summary>
[DisallowMultipleComponent]
public class SaveableEntity : MonoBehaviour
{
    [SerializeField] private PersistentId persistentId;
    private List<ISaveable> _saveables;

    public string Id
    {
        get
        {
            if (persistentId == null)
                persistentId = GetComponent<PersistentId>() ?? gameObject.AddComponent<PersistentId>();
            return persistentId.Id;
        }
    }

    private void Awake()
    {
        // Collect all ISaveable components attached to this GameObject
        _saveables = GetComponents<MonoBehaviour>().OfType<ISaveable>().ToList();

        // Register with SaveManager
        SaveManager.Register(this);
    }

    private void OnDestroy()
    {
        SaveManager.Unregister(this);
    }

    /// <summary>
    /// Capture the state of all ISaveable components on this entity.
    /// </summary>
    public IEnumerable<(string type, object state)> CaptureAll()
    {
        foreach (var s in _saveables)
        {
            var t = s.GetType().AssemblyQualifiedName;
            yield return (t, s.CaptureState());
        }
    }

    /// <summary>
    /// Restore all ISaveable components on this entity from saved data.
    /// </summary>
    public void RestoreAll(List<(string type, object state)> data)
    {
        if (data == null) return;

        // Map types to components
        var map = _saveables.ToDictionary(s => s.GetType().AssemblyQualifiedName, s => s);
        foreach (var entry in data)
        {
            if (entry.state == null) continue;
            if (map.TryGetValue(entry.type, out var comp))
            {
                comp.RestoreState(entry.state);
            }
        }
    }
}
