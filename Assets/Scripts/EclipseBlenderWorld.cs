using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads the Blender-authored Unity asset library for the short skyway run.
/// Close towers switch to the atlas-driven standard tower mesh at distance.
/// </summary>
public static class EclipseBlenderWorld
{
    private const int RoadSegments = 16;
    private const float SegmentLength = 39f;

    public static bool TryBuild()
    {
        GameObject roadPrefab = Resources.Load<GameObject>("EclipseWorld/RoadSegment");
        GameObject standardPrefab = Resources.Load<GameObject>("EclipseWorld/StandardTower");
        GameObject heroA = Resources.Load<GameObject>("EclipseWorld/HeroTowerA");
        GameObject heroB = Resources.Load<GameObject>("EclipseWorld/HeroTowerB");
        if (roadPrefab == null || standardPrefab == null || heroA == null || heroB == null)
        {
            Debug.LogWarning("Eclipse Blender environment assets are incomplete; using procedural fallback.");
            return false;
        }

        var root = new GameObject("Eclipse | Blender Skyway District").transform;
        var rng = new System.Random(2407);
        var roadBatchRoot = new GameObject("Eclipse | Road Static Batch").transform;
        roadBatchRoot.SetParent(root, false);
        var distantBatchRoot = new GameObject("Eclipse | Skyline Static Batch").transform;
        distantBatchRoot.SetParent(root, false);

        for (int i = 0; i < RoadSegments; i++)
        {
            float z = -SegmentLength + i * SegmentLength;
            GameObject road = UnityEngine.Object.Instantiate(roadPrefab, roadBatchRoot);
            road.name = "Blender road segment " + i.ToString("00");
            road.transform.localPosition = new Vector3(0f, 0f, z);
            MarkStatic(road);
            EnableNeon(road);
        }

        // Near-route landmarks use modeled geometry with a textured low-detail
        // LOD. Distant repeats use the shared atlas tower and varied transforms.
        for (int i = 0; i < 8; i++)
        {
            int side = i % 2 == 0 ? -1 : 1;
            GameObject hero = i % 2 == 0 ? heroA : heroB;
            float z = -5f + i * 78f;
            float x = side * (23f + (float)rng.NextDouble() * 7f);
            CreateHeroLod(root, hero, standardPrefab, x, -11f, z);
        }

        for (int i = 0; i < 15; i++)
        {
            float z = -4f + i * 40f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (38f + (float)rng.NextDouble() * 47f);
                float heightScale = 0.85f + (float)rng.NextDouble() * 2.35f;
                float widthScale = 0.82f + (float)rng.NextDouble() * 0.52f;
                float depthScale = 0.82f + (float)rng.NextDouble() * 0.52f;
                GameObject tower = UnityEngine.Object.Instantiate(standardPrefab, distantBatchRoot);
                tower.name = "Atlas skyline tower " + i.ToString("00") + (side < 0 ? " L" : " R");
                tower.transform.localPosition = new Vector3(x, -11f, z + (float)rng.NextDouble() * 18f);
                tower.transform.localScale = new Vector3(widthScale, heightScale, depthScale);
                tower.transform.localRotation = Quaternion.Euler(0f, side * (float)rng.NextDouble() * 9f, 0f);
                MarkStatic(tower);
                EnableNeon(tower);
            }
        }

        MarkStatic(roadBatchRoot.gameObject);
        MarkStatic(distantBatchRoot.gameObject);
        StaticBatchingUtility.Combine(roadBatchRoot.gameObject);
        StaticBatchingUtility.Combine(distantBatchRoot.gameObject);
        return true;
    }

    private static void CreateHeroLod(Transform parent, GameObject detailedPrefab,
        GameObject standardPrefab, float x, float y, float z)
    {
        var building = new GameObject(detailedPrefab.name + " | LOD");
        building.transform.SetParent(parent, false);
        building.transform.localPosition = new Vector3(x, y, z);

        GameObject detailed = UnityEngine.Object.Instantiate(detailedPrefab, building.transform);
        detailed.name = detailedPrefab.name + " | close geometry";
        GameObject standard = UnityEngine.Object.Instantiate(standardPrefab, building.transform);
        standard.name = "Standard tower | atlas LOD";
        bool variantA = detailedPrefab.name.Contains("A");
        standard.transform.localScale = new Vector3(
            variantA ? 16f / 14f : 13f / 14f,
            variantA ? 54f / 23f : 46f / 23f,
            variantA ? 14f / 12f : 15f / 12f);

        Renderer[] highRenderers = detailed.GetComponentsInChildren<Renderer>(true);
        Renderer[] lowRenderers = standard.GetComponentsInChildren<Renderer>(true);
        var lodGroup = building.AddComponent<LODGroup>();
        lodGroup.fadeMode = LODFadeMode.CrossFade;
        lodGroup.animateCrossFading = true;
        lodGroup.SetLODs(new[]
        {
            new LOD(0.12f, highRenderers),
            new LOD(0.018f, lowRenderers),
            new LOD(0.004f, Array.Empty<Renderer>())
        });
        lodGroup.RecalculateBounds();
        MarkStatic(building);
        EnableNeon(building);
    }

    private static void MarkStatic(GameObject root)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            item.gameObject.isStatic = true;
    }

    private static void EnableNeon(GameObject root)
    {
        var seen = new HashSet<Material>();
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || !seen.Add(material) || !material.name.Contains("Eclipse_Neon"))
                    continue;
                if (!material.HasProperty("_EmissionColor")) continue;
                material.EnableKeyword("_EMISSION");
                bool amber = material.name.Contains("Amber");
                material.SetColor("_EmissionColor", amber
                    ? new Color(1f, 0.16f, 0.025f) * 2.0f
                    : new Color(0.01f, 0.65f, 1.0f) * 2.2f);
            }
        }
    }
}
