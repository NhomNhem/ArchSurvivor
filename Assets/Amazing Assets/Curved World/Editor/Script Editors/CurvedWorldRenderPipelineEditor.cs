// Curved World <http://u3d.as/1W8h>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEditor;
using UnityEditor.Rendering.Universal;


namespace AmazingAssets.CurvedWorld.Editor
{

    [CustomEditor(typeof(CurvedWorldRenderPipeline), true)]
    public class CurvedWorldRenderPipelineEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor originalEditor;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CurvedWorldRenderPipeline.detailLitShader)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CurvedWorldRenderPipeline.wavingGrassShader)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CurvedWorldRenderPipeline.wavingGrassBillboardShader)));
            EditorGUILayout.Space();

            if (originalEditor == null)
            {
                originalEditor = UnityEditor.Editor.CreateEditor(target, typeof(UniversalRenderPipelineAssetEditor));
            }
            originalEditor.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
