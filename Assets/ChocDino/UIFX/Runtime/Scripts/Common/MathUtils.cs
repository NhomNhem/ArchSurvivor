//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	public enum FrustmIntersectResult
	{
		/// <summary>The object is completely outside of the planes.</summary>
		Out,
		/// <summary>The object is completely inside of the planes.</summary
		In,
		/// <summary>The object is partially intersecting the planes.</summary>
		Partial,
	}

	[UnityInternal.ExcludeFromDocs]
	public static class MathUtils
	{
		public static FrustmIntersectResult GetFrustumIntersectsOBB(Plane[] planes, Vector3[] points)
		{
			Debug.Assert(planes != null);
			Debug.Assert(planes.Length == 6);
			Debug.Assert(points != null);
			Debug.Assert(points.Length == 8);

			FrustmIntersectResult result = FrustmIntersectResult.In;
			for (int j = 0; j < 6; j++)
			{
				var plane = planes[j];
				int inCount = 0;
				int outCount = 0;
				for (int i = 0; i < 8 && (inCount == 0 || outCount == 0); i++)
				{
					// NOTE: We could use !s_planes[j].GetSide(s_boundsPoints[i]); but this doesn't allow 
					// for points being ON the plane, which is a not uncommon scenario.
					bool isBehindPlane = (Vector3.Dot(plane.normal, points[i]) + plane.distance) < 0f;
					if (isBehindPlane)
					{
						outCount++;
					}
					else
					{
						inCount++;
					}
				}

				if (inCount == 0)
				{
					result = FrustmIntersectResult.Out;
					break;
				}
				else if (outCount != 0)
				{
					result = FrustmIntersectResult.Partial;
					// NOTE: Don't break, keep looking, the object may still be outside other planes
				}
			}
			return result;
		}
		
		[UnityInternal.ExcludeFromDocs]
		public static float Snap(float v, float snap)
		{
			float isnap = 1f / snap;
			return Mathf.FloorToInt(v * isnap) / isnap;
		}

		/// <summary> 
		/// Adds padding to a number and then rounds up to the nearest multiple.
		/// This is useful for textures to ensure they have constant minimum padding amount, but also have a width/height that is a multiple size.
		/// This can allow a texture size that is frequently changing slightly (eg when filter sizes change) to not reallocate too frequently, and
		/// can stabilise flickering caused when downsampling very small which can cause textures to oscilate between odd/even sizes causing the
		/// texture sampling is jump around between frames and flicker.
		/// Eg params [9,10,10] = 20, [10,10,10] = 20, [11,10,10] = 30
		/// <summary>
		[UnityInternal.ExcludeFromDocs]
		public static int PadAndRoundToNextMultiple(float v, int pad, int multiple)
		{
			multiple = Mathf.Max(1, multiple);
			int result = Mathf.CeilToInt(((float)System.Math.Ceiling((v + pad) / multiple)) * multiple);
			Debug.Assert(result >= v);
			Debug.Assert((result % multiple) == 0);
			return result;
		}

		[UnityInternal.ExcludeFromDocs]
		// Based on https://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/
		public static float GetDampLerpFactor(float lambda, float deltaTime)
		{
			return 1f - Mathf.Exp(-lambda * deltaTime);
		}

		[UnityInternal.ExcludeFromDocs]
		public static float DampTowards(float a, float b, float lambda, float deltaTime)
		{
			return Mathf.Lerp(a, b, GetDampLerpFactor(lambda, deltaTime));
		}

		[UnityInternal.ExcludeFromDocs]
		public static Vector2 DampTowards(Vector2 a, Vector2 b, float lambda, float deltaTime)
		{
			return Vector2.Lerp(a, b, GetDampLerpFactor(lambda, deltaTime));
		}

		[UnityInternal.ExcludeFromDocs]
		public static Vector3 DampTowards(Vector3 a, Vector3 b, float lambda, float deltaTime)
		{
			return Vector3.Lerp(a, b, GetDampLerpFactor(lambda, deltaTime));
		}

		[UnityInternal.ExcludeFromDocs]
		public static Vector4 DampTowards(Vector4 a, Vector4 b, float lambda, float deltaTime)
		{
			return Vector4.Lerp(a, b, GetDampLerpFactor(lambda, deltaTime));
		}

		[UnityInternal.ExcludeFromDocs]
		public static Color DampTowards(Color a, Color b, float lambda, float deltaTime)
		{
			return Color.Lerp(a, b, GetDampLerpFactor(lambda, deltaTime));
		}

		[UnityInternal.ExcludeFromDocs]
		public static Matrix4x4 DampTowards(Matrix4x4 a, Matrix4x4 b, float lambda, float deltaTime)
		{
			float t = GetDampLerpFactor(lambda, deltaTime);
			return Matrix4x4.identity;
		}

		[UnityInternal.ExcludeFromDocs]
		public static Matrix4x4 LerpUnclamped(Matrix4x4 a, Matrix4x4 b, float t, bool preserveScale)
		{
			Vector3 targetScale = Vector3.zero;
			if (preserveScale)
			{
				targetScale = Vector3.LerpUnclamped(a.lossyScale, b.lossyScale, t);
			}

			Matrix4x4 result = new Matrix4x4();
			result.SetColumn(0, Vector4.LerpUnclamped(a.GetColumn(0), b.GetColumn(0), t));
			result.SetColumn(1, Vector4.LerpUnclamped(a.GetColumn(1), b.GetColumn(1), t));
			result.SetColumn(2, Vector4.LerpUnclamped(a.GetColumn(2), b.GetColumn(2), t));
			result.SetColumn(3, Vector4.LerpUnclamped(a.GetColumn(3), b.GetColumn(3), t));

			if (preserveScale)
			{
				Vector3 scale = result.lossyScale;
				result *= Matrix4x4.Scale(new Vector3(targetScale.x / scale.x, targetScale.y / scale.y, targetScale.z / scale.z));
			}

			return result;
		}

		[UnityInternal.ExcludeFromDocs]
		public static void LerpUnclamped(ref Matrix4x4 result, Matrix4x4 b, float t, bool preserveScale)
		{
			Vector3 targetScale = Vector3.zero;
			if (preserveScale)
			{
				targetScale = Vector3.LerpUnclamped(result.lossyScale, b.lossyScale, t);
			}

			result.SetColumn(0, Vector4.LerpUnclamped(result.GetColumn(0), b.GetColumn(0), t));
			result.SetColumn(1, Vector4.LerpUnclamped(result.GetColumn(1), b.GetColumn(1), t));
			result.SetColumn(2, Vector4.LerpUnclamped(result.GetColumn(2), b.GetColumn(2), t));
			result.SetColumn(3, Vector4.LerpUnclamped(result.GetColumn(3), b.GetColumn(3), t));

			if (preserveScale)
			{
				Vector3 scale = result.lossyScale;
				result *= Matrix4x4.Scale(new Vector3(targetScale.x / scale.x, targetScale.y / scale.y, targetScale.z / scale.z));
			}
		}

		/// <summary>
		/// Lerp between 3 values (a, b, c) using t with range [0..1]
		/// </summary>
		[UnityInternal.ExcludeFromDocs]
		public static float Lerp3(float a, float b, float c, float t)
		{
			// TODO: optimise this
			t *= 2.0f;
			float w1 = 1f - Mathf.Clamp01(t);
			float w2 = 1f - Mathf.Abs(1f - t);
			float w3 = Mathf.Clamp01(t-1f);
			return a * w1 + b * w2 + c * w3;
		}

		[UnityInternal.ExcludeFromDocs]
		public static bool HasMatrixChanged(Matrix4x4 a, Matrix4x4 b, bool ignoreTranslation)
		{
			// First check translation
			if (!ignoreTranslation)
			{
				if (a.m03 != b.m03 || a.m13 != b.m13 || a.m23 != b.m23)
				{
					return true;
				}
			}
			// Check the rest
			if (a.m00 != b.m00 || a.m01 != b.m01 || a.m02 != b.m02 ||
				a.m10 != b.m10 || a.m11 != b.m11 || a.m12 != b.m12 ||
				a.m20 != b.m20 || a.m21 != b.m21 || a.m22 != b.m22 ||
				a.m30 != b.m30 || a.m31 != b.m31 || a.m32 != b.m32 || a.m33 != b.m33)
			{
				return true;
			}
			return false;
		}
		
		[UnityInternal.ExcludeFromDocs]
		public static void CreateRandomIndices(ref int[] array, int length)
		{
			// Only recreate if required to grow
			if (array == null || array.Length < length)
			{
				array = new int[length];
			}
			
			// Populate
			for (int i = 0; i < length; i++)
			{
				array[i] = i;
			}

			// Shuffle
			for (int i = 0; i < length; i++)
			{
				int a = Random.Range(0, length);
				int b = Random.Range(0, length);
				if (a != b)
				{
					int t = array[a];
					array[a] = array[b];
					array[b] = t;
				}
			}
		}

		/// <summary>
		/// Given two rectangles in absolute coordinates, return the rectangle such that if src is remapped to range [0..1] relative to dst
		/// If src and dst are the same, rect(0, 0, 1, 1) will be returned
		/// If src is within dst, then rect values will be > 0 and < 1
		/// If src is larger than dst, rect values will be < 0 and > 1
		/// The returned rect could be used to offset and scale UV coordinates from one quad to another
		/// </summary>
		public static Rect GetRelativeRect(Rect src, Rect dst)
		{
			Rect r = Rect.zero;
			r.x = (src.x - dst.x) / dst.width;
			r.y = (src.y - dst.y) / dst.height;
			r.width = src.width / dst.width;
			r.height = src.height / dst.height;
			return r;
		}

		/// <summary>Move rect horizontally so that specific point along it's width matches the equivelent point along target's width. This is useful for snapping the ege of a rectangle to the edge of another.</summary>
		private static Rect SnapRectToRectHoriz(Rect rect, Rect target, float sizeT)
		{
			float posA = Mathf.LerpUnclamped(target.xMin, target.xMax, sizeT);
			float posB = Mathf.LerpUnclamped(rect.xMin, rect.xMax, sizeT);

			rect.x += (posA - posB);

			return rect;
		}

		/// <summary>Move rect vertically so that specific point along it's height matches the equivelent point along target's height. This is useful for snapping the ege of a rectangle to the edge of another.</summary>
		private static Rect SnapRectToRectVert(Rect rect, Rect target, float sizeT)
		{
			float posA = Mathf.LerpUnclamped(target.yMin, target.yMax, sizeT);
			float posB = Mathf.LerpUnclamped(rect.yMin, rect.yMax, sizeT);

			rect.y += (posA - posB);

			return rect;
		}

		/// <summary>
		/// Snap one rectangle to the edge of another (or fractional positions between)
		/// widthT 0 is left, widthT 1 is right
		/// heightT 0 is bottom, heightT 1 is top
		/// </summary>
		public static Rect SnapRectToRectEdges(Rect rect, Rect target, bool applyWidth, bool applyHeight, float widthT, float heightT)
		{
			if (applyWidth)
			{
				rect = SnapRectToRectHoriz(rect, target, widthT);
			}
			if (applyHeight)
			{
				rect = SnapRectToRectVert(rect, target, heightT);
			}
			return rect;
		}

		/// <summary>Return rectangle of aspect ratio using the scaling mode</summary>
		public static Rect ResizeRectToAspectRatio(Rect rect, ScaleMode scaleMode, float aspect)
		{
			float srcAspect = aspect;
			float dstAspect = rect.width / rect.height;
			
			float stretch = 1f;
			// src is wider than dst
			if (srcAspect > dstAspect)
			{
				stretch = dstAspect / srcAspect;
			}
			else
			{
				stretch = srcAspect / dstAspect;
			}

			Rect result = rect;
			switch (scaleMode)
			{
				case ScaleMode.StretchToFill:
				break;
				case ScaleMode.ScaleAndCrop:
				{
					// src is wider than dst
					if (srcAspect > dstAspect)
					{
						float newWidth = rect.width / stretch;
						result = new Rect(rect.xMin - (newWidth - rect.width) * 0.5f, rect.yMin, newWidth, rect.height);
					}
					else
					{
						float newHeight = rect.height / stretch;
						result = new Rect(rect.xMin, rect.yMin - (newHeight - rect.height) * 0.5f , rect.width, newHeight);
					}
				}
				break;
				case ScaleMode.ScaleToFit:
				{
					// src is wider than dst
					if (srcAspect > dstAspect)
					{
						result = new Rect(rect.xMin, rect.yMin + rect.height * (1f - stretch) * 0.5f, rect.width, stretch * rect.height);
					}
					else
					{
						result = new Rect(rect.xMin + rect.width * (1f - stretch) * 0.5f, rect.yMin, stretch * rect.width, rect.height);
					}
				}
				break;
			}
			return result;
		}
	}
}