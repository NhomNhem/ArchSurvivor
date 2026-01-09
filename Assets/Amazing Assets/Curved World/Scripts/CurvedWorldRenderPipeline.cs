// Curved World <http://u3d.as/1W8h>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace AmazingAssets.CurvedWorld
{
    [CreateAssetMenu(menuName = "Rendering/Curved World RP (Pipeline Asset)")]
    public class CurvedWorldRenderPipeline : UniversalRenderPipelineAsset
    {

        public Shader detailLitShader;
        public Shader wavingGrassShader;
        public Shader wavingGrassBillboardShader;


        public override Shader terrainDetailLitShader => detailLitShader;
        public override Shader terrainDetailGrassShader => wavingGrassShader;
        public override Shader terrainDetailGrassBillboardShader => wavingGrassBillboardShader;
    }
}
