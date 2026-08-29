using Unity.Netcode;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds Assets/Resources/Net/NetAvatar.prefab — the networked bird proxy other
/// players see. Kept as primitives so it needs no art dependencies; NetAvatar
/// tints it per player at runtime. Menu: Seeyuf → Build Net Avatar Prefab
/// (also invoked automatically by WebGLBuilder.Build).
/// </summary>
public static class NetAvatarPrefabBuilder
{
    const string PrefabPath = "Assets/Resources/Net/NetAvatar.prefab";

    [MenuItem("Seeyuf/Build Net Avatar Prefab")]
    public static void Build()
    {
        var root = new GameObject("NetAvatar");
        try
        {
            root.AddComponent<NetworkObject>();
            root.AddComponent<ClientNetworkTransform>();
            root.AddComponent<NetAvatar>();

            var body = Primitive(root.transform, PrimitiveType.Capsule, "Body",
                new Vector3(0, 0, 0), new Vector3(0.5f, 0.45f, 0.5f), new Vector3(90, 0, 0));
            Primitive(root.transform, PrimitiveType.Sphere, "Head",
                new Vector3(0, 0.15f, 0.55f), new Vector3(0.35f, 0.35f, 0.35f), Vector3.zero);
            Primitive(root.transform, PrimitiveType.Cube, "LeftWing",
                new Vector3(-0.75f, 0.05f, -0.05f), new Vector3(1.2f, 0.05f, 0.55f), new Vector3(0, 0, 8));
            Primitive(root.transform, PrimitiveType.Cube, "RightWing",
                new Vector3(0.75f, 0.05f, -0.05f), new Vector3(1.2f, 0.05f, 0.55f), new Vector3(0, 0, -8));
            Primitive(root.transform, PrimitiveType.Cube, "Tail",
                new Vector3(0, 0.02f, -0.65f), new Vector3(0.25f, 0.04f, 0.5f), new Vector3(-8, 0, 0));

            System.IO.Directory.CreateDirectory("Assets/Resources/Net");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"[NetAvatarPrefabBuilder] Saved {PrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
    }

    static GameObject Primitive(Transform parent, PrimitiveType type, string name,
        Vector3 pos, Vector3 scale, Vector3 euler)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.transform.localEulerAngles = euler;
        // No colliders: the avatar must not disturb Movement's spherecast bounces.
        var col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        return go;
    }
}
