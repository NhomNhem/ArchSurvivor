//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

//#define UIFX_OLD178_RESOLUTION_SCALING
#if UNITY_EDITOR
	#define UIFX_FILTER_DEBUG
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	public enum FilterRenderSpace
	{
		// Rendering is done in local canvas-space, before transforming the vertices to screen-space.
		Canvas,
		// Rendering is done after transform the vertices from local-space to screen-space.
		Screen,
	}

	public enum FilterExpand
	{
		None,
		Expand,
	}

	public enum FilterSourceArea
	{
		Geometry,
		RectTransform,
	}

	/// <summary>
	/// Base class for all derived filter classes
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer))]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	public abstract partial class FilterBase : UIBehaviour, IMaterialModifier, IMeshModifier, IClippable
	{
		[Tooltip("How strongly the effect is applied.")]
		[Range(0f, 1f)]
		[SerializeField] protected float _strength = 1f;

		[Tooltip("")]
		[SerializeField] protected FilterRenderSpace _renderSpace = FilterRenderSpace.Canvas;

		[SerializeField] protected FilterExpand _expand = FilterExpand.Expand;
		[SerializeField] internal FilterSourceArea _sourceArea = FilterSourceArea.Geometry;

		/// <summary>How much of the maximum effect to apply.  Range [0..1] Default is 1.0</summary>
		public float Strength { get { return _strength; } set { ChangeProperty(ref _strength, Mathf.Clamp01(value)); } }

		/// <summary>Scale all resolution calculations by this ammount.  Useful for allowing font size / transform scale to keep consistent rendering of effects.</summary>
		public float UserScale { get { return _userScale; } set { ChangeProperty(ref _userScale, value); } }

		/// <summary></summary>
		public FilterRenderSpace RenderSpace { get { return _renderSpace; } set { ChangeProperty(ref _renderSpace, value); } }

		/// <summary></summary>
		public FilterExpand Expand { get { return _expand; } set { ChangeProperty(ref _expand, value); } }

		protected readonly static Vector4 Alpha8TextureAdd = new Vector4(1f, 1f, 1f, 0f);

		protected static class ShaderProp
		{
			public readonly static int SourceTex = Shader.PropertyToID("_SourceTex");
			public readonly static int ResultTex = Shader.PropertyToID("_ResultTex");
			public readonly static int Strength = Shader.PropertyToID("_Strength");
		}

		private bool _isGraphicText;
		private Graphic _graphic;
		internal Graphic GraphicComponent { get { if (!_graphic) { _graphic = GetComponent<Graphic>(); _isGraphicText = _graphic is Text; } return _graphic; } }

		private MaskableGraphic _maskableGraphic;
		protected MaskableGraphic MaskableGraphicComponent { get { if (!_maskableGraphic) { _maskableGraphic = GraphicComponent as MaskableGraphic; } return _maskableGraphic; } }

		private CanvasRenderer _canvasRenderer;
		protected CanvasRenderer CanvasRenderComponent { get { if (!_canvasRenderer) { if (GraphicComponent) { _canvasRenderer = _graphic.canvasRenderer; } else { _canvasRenderer = GetComponent<CanvasRenderer>(); } } return _canvasRenderer; } }

		private RectTransform _rectTransform;
		protected RectTransform RectTransformComponent { get { if (!_rectTransform) { _rectTransform = GetComponent<RectTransform>(); } return _rectTransform; } }

		private Material _baseMaterial;
		protected ScreenRectFromMeshes _screenRect = new ScreenRectFromMeshes();
		internal Compositor _composite = new Compositor();
		protected Material _displayMaterial;
		private Mesh _mesh;
		private List<Color> _vertexColors;
		private Mesh _quadMesh;
		private Material _baseMaterialCopy;
		protected Canvas _canvas;
		private bool _materialOutputPremultipliedAlpha = false;
		private bool _isFilterEnabled = false;
		protected float _userScale = 1f;
		protected bool _forceUpdate = true;
		protected RectAdjustOptions _rectAdjustOptions = new RectAdjustOptions();

		internal RectAdjustOptions RectAdjustOptions { get { return _rectAdjustOptions; } }
		internal float ResolutionScalingFactor { get; private set; }

		internal const string DefaultBlendShaderPath = "Hidden/ChocDino/UIFX/Blend";
		internal const string ResolveShaderPath = "Hidden/ChocDino/UIFX/Resolve";
		private RenderTexture _resolveTexture;
		private Material _resolveMaterial;
		private Texture2D _readableTexture;
		private bool _issuedLargeTextureSizeWarning;

		internal bool DisableRendering { get; set; }
	
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

		public bool IsFiltered()
		{
			return CanApplyFilter();
		}

		internal virtual bool CanApplyFilter()
		{
			// TODO: Check Graphic is enabled?
			if (!this.isActiveAndEnabled) return false;
			if (_canvas == null) return false;
			if (!_canvas.isActiveAndEnabled) return false;
			return DoParametersModifySource();
		}

		protected virtual bool DoParametersModifySource()
		{
			if (_strength <= 0f) return false;
			return true;
		}

		private const string TextMeshProShaderPrefix = "TextMeshPro";
		private const string SpineShaderPrefix = "Spine/";
		private Material _lastBaseMaterial;
		private bool _isLastMaterialTMPro;
		private bool _isLastMaterialSpine;

		internal bool CanSelfRender()
		{
			return !_isLastMaterialTMPro && !DisableRendering;
		}

		public virtual Material GetModifiedMaterial(Material baseMaterial)
		{
			Material resultMaterial = baseMaterial;
			_isResolveTextureDirty = true;

			if (GraphicComponent != null && CanApplyFilter())
			{
				if (baseMaterial && baseMaterial.shader)
				{
					// Ignore TextMeshPro shaders as they aren't supported directly and require a component that renders directly, not via GetModifiedMaterial()
					if ((baseMaterial != _lastBaseMaterial))
					{
						if (baseMaterial.shader != Graphic.defaultGraphicMaterial.shader)
						{
							// NOTE: getting the shader name generates garbage, so we cache the material to minimise this
							_isLastMaterialTMPro = (baseMaterial.shader.name.StartsWith(TextMeshProShaderPrefix));
							if (!_isLastMaterialTMPro) { _isLastMaterialSpine = (baseMaterial.shader.name.StartsWith(SpineShaderPrefix)); }
						}
						else
						{
							_isLastMaterialTMPro = false;
							_isLastMaterialSpine = false;
						}
						_lastBaseMaterial = baseMaterial;
					}

					if (CanSelfRender())
					{
						if (_baseMaterial == null || _baseMaterialCopy == null || _baseMaterial != baseMaterial || _baseMaterialCopy.shader != baseMaterial.shader)
						{
							ObjectHelper.Destroy(ref _baseMaterialCopy);
							_baseMaterial = baseMaterial;
							_baseMaterialCopy = new Material(baseMaterial);
							_materialOutputPremultipliedAlpha = MaterialHelper.MaterialOutputsPremultipliedAlpha(baseMaterial);
						}
						if (_baseMaterialCopy)
						{
							SetupMaterialForRendering(baseMaterial);

							// If alpha <= 0 there is no need to render the filters
							if (_graphicMaterialDirtied || _lastRenderAlpha > 0f)
							{
								if (RenderFilter(true))
								{
									resultMaterial = _displayMaterial;
								}
							}

							_graphicMaterialDirtied = false;
						}
					}
				}
			}
			return resultMaterial;
		}

		protected virtual string GetDisplayShaderPath()
		{
			return DefaultBlendShaderPath;
		}

		private bool _graphicMaterialDirtied = false;

		protected void OnGraphicMaterialDirtied()
		{
			_graphicMaterialDirtied = true;
		}
		
		protected override void OnEnable()
		{
			_canvas = GetCanvas();

			if (GraphicComponent)
			{
				GraphicComponent.RegisterDirtyMaterialCallback(OnGraphicMaterialDirtied);
			}

			if (_displayMaterial == null)
			{
				string shaderPath = GetDisplayShaderPath();
				if (!string.IsNullOrEmpty(shaderPath))
				{
					var shader = Shader.Find(shaderPath);
					if (shader)
					{
						_displayMaterial = new Material(shader);
						#if UNITY_EDITOR
						_displayMaterial.name = "Filter-DisplayMaterial";
						#endif
						Debug.Assert(_displayMaterial != null);
					}
				}
			}

			ForceUpdate();

			base.OnEnable();
		}

		protected override void OnDisable()
		{
			if (GraphicComponent)
			{
				GraphicComponent.UnregisterDirtyMaterialCallback(OnGraphicMaterialDirtied);
			}

			ObjectHelper.Destroy(ref _blitMesh);
			if (_blitCommands != null)
			{
				_blitCommands.Release();
				_blitCommands = null;
			}

			RenderTextureHelper.ReleaseTemporary(ref _resolveTexture);
			ObjectHelper.Destroy(ref _resolveMaterial);
			ObjectHelper.Destroy(ref _readableTexture);
			ObjectHelper.Destroy(ref _quadMesh);
			ObjectHelper.Destroy(ref _mesh);
			_composite.FreeResources();
			_baseMaterial = null;

			UpdateClipParent();

			// NOTE: We have to force the Graphic to update when OnDisable() is called due to this component being destroyed due to the check this.isActiveAndEnabled
			// which would prevent it from running. this.isActiveAndEnabled is there to prevent forcing update when modifying values on a disabled component
			ForceUpdate(force: true);

			base.OnDisable();
		}

		protected override void OnDestroy()
		{
			ObjectHelper.Destroy(ref _baseMaterialCopy);
			ObjectHelper.Destroy(ref _displayMaterial);
			base.OnDestroy();
		}

		/// <summary>
		/// NOTE: OnRectTransformDimensionsChange() is called whenever any of the elements in RectTransform (or parents) change
		/// This doesn't get called when pixel-perfect option is disabled and the translation/rotation/scale etc changes..
		/// </summary>
		protected override void OnRectTransformDimensionsChange()
		{
			// NOTE: Ideally we wouldn't want to force a complete update when TRANSLATION changes as this wouldn't require rerendering, however
			// this is the simplest solution for now.
			//ForceUpdate();
			//OnPropertyChange();
			base.OnRectTransformDimensionsChange();
		}

		/// <summary>
		/// NOTE: OnDidApplyAnimationProperties() is called when the Animator is used to keyframe properties
		/// </summary>
		protected override void OnDidApplyAnimationProperties()
		{
			base.OnDidApplyAnimationProperties();
			OnPropertyChange();
		}

		/// <summary>
		/// OnCanvasHierarchyChanged() is called when the Canvas is enabled/disabled
		/// </summary>
		protected override void OnCanvasHierarchyChanged()
		{
			_canvas = GetCanvas();
			ForceUpdate();
			base.OnCanvasHierarchyChanged();
		}

		/// <summary>
		/// OnTransformParentChanged() is called when a parent is changed, in which case we may need to get a new Canvas
		/// </summary>
		protected override void OnTransformParentChanged()
		{
			var oldCanvas = _canvas;
			_canvas = GetCanvas();
			if (oldCanvas != _canvas)
			{
				ForceUpdate();
			}
			UpdateClipParent();
			base.OnTransformParentChanged();
		}

		/// <summary>
		/// Forces the filter to update.  Usually this happens automatically, but in some cases you may want to force an update.
		/// </summary>
		public void ForceUpdate(bool force = false)
		{
			_forceUpdate = true;

			if (force || this.isActiveAndEnabled)
			{
				var graphic = GraphicComponent;
				// There is no point setting the graphic dirty if it is not active/enabled (because SetMaterialDirty() will just return causing _forceUpdate to cleared prematurely)
				if (graphic != null && graphic.isActiveAndEnabled)
				{
					// We have to force the parent graphic to update so that the GetModifiedMaterial() and ModifyMesh() are called
					// TOOD: This wasteful, so ideally find a way to prevent this
					graphic.SetMaterialDirty();
					graphic.SetVerticesDirty();
					_forceUpdate = false;
				}
			}
			_lastModifyMeshFrame = -1;
		}

		#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			OnPropertyChange();

			// NOTE: Have to ForceUpdate() otherwise mesh doesn't update due to ModifyMesh being called multiple times a frame in this path and _lastModifyMeshFrame preventing update
			ForceUpdate();
		}
		
		protected override void OnValidate()
		{
			OnPropertyChange();
			base.OnValidate();

			// NOTE: Have to ForceUpdate() otherwise the Game View sometimes doesn't update the rendering, even though the Scene View does...
			ForceUpdate();
		}
		#endif

		private Matrix4x4 _previousLocalToWorldMatrix;
		private Matrix4x4 _previousCameraMatrix;
		private float _lastRenderAlpha = -1f;
		internal Vector2Int _lastRenderAdjustLeftDown;
		internal Vector2Int _lastRenderAdjustRightUp;

		protected void OnPropertyChange()
		{
			var graphic = GraphicComponent;
			if (!graphic) { return; }

			// If the property change caused the rectangle to change size, we need to SetVerticesDirty() to get the geometry regenerated
			Vector2Int leftDown = Vector2Int.zero;
			Vector2Int rightUp = Vector2Int.zero;
			if (_expand == FilterExpand.Expand)
			{
				GetFilterAdjustSize(ref leftDown, ref rightUp);
			}
			if (leftDown != _lastRenderAdjustLeftDown || rightUp != _lastRenderAdjustRightUp)
			{
				graphic.SetVerticesDirty();

				// In cases where the parameters are adjusted which means the filter goes between being active and not, need to also update the material
				bool needsMaterialUpdate = _isFilterEnabled != CanApplyFilter();
				if (needsMaterialUpdate)
				{
					graphic.SetMaterialDirty();
				}
			}
			else
			{
				// Otherwise the property change only needs the material updated
				if (_isFilterEnabled || CanApplyFilter())
				{
					graphic.SetMaterialDirty();
				}

				// In cases where the parameters are adjusted which means the filter goes between being active and not,
				// or the vertex alpha value changes, need to also update the vertices
				bool needsVerticesUpdate = (_isFilterEnabled != CanApplyFilter()) || (_lastRenderAlpha != GetAlpha());
				if (needsVerticesUpdate)
				{
					graphic.SetVerticesDirty();
				}
			}
			_isResolveTextureDirty = true;
		}

		protected virtual void Update()
		{
			#if UNITY_EDITOR
			// NOTE: Since we started using [ExecuteAlways] if you have a prefab open in the editor (in Scene view) and then enter Play mode, then
			// exit play mode - Unity will DESTROY all the Object references (materials etc) in the prefab GameObjects, but it will do so
			// WITHOUT calling OnDisable()/OnDestroy() and will not call OnEnable(), so here we detect this case and manually call OnEnable().
			{
				if (this.isActiveAndEnabled && !Application.IsPlaying(this.gameObject))
				{
					if (_displayMaterial == null)
					{
						OnDisable();
						OnEnable();
						return;
					}
				}
			}
			#endif
			
			if (CanSelfRender())
			{
				bool forceUpdate = _forceUpdate;

				// Only in screen-space can local transform or camera  transform affect filter rendering
				if (_renderSpace == FilterRenderSpace.Screen)
				{
					// Detect a change to the matrix (this also detects changes to the camera and viewport)
					if (MathUtils.HasMatrixChanged(_previousLocalToWorldMatrix, this.transform.localToWorldMatrix, ignoreTranslation: false))
					{
						forceUpdate = true;
						_previousLocalToWorldMatrix = this.transform.localToWorldMatrix;
					}

					// In world-space canvas, with screen-space rendering, when the camera moves we need to rerender
					if (_canvas && _canvas.renderMode == RenderMode.WorldSpace)
					{
						Camera camera = GetRenderCamera();
						if (camera)
						{
							if (MathUtils.HasMatrixChanged(_previousCameraMatrix, camera.transform.localToWorldMatrix, ignoreTranslation: false))
							{
								forceUpdate = true;
								_previousCameraMatrix = camera.transform.localToWorldMatrix;
							}
						}
					}
				}

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

				if (_isCullingDirty)
				{
					UpdateCulling();
				}

				if (forceUpdate)
				{
					ForceUpdate();
				}
			}
		}

		private Mesh _blitMesh;
		private CommandBuffer _blitCommands;

		internal Material GetDisplayMaterial()
		{
			return _displayMaterial;
		}
		
		internal bool RenderToTexture(RenderTexture sourceTexture, RenderTexture destTexture, bool renderDisplayMaterial = true)
		{
			Debug.Assert(sourceTexture != null);
			Debug.Assert(!renderDisplayMaterial || destTexture != null);
			
			RenderTexture prevRT = RenderTexture.active;

			GetResolutionScalingFactor();
			
			// Render filters and setup display material
			RenderTexture resultTexture = RenderFilters(sourceTexture);
			SetupDisplayMaterial(sourceTexture, resultTexture);

			if (renderDisplayMaterial)
			{
				// Final blit using display material (a clear is needed because we're blending into the texture).
				if (GetAlpha() >= 1f)
				{
					RenderTexture.active = destTexture;
					GL.Clear(false, true, Color.clear);
					Graphics.Blit(sourceTexture, destTexture, _displayMaterial);
				}
				else
				{
					// NOTE: No way to set alpha value in Graphics.Blit() so using CommandBuffer.DrawMesh()
					if (_blitMesh == null)
					{
						_blitMesh = new Mesh();
						_blitMesh.vertices = new Vector3[] { new Vector3(-1f, 1f, -1f), new Vector3(1f, 1f, -1f), new Vector3(1f, -1f, -1f), new Vector3(-1f, -1f, -1f) };
						_blitMesh.uv = new Vector2[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f) };
						_blitMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
					}
					Color32 alphaColor = new Color(1f, 1f, 1f, GetAlpha());
					_blitMesh.colors32 = new Color32[] { alphaColor, alphaColor, alphaColor, alphaColor };

					if (_blitCommands == null)
					{
						_blitCommands = new CommandBuffer();
					}
					_blitCommands.Clear();
					_blitCommands.SetRenderTarget(new RenderTargetIdentifier(destTexture));
					_blitCommands.ClearRenderTarget(false, true, Color.clear, 0f);
					_blitCommands.SetViewMatrix(Matrix4x4.identity);
					_blitCommands.SetProjectionMatrix(Matrix4x4.identity);
					_blitCommands.DrawMesh(_blitMesh, Matrix4x4.identity, _displayMaterial);
					Graphics.ExecuteCommandBuffer(_blitCommands);
				}
			}
 
			RenderTexture.active = prevRT;
			return true;
		}

		private bool _isResolveTextureDirty = true;

		/// <summary>
		/// Resolves to a final sRGB straight-alpha texture suitable for display or saving to image file.
		/// </summary>
		public RenderTexture ResolveToTexture()
		{
			if (_isResolveTextureDirty)
			{
				RenderTexture prevRT = RenderTexture.active;
				if (RenderFilter(true))
				{
					if (_displayMaterial && _composite.GetTexture())
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
						Graphics.Blit(_displayMaterial.mainTexture, displayTexture, _displayMaterial);

						// Resolve by removing premultiplied alpha and converting to sRGB
						Graphics.Blit(displayTexture, _resolveTexture, _resolveMaterial);

						RenderTextureHelper.ReleaseTemporary(ref displayTexture);

						_isResolveTextureDirty = false;
					}
				}
				else
				{
					RenderTextureHelper.ReleaseTemporary(ref _resolveTexture);
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
		
		public bool IsFilterEnabled()
		{
			return _isFilterEnabled;
		}

		/// <summary>
		/// This method is only intended to be used by the developers of UIFX.
		/// </summary>
		public void SetFilterEnabled(bool state)
		{
			_isFilterEnabled = state;
		}

		internal virtual float GetAlpha()
		{
			return 1f;
		}

		private static List<Canvas> s_canvasList;
		#if UNITY_EDITOR
		private static bool s_warnCanvasPlaneDistanceCulling = false;
		internal const string s_warnCanvasPlaneDistanceCullingMessage = "[UIFX] Canvas.planeDistance is beyond Camera.nearClipPlane and Camera.farClipPlane. This can result in the Graphic being culled.";
		#endif

		private Canvas GetCanvas()
		{
			Canvas result = null;
			if (GraphicComponent)
			{
				result = GraphicComponent.canvas;
			}
			else
			{
				if (s_canvasList == null)
				{
					s_canvasList = new List<Canvas>(4);
				}
				var list = s_canvasList;
				this.gameObject.GetComponentsInParent(false, list);
				if (list.Count > 0)
				{
					// Find the first active and enabled canvas.
					for (int i = 0; i < list.Count; ++i)
					{
						if (list[i].isActiveAndEnabled)
						{
							result = list[i];
							break;
						}
					}
				}
			}
			return result;
		}

		#if UNITY_EDITOR
		/// <summary>
		/// Warn users when the texture requested was not possible because it is larger than the support system size.
		/// </summary>
		internal bool IsTextureTooLarge()
		{
			return _composite.IsTextureTooLarge;
		}

		/// <summary>
		/// Warn users if Canvas.planeDistance is not between Camera.nearClipPlane and Camera.farClipPlane
		/// Some users scenes have this set incorrectly, so it's easy to add a test here.  For normal Unity UI this doesn't matter,
		/// but because in FilterRenderSpace.Screen mode we're converting to screen space and doing frustum clipping having incorrect Canvas.planeDistance
		/// can result in the Graphic being culled.
		/// </summary>
		internal bool IsCanvasPlaneDistanceOutOfRange()
		{
			bool result = false;
			var canvas = GetCanvas();
			if (canvas)
			{
				if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
				{
					var camera = canvas.worldCamera;
					if (camera)
					{
						if (canvas.planeDistance < camera.nearClipPlane || canvas.planeDistance > camera.farClipPlane)
						{
							result = true;
						}
					}
				}
			}
			return result;
		}
		#endif

		private Camera GetRenderCamera()
		{
			Debug.Assert(this.isActiveAndEnabled);
			Camera camera = null;
			if (_canvas)
			{
				camera = _canvas.worldCamera;
				if (camera == null && _canvas.renderMode == RenderMode.WorldSpace)
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
				else if (camera == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
				{
					camera = null;
				}
			}
			
			return camera;
		}

		private static UIVertex s_vertex;
		private bool HasMeshChanged(VertexHelper verts)
		{
			if (_mesh == null)
			{
				return true;
			}

			if (verts.currentVertCount != _mesh.vertexCount || verts.currentIndexCount != _mesh.GetIndexCount(0))
			{
				return true;
			}

			int vertexCount = verts.currentVertCount;
			for (int i = 0; i < vertexCount; i++)
			{
				verts.PopulateUIVertex(ref s_vertex, i);

				if (s_vertex.position != _mesh.vertices[i])
				{
					return true;
				}

				// From Unity 2020.2 UIVertex.uv0 changed from Vector2 to Vector4
				#if UNITY_2020_2_OR_NEWER
				if (s_vertex.uv0.x != _mesh.uv[i].x)
				{
					return true;
				}
				if (s_vertex.uv0.y != _mesh.uv[i].y)
				{
					return true;
				}
				#else
				if (s_vertex.uv0 != _mesh.uv[i])
				{
					return true;
				}
				#endif
			}

			return false;
		}

		private void GrabMesh(VertexHelper verts)
		{
			if (_mesh == null)
			{
				_mesh = new Mesh();
			}
			verts.FillMesh(_mesh);
			if (QualitySettings.activeColorSpace == ColorSpace.Linear)
			{
				ColorUtils.ConvertMeshVertexColorsToLinear(_mesh, ref _vertexColors);
			}
		}

		private void GrabMesh(Mesh mesh)
		{
			if (_mesh == null)
			{
				_mesh = Object.Instantiate(mesh);
			}
			else
			{
				_mesh.Clear();
				_mesh.vertices = mesh.vertices;
				_mesh.colors = mesh.colors;
				_mesh.uv = mesh.uv;
				_mesh.uv2 = mesh.uv2;
				_mesh.normals = mesh.normals;
				_mesh.tangents = mesh.tangents;
				_mesh.triangles = mesh.triangles;
			}
			if (QualitySettings.activeColorSpace == ColorSpace.Linear)
			{
				ColorUtils.ConvertMeshVertexColorsToLinear(_mesh, ref _vertexColors);
			}
		}

		private void BuildOutputQuad(VertexHelper verts)
		{
			Color32 color = new Color(1f, 1f, 1f, _lastRenderAlpha);
			if (_renderSpace == FilterRenderSpace.Canvas)
			{
				_screenRect.BuildScreenQuad(null, null, color, verts);
			}
			else
			{
				Camera renderCamera = GetRenderCamera();
				_screenRect.BuildScreenQuad(renderCamera, this.transform, color, verts);
			}
		}

		private void ApplyMesh(Mesh mesh)
		{
			CanvasRenderer cr = CanvasRenderComponent;

			_isFilterEnabled = false;
			_lastRenderAdjustLeftDown = _lastRenderAdjustRightUp = Vector2Int.zero;
			_isResolveTextureDirty = true;
			if (CanApplyFilter())
			{
				_isFilterEnabled = true;

				// NOTE: GraphicsComponent.canvas can become null when deleting the GameObject
				if (GraphicComponent != null && GraphicComponent.canvas == null)
				{
					return;
				}

				_lastRenderAlpha = GetAlpha();
				bool isFilterVisible = _lastRenderAlpha > 0f;
		
				if (isFilterVisible)
				{
					// Grab the current geometry
					GrabMesh(mesh);

					if (RenderFilter(true))
					{
						// Create quad to display the result
						if (_quadMesh == null)
						{
							_quadMesh = new Mesh();
						}
						_quadMesh.Clear();
						VertexHelper verts = new VertexHelper(_quadMesh);
						BuildOutputQuad(verts);
						verts.FillMesh(_quadMesh);
						verts.Dispose();

						cr.SetMesh(_quadMesh);
						if (_displayMaterial)
						{
							cr.materialCount = 1;
							cr.SetMaterial(_displayMaterial, 0);
						}
					}
					else
					{
						// There is nothing to render, probably because the area is zero
						cr.SetMesh(null);
					}
				}
				else
				{
					// There is nothing to render since the alpha is zero
					cr.SetMesh(null);
				}
			}
			else
			{
				// Filter is not applied
				cr.SetMesh(_mesh);
			}
		}

		private int _lastModifyMeshFrame = -1;

		/// <summary>
		/// Implements IMeshModifier.ModifyMesh() which is called by uGUI automatically when Graphic components generate geometry
		/// Note that this method is not called when using TextMeshPro as it bypasses the standard geometry generation and instead
		/// applies internally generated Mesh to the CanvasRenderer directly.
		/// Note that ModifyMesh() is called BEFORE GetModifiedMaterial()
		/// </summary>
		[UnityInternal.ExcludeFromDocs]
		public void ModifyMesh(VertexHelper verts)
		{
			_isFilterEnabled = false;
			_lastRenderAdjustLeftDown = _lastRenderAdjustRightUp = Vector2Int.zero;
			_isResolveTextureDirty = true;
			if (CanApplyFilter() && CanSelfRender())
			{
				_isFilterEnabled = true;

				// NOTE: GraphicsComponent.canvas can become null when deleting the GameObject
				if (GraphicComponent != null && GraphicComponent.canvas == null)
				{
					return;
				}

				_lastRenderAlpha = GetAlpha();
				bool isFilterVisible = _lastRenderAlpha > 0f;
		
				if (isFilterVisible)
				{
					// In the editor when pixel-perfect is enabled and the transform is adjusted, it will call ModifyMesh twice in a frame
					// So we added this check to reduce unnecessary processing during the second call.
					// But in some cases (like when font regenerates texture layout, the mesh will change in the middle of the frame, which does require a rebuild)
					if (_lastModifyMeshFrame != Time.frameCount || HasMeshChanged(verts))
					{
						_lastModifyMeshFrame = Time.frameCount;

						// Grab the current geometry
						GrabMesh(verts);

						// Create quad to display the result
						if (GenerateScreenRect())
						{
							BuildOutputQuad(verts);

							if (!_graphicMaterialDirtied)
							{
								// Only render the filter if GetModifiedMaterial() isn't also going to render the filter.
								RenderFilter(false);
							}
						}
						else
						{
							// There is nothing to render, probably because the area is zero
							verts.Clear();
						}
					}
					else
					{
						BuildOutputQuad(verts);
					}
				}
				else
				{
					// There is nothing to render since the alpha is zero
					verts.Clear();
				}
			}
			else
			{
				// Filter is not applied
			}
		}


		private static bool _issuedModifyMeshWarning = false;

		[UnityInternal.ExcludeFromDocs]
		[System.Obsolete("use IMeshModifier.ModifyMesh (VertexHelper verts) instead, or set useLegacyMeshGeneration to false", false)]
		public void ModifyMesh(Mesh mesh)
		{
			if (!_issuedModifyMeshWarning)
			{
				Debug.LogWarning("use IMeshModifier.ModifyMesh (VertexHelper verts) instead, or set useLegacyMeshGeneration to false");
				_issuedModifyMeshWarning = true;
			}

			VertexHelper vh = new VertexHelper(mesh);
			ModifyMesh(vh);
			vh.FillMesh(mesh);
			vh.Dispose();

			//	throw new System.NotImplementedException("use IMeshModifier.ModifyMesh (VertexHelper verts) instead, or set useLegacyMeshGeneration to false");
		}

		protected void GetResolutionScalingFactor()
		{
		#if UIFX_OLD178_RESOLUTION_SCALING
			Camera renderCamera = GetRenderCamera();
			ResolutionScalingFactor = Filters.GetScaling(renderCamera) * _userScale;
		#else
			float canvasScale = 1f;

			// TODO: only use  Canvas.scaleFactor in either screen/canvas space??
			//if (_renderSpace == FilterRenderSpace.Screen)
			{
				Debug.Assert(_canvas != null, "No Canvas found - ensure this gameObject is below a Canvas component.", this.gameObject);
				if (_canvas != null)
				{
					canvasScale = _canvas.scaleFactor;

					#if UNITY_EDITOR
					// Handle cases where we're in "in-context" prefab mode, in this case Canvas.ScaleFactor is not set, but
					// scale needs to be derived from the transform
					if (canvasScale == 1f && _canvas.worldCamera == null && _canvas.renderMode == RenderMode.WorldSpace)
					{
						if (EditorHelper.IsInContextPrefabMode())
						{
							canvasScale = _canvas.transform.localScale.x;
						}
					}
					#endif
				}
			}

			ResolutionScalingFactor = canvasScale * _userScale;
		#endif

			//Debug.Log("ResolutionScalingFactor "  + ResolutionScalingFactor);

			#if UNITY_EDITOR
			// Warn user about out of range Canvas.planeDistance
			if (!s_warnCanvasPlaneDistanceCulling && IsCanvasPlaneDistanceOutOfRange())
			{
				Debug.LogWarning(s_warnCanvasPlaneDistanceCullingMessage, this);
				s_warnCanvasPlaneDistanceCulling = true;
			}
			#endif
		}

		protected bool GenerateScreenRect()
		{
			bool result = false;

			GetResolutionScalingFactor();

			// Calculate screen area
			{
				// TODO: only recalculate when mesh/camera changes
				Camera renderCamera = GetRenderCamera();
				_screenRect.Start(_renderSpace == FilterRenderSpace.Canvas ? null : renderCamera, _renderSpace);
				if (_sourceArea == FilterSourceArea.Geometry && GraphicComponent)
				{
					_screenRect.AddMeshBounds(_renderSpace == FilterRenderSpace.Canvas ? null : this.transform, _mesh);
				}
				else if (RectTransformComponent)
				{
					_screenRect.AddRect(_renderSpace == FilterRenderSpace.Canvas ? null : this.transform, RectTransformComponent.rect);
				}
				_screenRect.End();

				// Expand to accomodate filter
				{
					_lastRenderAdjustLeftDown = Vector2Int.zero;
					_lastRenderAdjustRightUp = Vector2Int.zero;
					if (_expand == FilterExpand.Expand)
					{
						GetFilterAdjustSize(ref _lastRenderAdjustLeftDown, ref _lastRenderAdjustRightUp);
						Vector2Int leftDown = _lastRenderAdjustLeftDown;
						Vector2Int rightUp = _lastRenderAdjustRightUp;
						if (_renderSpace == FilterRenderSpace.Canvas)
						{
							// NOTE: Prevent a possible divide-by-zero which we've seen before.
							if (ResolutionScalingFactor != 0.0f)
							{
								// NOTE: Here we're removing the resolution scaling because in Canvas mode this
								// isn't needed, however the UserScale still needs to be applied.
								leftDown.x = Mathf.CeilToInt((leftDown.x / ResolutionScalingFactor) * UserScale);
								leftDown.y = Mathf.CeilToInt((leftDown.y / ResolutionScalingFactor) * UserScale);
								rightUp.x = Mathf.CeilToInt((rightUp.x / ResolutionScalingFactor) * UserScale);
								rightUp.y = Mathf.CeilToInt((rightUp.y / ResolutionScalingFactor) * UserScale);
							}
							else
							{
								leftDown = Vector2Int.zero;
								rightUp = Vector2Int.zero;
							}
						}
						_screenRect.Adjust(leftDown, rightUp);
					}
					_screenRect.OptimiseRects(_rectAdjustOptions);

					{
						Rect r = _screenRect.GetRect();
						RectInt textureRectDst = _screenRect.GetTextureRect();

						Debug.Assert(textureRectDst.xMin <= r.xMin);
						Debug.Assert(textureRectDst.yMin <= r.yMin);
						Debug.Assert(textureRectDst.xMax >= r.xMax);
						Debug.Assert(textureRectDst.yMax >= r.yMax);

						float xMin = Mathf.InverseLerp(textureRectDst.xMin, textureRectDst.xMax, r.xMin);
						float xMax = Mathf.InverseLerp(textureRectDst.xMin, textureRectDst.xMax, r.xMax);
						float yMin = Mathf.InverseLerp(textureRectDst.yMin, textureRectDst.yMax, r.yMin);
						float yMax = Mathf.InverseLerp(textureRectDst.yMin, textureRectDst.yMax, r.yMax);

						_rectRatio = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
						//_rectRatio = Rect.MinMaxRect(0f, 0f, 1f, 1f);
					}
				}
			}

			if (_screenRect.GetRect().width > 0 && _screenRect.GetRect().height > 0)
			{
				if (_screenRect.GetRect().width <= Filters.GetMaxiumumTextureSize() && _screenRect.GetRect().height <= Filters.GetMaxiumumTextureSize())
				{
					result = true;
				}
				else
				{
					// NOTE: If texture size limit has been reached, then the previously generates textures are used.
					// This is incorrect but better than a crash or undefined behaviour.
					result = true;
				}
			}
			else
			{
				// There is nothing to render since the rect area is zero
			}
			return result;
		}

		protected Rect GetScreenRect()
		{
			if (_canvas != null)
			{
				float sw = _canvas.pixelRect.width;
				float sh = _canvas.pixelRect.height;
				Rect r = _screenRect.GetRect();
				r.x /= sw;
				r.y /= sh;
				r.width /= sw;
				r.height /= sh;
				return r;
			}
			else
			{
				return new Rect(0f, 0f, 1f, 1f);
			}
		}

		internal void AdjustRect(ScreenRectFromMeshes rect)
		{
			_screenRect.SetRect(rect.GetRect());
			
			GetResolutionScalingFactor();

			Vector2Int leftDown = Vector2Int.zero;
			Vector2Int rightUp = Vector2Int.zero;
			if (_expand == FilterExpand.Expand)
			{
				GetFilterAdjustSize(ref leftDown, ref rightUp);
				if (_renderSpace == FilterRenderSpace.Canvas)
				{
					// NOTE: Prevent a possible divide-by-zero which we've seen before.
					if (ResolutionScalingFactor != 0.0f)
					{
						// NOTE: Here we're removing the resolution scaling because in Canvas mode this
						// isn't needed, however the UserScale still needs to be applied.
						leftDown.x = Mathf.CeilToInt((leftDown.x / ResolutionScalingFactor) * UserScale);
						leftDown.y = Mathf.CeilToInt((leftDown.y / ResolutionScalingFactor) * UserScale);
						rightUp.x = Mathf.CeilToInt((rightUp.x / ResolutionScalingFactor) * UserScale);
						rightUp.y = Mathf.CeilToInt((rightUp.y / ResolutionScalingFactor) * UserScale);
					}
					else
					{
						leftDown = Vector2Int.zero;
						rightUp = Vector2Int.zero;
					}
				}
				rect.Adjust(leftDown, rightUp);
			}

			// Store the rect
			_screenRect.SetRect(rect.GetRect());
			_screenRect.OptimiseRects(_rectAdjustOptions);
		}

		internal void SetFinalRect(ScreenRectFromMeshes finalRect)
		{
			RectInt textureRectSrc = _screenRect.GetTextureRect();
			RectInt textureRectDst = finalRect.GetTextureRect();

			Debug.Assert(textureRectDst.xMin <= textureRectSrc.xMin);
			Debug.Assert(textureRectDst.yMin <= textureRectSrc.yMin);
			Debug.Assert(textureRectDst.xMax >= textureRectSrc.xMax);
			Debug.Assert(textureRectDst.yMax >= textureRectSrc.yMax);

			// Get the ratio of the smaller internal rectangle for this filter, compared to the (potentially) larger texture size based on the accumulated size of all filters
			float xMin = Mathf.InverseLerp(textureRectDst.xMin, textureRectDst.xMax, textureRectSrc.xMin);
			float xMax = Mathf.InverseLerp(textureRectDst.xMin, textureRectDst.xMax, textureRectSrc.xMax);
			float yMin = Mathf.InverseLerp(textureRectDst.yMin, textureRectDst.yMax, textureRectSrc.yMin);
			float yMax = Mathf.InverseLerp(textureRectDst.yMin, textureRectDst.yMax, textureRectSrc.yMax);

			_rectRatio = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
		}

		protected Rect _rectRatio = new Rect(0f, 0f, 1f, 1f);

		private void SetupMaterialForRendering(Material baseMaterial)
		{
			Debug.Assert(_baseMaterialCopy != null);

			// Copy material properties (and enabled keywords) for rendering
			_baseMaterialCopy.CopyPropertiesFromMaterial(baseMaterial);

			var graphic = GraphicComponent;

			// NOTE: When we get a copy of the material via GetModifiedMaterial() the mainTexture usually 
			// hasn't been set yet, unless it's a material that the user has set.  So we must get this from 
			// the GraphicComponent and assign it ourselves.
			//bool isCustomUserMaterial = (graphic.material != graphic.defaultMaterial);
			//if (!isCustomUserMaterial)
			{
				// Only override the material mainTexture if one is specified in the Graphic
				var graphicTexture = graphic.mainTexture;
				if (graphicTexture)
				{
					_baseMaterialCopy.mainTexture = graphicTexture;
				}
				if (_baseMaterialCopy.mainTexture)
				{
					if (_isGraphicText)
					{
						// For Unity's Text as decoding vector needs to be set
						_baseMaterialCopy.SetVector(UnityShaderProp.TextureAddSample, Alpha8TextureAdd);
					}
					else
					{
						_baseMaterialCopy.SetVector(UnityShaderProp.TextureAddSample, Vector4.zero);
					}
				}
			}

			if (_isLastMaterialSpine)
			{
				// Recent versions of Spine shaders use _ClipRect even when there is no RectMask2D so because we're not using through CanvasRenderer which would
				// automatically set this variable, we must set it ourselves.
				UnityShaderProp.ResetClipRect(_baseMaterialCopy);
			}

			// Copy the stencil properties to the display material
			if (_displayMaterial && MaskableGraphicComponent)
			{
				if (MaskableGraphicComponent.maskable)
				{
					UnityShaderProp.CopyStencilProperties(_baseMaterialCopy, _displayMaterial);
				}
				else
				{
					UnityShaderProp.ResetStencilProperties(_displayMaterial);
				}
			}
		}

		public virtual bool IsOutputHdr()
		{
			return false;
		}

		private static GameObject _gameObjectPreviousFilterHasHdr = null;

		protected virtual bool RenderFilter(bool generateScreenRect)
		{
			bool result = false;

			GetResolutionScalingFactor();
			Camera renderCamera = GetRenderCamera();

			Debug.Assert(_canvas != null, "No Canvas found - ensure this gameObject is below a Canvas component.", this.gameObject);
			if (_canvas)
			{
				// Only call GenerateScreenRect() when needed
				if (generateScreenRect)
				{
					GenerateScreenRect();
				}

				bool forceHdr = Compositor.DefaultTextureFormat == Compositor.DefaultHdrTextureFormat;
				if (!forceHdr)
				{
					forceHdr = IsOutputHdr();

					// Determine whether a previously rendered filter in this gameObject was rendered with Hdr
					{
						// If rendering has started on a new GameObject, or the last material passed into GetModifiedMaterial() matches the Graphic material
						// then assume we're the first filter in the GameObject and reset the Hdr GameObject state.
						if (_lastBaseMaterial == GraphicComponent.material || this.gameObject != _gameObjectPreviousFilterHasHdr)
						{
							_gameObjectPreviousFilterHasHdr = null;
						}
						if (this.gameObject == _gameObjectPreviousFilterHasHdr)
						{
							forceHdr = true;
						}
						if (forceHdr && _gameObjectPreviousFilterHasHdr == null)
						{
							_gameObjectPreviousFilterHasHdr = this.gameObject;
						}
					}
				}

				// TODO: only recalculate when screenRect/mesh/camera changes
				if (_composite.Start(_renderSpace == FilterRenderSpace.Canvas ? null : renderCamera, _screenRect.GetTextureRect(), forceHdr, _renderSpace == FilterRenderSpace.Canvas ? _canvas.scaleFactor : 1f))
				{
					// Some shaders use _ScreenParams so we must set it here.
					{
						float sw = Screen.width;
						float sh = Screen.height;
						if (renderCamera != null)
						{
							sw = renderCamera.pixelWidth;
							sh = renderCamera.pixelHeight;
						}
						Shader.SetGlobalVector(UnityShaderProp.ScreenParams, new Vector4(sw, sh, 1f + (1f / sw), 1f + (1f / sh)));
					}

					if (Debug.isDebugBuild)
					{
						if (_composite.IsTextureTooLarge)
						{
							if (!_issuedLargeTextureSizeWarning)
							{
								_issuedLargeTextureSizeWarning = true;

								string objectName = this.gameObject.name;
#if UNITY_EDITOR
								objectName = UnityEditor.AnimationUtility.CalculateTransformPath(this.transform, transform.root);
#endif
								string cameraName = "none";
								if (renderCamera != null && _renderSpace == FilterRenderSpace.Screen)
								{
									cameraName = renderCamera.name;
								}
								Debug.LogError(string.Format("[UIFX] Texture too large {1}x{2} * {3} for object: {0} using camera: {4}", objectName, _screenRect.GetTextureRect().width, _screenRect.GetTextureRect().height, _renderSpace == FilterRenderSpace.Canvas ? _canvas.scaleFactor : 1f, cameraName), this);
							}

						}
						else
						{
							_issuedLargeTextureSizeWarning = false;
						}
					}

					if (GraphicComponent)
					{
						if (!_graphicMaterialDirtied)
						{
							if (_baseMaterial && _baseMaterialCopy)
							{
								SetupMaterialForRendering(_baseMaterial);
							}
						}
						_composite.AddMesh(_renderSpace == FilterRenderSpace.Canvas ? null : this.transform, _mesh, _baseMaterialCopy, _materialOutputPremultipliedAlpha, false);
					}
					_composite.End();

					// Optionally render the composite through filters
					{
						RenderTexture sourceTexture = _composite.GetTexture();
						RenderTexture resultTexture = RenderFilters(sourceTexture);
						SetupDisplayMaterial(sourceTexture, resultTexture);
					}
				}
				result = true;
			}
			return result;
		}

		protected virtual RenderTexture RenderFilters(RenderTexture source)
		{
			// Optionally apply some filters
			return source;
		}

		protected virtual void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
		}

		protected virtual void SetupDisplayMaterial(Texture source, Texture result)
		{
			if (_displayMaterial)
			{
				_displayMaterial.mainTexture = result;
				_displayMaterial.SetTexture(ShaderProp.SourceTex, source);
				_displayMaterial.SetTexture(ShaderProp.ResultTex, result);
			}
		}

		protected bool ChangeProperty<T>(ref T backing, T value) where T : struct
		{
			bool result = false;
			if (ObjectHelper.ChangeProperty(ref backing, value))
			{
				result = true;
				OnPropertyChange();
			}
			return result;
		}

		protected bool ChangePropertyRef<T>(ref T backing, T value) where T : class
		{
			bool result = false;
			if (backing != value)
			{
				result = true;
				backing = value;
				OnPropertyChange();
			}
			return result;
		}

		internal Rect GetLocalRect()
		{
			return _screenRect.GetLocalRect();
		}

		internal virtual string GetDebugString()
		{
			string result = string.Empty;
			if (_screenRect != null)
			{
				result += "Rect: " + _screenRect.GetRect().ToString() + "\n";
			}
			if (_expand == FilterExpand.Expand)
			{
				Vector2Int leftDown = Vector2Int.zero;
				Vector2Int rightUp = Vector2Int.zero;
				GetFilterAdjustSize(ref leftDown, ref rightUp);
				result += "Expand: [" + leftDown + " - " + rightUp + "]\n";
			}

			return result;
		}

		internal virtual Texture[] GetDebugTextures()
		{
			List<Texture> result = new List<Texture>();
			if (_composite != null)
			{
				result.Add(_composite.GetTexture());
			}
			if (_displayMaterial != null)
			{
				result.Add(_displayMaterial.mainTexture);
			}
			return result.ToArray();
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
			RectMask2D newParentMask = null;
			if (MaskableGraphicComponent != null)
			{
				newParentMask = (MaskableGraphicComponent.maskable && IsActive()) ? MaskUtilities.GetRectMaskForClippable(this) : null;
			}
			if (newParentMask != _parentMask)
			{
				if (_parentMask != null)
				{
					_parentMask.RemoveClippable(this);
					_parentMask = null;
				}

				_parentMask = newParentMask;
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

		private static List<FilterBase> _siblingFilters = new List<FilterBase>(4);

		protected bool IsLastEnabledFilter()
		{
			this.gameObject.GetComponents<FilterBase>(_siblingFilters);
			// Sanity check, these must be at least some components
			Debug.Assert(_siblingFilters != null);
			Debug.Assert(_siblingFilters.Count > 0);

			for (int i = _siblingFilters.Count - 1; i >= 0; i--)
			{
				var filter = _siblingFilters[i];
				if (filter.isActiveAndEnabled && filter.IsFilterEnabled())
				{
					return (filter == this);
				}
			}
			return false;
		}

		private void UpdateCulling()
		{
			// NOTE: This routine will still run even if there are no active filters being used.  This ensures
			// that culling works correctly when toggling filters on/off for the text and submeshes.
			if (!this.isActiveAndEnabled)
			{
				return;
			}

			// If this filter rendering is controlled by another component (eg FilterStackTextMeshPro) 
			// then don't run culling logic.
			if (!CanSelfRender())
			{
				return;
			}

			if (!CanvasRenderComponent.hasMoved)
			{
				//return;
			}

			// Only run clipping logic on the last filter because each filter can adjust the geometry size.
			if (!IsLastEnabledFilter())
			{
				return;
			}

			if (_canvas == null)
			{
				// If you delete the parent GO with RectMask2D then _canvas becomes null.
				return;
			}
			
			// Ensure that all sibling filters have valid geometry before
			// using them to get the screenRect.
			{
				for (int i = _siblingFilters.Count - 1; i >= 0; i--)
				{
					var filter = _siblingFilters[i];
					if (filter.isActiveAndEnabled && !filter.IsFilterEnabled())
					{
						return;
					}
				}
			}

			bool cull = !_lastClipRectValid && _parentMask != null;

			// Note: Check _mesh != null because Clipping can happen before the mesh has been grabbed.
			if (_mesh != null)
			{
				if (_lastClipRectValid && _parentMask != null)
				{
					GenerateScreenRect();

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

					min = _canvas.transform.InverseTransformPoint(min);
					max = _canvas.transform.InverseTransformPoint(max);

					Rect canvasRect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);

					cull = !_lastClipRect.Overlaps(canvasRect, true);
				}

				CanvasRenderComponent.cull = cull;
				_isCullingDirty = false;
			}
		}

		#endregion // IClippable implementation

	}
}