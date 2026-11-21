# Eclipse: Hollow Sky — Unity Android Benchmark

A **separate**, free-tool Unity 6.3 LTS / URP Android flight-and-rendering benchmark. This is **not** the Godot or Stride game repository.

## Status

- Source scaffold and build automation only until a Unity CI build completes successfully.
- A scene and URP renderer are generated in Unity by `EclipseBuild.PrepareScene`.
- The Android build is **manual** (`Actions > Unity Android APK > Run workflow`) to avoid wasting runner time before Unity licensing is configured.
- Do not call a GitHub Actions run a working APK until the build and artifact upload are verified.

## Unity project

Open this repository in **Unity 6000.3.27f2** with Android Build Support, SDK/NDK and OpenJDK installed. In the Unity menu choose **Eclipse > Prepare Benchmark Scene**, then open `Assets/Scenes/Benchmark.unity` and press Play.

The first test creates a simple stylized floating-city course and a flyable prototype. It is a **pipeline/controls smoke test**, not a graphical-quality milestone or apples-to-apples engine benchmark yet.

## CI / first APK

1. Activate a **Unity Personal** license through Unity Hub. Follow the [current GameCI guide](https://game.ci/docs/github/activation/). GameCI documents a `.ulf` file and three repository secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`. Keep credentials in **GitHub Actions secrets**, never in source code, issues, or chats.
2. Repository > **Settings > Secrets and variables > Actions**; add the three secrets. (The pipeline will stop early with a useful message if they are missing.)
3. In **Actions**, run **Unity Android APK** manually. After a successful build, download the `Eclipse-Hollow-Sky-Unity-APK` artifact from that workflow or get the APK from the new **pre-release** on the repository Releases page.
4. Check the APK on a real Android device; emulator startup alone does not establish graphics performance or stability.

**Signing:** Without a persistent Android signing key, development APK signatures may vary between builds, requiring an uninstall before reinstalling. Later configure a dedicated test keystore in secrets for install-over-upgrades; do not use an exposed/public signing key for a release build.

## Benchmark criteria

Record Android model, resolution, Unity build number, renderer (Vulkan/OpenGLES3), average and 1% low FPS, CPU/GPU frame times where available, draw calls, memory, and device temperature. Compare identical camera paths and content with Godot and Stride before choosing an engine.

## Files

- `Assets/Scripts/BenchmarkFlight.cs`: lightweight flight controls, geometry and on-screen telemetry.
- `Assets/Editor/EclipseBuild.cs`: deterministic URP scene setup and Android build entry point.
- `.github/workflows/source-checks.yml`: no-license repository checks on push.
- `.github/workflows/unity-android.yml`: on-demand Unity / Android APK + artifact + pre-release.

License: The project is a prototype; only original code and primitive geometry are included.