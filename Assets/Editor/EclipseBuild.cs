using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>One canonical scene initializer and Android CI build method.</summary>
public static class EclipseBuild
{
    private const string ScenePath = "Assets/Scenes/Benchmark.unity";
    private const string RendererPath = "Assets/Settings/EclipseRenderer.asset";
    private const string PipelinePath = "Assets/Settings/EclipseURP.asset";
    private const string ApkPath = "build/Android/Eclipse-Hollow-Sky-Unity.apk";

    [MenuItem("Eclipse/Prepare Benchmark Scene")]
    public static void PrepareScene()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Settings");

        UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }

        UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
        }

        pipeline.renderScale = 1f;
        EnsureRuntimeMaterial();
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        EditorUtility.SetDirty(pipeline);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var runner = new GameObject("Eclipse Benchmark Bootstrap");
        runner.AddComponent<BenchmarkFlight>();
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new Exception("Could not save benchmark scene.");

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Eclipse benchmark scene generated: " + ScenePath);
    }

    private static void EnsureRuntimeMaterial()
    {
        EnsureFolder("Assets/Resources");
        const string materialPath = "Assets/Resources/EclipseLit.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) throw new Exception("Could not find a URP shader to bundle for Android.");

        var material = new Material(shader) { name = "EclipseLit" };
        AssetDatabase.CreateAsset(material, materialPath);
        EditorUtility.SetDirty(material);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string[] parts = path.Split('/');
        string parent = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string child = parent + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(child))
                AssetDatabase.CreateFolder(parent, parts[i]);
            parent = child;
        }
    }

    /// <summary>Called by game-ci/unity-builder through buildMethod.</summary>
    public static void BuildAndroid()
    {
        PrepareScene();
        PlayerSettings.companyName = "Eclipse";
        PlayerSettings.productName = "Eclipse Hollow Sky Unity Test";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.eclipse.hollowsky.unitybench");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        // Use the broadly compatible GLES3 path while diagnosing device-side URP rendering.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        EditorUserBuildSettings.buildAppBundle = false;
        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));

        var result = BuildPipeline.BuildPlayer(new[] { ScenePath }, ApkPath,
            BuildTarget.Android, BuildOptions.None);
        if (result.summary.result != BuildResult.Succeeded)
            throw new Exception("Android build failed: " + result.summary.result);

        Debug.Log("APK generated at " + ApkPath + " (" + result.summary.totalSize + " bytes).");
    }
}
