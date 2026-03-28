using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MapPrefabColliderSetup
{
    private static readonly string[] TargetPrefabPaths =
    {
        "Assets/Scenes/Map1 1.prefab",
        "Assets/Scenes/Map1 2.prefab"
    };

    private static readonly HashSet<string> NonBlockingRootNames = new HashSet<string>
    {
        "Flowers&Plants",
        "Grass",
        "Rocks"
    };

    [MenuItem("Tools/EverWind/Setup Map Colliders")]
    public static void SetupMapCollidersFromMenu()
    {
        Run();
    }

    public static void RunFromBatchMode()
    {
        Run();
        EditorApplication.Exit(0);
    }

    private static void Run()
    {
        foreach (string prefabPath in TargetPrefabPaths)
        {
            SetupCollidersForPrefab(prefabPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void SetupCollidersForPrefab(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            int addedMeshColliders = 0;
            int addedBoxColliders = 0;
            int updatedTriggerColliders = 0;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (!ShouldAddCollider(child.gameObject))
                {
                    continue;
                }

                if (TryAddMeshCollider(child.gameObject))
                {
                    addedMeshColliders++;
                    continue;
                }

                if (TryAddBoxCollider(child.gameObject))
                {
                    addedBoxColliders++;
                }
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (!ShouldAllowPassThrough(child))
                {
                    continue;
                }

                foreach (Collider collider in child.GetComponentsInChildren<Collider>(true))
                {
                    if (!collider.isTrigger)
                    {
                        collider.isTrigger = true;
                        updatedTriggerColliders++;
                    }
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[MapPrefabColliderSetup] {prefabPath}: added {addedMeshColliders} MeshCollider(s), {addedBoxColliders} BoxCollider(s), updated {updatedTriggerColliders} Collider(s) to trigger.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool ShouldAddCollider(GameObject gameObject)
    {
        if (gameObject.GetComponent<Collider>() != null)
        {
            return false;
        }

        if (gameObject.GetComponent<MeshFilter>() != null)
        {
            return true;
        }

        Renderer renderer = gameObject.GetComponent<Renderer>();
        return renderer != null && !(renderer is ParticleSystemRenderer);
    }

    private static bool TryAddMeshCollider(GameObject gameObject)
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return false;
        }

        Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        if (rigidbody != null && !rigidbody.isKinematic)
        {
            return false;
        }

        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = false;
        meshCollider.isTrigger = false;
        return true;
    }

    private static bool TryAddBoxCollider(GameObject gameObject)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer == null || renderer is ParticleSystemRenderer)
        {
            return false;
        }

        Bounds worldBounds = renderer.bounds;
        if (worldBounds.size == Vector3.zero)
        {
            return false;
        }

        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();

        Vector3 lossyScale = gameObject.transform.lossyScale;
        Vector3 safeScale = new Vector3(
            Mathf.Approximately(lossyScale.x, 0f) ? 1f : lossyScale.x,
            Mathf.Approximately(lossyScale.y, 0f) ? 1f : lossyScale.y,
            Mathf.Approximately(lossyScale.z, 0f) ? 1f : lossyScale.z);

        Vector3 localCenter = gameObject.transform.InverseTransformPoint(worldBounds.center);
        Vector3 localSize = new Vector3(
            Mathf.Abs(worldBounds.size.x / safeScale.x),
            Mathf.Abs(worldBounds.size.y / safeScale.y),
            Mathf.Abs(worldBounds.size.z / safeScale.z));

        boxCollider.center = localCenter;
        boxCollider.size = localSize;
        boxCollider.isTrigger = false;
        return true;
    }

    private static bool ShouldAllowPassThrough(Transform transform)
    {
        while (transform != null)
        {
            if (NonBlockingRootNames.Contains(transform.name))
            {
                return true;
            }

            string lowerName = transform.name.ToLowerInvariant();
            if (lowerName.Contains("rock") || lowerName.Contains("grass") || lowerName.Contains("plant") || lowerName.Contains("flower"))
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }
}
