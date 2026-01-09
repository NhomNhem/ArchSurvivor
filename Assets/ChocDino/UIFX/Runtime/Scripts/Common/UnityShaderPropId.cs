//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	internal static class UnityShaderProp
	{
		public readonly static int TextureAddSample = Shader.PropertyToID("_TextureSampleAdd");
		public readonly static int ScreenParams = Shader.PropertyToID("_ScreenParams");
		public readonly static int UIVertexColorAlwaysGammaSpace = Shader.PropertyToID("_UIVertexColorAlwaysGammaSpace");
	
		public readonly static int Stencil = Shader.PropertyToID("_Stencil");
		public readonly static int StencilComp = Shader.PropertyToID("_StencilComp");
		public readonly static int StencilOp = Shader.PropertyToID("_StencilOp");
		public readonly static int StencilWriteMask = Shader.PropertyToID("_StencilWriteMask");
		public readonly static int StencilReadMask = Shader.PropertyToID("_StencilReadMask");

		public readonly static int BlendSrc = Shader.PropertyToID("_BlendSrc");
		public readonly static int BlendDst = Shader.PropertyToID("_BlendDst");

		public readonly static int ClipRect = Shader.PropertyToID("_ClipRect");

		private readonly static Vector4 DefaultClipRect = new Vector4(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity);

		public static void ResetClipRect(Material material)
		{
			material.SetVector(UnityShaderProp.ClipRect, DefaultClipRect);
		}

		public static void ResetStencilProperties(Material material)
		{
			material.SetFloat(UnityShaderProp.Stencil, 0f);
			material.SetFloat(UnityShaderProp.StencilComp, (float)(UnityEngine.Rendering.CompareFunction.Always));
			material.SetFloat(UnityShaderProp.StencilOp, (float)(UnityEngine.Rendering.StencilOp.Keep));
			material.SetFloat(UnityShaderProp.StencilReadMask, 255f);
			material.SetFloat(UnityShaderProp.StencilWriteMask, 255f);
		}

		public static void CopyStencilProperties(Material src, Material dst)
		{
			dst.SetFloat(UnityShaderProp.Stencil, src.GetFloat(UnityShaderProp.Stencil));
			dst.SetFloat(UnityShaderProp.StencilComp, src.GetFloat(UnityShaderProp.StencilComp));
			dst.SetFloat(UnityShaderProp.StencilOp, src.GetFloat(UnityShaderProp.StencilOp));
			dst.SetFloat(UnityShaderProp.StencilReadMask, src.GetFloat(UnityShaderProp.StencilReadMask));
			dst.SetFloat(UnityShaderProp.StencilWriteMask, src.GetFloat(UnityShaderProp.StencilWriteMask));
		}
	}
}