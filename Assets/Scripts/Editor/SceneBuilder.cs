using KombiRush.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KombiRush.EditorTools
{
    /// <summary>
    /// Regenerates Assets/Scenes/Boot.unity from code. The scene deliberately holds almost
    /// nothing - a camera and one GameRoot component - because everything else is built at
    /// runtime. Having a generator means the scene can never drift out of sync with the code,
    /// and a corrupted scene file is one menu click away from being fixed.
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Boot.unity";

        [MenuItem("Kombi Rush/Regenerate Boot Scene")]
        public static void Regenerate()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette.DustDark;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var gameObject = new GameObject("Game");
            gameObject.AddComponent<GameRoot>();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                Debug.LogError("[KombiRush] could not save " + ScenePath);
                return;
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[KombiRush] regenerated " + ScenePath);
        }

        /// <summary>Batch entry point: Unity -quit -batchmode -executeMethod ...RegenerateBatch</summary>
        public static void RegenerateBatch()
        {
            Regenerate();
            EditorApplication.Exit(0);
        }
    }
}
