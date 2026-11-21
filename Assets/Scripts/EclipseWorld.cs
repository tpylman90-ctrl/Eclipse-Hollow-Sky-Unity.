using UnityEngine;

/// <summary>
/// Builds the first art-direction pass for Eclipse Hollow Sky from original,
/// runtime-generated geometry. Shared materials and static batching keep the
/// environment practical for mid-range Android devices while the game has no
/// imported art assets yet.
/// </summary>
public static class EclipseWorld
{
    private static readonly Color Haze = new Color(0.42f, 0.67f, 0.78f);
    private static readonly Color WindowBlue = new Color(0.06f, 0.72f, 0.9f);
    private static readonly Color WarmLight = new Color(1f, 0.42f, 0.16f);

    public static void Build(Material[] palette)
    {
        var root = new GameObject("Eclipse | Skyway District").transform;
        Random.InitState(2407);

        // Deep cloud layer gives the course a real sense of height and scale.
        Shape(PrimitiveType.Plane, "Cloud sea", root, new Vector3(0f, -36f, 245f),
            new Vector3(120f, 1f, 105f), palette[0]);
        for (int i = 0; i < 34; i++)
        {
            float z = Random.Range(-50f, 590f);
            float x = Random.Range(-175f, 175f);
            float scale = Random.Range(18f, 48f);
            Shape(PrimitiveType.Sphere, "Cloud bank", root,
                new Vector3(x, Random.Range(-31f, -19f), z),
                new Vector3(scale, Random.Range(3f, 8f), scale * Random.Range(0.45f, 0.9f)),
                i % 4 == 0 ? palette[6] : palette[1]);
        }

        // Flight lane: segmented deck, lit edge rails and gate arches.
        for (int i = 0; i < 15; i++)
        {
            float z = -5f + i * 39f;
            float laneWidth = i % 4 == 2 ? 19f : 15f;
            Material deck = i % 3 == 0 ? palette[1] : palette[5];
            Shape(PrimitiveType.Cube, "Skyway deck segment", root,
                new Vector3(0f, 3.5f, z + 17f), new Vector3(laneWidth, 0.8f, 37f), deck);
            Shape(PrimitiveType.Cube, "Left guidance strip", root,
                new Vector3(-laneWidth * 0.5f + 0.24f, 4f, z + 17f),
                new Vector3(0.16f, 0.12f, 36f), palette[3]);
            Shape(PrimitiveType.Cube, "Right guidance strip", root,
                new Vector3(laneWidth * 0.5f - 0.24f, 4f, z + 17f),
                new Vector3(0.16f, 0.12f, 36f), palette[3]);

            // Architectural frames replace the repeated thin neon rectangles.
            float gateZ = z + 6f;
            Material gateLight = i % 3 == 1 ? palette[2] : palette[3];
            Shape(PrimitiveType.Cube, "Gate left pylon", root,
                new Vector3(-10f, 20f, gateZ), new Vector3(0.8f, 31f, 1.2f), palette[1]);
            Shape(PrimitiveType.Cube, "Gate right pylon", root,
                new Vector3(10f, 20f, gateZ), new Vector3(0.8f, 31f, 1.2f), palette[1]);
            Shape(PrimitiveType.Cube, "Gate crown", root,
                new Vector3(0f, 35.2f, gateZ), new Vector3(20.8f, 0.9f, 1.2f), palette[1]);
            Shape(PrimitiveType.Cube, "Gate left light", root,
                new Vector3(-9.48f, 20f, gateZ - 0.68f), new Vector3(0.13f, 28f, 0.12f), gateLight);
            Shape(PrimitiveType.Cube, "Gate right light", root,
                new Vector3(9.48f, 20f, gateZ - 0.68f), new Vector3(0.13f, 28f, 0.12f), gateLight);
            Shape(PrimitiveType.Cube, "Gate crown light", root,
                new Vector3(0f, 34.65f, gateZ - 0.68f), new Vector3(18.8f, 0.13f, 0.12f), gateLight);

            for (int dash = 0; dash < 4; dash++)
            {
                Shape(PrimitiveType.Cube, "Deck dash", root,
                    new Vector3(0f, 4.02f, z + 13f + dash * 8f),
                    new Vector3(0.28f, 0.08f, 3.4f), palette[6]);
            }

            // Near skyline: silhouettes and lit facade ribs frame the course.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int tower = 0; tower < 2; tower++)
                {
                    float x = side * Random.Range(27f, 56f);
                    float height = Random.Range(32f, 88f);
                    float width = Random.Range(10f, 20f);
                    float depth = Random.Range(12f, 22f);
                    float towerZ = z + Random.Range(-13f, 28f);
                    var building = Shape(PrimitiveType.Cube, "Sky district tower", root,
                        new Vector3(x, height * 0.5f - 11f, towerZ),
                        new Vector3(width, height, depth),
                        Random.value > 0.5f ? palette[1] : palette[0],
                        Quaternion.Euler(0f, side * Random.Range(-9f, 9f), side * Random.Range(-2f, 2f)));

                    Shape(PrimitiveType.Cube, "Tower crown", root,
                        new Vector3(x, height - 10.5f, towerZ),
                        new Vector3(width * 0.76f, 1.1f, depth * 0.72f), palette[5]);
                    Shape(PrimitiveType.Cube, "Facade light spine", root,
                        new Vector3(x + side * width * 0.36f, height * 0.43f - 11f, towerZ - depth * 0.51f),
                        new Vector3(0.18f, height * 0.72f, 0.16f),
                        tower % 2 == 0 ? palette[3] : palette[2]);
                    Shape(PrimitiveType.Cube, "Facade window band", root,
                        new Vector3(x, height * 0.61f - 11f, towerZ - depth * 0.52f),
                        new Vector3(width * 0.72f, 0.28f, 0.14f), palette[3]);
                }

                // Smaller lit beacons help the player read the flight corridor.
                float beaconX = side * Random.Range(13f, 19f);
                float beaconZ = z + Random.Range(14f, 32f);
                Shape(PrimitiveType.Cube, "Route beacon housing", root,
                    new Vector3(beaconX, 7f, beaconZ),
                    new Vector3(0.9f, 5.5f, 0.9f), palette[1]);
                Shape(PrimitiveType.Sphere, "Route beacon", root,
                    new Vector3(beaconX, 10f, beaconZ),
                    new Vector3(1.5f, 1.5f, 1.5f), palette[2]);
            }
        }

        // Far skyline creates depth behind the playable route.
        for (int i = 0; i < 42; i++)
        {
            int side = i % 2 == 0 ? -1 : 1;
            float x = side * Random.Range(100f, 245f);
            float height = Random.Range(45f, 145f);
            float z = Random.Range(-40f, 590f);
            Shape(PrimitiveType.Cube, "Distant skyline silhouette", root,
                new Vector3(x, height * 0.5f - 17f, z),
                new Vector3(Random.Range(9f, 23f), height, Random.Range(10f, 24f)),
                i % 4 == 0 ? palette[1] : palette[0]);
        }

        // A sunlit opening in the haze becomes a stable visual horizon cue.
        Shape(PrimitiveType.Sphere, "Horizon glow", root,
            new Vector3(0f, 64f, 520f), new Vector3(42f, 42f, 4f), palette[6]);

        var sunObject = new GameObject("Eclipse key light");
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.75f, 0.88f, 1f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
        sunObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);

        StaticBatchingUtility.Combine(root.gameObject);
    }

    private static GameObject Shape(PrimitiveType primitive, string name, Transform parent,
        Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
    {
        GameObject shape = GameObject.CreatePrimitive(primitive);
        shape.name = name;
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = position;
        shape.transform.localScale = scale;
        shape.transform.localRotation = rotation ?? Quaternion.identity;
        shape.isStatic = true;
        if (material != null)
            shape.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = shape.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
        return shape;
    }
}
