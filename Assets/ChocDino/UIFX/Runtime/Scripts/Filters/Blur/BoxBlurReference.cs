//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	/*internal class BlurMaterial : FilterMaterial
	{

		static Material CreateMaterialFromShader(string shaderName)
		{
			Material result = null;
			Shader shader = Shader.Find(shaderName);
			if (shader != null)
			{
				result = new Material(shader);
			}
			return result;
		}

		internal BlurMaterial()
		{
			_material = CreateMaterialFromShader(BlurShader.Id);
		}

		internal void SetKernelRadius(float value)
		{
			_material.SetFloat(BlurShader.Prop.KernelRadius, value);
		}

		//ObjectHelper.Destroy(ref _material);


		private Material _material;


	}*/

	public class BoxBlurReference : ITextureBlur
	{
		internal class BlurShader
		{
			internal const string Id = "Hidden/ChocDino/UIFX/BoxBlur-Reference";

			internal static class Prop
			{
				internal static readonly int KernelRadius = Shader.PropertyToID("_KernelRadius");
			}
			internal static class Pass
			{
				internal const int Horizontal = 0;
				internal const int Vertical = 1;
				internal const int HorizontalFrac = 2;
				internal const int VerticalFrac = 3;
				internal const int Box = 4;
				internal const int BoxFrac = 5;
			}
		}

		public bool Fractional { get { return _supportsFractionalKernel; } set { _supportsFractionalKernel = value; } }
		public BlurAxes2D BlurAxes2D { get { return _blurAxes2D; } set { _blurAxes2D = value; } }
		public Downsample Downsample { get { return _downSample; } set { if (_downSample != value) { _downSample = value; _materialDirty = true; } } }
		public int IterationCount { get { return _iterationCount; } set { value = Mathf.Clamp(value, 1, 6); if (_iterationCount != value) { _iterationCount = value; } } }

		private bool _supportsFractionalKernel = true;
		private int _iterationCount = 3;
		private Downsample _downSample = Downsample.Auto;
		private float _blurSize = 0.05f;
		private Material _material;
		private RenderTexture _sourceTexture;
		private RenderTexture _rtBlurH;
		private RenderTexture _rtBlurV;
		private BlurAxes2D _blurAxes2D = BlurAxes2D.Default;

		private bool _materialDirty = true;

		public void ForceDirty()
		{
			_materialDirty = true;
		}

		public void SetBlurSize(float diagonalPercent)
		{
			if (diagonalPercent != _blurSize)
			{
				_blurSize = diagonalPercent;
				_materialDirty = true;
			}
		}

		public void AdjustBoundsSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
			// Get radius for box blur
			float radius = GetScaledRadius();

			// Multiple iterations increases the radius area (simulating gaussian blur)
			// NOTE: This size is based off a solid white box which is the worst case, if
			// the contents of the image is dark then this radius could be significantly shrunk,
			// but there is no easy way to detect this. Also if the image is HDR then this expand
			// may be too small - perhaps expose option to the user.
			radius *= _iterationCount;
			radius *= GetDownsampleFactor();

			int x = Mathf.CeilToInt(radius);
			if (x > 0)
			{
				Vector2Int result = new Vector2Int(x, x);
				if (_blurAxes2D == BlurAxes2D.Horizontal)
				{
					result.y = 0;
				}
				else if (_blurAxes2D == BlurAxes2D.Vertical)
				{
					result.x = 0;
				}
				leftDown += result;
				rightUp += result;
			}
		}

		public RenderTexture Process(RenderTexture sourceTexture)
		{
			Debug.Assert(sourceTexture != null);

			// Early-out
			if (GetScaledRadius() <= 0f)
			{
				FreeTextures();
				return sourceTexture;
			}

			RenderTexture prevRT = RenderTexture.active;

			SetupResources(sourceTexture);

			if (_materialDirty)
			{
				UpdateMaterial();
			}

			RenderTexture src = _sourceTexture;

			if (GetScaledRadius() > 0f)
			{
				// Have to downsample first otherwise it will be biased in the first blur pass direction
				// leading to slightly stretched result
				if (GetDownsampleFactor() > 1)
				{
					Graphics.Blit(src, _rtBlurV);
					_rtBlurV.IncrementUpdateCount();
					src = _rtBlurV;
				}

				bool isFractionalKernel = _supportsFractionalKernel;
				if (isFractionalKernel)
				{
					float radius = GetScaledRadius();
					if (radius == Mathf.Floor(radius))
					{
						isFractionalKernel = false;
					}
				}

				// Blur
				Debug.Assert(_iterationCount > 0);
				for (int i = 0; i < _iterationCount; i++)
				{
					if (_blurAxes2D == BlurAxes2D.Default)
					{
						int pass = isFractionalKernel ? BlurShader.Pass.HorizontalFrac : BlurShader.Pass.Horizontal;
						Graphics.Blit(src, _rtBlurH, _material, pass);
						_rtBlurH.IncrementUpdateCount();

						pass = isFractionalKernel ? BlurShader.Pass.VerticalFrac : BlurShader.Pass.Vertical;
						Graphics.Blit(_rtBlurH, _rtBlurV, _material, pass);
						_rtBlurV.IncrementUpdateCount();
						src = _rtBlurV;
					}
					else
					{
						int pass = (_blurAxes2D == BlurAxes2D.Horizontal) ? BlurShader.Pass.Horizontal : BlurShader.Pass.Vertical;
						if (isFractionalKernel)
						{
							pass = (_blurAxes2D == BlurAxes2D.Horizontal) ? BlurShader.Pass.HorizontalFrac : BlurShader.Pass.VerticalFrac;
						}

						var dst = _rtBlurH;
						bool isOdd = (i&1) != 0;
						if (isOdd)
						{
							dst = _rtBlurV;
						}

						Graphics.Blit(src, dst, _material, pass);
						dst.IncrementUpdateCount();
						src = dst;
					}
				}

				// Free intermediate textures
				if (src == _rtBlurH)
				{
					RenderTextureHelper.ReleaseTemporary(ref _rtBlurV);
				}
				else
				{
					RenderTextureHelper.ReleaseTemporary(ref _rtBlurH);
				}
			}
			else
			{
				FreeTextures();
			}

			RenderTexture.active = prevRT;

			return src;
		}

		public void FreeResources()
		{
			FreeShaders();
			FreeTextures();
		}

		private static uint CreateTextureHash(int width, int height)
		{
			uint hash = 0;
			hash = (hash << 0) | (uint)width;
			hash = (hash << 13) | (uint)height;
			return hash;
		}

		private void RecreateTexture(ref RenderTexture rt, uint desiredHash, RenderTexture sourceTexture)
		{
			// Release the texture if it isn't suitable
			if (rt != null)
			{
				uint hash = CreateTextureHash(rt.width, rt.height);
				if (hash != desiredHash)
				{
					RenderTextureHelper.ReleaseTemporary(ref rt);
					_materialDirty = true;
				}
			}

			// Create texture
			if (rt == null && sourceTexture != null)
			{
				Debug.Assert(sourceTexture.width > 0 && sourceTexture.height > 0);
				int w = Mathf.Max(1, sourceTexture.width / GetDownsampleFactor());
				int h = Mathf.Max(1, sourceTexture.height / GetDownsampleFactor());

				RenderTextureFormat format = sourceTexture.format;
				if ((Filters.PerfHint & PerformanceHint.UseMorePrecision) != 0)
				{
					// TODO: create based on the input texture format, but just with more precision
					format = RenderTextureFormat.ARGBHalf;
				}

				rt = RenderTexture.GetTemporary(w, h, 0, format, RenderTextureReadWrite.Linear);
				rt.wrapMode = TextureWrapMode.Clamp;
			}
		}

		void SetupResources(RenderTexture sourceTexture)
		{
			uint desiredTextureHash = 0;
			if (sourceTexture != null)
			{
				desiredTextureHash = CreateTextureHash(sourceTexture.width / GetDownsampleFactor(), sourceTexture.height / GetDownsampleFactor());
			}

			RecreateTexture(ref _rtBlurH, desiredTextureHash, sourceTexture);
			RecreateTexture(ref _rtBlurV, desiredTextureHash, sourceTexture);

			#if UNITY_EDITOR
			_rtBlurH.name = "BoxBlurH";
			_rtBlurV.name = "BoxBlurV";
			#endif
	
			if (_sourceTexture != sourceTexture)
			{
				_materialDirty = true;
				_sourceTexture = sourceTexture;
			}
			if (_material == null)
			{
				CreateShaders();
			}
		}

		private float GetScaledRadius()
		{
			float radius = _blurSize / (float)GetDownsampleFactor();
			if (_iterationCount > 1)
			{
				// For multiple iterations, scale radius down for approximation of gaussian blur
				// This approximation was calculated by experimentation, but for some reasons seems to do a better job than:
				//float sigma = radius / 2f;
				// radius = 0.5f * Mathf.Sqrt(1f + ((12f * sigma * sigma) / (float)_iterationCount));
				// from derived from "Fast Almost-Gaussian Filtering / Peter Kovesi"
				radius /= Mathf.Sqrt((float)_iterationCount * 3.5f);
			}
			return radius;
		}

		void UpdateMaterial()
		{
			float kernelRadius = GetScaledRadius();
			//_blurShader.SetKernelRadius(kernalRadius);
			_material.SetFloat(BlurShader.Prop.KernelRadius, kernelRadius);
			_materialDirty = false;
		}

		static Material CreateMaterialFromShader(string shaderName)
		{
			Material result = null;
			Shader shader = Shader.Find(shaderName);
			if (shader != null)
			{
				result = new Material(shader);
			}
			return result;
		}

		void CreateShaders()
		{
			_material = CreateMaterialFromShader(BlurShader.Id);
			Debug.Assert(_material != null);
			_materialDirty = true;
		}

		void FreeShaders()
		{
			ObjectHelper.Destroy(ref _material);
		}

		void FreeTextures()
		{
			RenderTextureHelper.ReleaseTemporary(ref _rtBlurV);
			RenderTextureHelper.ReleaseTemporary(ref _rtBlurH);
			_sourceTexture = null;
		}

		private int GetDownsampleFactor()
		{
			int result = 1;
			if (_downSample == Downsample.Auto)
			{
				if ((Filters.PerfHint & PerformanceHint.AllowDownsampling) != 0)
				{
					result = 2;
				}
			}
			else
			{
				result = (int)_downSample;
			}

			if (_blurSize > 120f)
			{
				result *= 4;
			}
			else if (_blurSize > 60f)
			{
				result *= 2;
			}

			return result;
		}
	}
}