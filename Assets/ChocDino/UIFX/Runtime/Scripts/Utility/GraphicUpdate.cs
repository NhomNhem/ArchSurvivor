//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChocDino.UIFX
{
	public enum GraphicUpdateMode
	{
		Manually,
		EveryFrame,
		WebCamTexture,
		Time,
		UnscaledTime,
	}

	[RequireComponent(typeof(CanvasRenderer))]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Graphic Update", 1000)]
	/// <summary>
	/// Forces the Graphic to update it's material, causing all filters to update as well.
	/// This is useful for example if the Graphic texture is changing it's contents (eg Video / WebCamTexture),
	/// in which case the filters all need to re-render whenever this change happens.
	/// </summary>
	public class GraphicUpdate : MonoBehaviour
	{
		[SerializeField] GraphicUpdateMode _updateMode = GraphicUpdateMode.EveryFrame;

		[SerializeField, Min(0f)] float _timeInterval = 1f;

		/// <summary>Controls when to force update the graphic material.</summary>
		public GraphicUpdateMode UpdateMode { get { return _updateMode; } set { _updateMode = value; } }

		/// <summary></summary>
		public float TimeInterval { get { return _timeInterval; } set { _timeInterval = value; } }

#if UIFX_WEBCAMTEXTURE
		/// <summary>The webcam to use to control re-rendering.</summary>
		public WebCamTexture WebCam { get { return _webcam; } set { _webcam = value; } }
#endif

#if UIFX_WEBCAMTEXTURE
		private WebCamTexture _webcam;
#endif

		private float _updateTimer;
		private Graphic _graphic;
		internal Graphic GraphicComponent { get { if (!_graphic) { _graphic = GetComponent<Graphic>(); } return _graphic; } }

		public void ForceUpdate()
		{
			var graphic = GraphicComponent;
			if (graphic)
			{
				graphic.SetMaterialDirty();
				//graphic.SetAllDirty();
				//Debug.Log(Time.frameCount + " force update");
			}
		}

		void OnEnable()
		{
			#if UNITY_2020_2_OR_NEWER
			Canvas.preWillRenderCanvases += WillRenderCanvases;
			#else
			Canvas.willRenderCanvases += WillRenderCanvases;
			#endif
		}

		void OnDisable()
		{
			#if UNITY_2020_2_OR_NEWER
			Canvas.preWillRenderCanvases -= WillRenderCanvases;
			#else
			Canvas.willRenderCanvases -= WillRenderCanvases;
			#endif
		}

		void WillRenderCanvases()
		{
			if (_updateMode == GraphicUpdateMode.EveryFrame)
			{
				ForceUpdate();
			}
#if UIFX_WEBCAMTEXTURE
			else if (_updateMode == GraphicUpdateMode.WebCamTexture && _webcam != null)
			{
				if (_webcam.didUpdateThisFrame)
				{
					ForceUpdate();
				}
			}
#endif
			else if (_updateMode == GraphicUpdateMode.Time)
			{
				_updateTimer -= Time.deltaTime;
				if (_updateTimer <= 0f)
				{
					_updateTimer += _timeInterval;
					if (_updateTimer <= 0f)
					{
						_updateTimer = _timeInterval;
					}
					ForceUpdate();
				}
			}
			else if (_updateMode == GraphicUpdateMode.UnscaledTime)
			{
				_updateTimer -= Time.unscaledDeltaTime;
				if (_updateTimer <= 0f)
				{
					_updateTimer += _timeInterval;
					if (_updateTimer <= 0f)
					{
						_updateTimer = _timeInterval;
					}
					ForceUpdate();
				}
			}
		}
	}
}