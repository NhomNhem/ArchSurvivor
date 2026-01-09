//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

#if UNITY_2020_1_OR_NEWER
	#define UNITY_UI_PREMULTIPLIED
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChocDino.UIFX
{
	public static class Matrix4x4Helper
	{
		public static Matrix4x4 Rotate(Quaternion rotation)
		{
			#if UNITY_2017_1_OR_NEWER
			return Matrix4x4.Rotate(rotation);
			#else
			return Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
			#endif
		}
	}

	public static class ObjectHelper
	{
		public static void Destroy<T>(ref T obj) where T : Object
		{
			if (obj)
			{
				Destroy(obj);
				obj = null;
			}
		}

		public static void Destroy<T>(T obj) where T : Object
		{
			if (obj)
			{
				#if UNITY_EDITOR
				if (!Application.isPlaying)
				{
					Object.DestroyImmediate(obj);
				}
				else
				#endif
				{
					Object.Destroy(obj);
				}
			}
		}

		public static void Dispose<T>(ref T obj) where T : System.IDisposable
		{
			if (obj != null)
			{
				obj.Dispose();
				obj = default(T);
			}
		}

		public static bool ChangeProperty<T>(ref T backing, T value) where T : struct
		{
			if (!(System.Collections.Generic.EqualityComparer<T>.Default.Equals(backing, value)))
			{
				backing = value;
				return true;
			}
			return false;
		}

		public static void ChangeProperty<T>(ref T backing, T value, ref bool hasChanged) where T : struct
		{
			if (!(System.Collections.Generic.EqualityComparer<T>.Default.Equals(backing, value)))
			{
				backing = value;
				hasChanged = true;
			}
			hasChanged = false;
		}
	}

	public static class RenderTextureHelper
	{
		public static void ReleaseTemporary(ref RenderTexture rt)
		{
			if (rt)
			{
				RenderTexture.ReleaseTemporary(rt); rt = null;
			}
		}
	}

	public static class VertexHelperExtensions
	{
		public static void ReplaceUIVertexTriangleStream(this VertexHelper vh, List<UIVertex> vertices)
		{
			// NOTE: despite its name, this method actually replaces the vertices, it doesn't add to them
			vh.AddUIVertexTriangleStream(vertices);
		}
	}

	public static class MaterialHelper
	{
		public static bool MaterialOutputsPremultipliedAlpha(Material material)
		{
			bool result = false;
			Debug.Assert(material != null);
			if (material)
			{
				// Detect use of "UI/Default" shader
				if (material.shader == Graphic.defaultGraphicMaterial.shader)
				{
					#if UNITY_UI_PREMULTIPLIED
					return true;
					#else
					return false;
					#endif
				}
				
				if (material.HasProperty(UnityShaderProp.BlendSrc) && material.HasProperty(UnityShaderProp.BlendDst))
				{
					result = ((material.GetInt(UnityShaderProp.BlendSrc) == (int)UnityEngine.Rendering.BlendMode.One) && (material.GetInt(UnityShaderProp.BlendDst) == (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
					return result;
				}

				string tag = material.GetTag("OutputsPremultipliedAlpha", false, string.Empty);
				if (!string.IsNullOrEmpty(tag))
				{
					result = (tag.ToLower() == "true");
					return result;
				}
			}

			// Assume the output is pre-multiplied if Unity version > 2020
			// But we can't assume this for non-unity shaders so we're just guessing at this stage..
			#if UNITY_UI_PREMULTIPLIED
			result = true;
			#endif

			return result;
		}
	}

	public static class AnimationCurveHelper
	{
		public static bool HasOutOfRangeValues(this AnimationCurve curve, float timeMin, float timeMax, float valueMin, float valueMax = float.MaxValue)
		{
			bool result = false;
			foreach (var key in curve.keys)
			{
				if (key.time < timeMin || key.time > timeMax || key.value < valueMin || key.value > valueMax)
				{
					result = true;
					break;
				}
			}
			return result;
		}
	}

	public static class EditorHelper
	{
		#if UNITY_EDITOR
		public static bool IsEditingCurve()
		{
			return IsInteractingWithWindow("CurveEditorWindow");
		}

		private static bool IsInteractingWithWindow(string windowName)
		{
			bool result = false;
			var window = UnityEditor.EditorWindow.focusedWindow;
			if (window)
			{
				result = window.GetType().ToString().Contains(windowName);
			}
			if (!result)
			{
				window = UnityEditor.EditorWindow.mouseOverWindow;
				if (window)
				{
					result = window.GetType().ToString().Contains(windowName);
				}
			}
			return result;
		}
		#endif

		public static bool IsInContextPrefabMode()
		{
			bool result = false;
			#if UNITY_EDITOR
			#if UNITY_2021_2_OR_NEWER
			var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null && stage.mode == UnityEditor.SceneManagement.PrefabStage.Mode.InContext)
			{
				result = true;
			}
			#elif UNITY_2020_1_OR_NEWER
			var stage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null && stage.mode == UnityEditor.Experimental.SceneManagement.PrefabStage.Mode.InContext)
			{
				result = true;
			}
			#endif
			#endif
			return result;
		}
	}
}