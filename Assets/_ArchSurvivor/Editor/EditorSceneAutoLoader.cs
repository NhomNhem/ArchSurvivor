using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArchSurvivor.Editor {
    [InitializeOnLoad]
    public class EditorSceneAutoLoader {
        static EditorSceneAutoLoader() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingEditMode) {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                
                string bootScenePath = "Assets/Scenes/S00_Boot.unity";
                SceneAsset bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(bootScenePath);

                if (bootScene != null) {
                    EditorSceneManager.playModeStartScene = bootScene;
                    Debug.Log($"<color=green>[EditorSceneAutoLoader]</color> Set play mode start scene to: {bootScene.name}");
                }
            }

            if (state == PlayModeStateChange.EnteredEditMode) EditorSceneManager.playModeStartScene = null;
        }
    }
}