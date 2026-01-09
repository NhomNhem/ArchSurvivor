// Curved World <http://u3d.as/1W8h>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal.ShaderGUI;


namespace AmazingAssets.CurvedWorld.Editor.Universal.ShaderGUI
{
    internal class ParticlesSimpleLitShader : BaseShaderGUI
    {
        // Properties
        private SimpleLitGUI.SimpleLitProperties shadingModelProperties;
        private ParticleGUI.ParticleProperties particleProps;

        // List of renderers using this material in the scene, used for validating vertex streams
        List<ParticleSystemRenderer> m_RenderersUsingThisMaterial = new List<ParticleSystemRenderer>();


        //Curved World
        MaterialHeaderScopeList curvedWorldMaterialScope;


        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            base.FillAdditionalFoldouts(materialScopesList);


            //Curved World
            Material material = (Material)materialEditor.target;
            if (curvedWorldMaterialScope == null)
                curvedWorldMaterialScope = new MaterialHeaderScopeList();
            if (material.HasProperty("_CurvedWorldBendSettings"))
                curvedWorldMaterialScope.RegisterHeaderScope(new GUIContent("Curved World"), AmazingAssets.CurvedWorld.Editor.MaterialProperties.Expandable.CurvedWorld, _ => AmazingAssets.CurvedWorld.Editor.MaterialProperties.DrawCurvedWorldMaterialProperties(materialEditor, MaterialProperties.Style.None, false, false));
        }

        public override void OnGUI(MaterialEditor materialEditorIn, MaterialProperty[] properties)
        {
            if (materialEditorIn == null)
                throw new ArgumentNullException("materialEditorIn");

            materialEditor = materialEditorIn;
            Material material = materialEditor.target as Material;

            FindProperties(properties);   // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly

            // Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
            // material to a universal shader.
            if (m_FirstTimeApply)
            {
                OnOpenGUI(material, materialEditorIn);
                m_FirstTimeApply = false;
            }


            //Curved World
            curvedWorldMaterialScope.DrawHeaders(materialEditor, material);


            ShaderPropertiesGUI(material);
        }

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            shadingModelProperties = new SimpleLitGUI.SimpleLitProperties(properties);
            particleProps = new ParticleGUI.ParticleProperties(properties);


            //Curved World
            MaterialProperties.InitCurvedWorldMaterialProperties(properties);
        }

        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);

            //Curved World
            MaterialProperties.SetKeyWords(material);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            base.DrawSurfaceOptions(material);
            DoPopup(ParticleGUI.Styles.colorMode, particleProps.colorMode, System.Enum.GetNames(typeof(ParticleGUI.ColorMode)));
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            SimpleLitGUI.Inputs(shadingModelProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            SimpleLitGUI.Advanced(shadingModelProperties);

            materialEditor.ShaderProperty(particleProps.flipbookMode, ParticleGUI.Styles.flipbookMode);
            ParticleGUI.FadingOptions(material, materialEditor, particleProps);
            ParticleGUI.DoVertexStreamsArea(material, m_RenderersUsingThisMaterial, true);

            DrawQueueOffsetField();
        }

        public override void OnOpenGUI(Material material, MaterialEditor materialEditor)
        {
            CacheRenderersUsingThisMaterial(material);
            base.OnOpenGUI(material, materialEditor);
        }

        void CacheRenderersUsingThisMaterial(Material material)
        {
            m_RenderersUsingThisMaterial.Clear();

            ParticleSystemRenderer[] renderers = UnityEngine.Object.FindObjectsByType(typeof(ParticleSystemRenderer), FindObjectsSortMode.None) as ParticleSystemRenderer[];
            foreach (ParticleSystemRenderer renderer in renderers)
            {
                if (renderer.sharedMaterial == material)
                    m_RenderersUsingThisMaterial.Add(renderer);
            }
        }
    }
} // namespace UnityEditor
