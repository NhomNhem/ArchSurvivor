using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sisus.Init.Internal;
using UnityEditor;
using UnityEngine;
using static Sisus.NullExtensions;
using Object = UnityEngine.Object;

namespace Sisus.Init.EditorOnly.Internal
{
    internal sealed class ServicesWindow : EditorWindow
    {
		private Vector2 scrollPosition;
		private string filter = "";

		[MenuItem("Window/Analysis/Service Debugger", priority = 10000)]
        public static void Open()
        {
            var window = GetWindow<ServicesWindow>();
            window.titleContent = new GUIContent("Service Debugger");
        }

		private readonly HashSet<object> shown = new HashSet<object>();

		private void OnGUI()
		{
			filter = EditorGUILayout.TextField(filter, EditorStyles.toolbarSearchField);

			scrollPosition = GUILayout.BeginScrollView(scrollPosition);

			var iconSizeWas = EditorGUIUtility.GetIconSize();
			EditorGUIUtility.SetIconSize(new Vector2(16f, 16f));
			Texture scriptIcon = EditorGUIUtility.ObjectContent(null, typeof(MonoScript)).image;

			shown.Clear();

			GUIContent label = new GUIContent();
            foreach(var definingTypeAndService in ServiceInjector.services)
			{
				object service = definingTypeAndService.Value;

				if(service == Null || !shown.Add(service))
				{
					continue;
				}

				Type concreteType = service.GetType();
				if(concreteType.IsGenericType)
				{
					var typeDefinition = concreteType.GetGenericTypeDefinition();
					if(typeDefinition == typeof(Task<>))
					{
						concreteType = concreteType.GetGenericArguments()[0];
					}
				}

				Type definingType = definingTypeAndService.Key;
				if(concreteType == definingType || concreteType == typeof(object))
				{
					label.text = definingType.Name;
				}
				else
				{
					label.text = concreteType.Name + " (" + definingType.Name + ")";
				}

				if(filter.Length > 0 && label.text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) == -1)
				{
					continue;
				}

				GUILayout.BeginHorizontal();

				if(TryGetUnityObject(service, false, out Object unityObject))
				{
					label.image = EditorGUIUtility.ObjectContent(unityObject, concreteType).image;
				}
				else
				{
					label.image = scriptIcon;
				}

				if(GUILayout.Button(label, EditorStyles.label))
				{
					switch(Event.current.button)
					{
						case 0:
							PingService(service);
							SelectService(service);
							break;
						case 1:
							var menu = new GenericMenu();
							menu.AddItem(new GUIContent("Ping"), false, ()=>PingService(service));
							menu.AddItem(new GUIContent("Find Clients In Scenes"), false, () => ServiceTagUtility.SelectAllReferencesInScene(service));
							menu.ShowAsContext();
							break;
					}
				}

				GUILayout.EndHorizontal();
			}

			EditorGUIUtility.SetIconSize(iconSizeWas);

			GUILayout.EndScrollView();
        }

		private static void SelectService(object service)
		{
			if(TryGetUnityObject(service, true, out Object unityObject))
			{
				if(unityObject is Component component)
				{
					Selection.activeObject = component.gameObject;
				}
				else
				{
					Selection.activeObject = unityObject;
				}
			}
		}

		private static void PingService(object service)
		{
			if(TryGetUnityObject(service, true, out Object unityObject))
			{
				EditorGUIUtility.PingObject(unityObject);
			}
		}

		private static bool TryGetUnityObject(object obj, bool includingMonoScript, out Object unityObject)
		{ 
			unityObject = obj as Object;
			if(unityObject != null)
			{
				return true;
			}
			
			if(Find.WrapperOf(obj, out var wrapper))
			{
				unityObject = wrapper as Object;
				return unityObject != null;
			}

			if(includingMonoScript && Find.Script(obj.GetType(), out var script))
			{
				unityObject = script;
				return true;
			}

			return false;
		}
	}
}