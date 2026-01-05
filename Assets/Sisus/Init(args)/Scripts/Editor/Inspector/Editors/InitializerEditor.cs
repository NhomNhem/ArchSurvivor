using System;
using System.Linq;
using JetBrains.Annotations;
using Sisus.Init.Internal;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus.Init.EditorOnly.Internal
{
    using static InitializerEditorUtility;

    [CanEditMultipleObjects]
	internal class InitializerEditor : Editor
	{
        protected internal const string InitArgumentMetadataClassName = InitializerUtility.InitArgumentMetadataClassName;

        private SerializedProperty client;
        private SerializedProperty nullArgumentGuard;
        private GUIContent clientLabel;
        private bool clientIsInitializable;
        private bool hasServiceArguments;
        private InitializerDrawer drawer;
        [NonSerialized]
        private AnyDrawer[] propertyDrawerData = new AnyDrawer[0];
        private Type clientType;
        private bool drawNullArgumentGuard;

        protected virtual Type[] GetGenericArguments() => target.GetType().BaseType.GetGenericArguments();
        protected virtual Type GetClientType(Type[] genericArguments) => genericArguments[0];
        protected virtual Type[] GetInitArgumentTypes(Type[] genericArguments) => genericArguments.Skip(1).ToArray();

        private static bool ServicesShown => InitializerDrawer.ServicesShown;

        private bool IsNullAllowed
        {
            get
            {
                if(!drawNullArgumentGuard)
				{
                    return true;
				}

                var nullGuard = (NullArgumentGuard)nullArgumentGuard.intValue;
                return !nullGuard.IsEnabled(Application.isPlaying ? NullArgumentGuard.RuntimeException : NullArgumentGuard.EditModeWarning)
                   || (!nullGuard.IsEnabled(NullArgumentGuard.EnabledForPrefabs) && PrefabUtility.IsPartOfPrefabAsset(target));
            }
        }

        internal void Setup([CanBeNull] InitializerDrawer externalDrawer)
        {
            client = serializedObject.FindProperty("target");
            nullArgumentGuard = serializedObject.FindProperty(nameof(nullArgumentGuard));
            drawNullArgumentGuard = nullArgumentGuard != null;

            var genericArguments = GetGenericArguments();
            clientType = GetClientType(genericArguments);
            clientIsInitializable = IsInitializable(clientType);
            clientLabel = GetClientLabel();
            var initializers = targets;
            int count = targets.Length;
            var clients = new Object[count];
            for(int i = 0; i < count; i++)
			{
                var initializer = initializers[i] as IInitializer;
                clients[i] = initializer.Target;

                if(i == 0 && initializer is IInitializerEditorOnly initializerEditorOnly && !initializerEditorOnly.ShowNullArgumentGuard)
				{
                    drawNullArgumentGuard = false;
				}
            }

            if(externalDrawer is null)
            {
                var initArguments = GetInitArgumentTypes(genericArguments);
                drawer = new InitializerDrawer(clientType, clients, initArguments, this);
            }

            Setup(genericArguments, externalDrawer ?? drawer);
            hasServiceArguments = propertyDrawerData.Any(d => d.isService);

            GUIContent GetClientLabel()
			{
                var result = GetLabel(clientType);
                if(typeof(StateMachineBehaviour).IsAssignableFrom(clientType))
				{
                    result.text = "None (Animator → " + result.text + ")";
                }
                else if(typeof(ScriptableObject).IsAssignableFrom(clientType))
				{
                    result.text = "None (" + result.text + ")";
                }
                else
                {
                    result.text = "New Instance (" + result.text + ")";
                }

                return result;
            }
        }

        protected virtual void Setup(Type[] genericArguments, InitializerDrawer internalOrExternalDrawer)
        {
            var argumentTypes = GetInitArgumentTypes(genericArguments);
            DisposePropertyDrawerData();
            propertyDrawerData = GetPropertyDrawerData(clientType, argumentTypes, internalOrExternalDrawer);
        }

        private AnyDrawer[] GetPropertyDrawerData(Type clientType, Type[] argumentTypes, InitializerDrawer internalOrExternalDrawer)
            => InitializerEditorUtility.GetPropertyDrawerData(serializedObject, clientType, argumentTypes, internalOrExternalDrawer);

        public override void OnInspectorGUI()
		{
            serializedObject.Update();

			if(client == null)
			{
				Setup(null);
			}

			if(drawer == null)
			{
				return;
			}

			var rect = EditorGUILayout.GetControlRect();
            rect.y -= 2f;

            // Tooltip for icon must be drawn before drawer.OnInspectorGUI for it to
            // take precedence over Init header tooltip.
            var iconRect = rect;
            iconRect.x = EditorGUIUtility.labelWidth - 1f;
            iconRect.y += 5f;
            iconRect.width = 20f;
            iconRect.height = 20f;
            GUI.Label(iconRect, GetReferenceTooltip(client.serializedObject.targetObject, client.objectReferenceValue, clientIsInitializable));

            GUILayout.Space(-EditorGUIUtility.singleLineHeight - 2f);
			drawer.OnInspectorGUI();

            if(client.objectReferenceValue is Component)
            {
			    DrawClientField(rect, client, clientLabel, clientIsInitializable, hasServiceArguments);
            }

            serializedObject.ApplyModifiedProperties();
		}

		public void DrawArgumentFields()
        {
            if(client == null)
            {
                Setup(null);
            }

            foreach(var data in propertyDrawerData)
			{
                data.DrawArgumentField(IsNullAllowed, ServicesShown);
			}
        }

        private void OnDisable()
		{
			DisposePropertyDrawerData();
			DisposeInitializerDrawer();
		}

		private void DisposeInitializerDrawer()
		{
			if(drawer is null)
			{
				return;
			}

			drawer.Dispose();
			drawer = null;
		}

		private void DisposePropertyDrawerData()
		{
			for(int i = 0, count = propertyDrawerData.Length; i < count; i++)
			{
				propertyDrawerData[i].Dispose();
			}

            propertyDrawerData = Array.Empty<AnyDrawer>();
		}
	}
}