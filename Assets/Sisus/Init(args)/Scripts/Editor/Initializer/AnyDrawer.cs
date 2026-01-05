using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if ODIN_INSPECTOR
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif

namespace Sisus.Init.EditorOnly.Internal
{
	internal sealed class AnyDrawer : IDisposable
	{
		private static readonly GUIContent serviceLabel = new GUIContent("Service", "An instance of this service will be automatically provided during initialization.");
		private static readonly GUIContent blankLabel = new GUIContent(" ");
		
		private static int lastDraggedObjectCount = 0;

		public GUIContent label;
		public readonly SerializedProperty serializedProperty;
		public readonly SerializedProperty referenceSerializedProperty;
		public readonly Type argumentType;
		public readonly bool isService;
		public readonly PropertyDrawer propertyDrawer;
		private readonly MethodInfo getHasValueMethod;
		private readonly object[] getHasValueArgs = new object[] { null, Context.MainThread };
		
		#if ODIN_INSPECTOR
		private readonly InspectorProperty odinDrawer;
		#endif

		public bool SkipWhenDrawing
		{
			get
			{
				#if ODIN_INSPECTOR
				if(odinDrawer?.GetActiveDrawerChain()?.Current?.SkipWhenDrawing ?? false)
				{
					return true;
				}
				#endif

				return false;
			}
		}

		#if UNITY_LOCALIZATION
		private bool anyPropertyUsesCustomPropertyDrawer => referenceSerializedProperty.objectReferenceValue is Init.Internal.LocalizedString;
		#else
		private bool anyPropertyUsesCustomPropertyDrawer => false;
		#endif

		public AnyDrawer(GUIContent label, SerializedProperty serializedProperty, Type argumentType, InitializerDrawer initializerDrawer, PropertyDrawer propertyDrawer = null, Attribute[] attributes = null)
		{
			this.label = label;
			this.serializedProperty = serializedProperty;
			this.argumentType = argumentType;
			isService = ServiceUtility.TryGetFor(serializedProperty.serializedObject.targetObject, argumentType, out _, Context.MainThread);
			this.propertyDrawer = propertyDrawer;
			referenceSerializedProperty = serializedProperty.FindPropertyRelative("reference");

			var anyType = serializedProperty.GetValue().GetType();
			var clientType = serializedProperty.serializedObject.targetObject.GetType();
			getHasValueMethod = anyType.GetMethod(nameof(Any<object>.GetHasValue)).MakeGenericMethod(clientType);

			#if ODIN_INSPECTOR
			odinDrawer = GetOdinInspectorProperty(label, serializedProperty, argumentType, initializerDrawer.OdinPropertyTree, attributes);

			[CanBeNull]
			static InspectorProperty GetOdinInspectorProperty(GUIContent label, [NotNull] SerializedProperty anyProperty, [NotNull] Type argumentType, PropertyTree odinPropertyTree, [CanBeNull] Attribute[] attributes)
			{
				if(attributes is null)
				{
					return null;
				}

				const int None = 0;
				const int Reference = 1;
				const int Value = 2;
				int drawAs = None;

				foreach(var attribute in attributes)
				{
					if(attribute.GetType().Namespace.StartsWith("Sirenix."))
					{
						drawAs = typeof(Object).IsAssignableFrom(argumentType) || argumentType.IsInterface ? Reference : Value;
						break;
					}
				}

				if(drawAs == None)
				{
					return null;
				}

				string propertyPath;
				if(drawAs == Reference)
				{
					propertyPath = anyProperty.propertyPath + "." + nameof(Any<object>.reference);
				}
				else
				{
					propertyPath = anyProperty.propertyPath + "." + nameof(Any<object>.value);
				}
				
				var result = odinPropertyTree.GetPropertyAtUnityPath(propertyPath);
				if(result is null)
				{
					#if DEV_MODE
					Debug.LogWarning($"Failed to get InspectorProperty from {odinPropertyTree.TargetType.Name} path {propertyPath}.");
					#endif
					return null;
				}

				result.Label = label;
				result.AnimateVisibility = false;
		  		return result;
			}
			#endif
		}

		/// <summary>
		/// Draws Init argument field of an Initializer.
		/// </summary>
		/// <param name="anyProperty"> The <see cref="Any{T}"/> type field that holds the argument. </param>
		/// <param name="label"> Label for the Init argument field. </param>
		/// <param name="customDrawer">
		/// Custom property drawer that was defined for the field via a PropertyAttribute added to a field
		/// on an Init class nested inside the Initializer.
		/// <para>
		/// <see langword="null"/> if no custom drawer has been defined, in which case <see cref="AnyPropertyDrawer"/>
		/// is used to draw the field instead.
		/// </para>
		/// </param>
		/// <param name="isService">
		/// Is the argument a service?
		/// <para>
		/// If <see langword="true"/> then the field is drawn as a service tag.
		/// </para>
		/// </param>
		/// <param name="canBeNull">
		/// Is the argument allowed to be null or not?
		/// <para>
		/// If <see langword="false"/>, then the field will be tinted red if it has a null value.
		/// </para>
		/// </param>
		public void DrawArgumentField(bool canBeNull, bool servicesShown)
		{
			// Repaint whenever dragged object references change because
			// the controls can change in reaction to objects being dragged.
			if(lastDraggedObjectCount != DragAndDrop.objectReferences.Length)
			{
				GUI.changed = true;
				lastDraggedObjectCount = DragAndDrop.objectReferences.Length;
			}

			var anyProperty = serializedProperty;
			var any = anyProperty.GetValue();
			Type anyType = any.GetType();

			var targetObject = anyProperty.serializedObject.targetObject;
			bool hasSerializedValue = (bool)anyType.GetMethod("HasSerializedValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(any, null);

			if(isService && !hasSerializedValue)
			{
				if(!servicesShown)
				{
					return;
				}

				if(!IsDraggingObjectReferenceThatIsAssignableToProperty())
				{
					var totalRect = EditorGUILayout.GetControlRect();
					var controlRect = EditorGUI.PrefixLabel(totalRect, blankLabel);
					controlRect.width = Styles.ServiceTag.CalcSize(serviceLabel).x;

					ServiceTagUtility.Draw(controlRect, () => ServiceTagUtility.Ping(argumentType, anyProperty.serializedObject.targetObject as Component));
				
					var labelRect = totalRect;
					labelRect.width -= controlRect.width;
					GUI.Label(labelRect, label);
					return;
				}
			}

			if(anyProperty == null)
			{
				GUI.color = Color.red;
				EditorGUILayout.LabelField(label.text, "NULL");
				GUI.color = Color.white;
				return;
			}

			#if ODIN_INSPECTOR
			if(odinDrawer != null)
			{
				DrawUsingOdin();
				return;
			}
			#endif

			getHasValueArgs[0] = targetObject;
			bool tintValueRed = !canBeNull && !(bool)getHasValueMethod.Invoke(any, getHasValueArgs);
			if(tintValueRed)
			{
				if(propertyDrawer != null)
				{
					GUI.color = Color.red;
					DrawUsingCustomPropertyDrawer();
					GUI.color = Color.white;
				}
				// when using a custom property drawer, tint the whole thing red, since we don't know how prefab label drawing is handled.
				else if(anyPropertyUsesCustomPropertyDrawer)
				{
					GUI.color = Color.red;
					DrawUsingDefaultDrawer(label);
					GUI.color = Color.white;
				}
				// when not using a custom property drawer, we can tint only the control but not the prefix
				else
				{
					GUILayout.BeginHorizontal();

					EditorGUILayout.PrefixLabel(label);

					int indentLevelWas = EditorGUI.indentLevel;
					EditorGUI.indentLevel = 0;

					GUI.color = Color.red;
					DrawUsingDefaultDrawer(GUIContent.none);
					GUI.color = Color.white;

					EditorGUI.indentLevel = indentLevelWas;

					GUILayout.EndHorizontal();
				}
			}
			else if(propertyDrawer != null)
			{
				DrawUsingCustomPropertyDrawer();
			}
			else
			{
				DrawUsingDefaultDrawer(label);
			}

			anyProperty.serializedObject.ApplyModifiedProperties();

			bool IsDraggingObjectReferenceThatIsAssignableToProperty()
			{
				if(DragAndDrop.objectReferences.Length == 0)
				{
					return false;
				}

				var anyType = typeof(Any<>).MakeGenericType(argumentType);
				return AnyPropertyDrawer.TryGetAssignableTypeFromDraggedObject(DragAndDrop.objectReferences[0], anyType, argumentType, out _);
			}

			void DrawUsingDefaultDrawer(GUIContent label) => EditorGUILayout.PropertyField(anyProperty, label);

			void DrawUsingCustomPropertyDrawer()
			{
				SerializedProperty valueProperty;
				if(typeof(Object).IsAssignableFrom(argumentType))
				{
					valueProperty = anyProperty.FindPropertyRelative("reference");
				}
				else
				{
					valueProperty = anyProperty.FindPropertyRelative("value");
					if(valueProperty == null)
					{
						valueProperty = anyProperty.FindPropertyRelative("reference");
					}
				}

				float height = propertyDrawer.GetPropertyHeight(valueProperty, label);
				if(height < 0f)
				{
					height = EditorGUIUtility.singleLineHeight;
				}

				var rect = EditorGUILayout.GetControlRect(GUILayout.Height(height));
				propertyDrawer.OnGUI(rect, valueProperty, label);
			}

			#if ODIN_INSPECTOR
			void DrawUsingOdin()
			{
				odinDrawer.Tree.BeginDraw(true);
				odinDrawer.Draw();
				odinDrawer.Tree.EndDraw();
			}
			#endif
		}

		public void Dispose()
		{
			#if ODIN_INSPECTOR
			odinDrawer?.Dispose();
			#endif
		}
	}
}