//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEngine;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	/// <summary>
	/// The blending mode to use when combining two colors together
	/// </summary>
	public enum BlendMode
	{
		/// <summary>`Source` - Only use the original color, this ignores any trail gradient/alpha settings.</summary>
		Source,
		/// <summary>`Replace` - Ignore the original color and replace with the trail gradient/alpha settings.<br/>`Replace_Multiply`</summary>
		Replace,
		/// <summary>`Replace_Multiply` - Same as `Replace` for RGB, but multiply the original alpha with the trail gradient alpha.</summary>
		Replace_Multiply,
		/// <summary>`Multiply` - Multiply the original color with the trail gradient/alpha settings.</summary>
		Multiply,
		/// <summary>`Add_Multiply` - Add the original color RGB to the gradient gradient, but multiply the alpha value.</summary>
		Add_Multiply,
	}

	public enum BuiltInGradient
	{
		SoftRainbow,
		Grey80ToClear,
	}

	[UnityInternal.ExcludeFromDocs]
	public static class ColorUtils
	{
		private static Gradient s_softRainbowGradient;
		private static Gradient s_grey80ToClearGradient;

		[UnityInternal.ExcludeFromDocs]
		public static Gradient GetBuiltInGradient(BuiltInGradient b)
		{
			Gradient result = null;
			switch (b)
			{
				case BuiltInGradient.SoftRainbow:
				result = s_softRainbowGradient;
				break;
				case BuiltInGradient.Grey80ToClear:
				result = s_grey80ToClearGradient;
				break;
			}

			return CloneGradient(result);
		}

		static ColorUtils()
		{
			{
				GradientColorKey[] keys = new GradientColorKey[5];
				keys[0].time = 0f;
				keys[0].color = new Color(0.03116761f, 0.9716981f, 0.5848317f);
				keys[1].time = 0.25f;
				keys[1].color = new Color(0.3635457f, 0.4478632f, 0.8679245f);
				keys[2].time = 0.5f;
				keys[2].color = new Color(0.8301887f, 0.4025632f, 0.829066f);
				keys[3].time = 0.75f;
				keys[3].color = new Color(0.8490566f, 0.1874332f, 0.5700046f);
				keys[4].time = 1f;
				keys[4].color = new Color(0.8207547f, 0.7230087f, 0.1966714f);
				GradientAlphaKey[] alpha = new GradientAlphaKey[2];
				alpha[0].time = 0f;
				alpha[0].alpha = 1f;
				alpha[1].time = 1f;
				alpha[1].alpha = 1f;
				s_softRainbowGradient = new Gradient();
				s_softRainbowGradient.SetKeys(keys, alpha);
			}
			{
				GradientColorKey[] keys = new GradientColorKey[2];
				keys[0].time = 0f;
				keys[0].color = Color.white * 0.8f;
				keys[1].time = 1f;
				keys[1].color = Color.white * 0.8f;
				GradientAlphaKey[] alpha = new GradientAlphaKey[2];
				alpha[0].time = 0f;
				alpha[0].alpha = 1f;
				alpha[1].time = 1f;
				alpha[1].alpha = 0f;
				s_grey80ToClearGradient = new Gradient();
				s_grey80ToClearGradient.SetKeys(keys, alpha);
			}
		}

		public static Gradient CloneGradient(Gradient gradient)
		{
			Gradient result = new Gradient();
			var gradientRead = new GradientRead(gradient);
			gradientRead.Write(result);
			return result;
		}

		public static Color Blend(Color a, Color b, BlendMode mode)
		{
			switch (mode)
			{
				case BlendMode.Source:
				return a;
				case BlendMode.Replace:
				return b;
				case BlendMode.Replace_Multiply:
				{
					b.a *= a.a;
					return b;
				}
				case BlendMode.Multiply:
				return a * b;
				case BlendMode.Add_Multiply:
				{
					a.r += b.r;
					a.g += b.g;
					a.b += b.b;
					a.a *= b.a;
					return a;
				}
			}
			return a;
		}	
		
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
		
		public static Vector3 LinearToOklab(Color c)
		{
			float l = 0.4122214708f * c.r + 0.5363325363f * c.g + 0.0514459929f * c.b;
			float m = 0.2119034982f * c.r + 0.6806995451f * c.g + 0.1073969566f * c.b;
			float s = 0.0883024619f * c.r + 0.2817188376f * c.g + 0.6299787005f * c.b;

			float l_ = cbrtf(l);
			float m_ = cbrtf(m);
			float s_ = cbrtf(s);

			//float l_ = Mathf.Pow(l, 1f / 3f);
			//float m_ = Mathf.Pow(m, 1f / 3f);
			//float s_ = Mathf.Pow(s, 1f / 3f);

			return new Vector3 {
				x = 0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_,
				y = 1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_,
				z = 0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_
			};
		}

		// If using .NET core, please use System.Math.Cbrt for cuberoot
		private static float cbrtf(float v)
		{
			return Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), 1f / 3f);
		}

		// Certain color values are agnostic of gamma/linear color-space conversion, such as Color.white, Color.black, Color.red etc..
		internal static bool IsColorGammaLinearConversionAgnostic(Color c)
		{
			return ((c.r == 0f || c.r == 1f) && (c.g == 0f || c.g == 1f) && (c.b == 0f || c.b == 1f));
		}

		internal static void ConvertMeshVertexColorsToLinear(Mesh mesh, ref List<Color> colorCache)
		{
			int vertexCount = mesh.vertexCount;
			if (colorCache != null && colorCache.Count != vertexCount)
			{
				colorCache = null;
			}
			if (colorCache == null)
			{
				colorCache = new List<Color>(vertexCount);
			}
			mesh.GetColors(colorCache);

			// In some rare cases there can be no colors
			if (colorCache.Count > 0)
			{
				Debug.Assert(colorCache.Count == vertexCount);
				for (int i = 0; i < vertexCount; i++)
				{
					colorCache[i] = colorCache[i].linear;
				}
				mesh.SetColors(colorCache);
			}
		}
	}
}