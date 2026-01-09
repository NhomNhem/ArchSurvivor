//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	public enum GradientWrap
	{
		Clamp,
		Repeat,
		Mirror,
	}

	public enum GradientLerp
	{
		Step,
		Linear,
		Smooth,
	}

	public enum GradientColorSpace
	{
		Linear,
		Perceptual,
	}

	public enum GradientShape
	{
		None,
		Horizontal,
		Vertical,
		Diagonal,
		Linear,
		Radial,
		Conic,
	}

	/// <summary>
	/// Utility class for reading gradients for different Unity versions that minimizes garbage generation.
	/// </summary>
	internal struct GradientRead
	{
		#if UNITY_6000_2_OR_NEWER
		public const int MaxGradientKeys = 8;
		public static GradientColorKey[] s_colorKeys = new GradientColorKey[MaxGradientKeys];
		public static GradientAlphaKey[] s_alphaKeys = new GradientAlphaKey[MaxGradientKeys];
		#endif

		public int ColorKeysCount { get; private set; }
		public int AlphaKeysCount { get; private set; }

		#if UNITY_6000_2_OR_NEWER
		public GradientColorKey[] ColorKeys { get => s_colorKeys; }
		public GradientAlphaKey[] AlphaKeys { get => s_alphaKeys; }
		#else
		public GradientColorKey[] ColorKeys { get; private set; }
		public GradientAlphaKey[] AlphaKeys { get; private set; }
		#endif

		public GradientRead(Gradient gradient)
		{
			#if UNITY_6000_2_OR_NEWER
			ColorKeysCount = gradient.colorKeyCount;
			AlphaKeysCount = gradient.alphaKeyCount;
			System.Span<GradientColorKey> colorKeys = s_colorKeys;
			System.Span<GradientAlphaKey> alphaKeys = s_alphaKeys;
			gradient.GetColorKeys(colorKeys);
			gradient.GetAlphaKeys(alphaKeys);
			#else
			// NOTE: these two properties generate garbage
			ColorKeys = gradient.colorKeys;
			AlphaKeys = gradient.alphaKeys;
			ColorKeysCount = ColorKeys.Length;
			AlphaKeysCount = AlphaKeys.Length;
			#endif
		}

		public void Write(Gradient gradient)
		{
			#if UNITY_6000_2_OR_NEWER
			System.ReadOnlySpan<GradientColorKey> colorKeys = new System.ReadOnlySpan<GradientColorKey>(s_colorKeys, 0, ColorKeysCount);
			System.ReadOnlySpan<GradientAlphaKey> alphaKeys = new System.ReadOnlySpan<GradientAlphaKey>(s_alphaKeys, 0, AlphaKeysCount);;
			gradient.SetKeys(colorKeys, alphaKeys);
			#else
			gradient.SetKeys(ColorKeys, AlphaKeys);
			#endif
		}
	}

	internal class GradientChangeDetector
	{
		private const int MaxGradientKeys = 8;

		private int[] _ints = new int[4];
		private Color[] _colors = new Color[MaxGradientKeys];
		private float[] _floats = new float[MaxGradientKeys * 3];
		private Gradient _gradient;

		public GradientChangeDetector(Gradient gradient)
		{
			Set(gradient);
		}

		public bool IsChanged(Gradient gradient)
		{
			bool isChanged = false;

			if (_gradient == null)
			{
				isChanged = (gradient != null);
			}

			if (!isChanged && !ReferenceEquals(_gradient, gradient))
			{
				isChanged = true;
			}

			var gradientRead = new GradientRead(gradient);

			if (!isChanged)
			{
				if (_ints[0] != gradientRead.ColorKeysCount || _ints[1] != gradientRead.AlphaKeysCount || _ints[2] != (int)gradient.mode 
#if UNITY_2022_2_OR_NEWER
				|| _ints[3] != (int)gradient.colorSpace
#endif
				)
				{
					isChanged = true;
				}
			}
			if (!isChanged)
			{
				for (int i = 0; i < gradientRead.AlphaKeysCount; i++)
				{
					if (_floats[i * 2 + 0] != gradientRead.AlphaKeys[i].alpha || _floats[i * 2 + 1] != gradientRead.AlphaKeys[i].time)
					{
						isChanged = true;
					}
				}
			}
			if (!isChanged)
			{
				for (int i = 0; i < gradientRead.ColorKeysCount; i++)
				{
					if (_colors[i] != gradientRead.ColorKeys[i].color || _floats[gradientRead.AlphaKeysCount * 2 + i] != gradientRead.ColorKeys[i].time)
					{
						isChanged = true;
					}
				}
			}

			if (isChanged)
			{
				Set(gradient, gradientRead.AlphaKeys, gradientRead.ColorKeys, gradientRead.AlphaKeysCount, gradientRead.ColorKeysCount);
			}

			return isChanged;
		}

		private void Set(Gradient gradient)
		{
			var gradientRead = new GradientRead(gradient);
			Set(gradient, gradientRead.AlphaKeys, gradientRead.ColorKeys, gradientRead.AlphaKeysCount, gradientRead.ColorKeysCount);
		}

		private void Set(Gradient gradient, GradientAlphaKey[] alphaKeys, GradientColorKey[] colorKeys, int alphaCount, int colorCount)
		{
			_gradient = gradient;
			_ints[0] = colorCount;
			_ints[1] = alphaCount;
			_ints[2] = (int)gradient.mode;
#if UNITY_2022_2_OR_NEWER
			_ints[3] = (int)gradient.colorSpace;
#endif

			for (int i = 0; i < alphaCount; i++)
			{
				_floats[i * 2 + 0] = alphaKeys[i].alpha;
				_floats[i * 2 + 1] = alphaKeys[i].time;
			}
			for (int i = 0; i < colorCount; i++)
			{
				_colors[i] = colorKeys[i].color;
				_floats[alphaCount * 2 + i] = colorKeys[i].time;
			}
		}
	}

	[UnityInternal.ExcludeFromDocs]
	internal class GradientTexture : System.IDisposable
	{
		private static Color s_color = Color.black;

		private readonly int _resolution = 256;
		private Color[] _colors;
		private Texture2D _texture;
		private GradientChangeDetector _compare;
		private static GraphicsFormat s_gradientTextureFormat;
		private static GraphicsFormat s_curveTextureFormat;

		public int Resolution { get => _resolution; }
		public Texture2D Texture { get { return _texture; } }

		public static void Create(ref GradientTexture gradientTexture, int resolution)
		{
			if (gradientTexture != null && gradientTexture.Resolution != resolution)
			{
				ObjectHelper.Dispose(ref gradientTexture);
			}
			if (gradientTexture == null)
			{
				gradientTexture = new GradientTexture(resolution);
			}
		}

		public GradientTexture(int resolution)
		{
			Debug.Assert(resolution > 0 && resolution <= 8192);
			_resolution = resolution;
		}

		public void Update(Gradient gradient)
		{
			bool isDirty = false;

			if (_texture == null)
			{
				if (s_gradientTextureFormat == GraphicsFormat.None)
				{
#if UNITY_6000_0_OR_NEWER
					var formatUsage = GraphicsFormatUsage.SetPixels | GraphicsFormatUsage.Sample | GraphicsFormatUsage.Linear;
#else
					var formatUsage = FormatUsage.SetPixels | FormatUsage.Linear;
#endif

					if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, formatUsage))
					{
						s_gradientTextureFormat = GraphicsFormat.R16G16B16A16_SFloat;
					}
					else if (SystemInfo.IsFormatSupported(GraphicsFormat.R32G32B32A32_SFloat, formatUsage))
					{
						s_gradientTextureFormat = GraphicsFormat.R32G32B32A32_SFloat;
					}
					else if (SystemInfo.IsFormatSupported(GraphicsFormat.R8G8B8A8_UNorm, formatUsage))
					{
						s_gradientTextureFormat = GraphicsFormat.R8G8B8A8_UNorm;
					}
					else if (SystemInfo.IsFormatSupported(GraphicsFormat.B8G8R8A8_UNorm, formatUsage))
					{
						s_gradientTextureFormat = GraphicsFormat.B8G8R8A8_UNorm;
					}
				}

#if UNITY_2022_1_OR_NEWER
				_texture = new Texture2D(_resolution, 1, s_gradientTextureFormat, TextureCreationFlags.DontInitializePixels|TextureCreationFlags.DontUploadUponCreate);
#else
				_texture = new Texture2D(_resolution, 1, s_gradientTextureFormat, TextureCreationFlags.None);
#endif

				_colors = new Color[_resolution];
				_compare = new GradientChangeDetector(gradient);

				_texture.filterMode = FilterMode.Bilinear;
				_texture.wrapMode = TextureWrapMode.Clamp;
				isDirty = true;
			}

			if (!isDirty)
			{
				isDirty = _compare.IsChanged(gradient);
			}

			if (isDirty)
			{
				isDirty = false;
				float step = 1f / (_resolution - 1);
				for (int x = 0; x < _resolution; x++)
				{
					var t = x * step; // multiply instead of add for accuracy
					// TODO: convert to premultiplied and linear here?
					_colors[x] = gradient.Evaluate(t).linear;
				}
				_texture.SetPixels(_colors);
				_texture.Apply(false, false);
			}
		}

		public void Update(AnimationCurve curve)
		{
			if (_texture == null)
			{
				if (s_curveTextureFormat == GraphicsFormat.None)
				{
#if UNITY_6000_0_OR_NEWER
					var formatUsage = GraphicsFormatUsage.SetPixels | GraphicsFormatUsage.Sample | GraphicsFormatUsage.Linear;
#else
					var formatUsage = FormatUsage.SetPixels | FormatUsage.Linear;
#endif

					if (SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, formatUsage))
					{
						s_curveTextureFormat = GraphicsFormat.R16_SFloat;
					}
					else if (SystemInfo.IsFormatSupported(GraphicsFormat.R32_SFloat, formatUsage))
					{
						s_curveTextureFormat = GraphicsFormat.R32_SFloat;
					}
					else if (SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, formatUsage))
					{
						s_curveTextureFormat = GraphicsFormat.R8_UNorm;
					}
				}

#if UNITY_2022_1_OR_NEWER
				_texture = new Texture2D(_resolution, 1, s_curveTextureFormat, TextureCreationFlags.DontInitializePixels|TextureCreationFlags.DontUploadUponCreate);
#else
				_texture = new Texture2D(_resolution, 1, s_curveTextureFormat, TextureCreationFlags.None);
#endif

				_texture.filterMode = FilterMode.Bilinear;
				_texture.wrapMode = TextureWrapMode.Clamp;
			}
			float step = 1f / (_resolution - 1);
			for (int x = 0; x < _resolution; x++)
			{
				var t = x * step; // multiply instead of add for accuracy
				s_color.r = curve.Evaluate(t);
				_texture.SetPixel(x, 0, s_color);
			}
			_texture.Apply(false, false);
		}

		public void Dispose()
		{
			ObjectHelper.Destroy(ref _texture);
			_colors = null;
		}
	}

#if false

	public enum GradientWrap
	{
		Clamp,
		Repeat,
		Mirror,
	}

	public enum GradientMix
	{
		Step,
		Linear,
		Smooth,
	}

	public enum GradientColorSpace
	{
		sRGB,
		Linear,
		Perceptual,
	}
#endif

	[UnityInternal.ExcludeFromDocs]
	public static class GradientUtils
	{
#if false
		public static Color EvalGradient(float t, Gradient gradient, GradientWrapMode wrapMode, float offset = 0f, float scale = 1f, float scalePivot = 0f)
		{
			t -= scalePivot;
			t *= scale;
			t += scalePivot;
			t += offset;

			if (wrapMode == GradientWrapMode.Wrap)
			{
				// NOTE: Only wrap if we're outside of the range, otherwise for t=1.0 (which happens often) we'll evaulate 0.0 which in most cases is not what we want
				if (t < 0f || t > 1f)
				{
					t = Mathf.Repeat(t, 1f);
				}
			}
			else if (wrapMode == GradientWrapMode.Mirror)
			{
				t = Mathf.PingPong(t, 1f);
				if (Mathf.Sign(scale) < 0f)
				{
					t = 1f - t;
				}
			}

			return gradient.Evaluate(t);
		}
#endif

		/// <summary>
		/// Reverse the gradient
		/// </summary>
		public static void Reverse(Gradient gradient)
		{
			GradientColorKey[] colors = gradient.colorKeys;
			GradientAlphaKey[] alphas = gradient.alphaKeys;

			for (int i = 0; i < colors.Length; i++)
			{
				colors[i].time = 1f - colors[i].time;
			}
			for (int i = 0; i < alphas.Length; i++)
			{
				alphas[i].time = 1f - alphas[i].time;
			}

			gradient.SetKeys(colors, alphas);
		}

		/// <summary>
		/// CSS linear gradients always have the start and end colors at one of the edges/corners.
		/// This method calculates the parameters for our shader.
		/// </summary>
		public static void GetCssLinearGradientShaderParams(float angle, Rect rect, out Vector2 uvPointOnStartLine, out Vector2 uvStartLineDirection, out float uvGradientLength, out float uvRectRatio)
		{
			// Definitions:
			// Gradient angle 0..360 degrees clock-wise where 0 is upwards.
			// Gradient line goes from the center of the rectangle in the direction of the angle.
			// Method:
			// The gradient line runs in the direction of the angle.
			// The gradient goes from a Start line to an End line. These lines are parallel and are perpendicular to the Gradient line.
			// From the angle, we pick the closest opposite quadrant corner on the rectangle - this corner point will be a point on the Start line.
			// We get the direction of the start line, which is just 90 degree rotation of the gradient direction.
			// In the shader it does a dot product to find the closest point on the Start line from the uv coordinate - this distance is used to draw the gradient.

			// Get the coordinates of the closest quadrant corner in the opposite direction of our gradient
			// this point lies on the starting line
			Vector2 startingLineCorner = rect.min;
			{
				int quadrant = Mathf.FloorToInt((angle % 360f) / 90f);
				switch (quadrant)
				{
					case 0:
					break;
					case 1:
					startingLineCorner = new Vector2(rect.xMin, rect.yMax);
					break;
					case 2:
					startingLineCorner = rect.max;
					break;
					case 3:
					startingLineCorner = new Vector2(rect.xMax, rect.yMin);
					break;
					default:
					// Should never get here
					Debug.LogError("Invalid quadarant");
					break;
				}
			}

			float angleRad = Mathf.Deg2Rad * angle;
			uvRectRatio = rect.width / rect.height;

			// Calculate distance between Start and End line in UV-space
			{
				float uvWidth = uvRectRatio;
				float uvHeight = 1f;
				Vector2 gradientDirection = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
				uvGradientLength = Mathf.Abs(uvWidth * gradientDirection.x) + Mathf.Abs(uvHeight * gradientDirection.y);
			}

			// Convert pointOnStartLine to UV-space
			uvPointOnStartLine.x = (startingLineCorner.x - rect.x) / rect.width;
			uvPointOnStartLine.y = (startingLineCorner.y - rect.y) / rect.height;
			uvPointOnStartLine.x *= uvRectRatio;

			// Calculate direction of the start line
			uvStartLineDirection = new Vector2(Mathf.Sin(angleRad+Mathf.PI * 0.5f), Mathf.Cos(angleRad+Mathf.PI * 0.5f));
		}
	}

#if false
	[System.Serializable]
	internal class GradientShader
	{
		internal static class ShaderProp
		{
			public readonly static int GradientColorCount = Shader.PropertyToID("_GradientColorCount");
			public readonly static int GradientAlphaCount = Shader.PropertyToID("_GradientAlphaCount");
			public readonly static int GradientColors = Shader.PropertyToID("_GradientColors");
			public readonly static int GradientAlphas = Shader.PropertyToID("_GradientAlphas");
			public readonly static int GradientTransform = Shader.PropertyToID("_GradientTransform");
			public readonly static int GradientRadial = Shader.PropertyToID("_GradientRadial");
			public readonly static int GradientDither = Shader.PropertyToID("_GradientDither");
		}
		internal static class ShaderKeyword
		{
			public const string GradientMixSmooth = "GRADIENT_MIX_SMOOTH";
			public const string GradientMixLinear = "GRADIENT_MIX_LINEAR";
			public const string GradientMixStep = "GRADIENT_MIX_STEP";

			internal const string GradientColorSpaceSRGB = "GRADIENT_COLORSPACE_SRGB";
			internal const string GradientColorSpaceLinear = "GRADIENT_COLORSPACE_LINEAR";
			internal const string GradientColorSpacePerceptual = "GRADIENT_COLORSPACE_PERCEPTUAL";
		}

		[SerializeField] Gradient _gradient;
		[SerializeField] GradientMix _mixMode = GradientMix.Smooth;
		[SerializeField] GradientColorSpace _colorSpace = GradientColorSpace.Perceptual;
		[Range(0f, 1f)]
		[SerializeField] float _dither = 0.5f;
		/*[Range(-1f, 1f)]
		[SerializeField] float _centerX = 0f;
		[Range(-1f, 1f)]
		[SerializeField] float _centerY = 0f;
		[Range(0f, 16f)]
		[SerializeField] float _radius = 0.5f;*/
		[SerializeField] float _scale = 1f;
		[Range(0f, 1f)]
		[SerializeField] float _scalePivot = 0.5f;
		[SerializeField] float _offset = 0f;
		[SerializeField] GradientWrap _wrapMode = GradientWrap.Clamp;

		//public float GradientCenterX { get { return _gradientCenterX; } set { _gradientCenterX = value; ForceUpdate(); } }
		//public float GradientCenterY { get { return _gradientCenterY; } set { _gradientCenterY = value; ForceUpdate(); } }
		//public float GradientRadius { get { return _gradientRadius; } set { _gradientRadius = value; ForceUpdate(); } }
		//public Gradient Gradient { get { return _gradient; } set { _gradient = value; ForceUpdate(); } }

		private Vector4[] _colorKeys = new Vector4[8];
		private Vector4[] _alphaKeys = new Vector4[8];

		private void GradientToArrays()
		{
			int colorKeyCount = _gradient.colorKeys.Length;
			for (int i = 0; i < colorKeyCount; i++)
			{
				Color c = _gradient.colorKeys[i].color;

				switch (_colorSpace)
				{
					default:
					case GradientColorSpace.sRGB:
					_colorKeys[i] = new Vector4(c.r, c.g, c.b, _gradient.colorKeys[i].time);
					break;
					case GradientColorSpace.Linear:
					c = c.linear;
					_colorKeys[i] = new Vector4(c.r, c.g, c.b, _gradient.colorKeys[i].time);
					break;
					case GradientColorSpace.Perceptual:
					{
						Vector3 oklab = ColorUtils.LinearToOklab(c.linear);
						_colorKeys[i] = new Vector4(oklab.x, oklab.y, oklab.z, _gradient.colorKeys[i].time);
					}
					break;
				}
			}
			int alphaKeyCount = _gradient.alphaKeys.Length;
			for (int i = 0; i < alphaKeyCount; i++)
			{
				_alphaKeys[i] = new Vector4(_gradient.alphaKeys[i].alpha, 0f, 0f, _gradient.alphaKeys[i].time);
			}
		}

		internal void SetupMaterial(Material material)
		{
			if (_gradient == null ) { return; }

			GradientToArrays();

			material.SetInt(ShaderProp.GradientColorCount, _gradient.colorKeys.Length);
			material.SetInt(ShaderProp.GradientAlphaCount, _gradient.alphaKeys.Length);
			material.SetVectorArray(ShaderProp.GradientColors, _colorKeys);
			material.SetVectorArray(ShaderProp.GradientAlphas, _alphaKeys);
			material.SetVector(ShaderProp.GradientTransform, new Vector4(_scale, _scalePivot, _offset, (float)_wrapMode));
			//material.SetVector(ShaderProp.GradientRadial, new Vector4(_centerX, _centerY, _radius, 0f));
			//material.SetFloat(ShaderProp.GradientDither, Mathf.Lerp(0f, 0.05f, _dither));

			// Mixing mode
			switch (_mixMode)
			{
				default:
				case GradientMix.Smooth:
				material.DisableKeyword(ShaderKeyword.GradientMixLinear);
				material.DisableKeyword(ShaderKeyword.GradientMixStep);
				material.EnableKeyword(ShaderKeyword.GradientMixSmooth);
				break;
				case GradientMix.Linear:
				material.DisableKeyword(ShaderKeyword.GradientMixStep);
				material.DisableKeyword(ShaderKeyword.GradientMixSmooth);
				material.EnableKeyword(ShaderKeyword.GradientMixLinear);
				break;
				case GradientMix.Step:
				material.DisableKeyword(ShaderKeyword.GradientMixSmooth);
				material.DisableKeyword(ShaderKeyword.GradientMixLinear);
				material.EnableKeyword(ShaderKeyword.GradientMixStep);
				break;
			}

			// Mixing color space
			switch (_colorSpace)
			{
				default:
				case GradientColorSpace.sRGB:
				material.DisableKeyword(ShaderKeyword.GradientColorSpaceLinear);
				material.DisableKeyword(ShaderKeyword.GradientColorSpacePerceptual);
				material.EnableKeyword(ShaderKeyword.GradientColorSpaceSRGB);
				break;
				case GradientColorSpace.Linear:
				material.DisableKeyword(ShaderKeyword.GradientColorSpaceSRGB);
				material.DisableKeyword(ShaderKeyword.GradientColorSpacePerceptual);
				material.EnableKeyword(ShaderKeyword.GradientColorSpaceLinear);
				break;
				case GradientColorSpace.Perceptual:
				material.DisableKeyword(ShaderKeyword.GradientColorSpaceSRGB);
				material.DisableKeyword(ShaderKeyword.GradientColorSpaceLinear);
				material.EnableKeyword(ShaderKeyword.GradientColorSpacePerceptual);
				break;
			}
		}
	}
#endif
}