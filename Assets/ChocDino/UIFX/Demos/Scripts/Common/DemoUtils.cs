//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChocDino.UIFX.Demos
{
	internal static class DemoUtils
	{
		internal static string GetUrlParameter(string url, string paramName)
		{
			string result = string.Empty;
			int paramsIndex = url.IndexOf('?');
			if (paramsIndex > 0)
			{
				paramName +="=";
				Debug.Log("1 " + paramsIndex + " " + paramName);
				int keyIndex = url.IndexOf(paramName, paramsIndex);
				if (keyIndex > 0)
				{
					Debug.Log("2 " + paramsIndex);
					keyIndex += paramName.Length;
					int endIndex = url.IndexOf('&', keyIndex);
					if (endIndex < 0) { endIndex = url.Length;}
					int length = endIndex - keyIndex;
					if (length > 1)
					{
						result = url.Substring(keyIndex, endIndex - keyIndex);
					}
				}
			}
			return result;
		}
		internal static float EaseCubic(float t)
		{
			return Mathf.Pow(t, 3f);
		}

		internal static float EaseExpo(float t)
		{
			float result = 0f;
			if (t != 0f)
			{
				result = Mathf.Pow(2f, 10f * (t - 1f));
			}
			return result;
		}

		internal static float InOutCubic(float t, float p = 0.5f)
		{
			float result = 0f;
			if (t > 0f)
			{
				result = 1f;
				if (t < 1f)
				{
					if (t < p)
					{
						// convert t to [0..1] range and scale result to [0..p] range
						result = EaseCubic(t / p) * p;
					}
					else
					{
						// convert t to [0..1] range and scale result to [p..1] range
						result = (1f - (EaseCubic(1f - ((t - p) / (1f - p))))) * (1f - p) + p;
					}
				}
			}
			return result;
		}

		internal static float InOutExpo(float t, float p = 0.5f)
		{
			float result = 0f;
			if (t > 0f)
			{
				result = 1f;
				if (t < 1f)
				{
					if (t < p)
					{
						// convert t to [0..1] range and scale result to [0..p] range
						result = EaseExpo(t / p) * p;
					}
					else
					{
						// convert t to [0..1] range and scale result to [p..1] range
						result = (1f - (EaseExpo(1f - ((t - p) / (1f - p))))) * (1f - p) + p;
					}
				}
			}
			return result;
		}
	}
}