//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

#if UIFX_TMPRO

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityInternal = UnityEngine.Internal;
using TMPro;

namespace ChocDino.UIFX
{
	/// <summary>
	/// Allows multiple image filters derived from FilterBase to be applied to TextMeshPro
	/// Tested with TextMeshPro v2.1.6 (Unity 2019), v3.0.8 (Unity 2020), 3.2.0-pre.9 (Unity 2022)
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(TextMeshProUGUI)), DisallowMultipleComponent]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Filter Stack (TextMeshPro)", 200)]
	public class FilterStackTextMeshPro : UIBehaviour, IClippable
	{
		private Graphic _graphic;
		private Graphic GraphicComponent { get { if (_graphic == null) _graphic = GetComponent<Graphic>(); return _graphic; } }

		private RectTransform _rectTransform;
		private RectTransform RectTransformComponent { get { if (!_rectTransform) { _rectTransform = GetComponent<RectTransform>(); } return _rectTransform; } }

		private TextMeshProUGUI _textMeshPro;

		private List<TMP_SubMeshUI> _subMeshes = new List<TMP_SubMeshUI>(8);
		private static List<TMP_SubMeshUI> _subMeshTemp = new List<TMP_SubMeshUI>(8);

		#if UIFX_SUPPORT_TEXT_ANIMATOR
		private bool _hasTextAnimator;
		#endif
		
		private static readonly Color32 _color32White = Color.white;

		protected static class ShaderProp
		{
			public readonly static int SourceTex = Shader.PropertyToID("_SourceTex");
			public readonly static int ResultTex = Shader.PropertyToID("_ResultTex");
		}

		private ScreenRectFromMeshes _screenRect = new ScreenRectFromMeshes();
		private Compositor _composite = new Compositor();
		private RenderTexture _rt;
		private RenderTexture _rt2;
		private Material _displayMaterial;
		private Material _overrideDisplayMaterial;
		private float _lastFilterAlpha;
		private VertexHelper _quadVertices;
		private Mesh _quadMesh;
		private List<Vector3> _vertexPositions = new List<Vector3>(64);
		private List<Color32> _vertexColors = new List<Color32>(64);
		private List<int> _triangleIndices = new List<int>(64);
		private int _lastRenderFrame = -1;
		private bool _needsRendering = true;
		private bool _issuedLargeTextureSizeWarning;

		[SerializeField] bool _applyToSprites = true;
		[SerializeField] bool _updateOnTransform = false;
		[SerializeField] bool _relativeToTransformScale = false;
		[SerializeField] FilterRenderSpace _renderSpace = FilterRenderSpace.Canvas;
		[SerializeField, Delayed] float _relativeFontSize = 0f;
		[SerializeField] FilterBase[] _filters = new FilterBase[0];

		public bool ApplyToSprites { get { return _applyToSprites; } set { ChangeProperty(ref _applyToSprites, value); } }
		public bool UpdateOnTransform { get { return _updateOnTransform; } set { ChangeProperty(ref _updateOnTransform, value); } }
		public bool RelativeToTransformScale { get { return _relativeToTransformScale; } set { ChangeProperty(ref _relativeToTransformScale, value); } }
		public float RelativeFontSize { get { return _relativeFontSize; } set { ChangeProperty(ref _relativeFontSize, value); } }
		public FilterRenderSpace RenderSpace { get { return _renderSpace; } set { ChangeProperty(ref _renderSpace, value); } }
		public List<FilterBase> Filters { get { return new List<FilterBase>(_filters); } set { ChangePropertyArray(ref _filters, value.ToArray()); } }

		private bool CanApplyFilter()
		{
			if (!this.isActiveAndEnabled) return false;
			if (!GraphicComponent.enabled) return false;
			bool result = false;
			if (_filters != null)
			{
				// See if any filters are rendering
				foreach (var filter in _filters)
				{
					if (filter && filter.IsFiltered())
					{
						result = true;
						break;
					}
				}
			}
			return result;
		}

		[UnityInternal.ExcludeFromDocs]
		protected override void Awake()
		{
			_textMeshPro = GetComponent<TextMeshProUGUI>();
			base.Awake();
		}

		/// <summary>
		/// NOTE: OnDidApplyAnimationProperties() is called when the Animator is used to keyframe properties
		/// </summary>
		protected override void OnDidApplyAnimationProperties()
		{
			GraphicComponent.SetAllDirty();
			base.OnDidApplyAnimationProperties();
		}

		protected override void OnTransformParentChanged()
		{
			UpdateClipParent();
			base.OnTransformParentChanged();
		}

		#if UNITY_EDITOR
		protected override void Reset()
		{
			GraphicComponent.SetAllDirty();
			base.Reset();
		}
		protected override void OnValidate()
		{
			GraphicComponent.SetAllDirty();
		}
		#endif

		protected void ChangeProperty<T>(ref T backing, T value) where T : struct
		{
			if (ObjectHelper.ChangeProperty(ref backing, value))
			{
				backing = value;
				GraphicComponent.SetAllDirty();
			}
		}

		protected bool ChangePropertyArray<T>(ref T backing, T value) where T : System.Collections.ICollection
		{
			bool result = false;
			if (backing.Count != value.Count)
			{
				result = true;
			}
			else
			{
				var backingEnum = backing.GetEnumerator();
				var valueEnum = value.GetEnumerator();
				int index = 0;
				while (backingEnum.MoveNext() && valueEnum.MoveNext())
				{
					if (!backingEnum.Current.Equals(valueEnum.Current))
					{
						result = true;
						break;
					}
					index++;
				}
			}
			if (result)
			{
				backing = value;
				GraphicComponent.SetAllDirty();
			}
			return result;
		}

		[UnityInternal.ExcludeFromDocs]
		protected override void OnEnable()
		{
			_needsRendering = true;
			_isResolveTextureDirty = true;
			var shader = Shader.Find(FilterBase.DefaultBlendShaderPath);
			if (shader)
			{
				_displayMaterial = new Material(shader);
			}
			_quadVertices = new VertexHelper();
			Debug.Assert(_quadMesh == null);
			_quadMesh = new Mesh();

			#if UIFX_SUPPORT_TEXT_ANIMATOR
			_hasTextAnimator = GetComponent("Febucci.UI.TextAnimator_TMP") != null || GetComponent("Febucci.UI.TextAnimator") != null;
			#endif

			GraphicComponent.RegisterDirtyMaterialCallback(OnGraphicMaterialDirtied);
			GraphicComponent.RegisterDirtyVerticesCallback(OnGraphicVerticesDirtied);

			TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextGeomeryRebuilt);
			//CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
			//_textMeshPro.OnPreRenderText += OnTextVerticesChanged;
			Canvas.willRenderCanvases += WillRenderCanvases;

			UpdateClipParent();

			// This forces TMP to re-render
			_textMeshPro.SetAllDirty();

			base.OnEnable();
		}

		[UnityInternal.ExcludeFromDocs]
		protected override void OnDisable()
		{
			ObjectHelper.Destroy(ref _quadMesh);
			RenderTextureHelper.ReleaseTemporary(ref _rt2);
			RenderTextureHelper.ReleaseTemporary(ref _rt);
			RenderTextureHelper.ReleaseTemporary(ref _resolveTexture);
			ObjectHelper.Destroy(ref _resolveMaterial);
			ObjectHelper.Destroy(ref _readableTexture);
			_composite.FreeResources();

			GraphicComponent.UnregisterDirtyMaterialCallback(OnGraphicMaterialDirtied);
			GraphicComponent.UnregisterDirtyVerticesCallback(OnGraphicVerticesDirtied);

			Canvas.willRenderCanvases -= WillRenderCanvases;
			//_textMeshPro.OnPreRenderText -= OnTextVerticesChanged;
			//CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
			TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextGeomeryRebuilt);

			ObjectHelper.Dispose(ref _quadVertices);
			ObjectHelper.Destroy(ref _displayMaterial);
			_overrideDisplayMaterial = null;

			UpdateClipParent();

			// This forces TMP to re-render
			_textMeshPro.SetAllDirty();

			base.OnDisable();
		}

		protected void OnGraphicMaterialDirtied()
		{
			_needsRendering = true;
			_isResolveTextureDirty = true;
		}
		protected void OnGraphicVerticesDirtied()
		{
			_needsRendering = true;
			_isResolveTextureDirty = true;
		}

		//void OnTextVerticesChanged(TMP_TextInfo textInfo) {}

		void OnTextGeomeryRebuilt(Object obj)
		{
			if (obj == _textMeshPro)
			{
				_needsRendering = true;
				_isResolveTextureDirty = true;

				// For some reason we need to set things dirty here so that future changes to the filters will cause rerendering
				// Note that this is the reason why we can't currently support some of the TextMeshPro animation examples (eg 23 - Animating Vertex Attributes),
				// because that runs on a coroutine, which updates the TMP mesh, but because we're constantly dirtying the Graphic, TMP decides to REBUILD the original mesh
				// (without the animation then applied to the vertices). This happens during Canvas.PreWillRenderCanvases and then our WillRenderCanvases gets the unanimated mesh.
				// And the reason we need to call dirty here is because (I believe) that without it our filters will not get the signals that vertices/materials have changed...

				// NOTE: That calling SetAllDirty() prevents TMP from updating it's culling (from RectMask2D) as it defers it if Layout is dirty. This is not a problem through because
				// we're handling the culling ourselves (IClippable implementation).  Another approach explored was to call SetVerticesDirty()and SetMaterialDirty(), however this causes
				// an unsolved bug where disabling the last Filter in the FilterStack causes a colored quad to render.
				_isCullingDirty = true;
				GraphicComponent.SetAllDirty();
			}
		}

		void GatherSubMeshes()
		{
			GetComponentsInChildren<TMP_SubMeshUI>(false, _subMeshTemp);

			// NOTE: There have been cases where materialCount != meshInfo.Length, so we always take the minimum.
			int materialCount = _textMeshPro.textInfo.materialCount;
			if (_textMeshPro.textInfo.meshInfo != null)
			{
				materialCount = Mathf.Min(materialCount, _textMeshPro.textInfo.meshInfo.Length);
			}

			// SubMesh GameObject ordering doesn't always match the meshInfo order, so we have to reorder _subMeshTemp into _subMeshes
			_subMeshes.Clear();
			_subMeshes.Add(null);
			for (int i = 1; i < materialCount; i++)
			{
				var meshInfo = _textMeshPro.textInfo.meshInfo[i];
				for (int j = 0; j < _subMeshTemp.Count; j++)
				{
					if (_subMeshTemp[j].mesh == meshInfo.mesh)
					{
						_subMeshes.Add(_subMeshTemp[j]);
						break;
					}
				}
			}
		}

		void GatherRenderableMeshes()
		{
			int materialCount = _textMeshPro.textInfo.materialCount;
			if (materialCount > 1)
			{
				GatherSubMeshes();
			}
			else
			{
				_subMeshes.Clear();
				_subMeshTemp.Clear();
			}
		}

		void CalculateScreenRect()
		{
			_screenRect.Start(_renderSpace == FilterRenderSpace.Canvas ? null : GetRenderCamera(), _renderSpace);

			// Grow rectangle from geometry
			if (_textMeshPro)
			{
				// Determine whether any of our filters are limited to the RectTransform area
				FilterSourceArea sourceArea = FilterSourceArea.Geometry;
				if (_filters != null)
				{
					foreach (var filter in _filters)
					{
						if (filter && filter.IsFiltered())
						{
							if (filter._sourceArea == FilterSourceArea.RectTransform)
							{
								sourceArea = FilterSourceArea.RectTransform;
								break;
							}
						}
					}
				}

				// NOTE: There have been cases where materialCount != meshInfo.Length, so we always take the minimum.
				int materialCount = _textMeshPro.textInfo.materialCount;
				if (_textMeshPro.textInfo.meshInfo != null)
				{
					materialCount = Mathf.Min(materialCount, _textMeshPro.textInfo.meshInfo.Length);
				}

				if (sourceArea == FilterSourceArea.Geometry)
				{
					for (int i = 0; i < materialCount; i++)
					{
						var meshInfo = _textMeshPro.textInfo.meshInfo[i];
						if (meshInfo.mesh != null && meshInfo.vertexCount > 0)
						{
							// NOTE: i can be < _subMeshes.Count in some cases where GatherSubMeshes() doesn't find all the submeshes.
							if (i > 0 && i < _subMeshes.Count)
							{
								// Check if we need to skip the sprite
								var subMesh = _subMeshes[i];
								if (!_applyToSprites)
								{
									bool isSprite = (subMesh.spriteAsset != null);
									if (isSprite) { continue; }
								}
							}
							meshInfo.mesh.GetVertices(_vertexPositions);
							meshInfo.mesh.GetColors(_vertexColors);
							meshInfo.mesh.GetTriangles(_triangleIndices, 0);
							_screenRect.AddTriangleBounds(_renderSpace == FilterRenderSpace.Canvas ? null : this.transform, _vertexPositions, _triangleIndices, _vertexColors);
						}
					}
				}
				else
				{
					_screenRect.AddRect(_renderSpace == FilterRenderSpace.Canvas ? null : this.transform, RectTransformComponent.rect);
				}
			}

			_screenRect.End();

			RectAdjustOptions rectAdjustOptions = new RectAdjustOptions();
			// NOTE: Not sure why, but with FitlerStackTextMeshPro we need to round to multiple of 1 other wise when the expanded rectangle flucutates between
			// even and odd size, there are visual shifting artifacts.
			rectAdjustOptions.roundToNextMultiple = 2;

			// Grow rectangle for filters (if any)
			if (_filters != null)
			{
				foreach (var filter in _filters)
				{
					if (filter && filter.IsFiltered())
					{
						filter.RenderSpace = _renderSpace;
						// Apply relative scale based on font size and transform scale
						{
							float userScale = 1f;
							if (_relativeToTransformScale)
							{
								float canvasLocalScale = 1f;
								if (_textMeshPro.canvas)
								{
									var canvas = _textMeshPro.canvas;
									canvasLocalScale = canvas.transform.localScale.x;
									if (!canvas.isRootCanvas && canvas.rootCanvas)
									{
										canvasLocalScale = canvas.rootCanvas.transform.localScale.x;
									}
								}
								userScale *= filter.transform.lossyScale.x / canvasLocalScale;
							}
							if (_relativeFontSize > 0f)
							{
								userScale *= (_textMeshPro.fontSize / _relativeFontSize);
							}
							filter.UserScale = userScale;
						}

						filter.AdjustRect(_screenRect);

						rectAdjustOptions.padding = Mathf.Max(rectAdjustOptions.padding, filter.RectAdjustOptions.padding);
						rectAdjustOptions.roundToNextMultiple = Mathf.Max(rectAdjustOptions.roundToNextMultiple, filter.RectAdjustOptions.roundToNextMultiple);
					}
				}
			}

			_screenRect.OptimiseRects(rectAdjustOptions);

			if (_filters != null)
			{
				foreach (var filter in _filters)
				{
					if (filter && filter.IsFiltered())
					{
						filter.SetFinalRect(_screenRect);
					}
				}
			}
		}

		private void SetupMaterialTMPro(Material material, Camera camera, RectInt textureRect)
		{
			float sw = textureRect.width;
			float sh = textureRect.height;

			float canvasScale = 1f;
			float canvasLocalScale = 1f;
			if (_textMeshPro.canvas)
			{
				var canvas = _textMeshPro.canvas;
				canvasScale = canvas.scaleFactor;
				canvasLocalScale = canvas.transform.localScale.x;
				if (!canvas.isRootCanvas && canvas.rootCanvas)
				{
					canvasLocalScale = canvas.rootCanvas.transform.localScale.x;
				}
			}

			if (camera == null)
			{
				sw *= canvasScale;
				sh *= canvasScale;
			}
			else if (_renderSpace == FilterRenderSpace.Canvas)
			{
				sw *= canvasScale / canvasLocalScale;
				sh *= canvasScale / canvasLocalScale;
			}

			Shader.SetGlobalVector(UnityShaderProp.ScreenParams, new Vector4(sw, sh, 1f + (1f / sw), 1f + (1f / sh)));
		}

		private void HandleVertexColors(Mesh mesh, Material material, bool isSprite)
		{
			// We only have to potentially adjust vertex color processing in Linear color-space
			if (QualitySettings.activeColorSpace == ColorSpace.Linear)
			{
				if (isSprite && !_textMeshPro.tintAllSprites)
				{
					// If the sprite isn't tinted then it'll have white vertex colors which don't need adjusting
					return;
				}

				// If TMPro supports shader conversion from gamma to linear vertex color (for Canvas.vertexColorAlwaysGammaSpace support) then
				// we just need to tell the shader to do the conversion to linear.
				#if UNITY_2022_3_OR_NEWER && UIFX_TMPRO_SHADER_GAMMA
				{
					material.SetInt(UnityShaderProp.UIVertexColorAlwaysGammaSpace, 1);
				}
				#endif
			}
		}

		// Certain color values are agnostic of gamma/linear color-space conversion, such as Color.white, Color.black, Color.red etc..
		static bool IsVertexTintColorSpaceConversionAgnostic(TMP_Text textMeshPro)
		{
			Color c0 = textMeshPro.color;
			bool isAgnostic = ColorUtils.IsColorGammaLinearConversionAgnostic(c0);
			if (isAgnostic)
			{
				if (textMeshPro.enableVertexGradient)
				{
					if (textMeshPro.colorGradientPreset != null)
					{
						var g = textMeshPro.colorGradientPreset;
						Color c1 = g.topLeft * c0;
						Color c2 = g.topRight * c0;
						Color c3 = g.bottomLeft * c0;
						Color c4 = g.bottomRight * c0;
						if (!ColorUtils.IsColorGammaLinearConversionAgnostic(c1) ||
							!ColorUtils.IsColorGammaLinearConversionAgnostic(c2) ||
							!ColorUtils.IsColorGammaLinearConversionAgnostic(c3) ||
							!ColorUtils.IsColorGammaLinearConversionAgnostic(c4))
						{
							isAgnostic = false;
						}
					}
					else
					{
						var g = textMeshPro.colorGradient;
						Color c1 = g.topLeft * c0;
						Color c2 = g.topRight * c0;
						Color c3 = g.bottomLeft * c0;
						Color c4 = g.bottomRight * c0;
						if (!ColorUtils.IsColorGammaLinearConversionAgnostic(c1) ||
							!ColorUtils.IsColorGammaLinearConversionAgnostic(c2) ||
							!ColorUtils.IsColorGammaLinearConversionAgnostic(c3) ||
							!ColorUtils.IsColorGammaLinearConversionAgnostic(c4))
						{
							isAgnostic = false;
						}
					}
				}
			}
			return isAgnostic;
		}

		private Shader _lastTextMaterialShader;
		private bool _lastMaterialShaderIsMobile;

		bool IsMaterialHdr(TextMeshProUGUI textMeshPro, Material material)
		{
			// All shaders have _FaceColor
			if (material.GetColor(ShaderUtilities.ID_FaceColor).maxColorComponent > 1f)
			{
				return true;
			}

			// Almost all shaders have _OutlineColor (not Bitmap), but some (mobile?) shaders require additional "OUTLINE_ON" keyword
			if (material.HasProperty(ShaderUtilities.ID_OutlineColor) &&  material.GetColor(ShaderUtilities.ID_OutlineColor).maxColorComponent > 1f)
			{
				float outlineWidth = 0f;
				bool isMainMaterial = material == textMeshPro.fontSharedMaterial;
				if (isMainMaterial)
				{
					outlineWidth = textMeshPro.outlineWidth;
				}
				else
				{
					outlineWidth = material.GetFloat(ShaderUtilities.ID_OutlineWidth);
				}

				if (outlineWidth > 0f)
				{
					if (material.IsKeywordEnabled(ShaderUtilities.Keyword_Outline))
					{
						return true;
					}
					else 
					{
						// NOTE: getting the shader name generates garbage, so we cache the material to minimise this.
						// NOTE: multiple text submeshs will break this caching if they use different shaders.
						if (_lastTextMaterialShader != material.shader)
						{
							_lastTextMaterialShader = material.shader;
							_lastMaterialShaderIsMobile = material.shader.name.StartsWith("TextMeshPro/Mobile");
						}
						if (!_lastMaterialShaderIsMobile)
						{
							return true;
						}
					}
				}
			}

			// Not all shaders have _UnderlayColor, but those that do require additional "UNDERLAY_ON" or "UNDERLAY_INNER" keyword
			if (material.HasProperty(ShaderUtilities.ID_UnderlayColor) && material.GetColor(ShaderUtilities.ID_UnderlayColor).maxColorComponent > 1f && (material.IsKeywordEnabled(ShaderUtilities.Keyword_Underlay) || material.IsKeywordEnabled("UNDERLAY_INNER")))
			{
				return true;
			}

			// Not all shaders have _GlowColor, but those that do require additional "GLOW_ON" keyword
			if (material.HasProperty(ShaderUtilities.ID_GlowColor) && material.GetColor(ShaderUtilities.ID_GlowColor).maxColorComponent > 1f && material.IsKeywordEnabled(ShaderUtilities.Keyword_Glow))
			{
				return true;
			}

			return false;
		}

		private bool _isResolveTextureDirty = true;
		internal const string ResolveShaderPath = "Hidden/ChocDino/UIFX/Resolve";
		private RenderTexture _resolveTexture;
		private Material _resolveMaterial;
		private Texture2D _readableTexture;

		/// <summary>
		/// Resolves to a final sRGB straight-alpha texture suitable for display or saving to image file.
		/// </summary>
		public RenderTexture ResolveToTexture()
		{
			if (_isResolveTextureDirty)
			{
				RenderTexture prevRT = RenderTexture.active;
				RenderToTexture();//if (RenderFilter(true))
				{
					if (DisplayMaterial && _composite.GetTexture())
					{
						int outputWidth = _composite.GetTexture().width;
						int outputHeight = _composite.GetTexture().height;

						if (!_resolveMaterial)
						{
							_resolveMaterial = new Material(Shader.Find(ResolveShaderPath));
							_resolveMaterial.name = "Resolve";
						}
						if (_resolveTexture && (_resolveTexture.width != outputWidth || _resolveTexture.height != outputHeight))
						{
							RenderTextureHelper.ReleaseTemporary(ref _resolveTexture);
						}
						if (!_resolveTexture)
						{
							_resolveTexture = RenderTexture.GetTemporary(outputWidth, outputHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
							_resolveTexture.name = "Resolve";
						}

						RenderTexture displayTexture = RenderTexture.GetTemporary(outputWidth, outputHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

						// Render as if displaying - this will output premultiplied alpha and possibly in linear space
						RenderTexture.active = displayTexture;
						GL.Clear(false, true, Color.clear);
						Graphics.Blit(DisplayMaterial.mainTexture, displayTexture, DisplayMaterial);

						// Resolve by removing premultiplied alpha and converting to sRGB
						Graphics.Blit(displayTexture, _resolveTexture, _resolveMaterial);

						RenderTextureHelper.ReleaseTemporary(ref displayTexture);

						_isResolveTextureDirty = false;
					}
				}
				//else 
				{
					//RenderTextureHelper.ReleaseTemporary(ref _resolveTexture);
				}
				RenderTexture.active = prevRT;
			}
			return _resolveTexture;
		}

		/// <summary>
		/// Resolve the filter output to a sRGB texture and write it to a PNG file
		/// </summary>
		public bool SaveToPNG(string path)
		{
			if (CanApplyFilter())
			{
				RenderTexture texture = ResolveToTexture();
				if (texture)
				{
					// Create a new readable texture if necessary
					if (_readableTexture && (_readableTexture.width != texture.width || _readableTexture.height != texture.height))
					{
						ObjectHelper.Destroy(ref _readableTexture);
					}
					if (!_readableTexture)
					{
						#if UNITY_2022_1_OR_NEWER
						_readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, mipChain: false, linear: false, createUninitialized: true);
						#else
						_readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, mipChain: false);
						#endif
					}

					return TextureUtils.WriteToPNG(texture, _readableTexture, path);
				}
			}
			return false;
		}

		void RenderToTexture()
		{
			_lastFilterAlpha = 1f;
			_overrideDisplayMaterial = null;

			// Composite the TMP to a RenderTexture
			Camera canvasCamera = GetRenderCamera();

			float canvasScale = 1f;
			if (_textMeshPro.canvas)
			{
				canvasScale = _textMeshPro.canvas.scaleFactor;
			}

			// NOTE: There have been cases where materialCount != meshInfo.Length, so we always take the minimum.
			int materialCount = _textMeshPro.textInfo.materialCount;
			if (_textMeshPro.textInfo.meshInfo != null)
			{
				materialCount = Mathf.Min(materialCount, _textMeshPro.textInfo.meshInfo.Length);
			}

			// Set forceHdr to true when detecting TMP material properties (_FaceColor, _OutlineColor, _UnderlayColor etc) are HDR.
			// Best attempt at detecting whether TMP requires hdr rendering.
			bool forceHdr = Compositor.DefaultTextureFormat == Compositor.DefaultHdrTextureFormat;
			if (!forceHdr && _textMeshPro.fontSharedMaterial != null)
			{
				forceHdr = IsMaterialHdr(_textMeshPro, _textMeshPro.fontSharedMaterial);
				if (!forceHdr)
				{
					// Check submeshes that are text (not sprites)
					for (int i = 1; i < materialCount; i++)
					{
						TMP_MeshInfo meshInfo = _textMeshPro.textInfo.meshInfo[i];
						if (meshInfo.vertexCount > 0 && i < _subMeshes.Count)
						{
							var subMesh = _subMeshes[i];
							bool isSprite = (subMesh.spriteAsset != null);

							if (!isSprite)
							{
								if (IsMaterialHdr(_textMeshPro, meshInfo.material))
								{
									forceHdr = true;
									break;
								}
							}
						}
					}
				}
			}

			if (_composite.Start(_renderSpace == FilterRenderSpace.Canvas ? null : canvasCamera, _screenRect.GetTextureRect(), forceHdr, _renderSpace == FilterRenderSpace.Canvas ? canvasScale : 1f))
			{
				if (Debug.isDebugBuild)
				{
					if (_composite.IsTextureTooLarge && !_issuedLargeTextureSizeWarning)
					{
						Debug.LogWarning("[UIFX] Filter " + this.name + "/" + this.GetType().Name + " requested texture that is larger than the supported size of " + ChocDino.UIFX.Filters.GetMaxiumumTextureSize() + ", rescaling texture to supported size, this can lead to lower texture quality. Consider invstigating why such a large texture is required.", this);
						_issuedLargeTextureSizeWarning = true;
					}
				}

				bool requiresTextVertexGammaToLinearConversion = false;
				bool requiresSpriteVertexGammaToLinearConversion = false;
				if (QualitySettings.activeColorSpace == ColorSpace.Linear)
				{
					#if UNITY_2022_3_OR_NEWER && UIFX_TMPRO_SHADER_GAMMA
					{
						// We'll handle conversion of vertex colors from gamma to linear in the shader.
					}
					#else
					if (!IsVertexTintColorSpaceConversionAgnostic(_textMeshPro))
					{
						requiresTextVertexGammaToLinearConversion = true;
						if (_textMeshPro.tintAllSprites)
						{
							requiresSpriteVertexGammaToLinearConversion = requiresTextVertexGammaToLinearConversion;
						}
					}
					#endif
				}

				for (int i = 0; i < materialCount; i++)
				{
					TMP_MeshInfo meshInfo = _textMeshPro.textInfo.meshInfo[i];
					if (meshInfo.vertexCount > 0)
					{
						if (i == 0)
						{
							// First mesh is always text
							HandleVertexColors(meshInfo.mesh, _textMeshPro.fontSharedMaterial, false);
							SetupMaterialTMPro(_textMeshPro.fontSharedMaterial, canvasCamera, _screenRect.GetTextureRect());
							_composite.AddMesh(_renderSpace == FilterRenderSpace.Canvas ? null : this.transform, meshInfo.mesh, _textMeshPro.fontSharedMaterial, true, requiresTextVertexGammaToLinearConversion);
						}
						else
						{
							// SubMesh can be text or sprites
							var subMesh = _subMeshes[i];
							bool isSprite = (subMesh.spriteAsset != null);

							// Check if we need to skip the sprite
							if (isSprite && !_applyToSprites) { continue; }
							
							{
								HandleVertexColors(meshInfo.mesh, meshInfo.material, isSprite);
								if (!isSprite)
								{
									SetupMaterialTMPro(meshInfo.material, canvasCamera, _screenRect.GetTextureRect());
								}
								bool requiresVertexGammaToLinearConversion = isSprite ? requiresSpriteVertexGammaToLinearConversion : requiresTextVertexGammaToLinearConversion;
								_composite.AddMesh(_renderSpace == FilterRenderSpace.Canvas ? null : this.transform, meshInfo.mesh, meshInfo.material, !isSprite, requiresVertexGammaToLinearConversion);

								// Hide the submesh
								subMesh.canvasRenderer.SetMesh(null);
							}
						}
					}
				}
				_composite.End();

				var sourceTexture = _composite.GetTexture();

				// Render the filters (if any)
				if (_filters != null && sourceTexture)
				{
					if (_rt && (_rt.width != sourceTexture.width || _rt.height != sourceTexture.height || _rt.format != sourceTexture.format))
					{
						RenderTextureHelper.ReleaseTemporary(ref _rt);
					}
					if (_rt2 && (_rt2.width != sourceTexture.width || _rt2.height != sourceTexture.height || _rt2.format != sourceTexture.format))
					{
						RenderTextureHelper.ReleaseTemporary(ref _rt2);
					}

					FilterBase lastActiveFilter = null;
					int activeFilterCount = 0;
					foreach (var filter in _filters)
					{
						if (filter && filter.IsFiltered())
						{
							activeFilterCount++;

							// Find the last active filter
							// The last filter will not be fully rendered, instead we'll use it's DisplayMaterial.
							lastActiveFilter = filter;
						}
					}

					if (activeFilterCount > 0)
					{
						if (!_rt && activeFilterCount > 1)
						{
							_rt = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, sourceTexture.format, RenderTextureReadWrite.Linear);
							#if UNITY_EDITOR
							_rt.name = "FilterStack-Output1";
							#endif
						}
						if (!_rt2 && activeFilterCount > 2)
						{
							_rt2 = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, sourceTexture.format, RenderTextureReadWrite.Linear);
							#if UNITY_EDITOR
							_rt2.name = "FilterStack-Output2";
							#endif
						}

						RenderTextureFormat hdrFormat = Compositor.DefaultHdrTextureFormat;
						bool isHdr = sourceTexture.format == hdrFormat;

						RenderTexture destTexture = _rt;
						foreach (var filter in _filters)
						{
							if (filter)
							{
								filter.SetFilterEnabled(false);
								if (filter.IsFiltered())
								{
									filter.SetFilterEnabled(true);
									filter.RenderSpace = _renderSpace;

									bool renderToDestTexture = (filter != lastActiveFilter);

									// Upgrade texture to HDR if required.
									if (renderToDestTexture)
									{
										if (filter.IsOutputHdr())
										{
											isHdr = true;
										}

										Debug.Assert(destTexture != null);
										if ((isHdr && destTexture.format != hdrFormat))
										{
											if (destTexture == _rt)
											{
												RenderTextureHelper.ReleaseTemporary(ref _rt);
												_rt = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, hdrFormat, RenderTextureReadWrite.Linear);
												#if UNITY_EDITOR
												_rt.name = "FilterStack-Output1";
												#endif
												destTexture = _rt;
											}
											else if (destTexture == _rt2)
											{
												RenderTextureHelper.ReleaseTemporary(ref _rt2);
												_rt2 = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, hdrFormat, RenderTextureReadWrite.Linear);
												#if UNITY_EDITOR
												_rt2.name = "FilterStack-Output2";
												#endif
												destTexture = _rt2;
											}
										}
									}

									Debug.Assert(_overrideDisplayMaterial == null);
									if (filter.RenderToTexture(sourceTexture, destTexture, renderToDestTexture))
									{
										if (renderToDestTexture)
										{
											sourceTexture = destTexture;
											destTexture = (sourceTexture == _rt) ? _rt2 : _rt;
										}
									}

									if (!renderToDestTexture)
									{
										_overrideDisplayMaterial = lastActiveFilter.GetDisplayMaterial();
										_lastFilterAlpha = lastActiveFilter.GetAlpha();
										break;
									}
								}
							}
						}
					}
				}

				// Release unused temporary textures
				if (sourceTexture == _rt)
				{
					RenderTextureHelper.ReleaseTemporary(ref _rt2);
				}
				else if (sourceTexture == _rt2)
				{
					RenderTextureHelper.ReleaseTemporary(ref _rt);
				}
				else
				{
					RenderTextureHelper.ReleaseTemporary(ref _rt2);
					RenderTextureHelper.ReleaseTemporary(ref _rt);
				}

				// Update the display material
				if (_overrideDisplayMaterial == null)
				{
					if (sourceTexture)
					{
						if (_displayMaterial)
						{
							_displayMaterial.mainTexture = sourceTexture;
							_displayMaterial.SetTexture(ShaderProp.SourceTex, sourceTexture);
							_displayMaterial.SetTexture(ShaderProp.ResultTex, sourceTexture);
						}
					}
				}
			}
		}

		#if UNITY_EDITOR
		/// <summary>
		/// Warn users when the texture requested was not possible because it is larger than the support system size.
		/// </summary>
		internal bool IsTextureTooLarge()
		{
			return _composite.IsTextureTooLarge;
		}
		#endif
		
		private Matrix4x4 _previousLocalToWorldMatrix;
		private Matrix4x4 _previousCameraMatrix;

		#if UIFX_SUPPORT_TEXT_ANIMATOR
		private TMP_MeshInfo[] _cachedMeshInfo = null;

		private bool HasMeshChanged(TMP_MeshInfo[] meshInfo)
		{
			bool hasChanged = false;
			if (_cachedMeshInfo == null || _cachedMeshInfo.Length != meshInfo.Length)
			{
				// Early out if the array hasn't been initialised yet
				if (meshInfo.Length > 0 && (meshInfo[0].mesh == null || meshInfo[0].vertices == null))
				{
					return false;
				}

				// Allocate initial arrays
				_cachedMeshInfo = new TMP_MeshInfo[meshInfo.Length];
				for (int i = 0; i < _cachedMeshInfo.Length; i++)
				{
					var mesh = meshInfo[i].mesh;
					if (mesh)
					{
						ref var cachedMesh = ref _cachedMeshInfo[i];

						int numVerts = mesh.vertices.Length;
						int numColors = mesh.colors32.Length;

						cachedMesh.vertices = new Vector3[numVerts];
						cachedMesh.colors32 = new Color32[numColors];

						System.Array.Copy(mesh.vertices, cachedMesh.vertices, numVerts);
						System.Array.Copy(mesh.colors32, cachedMesh.colors32, numColors);
					}
				}
				hasChanged = true;
			}

			if (!hasChanged)
			{
				for (int i = 0; i < _cachedMeshInfo.Length; i++)
				{
					bool hasChangedLocal = false;

					var mesh = meshInfo[i].mesh;
					if (mesh)
					{
						ref var cachedMesh = ref _cachedMeshInfo[i];
						Debug.Assert(mesh != null);
						Debug.Assert(cachedMesh.vertices != null);

						// Check if the number of position values has changed
						int numVerts = mesh.vertices.Length;
						if (cachedMesh.vertices.Length != numVerts)
						{
							cachedMesh.vertices = new Vector3[numVerts];
							hasChangedLocal = true;
						}

						// Check if the number of color values has changed
						int numColors = mesh.colors32.Length;
						if (cachedMesh.colors32.Length != numColors)
						{
							cachedMesh.colors32 = new Color32[numColors];
							hasChangedLocal = true;
						}

						Debug.Assert(cachedMesh.vertices.Length == numVerts);
						Debug.Assert(cachedMesh.colors32.Length == numColors);

						// Check if any vertex position values have changed
						if (!hasChangedLocal)
						{
							var vertices = mesh.vertices;
							var cachedVertices = cachedMesh.vertices;
							for (int k = 0; k < numVerts; k++)
							{
								if (vertices[k] != cachedVertices[k])
								{
									hasChangedLocal = true;
									break;
								}
							}
						}

						// Check if any vertex color values have changed
						if (!hasChangedLocal)
						{
							var colors = mesh.colors32;
							var cachedColors = cachedMesh.colors32;
							for (int k = 0; k < numColors; k++)
							{
								if (colors[k].a != cachedColors[k].a ||
									colors[k].r != cachedColors[k].r ||
									colors[k].g != cachedColors[k].g ||
									colors[k].b != cachedColors[k].b)
								{
									hasChangedLocal = true;
									break;
								}
							}
						}

						if (hasChangedLocal)
						{
							System.Array.Copy(mesh.vertices, cachedMesh.vertices, numVerts);
							System.Array.Copy(mesh.colors32, cachedMesh.colors32, numColors);
							hasChanged = true;
						}
					}
				}
			}
			return hasChanged;
		}
		#endif

		protected virtual void Update()
		{
			#if UNITY_EDITOR
			// NOTE: Since we started using [ExecuteAlways] if you have a prefab open in the editor (in Scene view) and then enter Play mode, then
			// exit play mode - Unity will DESTROY all the Object references (materials etc) in the prefab GameObjects, but it will do so
			// WITHOUT calling OnDisable()/OnDestroy() and will not call OnEnable(), so here we detect this case and manually call OnEnable().
			{
				if (this.isActiveAndEnabled && !Application.IsPlaying(this.gameObject))
				{
					if (_quadMesh == null)
					{
						OnDisable();
						OnEnable();
						return;
					}
				}
			}
			#endif
			
			bool forceUpdate = false;

			if (_renderSpace == FilterRenderSpace.Screen || _updateOnTransform)
			{
				{
					// Detect a change to the matrix (this also detects changes to the camera and viewport)
					if (MathUtils.HasMatrixChanged(_previousLocalToWorldMatrix, this.transform.localToWorldMatrix, false))
					{
						_previousLocalToWorldMatrix = this.transform.localToWorldMatrix;
						forceUpdate = true;
					}
					if (_textMeshPro.canvas && _textMeshPro.canvas.renderMode == RenderMode.WorldSpace)
					{
						Camera camera = GetRenderCamera();
						if (camera)
						{
							if (MathUtils.HasMatrixChanged(_previousCameraMatrix, camera.transform.localToWorldMatrix, ignoreTranslation: false))
							{
								_previousCameraMatrix = camera.transform.localToWorldMatrix;
								forceUpdate = true;
							}
						}
					}
				}
			}

		#if UIFX_SUPPORT_TEXT_ANIMATOR
			if (!forceUpdate)
			{
				forceUpdate = HasMeshChanged(_textMeshPro.textInfo.meshInfo);
			}
		#endif

			if (!forceUpdate)
			{
			#if UIFX_FILTERS_FORCE_UPDATE_PLAYMODE
				if (Application.isPlaying)
				{
					forceUpdate = true;
				}
			#endif
			#if UIFX_FILTERS_FORCE_UPDATE_EDITMODE
				if (!Application.isPlaying)
				{
					forceUpdate = true;
				}
			#endif
			}

			if (forceUpdate)
			{
				GraphicComponent.SetVerticesDirty();
				GraphicComponent.SetMaterialDirty();
			}
		}

		void WillRenderCanvases()
		{
			if (!CanApplyFilter())
			{
				return;
			}

			if (_isCullingDirty)
			{
				UpdateCulling();
			}

			if (_textMeshPro.canvasRenderer.cull)
			{
				return;
			}

			if (_lastRenderFrame != Time.frameCount || _needsRendering)
			{
				if (HasActiveFilters())
				{
					// Prevent re-rendering unnecessarily
					_lastRenderFrame = Time.frameCount;

					// Do the rendering
					if (_needsRendering)
					{
						GatherRenderableMeshes();
						CalculateScreenRect();
						RenderToTexture();
						ApplyOutputMeshAndMaterial();
						_needsRendering = false;
					}
					else
					{
						ApplyPreviousOutput();
					}
				}
			}
			else
			{
				ApplyPreviousOutput();
			}
		}

		private Material DisplayMaterial
		{
			get
			{
				if (_overrideDisplayMaterial != null)
				{
					return _overrideDisplayMaterial;
				}
				return _displayMaterial;
			}
		}

		private void ApplyOutputMeshAndMaterial()
		{
			if (_renderSpace == FilterRenderSpace.Canvas)
			{
				if (_lastFilterAlpha == 1f)
				{
					_screenRect.BuildScreenQuad(null, null, _color32White, _quadVertices);
				}
				else
				{
					Color fadeColor = new Color(1f, 1f, 1f, _lastFilterAlpha);
					_screenRect.BuildScreenQuad(null, null, fadeColor, _quadVertices);
				}
				
			}
			else
			{
				Camera renderCamera = GetRenderCamera();
				if (_lastFilterAlpha == 1f)
				{
					_screenRect.BuildScreenQuad(renderCamera, this.transform, _color32White, _quadVertices);
				}
				else
				{
					Color fadeColor = new Color(1f, 1f, 1f, _lastFilterAlpha);
					_screenRect.BuildScreenQuad(renderCamera, this.transform, fadeColor, _quadVertices);
				}
			}

			if (_quadMesh == null)
			{
				_quadMesh = new Mesh();
			}
			_quadVertices.FillMesh(_quadMesh);

			if (DisplayMaterial)
			{
				// Copy the stencil properties to the display material
				if (_textMeshPro.maskable)
				{
					UnityShaderProp.CopyStencilProperties(_textMeshPro.GetModifiedMaterial(_textMeshPro.fontSharedMaterial), DisplayMaterial);
				}
				else
				{
					UnityShaderProp.ResetStencilProperties(DisplayMaterial);
				}

				var cr = _textMeshPro.canvasRenderer;
				cr.SetMesh(_quadMesh);
				cr.materialCount = 1;
				cr.SetMaterial(DisplayMaterial, 0);
			}
		}

		private void ApplyPreviousOutput()
		{
			if (DisplayMaterial)
			{
				var cr = _textMeshPro.canvasRenderer;
				cr.SetMesh(_quadMesh);
				cr.materialCount = 1;
				cr.SetMaterial(DisplayMaterial, 0);
			}
		}

		private Camera GetRenderCamera()
		{
			Camera camera = null;
			Canvas canvas = _textMeshPro.canvas;
			if (canvas)
			{
				camera = canvas.worldCamera;
				if (camera == null && canvas.renderMode == RenderMode.WorldSpace)
				{
					camera = Camera.main;

					#if UNITY_EDITOR
					// NOTE: if we're in the "in-context" prefab editing mode, it uses a World Space canvas with no camera set.
					// when the original scene camera for the canvas was in Overlay mode, it would cause the filter to not render,
					// because the Camera.main camera would be used, and it wouldn't be looking at the UI component.  So instead
					// we detect this case and just use null camera as if it were in overlay mode.  Not sure how robust this is...
					if (EditorHelper.IsInContextPrefabMode())
					{
						camera = null;
					}
					#endif
				}
				else if (camera == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
				{
					camera = null;
				}
			}
			return camera;
		}

		private bool HasActiveFilters()
		{
			bool result = false;
			if (_filters != null)
			{
				for (int i = 0; i < _filters.Length; i++)
				{
					if (_filters[i] != null)
					{
						if (_filters[i].enabled)
						{
							result = true;
							break;
						}
					}
				}
			}
			return result;
		}

		protected void LOG(string message, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
		{
			ChocDino.UIFX.Log.LOG(message, this, LogType.Log, callerName);
		}

		protected void LOGFUNC(string message = null, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
		{
			if (string.IsNullOrEmpty(message))
			{
				ChocDino.UIFX.Log.LOG(callerName, this, LogType.Log, callerName);
			}
			else
			{
				ChocDino.UIFX.Log.LOG(callerName + " " + message, this, LogType.Log, callerName);
			}
		}

		#region IClippable implementation

		public RectTransform rectTransform => RectTransformComponent;

		[System.NonSerialized]
		private RectMask2D _parentMask;

		private Rect _lastClipRect;
		private bool _lastClipRectValid;
		private bool _isCullingDirty;

		public void RecalculateClipping()
		{
			UpdateClipParent();
		}

		private void UpdateClipParent()
		{
			if (_textMeshPro == null)
			{
				// Note: RectMask2D.OnEnable() calls RecalculateClipping() before this component's Awake() is called.
				_textMeshPro = GetComponent<TextMeshProUGUI>();
			}
			Debug.Assert(_textMeshPro != null);
			var newParent = (_textMeshPro.maskable && _textMeshPro.IsActive() && IsActive()) ? MaskUtilities.GetRectMaskForClippable(this) : null;
			if (newParent != _parentMask)
			{
				if (_parentMask != null)
				{
					_parentMask.RemoveClippable(this);
					_parentMask = null;
				}

				_parentMask = newParent;
				if (_parentMask != null)
				{
					_parentMask.AddClippable(this);
				}
				_isCullingDirty = true;
			}
		}

		public void Cull(Rect clipRect, bool validRect)
		{
		}

		public void SetClipRect(Rect value, bool validRect)
		{
			_lastClipRect = value;
			_lastClipRectValid = validRect;
			_isCullingDirty = true;
		}

		public void SetClipSoftness(Vector2 clipSoftness)
		{
			// NOTE: We have the clipping logic in this method instead of SetClipRect()/Cull() because
			// RectMask2D.PerformClipping() peforms clipping with IClippable components first and THEN MaskableGraphic, so
			// whatever you do in your IClippable implementation will get overwriten by the MaskableGraphic logic.
			// SetClipSoftness() is called LAST in the RectMask2D.PerformClipping() so we'll perform culling here to 
			// overrride the culling that the MaskableGraphic does.
			UpdateCulling();
		}

		private void UpdateCulling()
		{
			// NOTE: This routine will still run even if there are no active filters being used.  This ensures
			// that culling works correctly when toggling filters on/off for the text and submeshes.
			if (!this.isActiveAndEnabled)
			{
				return;
			}

			if (!_textMeshPro.canvasRenderer.hasMoved)
			{
				return;
			}

			if (_textMeshPro.canvas == null)
			{
				// If you delete the parent GO with RectMask2D then _textMeshPro.canvas becomes null.
				return;
			}
			
			bool cull = !_lastClipRectValid && _parentMask != null;

			if (_lastClipRectValid && _parentMask != null)
			{
				GatherSubMeshes();
				CalculateScreenRect();

				Rect localRect = _screenRect.GetRect();
				Vector3 v0 = new Vector2(localRect.xMin, localRect.yMax);
				Vector3 v1 = new Vector2(localRect.xMax, localRect.yMax);
				Vector3 v2 = new Vector2(localRect.xMax, localRect.yMin);
				Vector3 v3 = new Vector2(localRect.xMin, localRect.yMin);

				if (_renderSpace == FilterRenderSpace.Canvas)
				{
					Matrix4x4 localToWorld = base.transform.localToWorldMatrix;
					v0 = localToWorld.MultiplyPoint(v0);
					v1 = localToWorld.MultiplyPoint(v1);
					v2 = localToWorld.MultiplyPoint(v2);
					v3 = localToWorld.MultiplyPoint(v3);
				}
				else
				{
					Camera camera = GetRenderCamera();
					if (camera)
					{
						v0 = camera.ScreenToWorldPoint(v0);
						v1 = camera.ScreenToWorldPoint(v1);
						v2 = camera.ScreenToWorldPoint(v2);
						v3 = camera.ScreenToWorldPoint(v3);
					}
				}

				Vector2 min = v0;
				Vector2 max = v0;
				min.x = Mathf.Min(v1.x, min.x);
				min.y = Mathf.Min(v1.y, min.y);
				max.x = Mathf.Max(v1.x, max.x);
				max.y = Mathf.Max(v1.y, max.y);

				min.x = Mathf.Min(v2.x, min.x);
				min.y = Mathf.Min(v2.y, min.y);
				max.x = Mathf.Max(v2.x, max.x);
				max.y = Mathf.Max(v2.y, max.y);

				min.x = Mathf.Min(v3.x, min.x);
				min.y = Mathf.Min(v3.y, min.y);
				max.x = Mathf.Max(v3.x, max.x);
				max.y = Mathf.Max(v3.y, max.y);

				min = _textMeshPro.canvas.transform.InverseTransformPoint(min);
				max = _textMeshPro.canvas.transform.InverseTransformPoint(max);

				Rect canvasRect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);

				cull = !_lastClipRect.Overlaps(canvasRect, true);
			}

			_textMeshPro.canvasRenderer.cull = cull;
			foreach (var subMesh in _subMeshes)
			{
				if (subMesh != null)
				{
					bool isSprite = (subMesh.spriteAsset != null);

					// Check if we need to skip the sprite
					if (isSprite && !_applyToSprites)
					{
						// If the sprite is not being handled by the filter stack, then
						// we have to handle culling ourselves as TextMeshProUGUI::Cull() sometimes doesn't set the culling state due to a check for dirty layout caused by us calling SetAllDirty()...
						if (_parentMask == null)
						{
							subMesh.canvasRenderer.cull = false;
						}
						else
						{
							Bounds subBounds = subMesh.mesh.bounds;
							Vector2 position = _textMeshPro.canvas.transform.InverseTransformPoint(rectTransform.position);
							Vector2 canvasLossyScale = _textMeshPro.canvas.transform.lossyScale;
							Vector2 lossyScale = rectTransform.lossyScale / canvasLossyScale;
							Rect subMeshCanvasRect = new Rect(position + subBounds.min * lossyScale, subBounds.size * lossyScale);
							subMesh.canvasRenderer.cull = !_lastClipRect.Overlaps(subMeshCanvasRect, true);
						}
						continue;
					}

					subMesh.canvasRenderer.cull = cull;
				}
			}
			_isCullingDirty = false;
		}

		#endregion // IClippable implementation

	}
}
#endif