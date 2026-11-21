using UnityEngine;

/// <summary>
/// Self-contained flight/rendering smoke test. Uses built-in shapes, with no
/// licensed external assets. Designed to run on Android and in the Editor.
/// Not yet a graphics-equivalent Godot/Stride comparison.
/// </summary>
public sealed class BenchmarkFlight : MonoBehaviour
{
    private Transform craft;
    private Camera followCamera;
    private float speed = 42f;
    private float fps = 60f;
    private float elapsed;
    private int samples;
    private float accumulatedFrames;
    private float minFps = 1000f;
    private readonly Material[] palette = new Material[7];

    private static readonly Color Midnight = new Color(0.045f, 0.075f, 0.13f);
    private static readonly Color Steel = new Color(0.14f, 0.22f, 0.31f);
    private static readonly Color Orange = new Color(1f, 0.31f, 0.06f);
    private static readonly Color Cyan = new Color(0.04f, 0.74f, 0.91f);
    private static readonly Color Glass = new Color(0.075f, 0.24f, 0.34f);
    private static readonly Color Dark = new Color(0.035f, 0.055f, 0.08f);
    private static readonly Color Light = new Color(0.4f, 0.59f, 0.63f);

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.045f, 0.10f, 0.17f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 75f;
        RenderSettings.fogEndDistance = 320f;
        RenderSettings.ambientLight = new Color(0.26f, 0.36f, 0.46f);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Color[] colors = { Midnight, Steel, Orange, Cyan, Glass, Dark, Light };
        for (int i = 0; i < colors.Length; i++)
        {
            palette[i] = new Material(shader);
            palette[i].name = "EclipsePalette_" + i;
            if (palette[i].HasProperty("_BaseColor")) palette[i].SetColor("_BaseColor", colors[i]);
            if (palette[i].HasProperty("_Color")) palette[i].SetColor("_Color", colors[i]);
            palette[i].enableInstancing = true;
        }

        CreateWorld();
        CreateCraft();
        CreateCamera();
    }

    private static GameObject Shape(PrimitiveType primitive, string name, Transform parent,
        Vector3 localPosition, Vector3 size, Material material, Quaternion? rotation = null)
    {
        GameObject shape = GameObject.CreatePrimitive(primitive);
        shape.name = name;
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = localPosition;
        shape.transform.localScale = size;
        shape.transform.localRotation = rotation ?? Quaternion.identity;
        shape.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = shape.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        return shape;
    }

    private void CreateCraft()
    {
        craft = new GameObject("Benchmark Hovercraft").transform;
        craft.position = new Vector3(0f, 23f, 0f);
        Shape(PrimitiveType.Cube, "Main fuselage", craft, Vector3.zero,
            new Vector3(3.2f, 0.8f, 7.0f), palette[1]);
        Shape(PrimitiveType.Cube, "Forward snout", craft, new Vector3(0f, -0.2f, 3.5f),
            new Vector3(2.45f, 0.45f, 2.1f), palette[6]);
        Shape(PrimitiveType.Capsule, "Canopy", craft, new Vector3(0f, 0.67f, -0.6f),
            new Vector3(1.32f, 0.6f, 2.1f), palette[4]);
        for (int side = -1; side <= 1; side += 2)
        {
            Shape(PrimitiveType.Cube, "Side sponson", craft,
                new Vector3(side * 2.6f, -0.25f, -0.35f),
                new Vector3(1.45f, 0.75f, 5.65f), palette[1]);
            Shape(PrimitiveType.Cube, "Outrigger wing", craft,
                new Vector3(side * 3.1f, -0.12f, -1.6f),
                new Vector3(2.6f, 0.22f, 2.8f), palette[5]);
            Shape(PrimitiveType.Cylinder, "Rear thruster", craft,
                new Vector3(side * 2.55f, -0.35f, -3.4f),
                new Vector3(0.62f, 0.42f, 0.62f), palette[2],
                Quaternion.Euler(90f, 0f, 0f));
            Shape(PrimitiveType.Cube, "Running light", craft,
                new Vector3(side * 3.4f, 0.05f, 0.0f),
                new Vector3(0.18f, 0.12f, 4.8f), palette[3]);
            Shape(PrimitiveType.Cube, "Tail fin", craft,
                new Vector3(side * 1.35f, 0.8f, -2.9f),
                new Vector3(0.25f, 1.8f, 1.2f), palette[6]);
        }
    }

    private void CreateWorld()
    {
        Random.InitState(2407);
        var root = new GameObject("Floating Sky Course").transform;

        Shape(PrimitiveType.Plane, "Ocean beneath city", root,
            new Vector3(0f, -24f, 235f), new Vector3(90f, 1f, 95f), palette[0]);

        for (int i = 0; i < 13; i++)
        {
            float z = i * 38f + 28f;
            Shape(PrimitiveType.Cube, "Suspended central causeway", root,
                new Vector3(0f, 4f, z), new Vector3(16f, 1.0f, 26f), palette[5]);

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * Random.Range(26f, 78f);
                float height = Random.Range(25f, 95f);
                Shape(PrimitiveType.Cube, "Vertical skyscraper", root,
                    new Vector3(x, height * 0.5f - 18f, z + Random.Range(-12f, 12f)),
                    new Vector3(Random.Range(9f, 20f), height, Random.Range(11f, 22f)),
                    palette[(i + side + 8) % 3 == 0 ? 6 : 1]);
                Shape(PrimitiveType.Cube, "Neon facade", root,
                    new Vector3(x, height - 18f, z),
                    new Vector3(4.4f, 0.35f, 10f), palette[3]);
            }

            Shape(PrimitiveType.Cube, "Navigation gate beam", root,
                new Vector3(0f, 42f, z), new Vector3(23f, 0.75f, 0.75f), palette[3]);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape(PrimitiveType.Cube, "Navigation gate pillar", root,
                    new Vector3(side * 11.5f, 30f, z),
                    new Vector3(0.75f, 23.5f, 0.75f), palette[3]);
            }
        }

        var sunObject = new GameObject("Directional sun");
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.80f, 0.91f, 1f);
        sun.intensity = 1.3f;
        sun.shadows = LightShadows.Soft;
        sunObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
    }

    private void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Flight Camera");
        followCamera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        followCamera.fieldOfView = 72f;
        followCamera.nearClipPlane = 0.12f;
        followCamera.farClipPlane = 540f;
        followCamera.clearFlags = CameraClearFlags.SolidColor;
        followCamera.backgroundColor = new Color(0.045f, 0.10f, 0.17f);
        followCamera.transform.position = craft.position - craft.forward * 13f + Vector3.up * 5f;
        followCamera.transform.LookAt(craft.position + craft.forward * 24f);
    }

    private static Rect ControlRect(int index)
    {
        float w = Mathf.Min(145f, (Screen.width - 48f) / 6f);
        float gap = 5f;
        float x = 12f + index * (w + gap);
        return new Rect(x, Screen.height - 98f, w, 86f);
    }

    private bool Pressed(int index)
    {
        Rect target = ControlRect(index);
        if (Input.GetMouseButton(0))
        {
            var cursor = Input.mousePosition;
            if (target.Contains(new Vector2(cursor.x, Screen.height - cursor.y))) return true;
        }
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;
            Vector2 p = new Vector2(touch.position.x, Screen.height - touch.position.y);
            if (target.Contains(p)) return true;
        }
        return false;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.06f);
        float yaw = ((Pressed(0) || Input.GetKey(KeyCode.A)) ? -1f : 0f)
                  + ((Pressed(1) || Input.GetKey(KeyCode.D)) ? 1f : 0f);
        float pitch = ((Pressed(2) || Input.GetKey(KeyCode.W)) ? 1f : 0f)
                    - ((Pressed(3) || Input.GetKey(KeyCode.S)) ? 1f : 0f);

        if (Pressed(4) || Input.GetKey(KeyCode.Space)) speed = Mathf.Min(speed + dt * 32f, 85f);
        else if (Pressed(5) || Input.GetKey(KeyCode.LeftShift)) speed = Mathf.Max(speed - dt * 35f, 16f);
        else speed = Mathf.MoveTowards(speed, 42f, dt * 7f);

        craft.Rotate(-pitch * dt * 34f, yaw * dt * 48f, -yaw * dt * 20f, Space.Self);
        craft.position += craft.forward * (speed * dt);
        Vector3 pos = craft.position;
        pos.y = Mathf.Clamp(pos.y, 12f, 95f);
        craft.position = pos;
        if (craft.position.z > 510f || craft.position.z < -35f)
        {
            craft.position = new Vector3(0f, 23f, 0f);
            craft.rotation = Quaternion.identity;
        }

        if (Time.unscaledDeltaTime > 0f)
        {
            float instantaneousFps = 1f / Time.unscaledDeltaTime;
            fps = Mathf.Lerp(fps, instantaneousFps, 0.065f);
            minFps = Mathf.Min(minFps, instantaneousFps);
            accumulatedFrames += instantaneousFps;
            samples++;
            elapsed += Time.unscaledDeltaTime;
        }
    }

    private void LateUpdate()
    {
        Vector3 target = craft.position - craft.forward * 14f + craft.up * 5.3f;
        followCamera.transform.position = Vector3.Lerp(followCamera.transform.position,
            target, 1f - Mathf.Exp(-4.5f * Time.deltaTime));
        Quaternion facing = Quaternion.LookRotation(craft.position + craft.forward * 22f -
            followCamera.transform.position, Vector3.up);
        followCamera.transform.rotation = Quaternion.Slerp(followCamera.transform.rotation,
            facing, 1f - Mathf.Exp(-5f * Time.deltaTime));
    }

    private void OnGUI()
    {
        GUI.skin.box.fontSize = Mathf.Clamp(Screen.height / 32, 13, 26);
        GUI.skin.box.alignment = TextAnchor.MiddleCenter;
        GUI.Box(new Rect(12f, 12f, Mathf.Min(Screen.width - 24f, 620f), 94f),
            "ECLIPSE : HOLLOW SKY  |  UNITY URP TEST\n" +
            "FPS " + fps.ToString("F0") + "  •  Avg " +
            (samples > 0 ? accumulatedFrames / samples : 0f).ToString("F0") +
            "  •  Speed " + speed.ToString("F0") + "  •  y " + craft.position.y.ToString("F0") +
            "  •  Run " + elapsed.ToString("F0") + "s");
        string[] buttons = { "LEFT", "RIGHT", "CLIMB", "DIVE", "BOOST", "BRAKE" };
        for (int i = 0; i < buttons.Length; i++) GUI.Box(ControlRect(i), buttons[i]);
    }
}
