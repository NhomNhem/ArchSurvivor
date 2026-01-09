//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace ChocDino.UIFX
{
	internal static class Log
	{
		private static int _lastFrameLog = -1;
		private static string _lastCaller;
		private static string _lastMessage;

 		[System.Diagnostics.Conditional("UIFX_LOG")]
		[MethodImpl(MethodImplOptions.NoInlining)]  //This will prevent inlining by the complier.
		internal static void LOG(string message, Object obj = null, LogType type = LogType.Log, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
		{
			//bool isDuplicateMessage = (_lastMessage == message && _lastCaller == callerName);
			//if (!isDuplicateMessage)
			{
				if (Time.frameCount != _lastFrameLog)
				{
					_lastFrameLog = Time.frameCount;
					UnityEngine.Debug.Log(string.Format("<color=yellow>-------------------------------------------------------- NEW FRAME {0}</color>", _lastFrameLog));
					_lastCaller = string.Empty;
					_lastMessage = string.Empty;
				}
			}

			if (obj == null)
			{
				StackTrace stackTrace = new StackTrace(1, false);
				if (stackTrace != null)
				{
					var frame = stackTrace.GetFrame(0);
					if (frame != null)
					{
						var objectType = frame.GetMethod().DeclaringType;
						if (objectType != null)
						{
							callerName = objectType.Name + "::" + callerName;
						}
					}
				}
			}
			else
			{
				callerName = obj.GetType().Name + "::" + callerName;
			}

			callerName += "()";

			// To improve readability (make subsequent logs from the same caller more obvious), remove the caller text if it's repeated
			if (callerName == _lastCaller)
			{
				callerName = string.Empty;
			}
			else
			{
				_lastCaller = callerName;
			}
			
			_lastMessage = message;

			//string frame = _lastFrameLog.ToString().PadLeft(4).Substring(0, 4);
			callerName = callerName.PadRight(48).Substring(0, 48);
			string output = string.Format("{0} <color=white>{1}</color>", callerName, message);
			UnityEngine.Debug.Log(output);
		}
	}
}