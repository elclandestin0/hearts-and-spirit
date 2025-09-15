using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Assigns a stable GUID to each instance so it can be referenced across save/load.
/// The GUID is serialized so it stays constant between play sessions and builds.
/// </summary>
[DisallowMultipleComponent]
public class PersistentId : MonoBehaviour
{
    [SerializeField, HideInInspector] private string id;

    public string Id
    {
        get
        {
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString("N");
            }
            return id;
        }
    }

#if UNITY_EDITOR
    // Ensure uniqueness in editor when duplicating
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString("N");
            EditorUtility.SetDirty(this);
        }
    }
#endif
}
