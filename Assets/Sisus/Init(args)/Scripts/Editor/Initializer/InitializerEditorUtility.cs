using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Sisus.Init.Internal;
using Sisus.Init.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace Sisus.Init.EditorOnly.Internal
{
	internal static class InitializerEditorUtility
	{
		private static readonly GUIContent clientNullTooltip = new GUIContent("", "A new instance will be added to this GameObject during initialization.");
		private static readonly GUIContent clientPrefabTooltip = new GUIContent("", "A new instance will be created by cloning this prefab during initialization.");
		private static readonly GUIContent clientInstantiateTooltip = new GUIContent("", "A new instance will be created by cloning this scene object during initialization.");
		private static readonly GUIContent clientNotInitializableTooltip = new GUIContent("", "Can not inject arguments to client because it does not implement IInitializable.");
		private static GUIContent warningIcon;
		private static GUIContent prefabIcon;
		private static GUIContent gameObjectIcon;
		private static GUIContent scriptableObjectIcon;

		private static Color ObjectFieldBackgroundColor => EditorGUIUtility.isProSkin ? new Color32(42, 42, 42, 255) : new Color32(237, 237, 237, 255);

		private static readonly Dictionary<int, string[]> propertyNamesByArgumentCount = new Dictionary<int, string[]>(6)
		{
			{ 1, new string[] {"argument"} },
			{ 2, new string[] {"firstArgument", "secondArgument"} },
			{ 3, new string[] {"firstArgument", "secondArgument", "thirdArgument"} },
			{ 4, new string[] {"firstArgument", "secondArgument", "thirdArgument", "fourthArgument"} },
			{ 5, new string[] {"firstArgument", "secondArgument", "thirdArgument", "fourthArgument", "fifthArgument"} },
			{ 6, new string[] {"firstArgument", "secondArgument", "thirdArgument", "fourthArgument", "fifthArgument", "sixthArgument"} }
		};

		/// <summary>
		/// Maps initializer Init argument property names to their zero-based Init argument indexes.
		/// </summary>
		private static readonly Dictionary<string, int> propertyNameToInitParameterIndex = new Dictionary<string, int>(6)
		{
			{ "argument", 0 },
			{ "firstArgument", 0 },
			{ "secondArgument", 1 },
			{ "thirdArgument", 2 },
			{ "fourthArgument", 3 },
			{ "fifthArgument",  4 },
			{ "sixthArgument", 5}
		};


		internal static IEnumerable<Type> GetInitializerTypes(Type clientType)
		{
			foreach(var type in TypeCache.GetTypesDerivedFrom(typeof(IInitializer)))
			{
				if(IsInitializerFor(type, clientType))
				{
					yield return type;
				}
			}
		}

		internal static void AddInitializer(Object[] clients, Type initializerType)
		{
			GUI.changed = true;
			int count = clients.Length;

			if(typeof(Component).IsAssignableFrom(initializerType))
			{
				for(int i = 0; i < count; i++)
				{
					var component = clients[i] as Component;
					if(component != null)
					{
						IInitializer initializer = component.gameObject.AddComponent(initializerType) as IInitializer;
						initializer.Target = component;
					}
				}

				return;
			}

			if(typeof(ScriptableObject).IsAssignableFrom(initializerType))
			{
				var selectionWas = Selection.objects;

				for(int i = 0; i < count; i++)
				{
					var scriptableObjectClient = clients[i] as ScriptableObject;
					if(scriptableObjectClient != null)
					{
						const string UNDO_NAME = "Add Initializer";
						Undo.RecordObject(scriptableObjectClient, UNDO_NAME);

						var initializerInstance = ScriptableObject.CreateInstance(initializerType);
						initializerInstance.name = "Initializer";
						Undo.RegisterCreatedObjectUndo(initializerInstance, UNDO_NAME);

						(initializerInstance as IInitializer).Target = scriptableObjectClient;
						AssetDatabase.StartAssetEditing();
						string path = AssetDatabase.GetAssetPath(scriptableObjectClient);
						EditorUtility.SetDirty(scriptableObjectClient);
						AssetDatabase.AddObjectToAsset(initializerInstance, path);
						AssetDatabase.StopAssetEditing();
						AssetDatabase.ImportAsset(path);
						
						foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
						{
							if(asset.GetType() == initializerType)
							{
								initializerInstance = asset as ScriptableObject;
								break;
							}
						}

						if(scriptableObjectClient is IInitializableEditorOnly initializableEditorOnly)
						{
							Undo.RecordObject(scriptableObjectClient, UNDO_NAME);
                            initializableEditorOnly.Initializer = initializerInstance as IInitializer;
						}
					}

					Selection.objects = selectionWas;
				}
			}
		}

		public static void GenerateAndAttachInitializer(Object[] clients, MonoScript monoScript)
		{
			GUI.changed = true;

			string initializerPath = ScriptGenerator.CreateInitializer(monoScript);
			var initializerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(initializerPath);
			
			Debug.Log($"Initializer class created at \"{initializerPath}\".", initializerScript);
			
			var initializerGuid = AssetDatabase.AssetPathToGUID(initializerPath);
			EditorPrefs.SetString(InitializerDrawer.SetInitializerTargetOnScriptsReloadedKey, initializerGuid + ":" + string.Join(";", clients.Select(t => t.GetInstanceID())));

			if(initializerScript is null)
			{
				return;
			}

			var addScriptMethod = typeof(InternalEditorUtility).GetMethod("AddScriptComponentUncheckedUndoable", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			if(addScriptMethod is null)
			{
				#if DEV_MODE
				Debug.LogWarning("Method InternalEditorUtility.AddScriptComponentUncheckedUndoable not found.");
				#endif
				return;
			}

			foreach(var client in clients)
			{
				if(client is Component component)
				{
					addScriptMethod.Invoke(null, new Object[] { component.gameObject, initializerScript });
				}
			}
		}

		private static bool IsInitializerFor(Type initializerType, Type clientType)
		{
			if(initializerType.IsAbstract)
			{
				return false;
			}

			for(var baseType = initializerType; baseType != null; baseType = baseType.BaseType)
			{
				if(!baseType.IsGenericType)
				{
					continue;
				}

				var arguments = baseType.GetGenericArguments();
				if(arguments.Length < 2)
				{
					continue;
				}

				if(arguments[0] == clientType)
				{
					return true;
				}
			}

			return false;
		}

		internal static bool CanAssignUnityObjectToField(Type type)
		{
			if(typeof(Object).IsAssignableFrom(type))
			{
				return true;
			}

			if(!type.IsInterface)
			{
				return false;
			}

			foreach(var derivedType in TypeCache.GetTypesDerivedFrom(type))
			{
				if(typeof(Object).IsAssignableFrom(derivedType) && !derivedType.IsAbstract)
				{
					return true;
				}
			}

			return false;
		}

		internal static void DrawClientField(Rect rect, SerializedProperty client, GUIContent clientLabel, bool isInitializable, bool hasServiceArguments)
		{
			rect.y += 5f;
			rect.width -= 28f;
			if(hasServiceArguments)
			{
				rect.width -= 20f;
			}

			var fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent(" "));
			var reference = client.objectReferenceValue;

			EditorGUI.ObjectField(fieldRect, client, GUIContent.none);

			bool mouseovered = rect.Contains(Event.current.mousePosition) && DragAndDrop.visualMode != DragAndDropVisualMode.None;

			fieldRect.x += 2f;
			fieldRect.width -= 21f;
			fieldRect.y += 2f;
			fieldRect.height -= 3f;

			if(!isInitializable && !mouseovered)
			{
				GUI.color = Color.red;
			}

			if(reference == null)
			{
				if(mouseovered)
				{
					return;
				}

				clientLabel.tooltip = isInitializable ? clientNullTooltip.text : clientNotInitializableTooltip.text;

				EditorGUI.DrawRect(fieldRect, ObjectFieldBackgroundColor);
				GUI.Label(fieldRect, clientLabel);
			}
			else
			{
				Component component = reference as Component;
				var gameObject = component != null ? component.gameObject : null;
				bool isPrefab = gameObject != null && !gameObject.scene.IsValid();
				bool isSceneObject = gameObject != null && gameObject.scene.IsValid();
				bool isScriptableObject = reference is ScriptableObject;

				GUIContent icon;
				if(!isInitializable)
				{
					if(warningIcon == null)
					{
						warningIcon = EditorGUIUtility.IconContent("Warning");
					}
					icon = warningIcon;
				}
				else if(isPrefab)
				{
					if(prefabIcon == null)
					{
						prefabIcon = EditorGUIUtility.IconContent("Prefab Icon");
					}
					icon = prefabIcon;
				}
				else if(isSceneObject)
				{
					if(gameObjectIcon == null)
					{
						gameObjectIcon = EditorGUIUtility.IconContent("GameObject Icon");
					}
					icon = gameObjectIcon;
				}
				else if(isScriptableObject)
				{
					if(scriptableObjectIcon == null)
					{
						scriptableObjectIcon = EditorGUIUtility.IconContent("ScriptableObject Icon");
					}
					icon = scriptableObjectIcon;
				}
				else
				{
					icon = GUIContent.none;
				}

				string tooltip = GetReferenceTooltip(client.serializedObject.targetObject, reference, isInitializable).tooltip;
				clientLabel.tooltip = tooltip;
				icon.tooltip = tooltip;

				GUI.Label(fieldRect, new GUIContent("", clientLabel.tooltip));

				var iconSize = EditorGUIUtility.GetIconSize();
				EditorGUIUtility.SetIconSize(new Vector2(15f, 15f));
				
				var iconRect = fieldRect;
				iconRect.y -= 4f;
				iconRect.x -= 22f;
				iconRect.width = 20f;
				iconRect.height = 20f;
				if(GUI.Button(iconRect, icon, EditorStyles.label))
				{
					EditorGUIUtility.PingObject(gameObject != null ? gameObject : reference);
				}

				EditorGUIUtility.SetIconSize(iconSize);
			}

			GUI.color = Color.white;
		}

		internal static GUIContent GetReferenceTooltip(Object objectWithField, Object reference, bool isInitializable)
		{
			if(!isInitializable)
			{
				return clientNotInitializableTooltip;
			}

			if(reference == null)
			{
				return clientNullTooltip;
			}

			Component component = reference as Component;
			if(component == null)
			{
				return GUIContent.none;
			}

			var gameObject = component.gameObject;
			if(gameObject == null)
			{
				return GUIContent.none;
			}

			var gameObjectWithField = objectWithField is Component componentWithField ? componentWithField.gameObject : null;
			if(gameObjectWithField == null || gameObjectWithField == gameObject)
			{
				return GUIContent.none;
			}

			bool isPrefab = !gameObject.scene.IsValid();
			return isPrefab ? clientPrefabTooltip : clientInstantiateTooltip;
		}

		internal static GUIContent GetLabel([NotNull] Type type)
		{
			if(!(type.GetCustomAttribute<AddComponentMenu>() is AddComponentMenu addComponentMenu))
			{
				return GetLabel(TypeUtility.ToString(type));
			}

			string menuPath = addComponentMenu.componentMenu;
			if(string.IsNullOrEmpty(menuPath))
			{
				return GetLabel(TypeUtility.ToString(type));
			}

			int nameStart = menuPath.LastIndexOf('/') + 1;
			return new GUIContent(nameStart <= 0 ? menuPath : menuPath.Substring(nameStart));
		}

		internal static GUIContent GetLabel(string typeOfFieldName)
		{
			typeOfFieldName = ObjectNames.NicifyVariableName(typeOfFieldName);
			return new GUIContent(typeOfFieldName.StartsWith("I ") ? typeOfFieldName.Substring(2) : typeOfFieldName);
		}

		[CanBeNull]
		internal static bool TryGetInitParameterAttributesFromMetadata(string initializerPropertyName, Type parameterType, Type metadataClass, out Attribute[] results)
		{
			// "firstArgument" => 0, "secondArgument" => 1 etc.
			if(!propertyNameToInitParameterIndex.TryGetValue(initializerPropertyName, out int parameterIndex))
			{
				FieldInfo[] fields = metadataClass.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if(parameterIndex < fields.Length)
				{
					var fieldAtIndex = fields[parameterIndex];
					if(fieldAtIndex.FieldType == parameterType)
					{
						results = Attribute.GetCustomAttributes(fieldAtIndex, typeof(Attribute));
						return results.Length > 0;
					}
				}
			}

			if(!TryGetArgumentTargetFieldName(metadataClass, parameterType, parameterIndex, out string fieldName))
			{
				results = null;
				return false;
			}

			var field = metadataClass.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			results = Attribute.GetCustomAttributes(field, typeof(Attribute));
			return results.Length > 0;
		}

		internal static bool TryGetInitParameterMetadataMember(Type metadataClass, Type argumentType, string propertyName, out MemberInfo result)
		{
			if(propertyNameToInitParameterIndex.TryGetValue(propertyName, out int parameterIndex))
			{
				int i = 0;
				foreach(var field in metadataClass.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				{
					if(i == parameterIndex && field.FieldType == argumentType)
					{
						result = field;
						return true;
					}

					i++;
				}
			}

			if(InjectionUtility.TryGetInitArgumentTargetMember(metadataClass, argumentType, -1, out var member))
			{
				result = member;
				return true;
			}

			result = null;
			return false;
		}

		#if ODIN_INSPECTOR
		internal static bool TryGetOdinDrawer(GUIContent label, [NotNull] SerializedProperty anyProperty, [NotNull] Type argumentType, PropertyTree odinPropertyTree, out InspectorProperty odinDrawer)
		{
			SerializedProperty valueProperty;
			if(typeof(Object).IsAssignableFrom(argumentType))
			{
				valueProperty = anyProperty.FindPropertyRelative(nameof(Any<object>.reference));
			}
			else
			{
				valueProperty = anyProperty.FindPropertyRelative(nameof(Any<object>.value));
				if(valueProperty is null)
				{
					valueProperty = anyProperty.FindPropertyRelative(nameof(Any<object>.reference));
				}
			}

			odinDrawer = odinPropertyTree.GetPropertyAtUnityPath(valueProperty.propertyPath);
			if(odinDrawer is null)
			{
				#if DEV_MODE
				Debug.LogWarning($"Failed to get InspectorProperty from {odinPropertyTree.TargetType.Name} path {valueProperty.propertyPath}.");
				#endif

				return false;
			}

			odinDrawer.Label = label;
			odinDrawer.AnimateVisibility = false;
		  	return true;
		}
		#endif

		public static AnyDrawer[] GetPropertyDrawerData(SerializedObject serializedObject, Type clientType, Type[] argumentTypes, InitializerDrawer drawer)
			=> GetPropertyDrawerData(serializedObject, clientType, argumentTypes, GetArgumentSerializedProperties(serializedObject, argumentTypes.Length), drawer);

		private static SerializedProperty[] GetArgumentSerializedProperties(SerializedObject serializedObject, int argumentCount)
			=> propertyNamesByArgumentCount[argumentCount].Select(serializedObject.FindProperty).ToArray();


		internal static Type GetMetaDataClassType(Type initializerType) => initializerType.GetNestedType(InitializerEditor.InitArgumentMetadataClassName, BindingFlags.Public | BindingFlags.NonPublic);

		internal static string GetMemberNameInInitializer([NotNull] Type metadataClass, string nameInMetadata)
		{
			var members = GetArgumentMetadata(metadataClass);
			if(!propertyNamesByArgumentCount.TryGetValue(members.Length - 1, out var namesInInitializer))
			{
				return nameInMetadata;
			}

			for(int i = 0; i < members.Length; i++)
			{
				if(string.Equals(members[i].Name, nameInMetadata, StringComparison.OrdinalIgnoreCase))
				{
					return namesInInitializer[i];
				}
			}

			#if DEV_MODE
			Debug.Log($"\"{nameInMetadata}\" not found among options: {string.Join(", ", members.Select(m => m.Name))}.");
			#endif

			return namesInInitializer.Length == 1 ? namesInInitializer[0] : nameInMetadata;
		}

		private static MemberInfo[] GetArgumentMetadata([NotNull] Type metadataClass)
		{ 
			var members = metadataClass.GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
			Array.Sort(members, CompareOrderOfDefinition);
			return members;
			static int CompareOrderOfDefinition(MemberInfo x, MemberInfo y) => x.MetadataToken.CompareTo(y.MetadataToken);
		}

		private static AnyDrawer[] GetPropertyDrawerData(SerializedObject serializedObject, Type clientType, Type[] argumentTypes, SerializedProperty[] serializedProperties, InitializerDrawer initializerDrawer)
		{
			var target = serializedObject.targetObject;
			var initializerType = target.GetType();
            var metadataClass = GetMetaDataClassType(initializerType);

			int argumentCount = argumentTypes.Length;
			var results = new AnyDrawer[argumentCount];

			var members = metadataClass is null ? Array.Empty<MemberInfo>() : GetArgumentMetadata(metadataClass);

			// one member per init argument + constructor
            if(members.Length == argumentCount + 1)
			{
				for(int i = 0, count = serializedProperties.Length; i < count; i++)
				{
					var serializedProperty = serializedProperties[i];
					var argumentType = argumentTypes[i];

					PropertyDrawer propertyDrawer = null;
					if(TryGetInitParameterAttributesFromMetadata(serializedProperty.name, argumentType, metadataClass, out Attribute[] attributes))
					{
						TryGetAttributeBasedPropertyDrawer(serializedProperty, attributes, out propertyDrawer);
					}

					var label = GetLabel(members[i]);
					results[i] = new AnyDrawer
					(
						label,
						serializedProperty,
						argumentType,
						initializerDrawer,
						propertyDrawer,
						attributes
					);
				}
            }
            else
            {
				for(int i = 0, count = serializedProperties.Length; i < count; i++)
				{
					var argumentType = argumentTypes[i];
					var serializedProperty = serializedProperties[i];
					var label = GetArgumentLabel(clientType, argumentType, i);
					results[i] = new AnyDrawer
					(
						label,
						serializedProperty,
						argumentType,
						initializerDrawer
					);
				}
            }

			return results;
		}

		internal static bool TryGetAttributeBasedPropertyDrawer([NotNull] SerializedProperty serializedProperty, [NotNull] Attribute[] attributes, out PropertyDrawer propertyDrawer)
		{
			foreach(var attribute in attributes)
			{
				if(attribute is PropertyAttribute propertyAttribute
					&& TryGetAttributeBasedPropertyDrawer(serializedProperty, propertyAttribute, out propertyDrawer))
				{
					return true;
				}
			}

			propertyDrawer = null;
			return false;
		}

		internal static bool TryGetAttributeBasedPropertyDrawer([NotNull] SerializedProperty serializedProperty, [CanBeNull] PropertyAttribute propertyAttribute, out PropertyDrawer propertyDrawer)
		{
			if(!TryGetDrawerType(propertyAttribute, out Type drawerType))
			{
				propertyDrawer = null;
				return false;
			}

			propertyDrawer = CreateInstance(drawerType) as PropertyDrawer;

			if(propertyDrawer == null)
			{
				return false;
			}

			if(propertyAttribute != null)
			{
				var attributeField = typeof(PropertyDrawer).GetField("m_Attribute", BindingFlags.Instance | BindingFlags.NonPublic);
				#if UNITY_2019_1_OR_NEWER && ENABLE_UI_SUPPORT
				//var element = propertyDrawer.CreatePropertyGUI(serializedProperty);
				#endif
				attributeField.SetValue(propertyDrawer, propertyAttribute);
			}

			if(serializedProperty.GetMemberInfo() is FieldInfo fieldInfo)
			{
				typeof(PropertyDrawer).GetField("m_FieldInfo", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(propertyAttribute, fieldInfo);
			}

			return true;
		}

		private static bool TryGetDrawerType([NotNull] PropertyAttribute propertyAttribute, out Type drawerType)
		{
			var propertyAttributeType = propertyAttribute.GetType();
			var typeField = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance);
			var useForChildrenField = typeof(CustomPropertyDrawer).GetField("m_UseForChildren", BindingFlags.NonPublic | BindingFlags.Instance);
			drawerType = null;

			foreach(var propertyDrawerType in TypeCache.GetTypesWithAttribute<CustomPropertyDrawer>())
			{
				foreach(var attribute in propertyDrawerType.GetCustomAttributes<CustomPropertyDrawer>())
				{
					var targetType = typeField.GetValue(attribute) as Type;
					if(targetType == propertyAttributeType)
					{
						drawerType = propertyDrawerType;
						return true;
					}

					if(targetType.IsAssignableFrom(propertyAttributeType) && (bool)useForChildrenField.GetValue(attribute))
					{
						drawerType = propertyDrawerType;
					}
				}
			}

			return drawerType != null;
		}

		internal static object CreateInstance(Type type)
        {
			try
			{
				return Activator.CreateInstance(type);
			}
			catch(Exception)
			{
				return FormatterServices.GetUninitializedObject(type);
			}
        }

		internal static GUIContent GetArgumentLabel(Type clientType, Type parameterType, int parameterIndex)
		{
			if(TryGetArgumentTargetMember(clientType, parameterType, parameterIndex, out var member))
			{
				var label = GetLabel(member);
				if(member.GetCustomAttribute<TooltipAttribute>() is TooltipAttribute tooltip)
				{
					label.tooltip = tooltip.tooltip;
				}
				return label;
			}
			
			return GetLabel(parameterType);
		}

		internal static GUIContent GetLabel([NotNull] MemberInfo member)
		{
			var label = GetLabel(member.Name);
			label.tooltip = GetTooltip(member);
			return label;
		}

		internal static string GetTooltip(MemberInfo member) => member.GetCustomAttribute<TooltipAttribute>() is TooltipAttribute tooltip ? tooltip.tooltip : "";

		/// <summary>
		/// Pr
		/// </summary>
		/// <param name="clientType"></param>
		/// <param name="parameterType">
		/// Type which returned member must be assignable from.
		/// </param>
		/// <param name="argumentIndex">
		/// Zero-based index of the Init function argument whose target member is retrieved.
		/// <para>
		/// If the nth public member of <paramref name="clientType"/> is of type <paramref name="parameterType"/>
		/// then that will be returned.
		/// </para>
		/// </param>
		/// <param name="member">
		/// 
		/// </param>
		/// <returns></returns>
		internal static bool TryGetArgumentTargetMember(Type clientType, Type parameterType, int argumentIndex, out MemberInfo member)
			=> InjectionUtility.TryGetInitArgumentTargetMember(clientType, parameterType, argumentIndex, out member);

		internal static bool TryGetArgumentTargetFieldName(Type clientType, Type parameterType, int argumentIndex, out string targetFieldName)
		{
			if(InjectionUtility.TryGetInitArgumentTargetMember(clientType, parameterType, argumentIndex, out var member))
			{
				targetFieldName = member.Name;
				return true;
			}

			targetFieldName = null;
			return false;
		}

		internal static bool IsInitializable(Type clientType) => GetClientInitArgumentCount(clientType) > 0;

		internal static int GetClientInitArgumentCount(Type clientType)
		{
			if(typeof(IOneArgument).IsAssignableFrom(clientType))
			{
				return 1;
			}

			if(typeof(ITwoArguments).IsAssignableFrom(clientType))
			{
				return 2;
			}

			if(typeof(IThreeArguments).IsAssignableFrom(clientType))
			{
				return 3;
			}

			if(typeof(IFourArguments).IsAssignableFrom(clientType))
			{
				return 4;
			}

			if(typeof(IFiveArguments).IsAssignableFrom(clientType))
			{
				return 5;
			}

			if(typeof(ISixArguments).IsAssignableFrom(clientType))
			{
				return 5;
			}

			return 0;
		}
	}
}