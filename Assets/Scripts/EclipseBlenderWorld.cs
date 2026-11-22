using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Loads the Blender-authored Unity asset library for the short skyway run.
/// Close towers switch to the atlas-driven standard tower mesh at distance.
/// </summary>
public static class EclipseBlenderWorld
{
    private static readonly Dictionary<string, Material> RuntimeMaterials = new Dictionary<string, Material>();
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

        ConfigureSkyAndLighting();
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
            ApplyRuntimeMaterials(road);
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
                ApplyRuntimeMaterials(tower);
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
        ApplyRuntimeMaterials(building);
        EnableNeon(building);
    }

    private static void ConfigureSkyAndLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.46f, 0.60f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.28f, 0.39f, 0.56f);
        RenderSettings.ambientGroundColor = new Color(0.10f, 0.14f, 0.25f);
        RenderSettings.ambientIntensity = 1.15f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.30f, 0.39f, 0.55f);
        RenderSettings.fogStartDistance = 180f;
        RenderSettings.fogEndDistance = 720f;

        var sunObject = new GameObject("Eclipse | Skyway key light");
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.72f, 0.84f, 1.0f);
        sun.intensity = 1.45f;
        sun.shadows = LightShadows.Soft;
        sunObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
        RenderSettings.sun = sun;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) return;

        Material cloudMaterial = new Material(shader) { name = "Eclipse | Cloud Sea" };
        SetColor(cloudMaterial, new Color(0.085f, 0.15f, 0.28f));
        cloudMaterial.enableInstancing = true;
        GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sea.name = "Eclipse | Cloud Sea Horizon";
        sea.transform.position = new Vector3(0f, -52f, 260f);
        sea.transform.localScale = new Vector3(180f, 1f, 180f);
        sea.GetComponent<Renderer>().sharedMaterial = cloudMaterial;
        Collider seaCollider = sea.GetComponent<Collider>();
        if (seaCollider != null) UnityEngine.Object.Destroy(seaCollider);

        Material cloudBank = new Material(shader) { name = "Eclipse | Soft Cloud Banks" };
        SetColor(cloudBank, new Color(0.14f, 0.22f, 0.36f));
        cloudBank.enableInstancing = true;
        var rng = new System.Random(813);
        for (int i = 0; i < 18; i++)
        {
            int side = i % 2 == 0 ? -1 : 1;
            GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloud.name = "Eclipse | Distant cloud bank";
            cloud.transform.position = new Vector3(
                side * (75f + (float)rng.NextDouble() * 115f),
                -36f - (float)rng.NextDouble() * 14f,
                -30f + i * 36f);
            cloud.transform.localScale = new Vector3(
                38f + (float)rng.NextDouble() * 58f,
                7f + (float)rng.NextDouble() * 8f,
                30f + (float)rng.NextDouble() * 45f);
            cloud.GetComponent<Renderer>().sharedMaterial = cloudBank;
            Collider collider = cloud.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.Destroy(collider);
        }
    }

    private static void ApplyRuntimeMaterials(GameObject root)
    {
        Texture2D facade = Resources.Load<Texture2D>("EclipseWorld/Eclipse_FacadeAtlas");
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] source = renderer.sharedMaterials;
            if (source == null || source.Length == 0) continue;
            var replacement = new Material[source.Length];
            bool changed = false;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null) { replacement[i] = null; continue; }
                Material runtime = GetRuntimeMaterial(source[i].name, facade);
                replacement[i] = runtime != null ? runtime : source[i];
                changed |= runtime != null;
            }
            if (changed) renderer.sharedMaterials = replacement;
        }
    }

    private static Material GetRuntimeMaterial(string sourceName, Texture2D facade)
    {
        if (RuntimeMaterials.TryGetValue(sourceName, out Material cached)) return cached;

        bool isNeon = sourceName.Contains("Eclipse_Neon");
        Shader shader = Shader.Find(isNeon
            ? "Universal Render Pipeline/Unlit"
            : "Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) return null;

        Color baseColor = new Color(0.16f, 0.27f, 0.40f);
        float metallic = 0.30f;
        float smoothness = 0.28f;
        if (sourceName.Contains("Facade_BakedColor"))
        {
            baseColor = Color.white;
            metallic = 0f;
            smoothness = 0.18f;
        }
        else if (sourceName.Contains("Brushed_Titanium"))
        {
            baseColor = new Color(0.52f, 0.67f, 0.76f);
            metallic = 0.48f;
            smoothness = 0.42f;
        }
        else if (sourceName.Contains("Dark_Glass"))
        {
            baseColor = new Color(0.06f, 0.19f, 0.29f);
            metallic = 0.20f;
            smoothness = 0.56f;
        }
        else if (sourceName.Contains("Road_Carbon"))
        {
            baseColor = new Color(0.09f, 0.14f, 0.22f);
            metallic = 0.24f;
            smoothness = 0.32f;
        }
        else if (sourceName.Contains("Eclipse_Ceramic_Blue"))
        {
            baseColor = new Color(0.18f, 0.30f, 0.45f);
            metallic = 0.30f;
            smoothness = 0.30f;
        }
        else if (sourceName.Contains("Neon_Amber"))
        {
            baseColor = new Color(1f, 0.25f, 0.06f);
            metallic = 0f;
        }
        else if (sourceName.Contains("Neon_Cyan"))
        {
            baseColor = new Color(0.02f, 0.70f, 1f);
            metallic = 0f;
        }
        else return null;

        var material = new Material(shader) { name = sourceName + " | Eclipse URP" };
        SetColor(material, baseColor);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        material.enableInstancing = true;

        if (sourceName.Contains("Facade_BakedColor") && facade != null)
        {
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", facade);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", facade);
            if (material.HasProperty("_EmissionMap")) material.SetTexture("_EmissionMap", facade);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.white * 0.72f);
                material.EnableKeyword("_EMISSION");
            }
        }
        else if (isNeon && material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", baseColor * 1.7f);
            material.EnableKeyword("_EMISSION");
        }

        RuntimeMaterials.Add(sourceName, material);
        return material;
    }

    private static void SetColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
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
