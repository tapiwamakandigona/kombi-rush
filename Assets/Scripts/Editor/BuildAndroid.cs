using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KombiRush.EditorTools
{
    /// <summary>
    /// Android build entry point. Everything the build needs comes from environment variables so
    /// the same code path runs locally and in CI:
    ///
    ///   KOMBI_OUTPUT           output path, default Builds/kombi-rush.apk
    ///   KOMBI_VERSION          bundleVersion, default the value already in ProjectSettings
    ///   KOMBI_VERSION_CODE     Android versionCode, default 1
    ///   KOMBI_DEVELOPMENT      "1" for a development build with a script debugger
    ///   KOMBI_KEYSTORE         path to a keystore; unset means Unity's debug key
    ///   KOMBI_KEYSTORE_PASS    keystore password
    ///   KOMBI_KEY_ALIAS        key alias
    ///   KOMBI_KEY_PASS         key alias password
    ///
    /// Run headless with:
    ///   Unity -quit -batchmode -nographics -projectPath . \
    ///         -executeMethod KombiRush.EditorTools.BuildAndroid.Build -logFile -
    /// </summary>
    public static class BuildAndroid
    {
        private const string ScenePath = "Assets/Scenes/Boot.unity";

        [MenuItem("Kombi Rush/Build Android APK")]
        public static void BuildFromMenu()
        {
            BuildResult result = Run();
            Debug.Log("[KombiRush] build finished: " + result);
        }

        /// <summary>Called by -executeMethod. Exits the editor with 0 on success, 1 on failure.</summary>
        public static void Build()
        {
            BuildResult result = BuildResult.Failed;
            try
            {
                result = Run();
            }
            catch (Exception ex)
            {
                Debug.LogError("[KombiRush] build threw: " + ex);
            }
            EditorApplication.Exit(result == BuildResult.Succeeded ? 0 : 1);
        }

        private static BuildResult Run()
        {
            string output = Env("KOMBI_OUTPUT", "Builds/kombi-rush.apk");
            string version = Env("KOMBI_VERSION", PlayerSettings.bundleVersion);
            int versionCode = ParseInt(Env("KOMBI_VERSION_CODE", "1"), 1);
            bool development = Env("KOMBI_DEVELOPMENT", "0") == "1";

            ConfigurePlayer(version, versionCode);
            ConfigureSigning();

            string directory = Path.GetDirectoryName(Path.GetFullPath(output));
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            EditorUserBuildSettings.buildAppBundle = output.EndsWith(".aab", StringComparison.OrdinalIgnoreCase);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None
            };

            Debug.Log("[KombiRush] building " + output + " version " + version + " (" + versionCode + ")"
                      + (development ? " development" : " release"));

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log("[KombiRush] result " + summary.result
                      + ", size " + (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB"
                      + ", errors " + summary.totalErrors
                      + ", warnings " + summary.totalWarnings
                      + ", time " + summary.totalTime);
            return summary.result;
        }

        private static void ConfigurePlayer(string version, int versionCode)
        {
            var android = NamedBuildTarget.Android;

            PlayerSettings.companyName = "Tsoro Studios";
            PlayerSettings.productName = "Kombi Rush";
            PlayerSettings.SetApplicationIdentifier(android, "com.tsorostudios.kombirush");
            PlayerSettings.bundleVersion = version;
            PlayerSettings.Android.bundleVersionCode = versionCode;

            // portrait only: this game is played with one thumb on the bus
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // entry-level device targets
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(android, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Low);
            PlayerSettings.SetApiCompatibilityLevel(android, ApiCompatibilityLevel.NET_Unity_4_8);

            // no network features in v1, so do not ask for the permission
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.forceSDCardPermission = false;
            PlayerSettings.Android.androidTVCompatibility = false;
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.Android.startInFullscreen = true;

            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
            {
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3,
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan
            });
            PlayerSettings.MTRendering = true;
            PlayerSettings.gpuSkinning = false;
        }

        private static void ConfigureSigning()
        {
            string keystore = Env("KOMBI_KEYSTORE", string.Empty);
            if (string.IsNullOrEmpty(keystore))
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.Log("[KombiRush] no KOMBI_KEYSTORE set, signing with Unity's debug key");
                return;
            }

            if (!File.Exists(keystore))
                throw new FileNotFoundException("KOMBI_KEYSTORE points at a file that does not exist", keystore);

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(keystore);
            PlayerSettings.Android.keystorePass = Env("KOMBI_KEYSTORE_PASS", string.Empty);
            PlayerSettings.Android.keyaliasName = Env("KOMBI_KEY_ALIAS", string.Empty);
            PlayerSettings.Android.keyaliasPass = Env("KOMBI_KEY_PASS", string.Empty);
            Debug.Log("[KombiRush] signing with the supplied release keystore");
        }

        private static string Env(string name, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, out int parsed) ? parsed : fallback;
        }
    }
}
