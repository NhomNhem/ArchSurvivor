//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEngine;

namespace ChocDino.UIFX
{
	internal class Compositor
	{
		private const string CompositeShaderPath = "Hidden/ChocDino/UIFX/Composite";

		private static class ShaderPass
		{
			internal const int AlphaBlendedToPremultipliedAlpha = 0;
		}

		private RenderTexture _composite;
		private Matrix4x4 _projectionMatrix;
		private RenderTexture _prevRT;
		private Material _compositeMaterial;
		private Camera _prevCamera;
		private Matrix4x4 _viewMatrix;
		private List<Color> _vertexColors;
		private List<Color> _vertexColorsLinear;

		public bool IsTextureTooLarge { get; private set; }

		private static bool _isTextureFormatCreated;
		private static RenderTextureFormat _defaultTextureFormat;
		private static RenderTextureFormat _defaultHdrTextureFormat;

		public static RenderTextureFormat DefaultTextureFormat { get { return _defaultTextureFormat; } }
		public static RenderTextureFormat DefaultHdrTextureFormat { get { return _defaultHdrTextureFormat; } }

		private static void CreateTextureFormats()
		{
			Debug.Assert(!_isTextureFormatCreated);

			_defaultTextureFormat = RenderTextureFormat.Default;
			if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
			{
				_defaultTextureFormat = RenderTextureFormat.ARGBHalf;
			}
			if ((Filters.PerfHint & PerformanceHint.UseLessPrecision) != 0)
			{
				if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
				{
					_defaultTextureFormat = RenderTextureFormat.ARGB32;
				}
			}

			_defaultHdrTextureFormat = RenderTextureFormat.DefaultHDR;
			if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
			{
				_defaultHdrTextureFormat = RenderTextureFormat.ARGBHalf;
			}
		}

		public void FreeResources()
		{
			_vertexColorsLinear = null;
			_vertexColors = null;
			RenderTextureHelper.ReleaseTemporary(ref _composite);
			ObjectHelper.Destroy(ref _compositeMaterial);
		}

		public bool Start(Camera camera, RectInt textureRect, bool forceHdr, float canvasScale = 1f)
		{
			if (_compositeMaterial == null)
			{
				_compositeMaterial = new Material(Shader.Find(CompositeShaderPath));
				Debug.Assert(_compositeMaterial != null);
			}

			if (!_isTextureFormatCreated)
			{
				CreateTextureFormats();
				_isTextureFormatCreated = true;
			}

			RectInt scaledTextureRect = textureRect;

			scaledTextureRect.xMin = Mathf.FloorToInt(textureRect.xMin * canvasScale);
			scaledTextureRect.yMin = Mathf.FloorToInt(textureRect.yMin * canvasScale);
			scaledTextureRect.xMax = Mathf.CeilToInt(textureRect.xMax * canvasScale);
			scaledTextureRect.yMax = Mathf.CeilToInt(textureRect.yMax * canvasScale);
		
			IsTextureTooLarge = false;
			if (scaledTextureRect.width > Filters.GetMaxiumumTextureSize() || scaledTextureRect.height > Filters.GetMaxiumumTextureSize())
			{
				IsTextureTooLarge = true;

				// Modify the texture rectangle so it fits within the maximum supported texture size.
				// NOTE: This will lead to lower image quality.
				{
					float aspect = (float)scaledTextureRect.width / (float)scaledTextureRect.height;
					float origWidth = scaledTextureRect.width;
					float origHeight = scaledTextureRect.height;
					scaledTextureRect.width = Mathf.Min(scaledTextureRect.width, Filters.GetMaxiumumTextureSize());
					scaledTextureRect.height = Mathf.Min(scaledTextureRect.height, Filters.GetMaxiumumTextureSize());

					if (aspect > 1f)
					{
						scaledTextureRect.height = Mathf.CeilToInt(scaledTextureRect.width / aspect);
						Debug.Assert(scaledTextureRect.height <= Filters.GetMaxiumumTextureSize());
						scaledTextureRect.height = Mathf.Min(scaledTextureRect.height, Filters.GetMaxiumumTextureSize());
					}
					else
					{
						scaledTextureRect.width = Mathf.CeilToInt(scaledTextureRect.height * aspect);
						Debug.Assert(scaledTextureRect.width <= Filters.GetMaxiumumTextureSize());
						scaledTextureRect.width = Mathf.Min(scaledTextureRect.width, Filters.GetMaxiumumTextureSize());
					}
				}
			}

			RenderTextureFormat targetFormat = _defaultTextureFormat;
			if (forceHdr)
			{
				targetFormat = _defaultHdrTextureFormat;
			}

			if (_composite != null && (_composite.width != scaledTextureRect.width || _composite.height != scaledTextureRect.height || _composite.format != targetFormat))
			{
				RenderTextureHelper.ReleaseTemporary(ref _composite);
			}

			if (scaledTextureRect.width <= 0 || scaledTextureRect.height <= 0)
			{
				return false;
			}

			if (_composite == null)
			{
				_composite = RenderTexture.GetTemporary(scaledTextureRect.width, scaledTextureRect.height, 0, targetFormat, RenderTextureReadWrite.Linear);
				_composite.wrapMode = TextureWrapMode.Clamp;
				#if UNITY_EDITOR
				_composite.name = "RT-Composite" + Time.frameCount;
				#endif
			}

			// Calculate our projection matrix, but cropped to the render area
			{
				if (camera == null)
				{
					// Note: in Overlay canvas mode, the z clip planes seem to be hardcoded to range [-1000f * Canvas.scaleFactor, 1000f * Canvas.scaleFactor]
					_projectionMatrix = Matrix4x4.Ortho(textureRect.xMin, textureRect.xMax, textureRect.yMin, textureRect.yMax, -1000f * canvasScale, 1000f * canvasScale);
				}
				else
				{
					Rect rect = Rect.zero;
					rect.x = textureRect.x;
					rect.y = textureRect.y;
					rect.xMax = textureRect.xMax;
					rect.yMax = textureRect.yMax;
					rect.x -= camera.pixelRect.x;
					rect.y -= camera.pixelRect.y;
					rect.x /= camera.pixelWidth;
					rect.y /= camera.pixelHeight;
					rect.width /= camera.pixelWidth;
					rect.height /= camera.pixelHeight;

					float inverseWidth = 1f / rect.width;
					float inverseHeight = 1f / rect.height;
					Matrix4x4 matrix1 = Matrix4x4.Translate(new Vector3(-rect.x * 2f * inverseWidth, -rect.y * 2f * inverseHeight, 0f));
					Matrix4x4 matrix2 = Matrix4x4.Translate(new Vector3(inverseWidth - 1f, inverseHeight - 1f, 0f)) * Matrix4x4.Scale(new Vector3(inverseWidth, inverseHeight, 1f));

					_projectionMatrix = matrix1 * matrix2 * camera.projectionMatrix;

					//_projectionMatrix = Matrix4x4.Ortho(textureRect.xMin, textureRect.xMax, textureRect.yMin, textureRect.yMax, -1000f, 1000f);
				}
			}

			_prevRT = RenderTexture.active;

			_viewMatrix = Matrix4x4.identity;
			if (camera)
			{
				_viewMatrix = camera.worldToCameraMatrix;
			}
			else
			{
				_viewMatrix = Matrix4x4.TRS(new Vector3(0f, 0f, -10f), Quaternion.identity, Vector3.one);
			}

			// NOTE: Camera.current can be non-null for example when draging the Camera.Size property with the Scene view visible
			// because it is rendering to the scene view.  In this case flickering can occur unless we use the below logic.
			if (Camera.current != null)
			{
				_prevCamera = Camera.current;
				_prevCamera.worldToCameraMatrix = _viewMatrix;
			}
			
			RenderTexture.active = _composite;
			GL.Clear(false, true, Color.clear);

			return true;
		}

		public void End()
		{
			_composite.IncrementUpdateCount();
			
			if (_prevCamera)
			{
				_prevCamera.ResetWorldToCameraMatrix();
				_prevCamera = null;
			}
			RenderTexture.active = _prevRT;
		}

		private void SaveMeshVertexColorsAndConvertToLinear(Mesh mesh)
		{
			int vertexCount = mesh.vertexCount;
			if (_vertexColors != null && _vertexColors.Count != vertexCount)
			{
				_vertexColors = null;
				_vertexColorsLinear = null;
			}
			if (_vertexColors == null)
			{
				_vertexColors = new List<Color>(vertexCount);
				_vertexColorsLinear = new List<Color>(vertexCount);
			}
			mesh.GetColors(_vertexColors);
			mesh.GetColors(_vertexColorsLinear);

			// In some rare cases there can be no colors
			if (_vertexColorsLinear.Count > 0)
			{
				Debug.Assert(_vertexColorsLinear.Count == vertexCount);
				for (int i = 0; i < vertexCount; i++)
				{
					_vertexColorsLinear[i] = _vertexColorsLinear[i].linear;
				}
				mesh.SetColors(_vertexColorsLinear);
			}
		}

		private void RestoreMeshVertexColors(Mesh mesh)
		{
			// In some rare cases there can be no colors
			if (_vertexColors.Count > 0)
			{
				mesh.SetColors(_vertexColors);
			}
		}

		public void AddMesh(Transform xform, Mesh mesh, Material material, bool materialOutputPremultipliedAlpha, bool convertVertexColorsFromGammaToLinear)
		{
			if (!mesh || !material) return;

			// When in linear color-space, Unity's UI rendering system will convert mesh vertex colors from their native gamma to linear space.
			// However when we render the meshes this automatic conversion doesn't happen, so we must do it manually.  For most Graphic objects
			// this can be done by modifying our copy of the mesh, but for TextMeshPro we can't modify those native meshes, so we have to make
			// a copy, modify it, render it, and then restore the colors.
			if (convertVertexColorsFromGammaToLinear)
			{
				Debug.Assert(QualitySettings.activeColorSpace == ColorSpace.Linear);
				SaveMeshVertexColorsAndConvertToLinear(mesh);
			}

			if (materialOutputPremultipliedAlpha)
			{
				RenderMeshDirectly(xform, mesh, material);
			}
			else
			{
				int pass = ShaderPass.AlphaBlendedToPremultipliedAlpha;
				RenderMeshWithAdjustment(xform, mesh, material, pass);
			}

			if (convertVertexColorsFromGammaToLinear)
			{
				RestoreMeshVertexColors(mesh);
			}
		}

		private void RenderMeshDirectly(Transform xform, Mesh mesh, Material material)
		{
			RenderTexture.active = _composite;
			RenderMeshToActiveTarget(xform, mesh, material);
		}

		private void RenderMeshWithAdjustment(Transform xform, Mesh mesh, Material material, int pass)
		{
			// If the material doesn't output premultiplied-alpha then first render normally and then
			// blit to convert to premuliplied-alpha and composite it into the _composite buffer.
			// NOTE: this assumed a standard alpha blend and doesn't support other blend modes.

			var rtRawSource = RenderTexture.GetTemporary(_composite.width, _composite.height, 0, _composite.format, RenderTextureReadWrite.Linear);
			rtRawSource.wrapMode = TextureWrapMode.Clamp;
			#if UNITY_EDITOR
			rtRawSource.name = "RT-RawSource";
			#endif

			RenderTexture.active = rtRawSource;
			GL.Clear(false, true, Color.clear);

			RenderMeshToActiveTarget(xform, mesh, material);

			rtRawSource.IncrementUpdateCount();

			// Blit to fix the alpha channel and composite using pre-multiplied alpha, or another adjustment.
			Graphics.Blit(rtRawSource, _composite, _compositeMaterial, pass);

			RenderTextureHelper.ReleaseTemporary(ref rtRawSource);
		}

		private void RenderMeshToActiveTarget(Transform xform, Mesh mesh, Material material)
		{
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.modelview = _viewMatrix;
			GL.LoadProjectionMatrix(GL.GetGPUProjectionMatrix(_projectionMatrix, false));
			for (int i = 0; i < material.passCount; i++)
			{
				if (material.SetPass(i))
				{
					if (xform)
					{
						Graphics.DrawMeshNow(mesh, xform.localToWorldMatrix);
					}
					else
					{
						Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
					}
				}
			}
			GL.PopMatrix();
		}

		public RenderTexture GetTexture()
		{
			return _composite;
		}
	}
}