using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PlagueGame.Clinic;
using PlagueGame.Kingdom;

namespace PlagueGame.EditorTools
{
    public static class ProjectBootstrap
    {
        private const string CheckScene = "Assets/Scenes/EnvironmentCheck.unity";
        private const string PrototypeScene = "Assets/Scenes/ClinicPrototype.unity";
        private const string KingdomScene = "Assets/Scenes/KingdomPrototype.unity";

        [MenuItem("PlagueGame/Setup/Configure project")]
        public static void Configure()
        {
            foreach (var folder in new[] { "Scenes", "Scripts", "Prefabs", "Models", "Materials", "Animations", "Audio", "UI", "Data", "Settings" })
                Directory.CreateDirectory("Assets/" + folder);

            PlayerSettings.companyName = "Independent";
            PlayerSettings.productName = "PlagueGame";
            PlayerSettings.bundleVersion = "0.0.1";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.plaguegame.prototype");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.use32BitDisplayBuffer = true;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorSettings.serializationMode = SerializationMode.ForceText;

            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AndroidExternalToolsSettings.sdkRootPath = Path.Combine(local, "Android", "Sdk");
            AndroidExternalToolsSettings.ndkRootPath = Path.Combine(local, "Programs", "GameDev", "Android", "android-ndk-r27c");
            AndroidExternalToolsSettings.jdkRootPath = Path.Combine(local, "Programs", "GameDev", "Android", "OpenJDK17");
            EditorPrefs.SetBool("SdkUseEmbedded", false);
            EditorPrefs.SetBool("NdkUseEmbedded", false);
            EditorPrefs.SetBool("JdkUseEmbedded", false);

            const string rendererPath = "Assets/Settings/MobileRenderer.asset";
            const string pipelinePath = "Assets/Settings/MobilePipeline.asset";
            AssetDatabase.Refresh();
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.supportsHDR = false;
                pipeline.msaaSampleCount = 1;
                pipeline.shadowDistance = 25f;
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            // GLES3 with separate background and viewport cameras needs matching single-sample backbuffers.
            pipeline.msaaSampleCount = 1;
            pipeline.shadowDistance = 65;
            pipeline.shadowCascadeCount = 2;
            var pipelineSettings = new SerializedObject(pipeline);
            pipelineSettings.FindProperty("m_SoftShadowsSupported").boolValue = true;
            pipelineSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            QualitySettings.vSyncCount = 0;

            if (!File.Exists(CheckScene))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.transform.position = new Vector3(0, 8, -8);
                camera.transform.rotation = Quaternion.Euler(45, 0, 0);
                camera.orthographic = true;
                camera.orthographicSize = 5;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.15f, 0.19f);
                camera.gameObject.AddComponent<AudioListener>();
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                var light = new GameObject("Directional Light").AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Environment check ground";
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "Environment check marker";
                marker.transform.position = new Vector3(0, 0.5f, 0);
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = new Color(0.36f, 0.48f, 0.58f);
                AssetDatabase.CreateAsset(material, "Assets/Materials/EnvironmentCheck.mat");
                marker.GetComponent<Renderer>().sharedMaterial = material;
                ground.GetComponent<Renderer>().sharedMaterial = material;
                EditorSceneManager.SaveScene(scene, CheckScene);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(CheckScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("PLAGUE_SETUP_PROJECT_OK");
        }

        [MenuItem("PlagueGame/Prototype/Open clinic")]
        public static void ConfigurePrototype()
        {
            Configure();
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 2;
            if (!File.Exists(PrototypeScene))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.transform.position = new Vector3(12, 19, -17);
                camera.transform.LookAt(new Vector3(0, .6f, -.2f));
                camera.orthographic = true;
                camera.orthographicSize = 15;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 100;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.82f, .84f, .78f);
                camera.gameObject.AddComponent<AudioListener>();
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                var light = new GameObject("Daylight").AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(55, -35, 0);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.73f, .78f, .77f);
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ClinicBase.mat");
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(material, "Assets/Materials/ClinicBase.mat");
                }
                var clinic = new GameObject("Clinic prototype").AddComponent<ClinicPrototype>();
                clinic.uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PTSans-Regular.ttf");
                clinic.worldMaterial = material;
                if (clinic.uiFont == null) throw new Exception("PT Sans UI font is missing");
                EditorSceneManager.SaveScene(scene, PrototypeScene);
            }
            else EditorSceneManager.OpenScene(PrototypeScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(PrototypeScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("PLAGUE_CLINIC_PROJECT_OK portrait-only");
        }

        [MenuItem("PlagueGame/Prototype/Build Android clinic")]
        public static void ConfigureAndBuildPrototype()
        {
            ConfigurePrototype();
            Directory.CreateDirectory("Builds/Android");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PrototypeScene },
                locationPathName = "Builds/Android/PlagueGame-Clinic.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Clinic Android build failed: " + report.summary.result);
            Debug.Log("PLAGUE_CLINIC_ANDROID_BUILD_OK");
        }

        [MenuItem("PlagueGame/Setup/Build Android environment check")]
        public static void ConfigureAndBuild()
        {
            Configure();
            Directory.CreateDirectory("Builds/Android");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { CheckScene },
                locationPathName = "Builds/Android/PlagueGame-EnvironmentCheck.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Android environment check failed: " + report.summary.result);
            Debug.Log("PLAGUE_SETUP_ANDROID_BUILD_OK " + report.summary.totalSize + " bytes");
        }

        [MenuItem("PlagueGame/Prototype/Open kingdom")]
        public static void ConfigureKingdom()
        {
            Configure();
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.Android.bundleVersionCode = 4;
            if (!File.Exists(KingdomScene))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var backdrop = new GameObject("Background camera").AddComponent<Camera>();
                backdrop.depth = -10; backdrop.cullingMask = 0; backdrop.clearFlags = CameraClearFlags.SolidColor;
                backdrop.backgroundColor = new Color(.914f, .906f, .847f);
                backdrop.gameObject.AddComponent<UniversalAdditionalCameraData>();
                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 14;
                camera.nearClipPlane = .1f; camera.farClipPlane = 150;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.gameObject.AddComponent<AudioListener>();
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                var light = new GameObject("Daylight").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.4f;
                light.shadows = LightShadows.Soft; light.transform.rotation = Quaternion.Euler(55, -35, 0);
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.77f, .8f, .76f);
                var kingdom = new GameObject("Kingdom prototype").AddComponent<KingdomPrototype>();
                kingdom.uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PTSans-Regular.ttf");
                kingdom.worldMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ClinicBase.mat");
                if (kingdom.uiFont == null || kingdom.worldMaterial == null) throw new Exception("Kingdom font/material missing");
                EditorSceneManager.SaveScene(scene, KingdomScene);
            }
            else EditorSceneManager.OpenScene(KingdomScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(KingdomScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("PLAGUE_KINGDOM_PROJECT_OK portrait-only");
        }

        [MenuItem("PlagueGame/Prototype/Build Android kingdom")]
        public static void ConfigureAndBuildKingdom()
        {
            ConfigureKingdom();
            Directory.CreateDirectory("Builds/Android");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { KingdomScene }, locationPathName = "Builds/Android/PlagueGame-Kingdom.apk",
                target = BuildTarget.Android, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Kingdom Android build failed: " + report.summary.result);
            Debug.Log("PLAGUE_KINGDOM_ANDROID_BUILD_OK " + report.summary.totalSize + " bytes");
        }
    }
}
