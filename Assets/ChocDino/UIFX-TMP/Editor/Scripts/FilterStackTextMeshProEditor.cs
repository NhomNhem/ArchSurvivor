//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//
#if UIFX_TMPRO

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using TMPro;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(FilterStackTextMeshPro), true)]
	[CanEditMultipleObjects]
	internal class FilterStackTextMeshProEditor : BaseEditor
	{
		private static readonly GUIContent Content_Filters = new GUIContent("Filters");
		private static readonly GUIContent Content_FilterList = new GUIContent("Filter List");
		private static readonly GUIContent Content_Filter88 = new GUIContent("Filter 88  ");
		private static readonly GUIContent Content_AddFilter = new GUIContent("Add Filter:");
		private static readonly GUIContent Content_SaveToPNG = new GUIContent("Save to PNG");
		private static readonly GUIContent Content_PreviewTitle = new GUIContent("UIFX - FilterStack TextMeshPro");
		private static readonly string Pref_Prefix = "UIFX.FilterStack.";
		private static readonly string Pref_SelectedTypeIndex = Pref_Prefix + "SelectedTypeIndex";

		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Filter Stack TextMeshPro\n© Chocolate Dinosaur Ltd", "uifx-icon")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/bundle/components/filter-stack-text-mesh-pro/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX Bundle</b>", AssetStoreBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Discussions", ForumBundleUrl),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, s_aboutInfo } );

		private ReorderableList _reorderableList;

		private SerializedProperty _propApplyToSprites;
		private SerializedProperty _propRelativeToTransformScale;
		private SerializedProperty _propRelativeFontSize;
		private SerializedProperty _propUpdateOnTransform;
		private SerializedProperty _propRenderSpace;

		private static readonly GUIContent Content_Add = new GUIContent("Add");

		private SerializedProperty _propFilters;

		private List<System.Type> _filterTypes;
		private List<FilterBase> _unusedFilterComponents = new List<FilterBase>(4);
		private bool _hasEmptyFilterSlots = false;
		private GUIContent[] _filterTypesNames;
		private int _selectedTypeIndex;

		private void ShowSaveToPngButton()
		{
			// TODO: Add baking to Image option that bakes to a new GameObject and doesn't destroy the old object
			// TODO: Convert the image baking buttons into a EditorGUILayout.DropDownToggle()
			if (GUILayout.Button(Content_SaveToPNG))
			{
				foreach (FilterStackTextMeshPro filter in this.targets)
				{
					System.IO.Directory.CreateDirectory(Application.dataPath + "/../Captures/");
					string timestamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
					string filterName = filter.GetType().Name;
					string path = System.IO.Path.GetFullPath(Application.dataPath + string.Format("/../Captures/Image-{0}-{1}-{2}.png", timestamp, filter.gameObject.name, filterName));
					if (filter.SaveToPNG(path))
					{
						Debug.Log("Saving image to: <b>" + path + "</b>");
					}
					else
					{
						Debug.LogError("Failed to save image to: <b>" + path + "</b>");
					}
				}
			}
		}

		#if !UIFX_FILTER_HIDE_INSPECTOR_PREVIEW

		public override GUIContent GetPreviewTitle()
		{
			return Content_PreviewTitle;
		}

		public override bool HasPreviewGUI()
		{
			// We can't support multiple targets because the below logic to only show the last item doesn't work
			if (targets.Length > 1) return false;

			var filter = target as FilterStackTextMeshPro;
			if (!filter.isActiveAndEnabled) return false;

			return true;
		}

		public override string GetInfoString()
		{
			var filter = target as FilterStackTextMeshPro;
			var texture = filter.ResolveToTexture();
			if (texture)
			{
				return (texture.width + " x " + texture.height);
			}
			return string.Empty;
		}

		public override void OnPreviewSettings()
		{
			ShowSaveToPngButton();
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			if (Event.current.type == EventType.Repaint)
			{
				var filter = target as FilterStackTextMeshPro;
				EditorGUI.DrawTextureTransparent(r, Texture2D.blackTexture, ScaleMode.StretchToFill);
				var texture = filter.ResolveToTexture();
				if (texture)
				{
					GUI.DrawTexture(r, texture, ScaleMode.ScaleToFit, true);
				}
			}
		}

		#endif

		void OnEnable()
		{
			// Note: Had a case where in a prefab if the component is removed it can still call OnEnable()
			if (this.target == null) return;

			var extractedTypes = TypeCache.GetTypesDerivedFrom<FilterBase>();
			if (extractedTypes.Count > 0)
			{
				_filterTypes = extractedTypes.OrderBy(type => type.Name).ToList();
				_filterTypesNames = new GUIContent[_filterTypes.Count];
				for (int i = 0; i < _filterTypes.Count; i++)
				{
					_filterTypesNames[i] = new GUIContent(GetDisplayNameForComponentType(_filterTypes[i]));
				}
			}
			else
			{
				_filterTypesNames = new GUIContent[0];
			}

			_selectedTypeIndex = EditorPrefs.GetInt(Pref_SelectedTypeIndex, 0);
			_selectedTypeIndex = Mathf.Clamp(_selectedTypeIndex, 0, _filterTypesNames.Length - 1);

			_propFilters = VerifyFindProperty("_filters");
			_propApplyToSprites = VerifyFindProperty("_applyToSprites");
			_propRelativeToTransformScale = VerifyFindProperty("_relativeToTransformScale");
			_propRelativeFontSize = VerifyFindProperty("_relativeFontSize");
			_propUpdateOnTransform = VerifyFindProperty("_updateOnTransform");
			_propRenderSpace = VerifyFindProperty("_renderSpace");

			_reorderableList = new ReorderableList(serializedObject, _propFilters, true, false, true, true)
			{
				drawHeaderCallback = DrawListHeader,
				drawElementCallback = DrawListElement,
				onAddDropdownCallback = AddDropdownCallback
			};
			
			UpdateFilterList();
		}

		private void UpdateFilterList()
		{
			UpdateEmptyFilterSlots();
			UpdateUnusedFilterList();
		}

		private void UpdateEmptyFilterSlots()
		{
			_hasEmptyFilterSlots = false;

			foreach (var obj in this.targets)
			{
				FilterBase filterComponent = null;
				var so = new SerializedObject(obj);
				var prop = so.FindProperty("_filters");
				IEnumerator it = prop.GetEnumerator();
				while (it.MoveNext())
				{
					var property = it.Current as SerializedProperty;
					filterComponent = property.objectReferenceValue as FilterBase;
					if (filterComponent == null)
					{
						_hasEmptyFilterSlots = true;
						break;
					}
				}
				if (filterComponent == null)
				{
					break;
				}
			}
		}

		private void UpdateUnusedFilterList()
		{
			// Find all filters in the GameObjects
			List<FilterBase> filtersInGameObjects = new List<FilterBase>(16);
			foreach (var obj in this.targets)
			{
				var filterStack = obj as FilterStackTextMeshPro;
				var filterComponents = filterStack.gameObject.GetComponents<FilterBase>();
				filtersInGameObjects.AddRange(filterComponents);
			}

			// Remove any that aren't in the FilterList
			foreach (var obj in this.targets)
			{
				var so = new SerializedObject(obj);
				var prop = so.FindProperty("_filters");
				IEnumerator it = prop.GetEnumerator();
				while (it.MoveNext())
				{
					var property = it.Current as SerializedProperty;
					var filterComponent = property.objectReferenceValue as FilterBase;
					if (filtersInGameObjects.Contains(filterComponent))
					{
						filtersInGameObjects.Remove(filterComponent);
					}
				}
			}

			_unusedFilterComponents = filtersInGameObjects;
		}

		void OnDisable()
		{
			_filterTypes = null;
			_filterTypesNames = null;
			EditorPrefs.SetInt(Pref_SelectedTypeIndex, _selectedTypeIndex);
		}

		static void InsertIntoList(SerializedProperty listProp, Component component)
		{
			// Insert into the filter list, find the last null child, or append
			int indexToInsert = -1;
			if (listProp.arraySize > 0)
			{
				var lastChildProp = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
				if (lastChildProp.objectReferenceValue == null)
				{
					indexToInsert = listProp.arraySize - 1;
				}
			}
			if (indexToInsert < 0)
			{
				listProp.InsertArrayElementAtIndex(listProp.arraySize);
				indexToInsert = listProp.arraySize - 1;
			}
			listProp.GetArrayElementAtIndex(indexToInsert).objectReferenceValue = component;
		}

		private void DrawListHeader(Rect rect)
		{
			EditorGUI.LabelField(rect, Content_FilterList);
		}

		private void OnClickFilterTypeName(object target)
		{
			serializedObject.Update();

			if (target != null)
			{
				var type = (System.Type)target;
				foreach (var obj in this.targets)
				{
					var filterStack = obj as FilterStackTextMeshPro;
					var component = ObjectFactory.AddComponent(filterStack.gameObject, type);

					var so = new SerializedObject(obj);
					var prop = so.FindProperty("_filters");
					InsertIntoList(prop, component);
					so.ApplyModifiedProperties();
				}
			}
			else
			{
				// Add an empty slot
				foreach (var obj in this.targets)
				{
					var so = new SerializedObject(obj);
					var prop = so.FindProperty("_filters");
					InsertIntoList(prop, null);
					so.ApplyModifiedProperties();
				}
			}
			serializedObject.ApplyModifiedProperties();
			serializedObject.SetIsDifferentCacheDirty();
			serializedObject.Update();
		}

		private void AddDropdownCallback(Rect rect, ReorderableList list)
		{
			var menu = new GenericMenu();

			menu.AddItem(new GUIContent("Empty"), false, OnClickFilterTypeName, null);

			int index = 0;
			foreach (var filterTypeName in _filterTypesNames)
			{
				menu.AddItem(filterTypeName, false, OnClickFilterTypeName, _filterTypes[index]);
				index++;
			}

			menu.ShowAsContext();
		}

		private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			SerializedProperty prop = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
	
			// By default, the array dropdown is offset to the left which intersects with
			// the drag handle, so we can either indent the array property or inset the rect.
			//EditorGUI.indentLevel++;
	
			// Take note of the last argument, since this is an array,
			// we want to draw it with all of its children.
			
			Vector2 labelSize = GUI.skin.label.CalcSize(Content_Filter88);
			Rect labelRect = rect;
			labelRect.width = labelSize.x;
			EditorGUI.PrefixLabel(labelRect, new GUIContent("Filter " + index));
			rect.xMin += labelRect.width;
	
			try
			{
				FilterBase filterComponent = prop.objectReferenceValue as FilterBase;
				if (filterComponent)
				{
					Vector2 toggleSize = GUI.skin.toggle.CalcSize(GUIContent.none);
					Rect toggleRect = rect;
					toggleRect.width = toggleSize.x * 2f;

					if (this.targets.Length > 1)
					{
						// Determine whether to show mixed values
						bool prevMixed = EditorGUI.showMixedValue;
						int lastState = -2;
						foreach (var obj in this.targets)
						{
							var so = new SerializedObject(obj);
							var prop2 = so.FindProperty("_filters");
							SerializedProperty prop3 = prop2.GetArrayElementAtIndex(index);
							FilterBase filterComponent2 = prop3.objectReferenceValue as FilterBase;
							int state = -1;
							if (filterComponent2)
							{
								state = filterComponent2.enabled ? 0 : 1;
							}

							if (lastState != -2 && state != lastState)
							{
								EditorGUI.showMixedValue = true;
								break;
							}

							lastState = state;
						}

						EditorGUI.BeginChangeCheck();
						bool newValue = EditorGUI.Toggle(toggleRect, filterComponent.enabled);
						if (EditorGUI.EndChangeCheck())
						{
							foreach (var obj in this.targets)
							{
								var so = new SerializedObject(obj);
								var prop2 = so.FindProperty("_filters");

								SerializedProperty prop3 = prop2.GetArrayElementAtIndex(index);
								FilterBase filterComponent2 = prop3.objectReferenceValue as FilterBase;
								if (filterComponent2)
								{
									filterComponent2.enabled = newValue;
									EditorUtility.SetDirty(filterComponent2);
								}
							}
						}

						EditorGUI.showMixedValue = prevMixed;
					}
					else
					{
						EditorGUI.BeginChangeCheck();
						bool newValue = EditorGUI.Toggle(toggleRect, filterComponent.enabled);
						if (EditorGUI.EndChangeCheck())
						{
							filterComponent.enabled = newValue;
							EditorUtility.SetDirty(filterComponent);
						}
					}

					rect.xMin += toggleRect.width;
				}
			}
			catch(System.Exception)
			{

			}

			EditorGUI.PropertyField(rect, prop, GUIContent.none, includeChildren: false);
			//EditorGUI.indentLevel--;

			//EditorGUI.PropertyField(rect, prop, new GUIContent("Filter " + index), includeChildren: false);
		}

		private void AddUnusedFilters()
		{
			serializedObject.Update();
			foreach (var component in _unusedFilterComponents)
			{
				var filterStack = component.gameObject.GetComponent<FilterStackTextMeshPro>();
				var so = new SerializedObject(filterStack);
				var prop = so.FindProperty("_filters");
				InsertIntoList(prop, component);
				so.ApplyModifiedProperties();
			}
			_unusedFilterComponents.Clear();
			serializedObject.ApplyModifiedProperties();
			serializedObject.SetIsDifferentCacheDirty();
			serializedObject.Update();
		}

		private void RemoveEmptySlots()
		{
			foreach (var obj in this.targets)
			{
				var so = new SerializedObject(obj);
				var prop = so.FindProperty("_filters");
				int arraySize = prop.arraySize;
				for (int index = 0; index < arraySize;)
				{
					var arrayElement = prop.GetArrayElementAtIndex(index);
					if (arrayElement.objectReferenceValue == null)
					{
						// Older versions (pre 2021.2 and 2020.3) of Unity requires 2 deletes, this code covers bother cases
						var oldLength = prop.arraySize;
						prop.DeleteArrayElementAtIndex(index);
						if (prop.arraySize == oldLength)
						{
							prop.DeleteArrayElementAtIndex(index);
						}
						arraySize--;
					}
					else
					{
						index++;
					}
				}
				so.ApplyModifiedProperties();
				so.SetIsDifferentCacheDirty();
				so.Update();
			}
		}

		private const string TextAnimator2TypeName = "Febucci.UI.TextAnimator_TMP";
		private const string TextAnimator1TypeName = "Febucci.UI.TextAnimator";

		bool NeedsTextAnimatorSupportEnabled()
		{
			bool result = false;
			#if !UIFX_SUPPORT_TEXT_ANIMATOR
			foreach (var obj in this.targets)
			{
				var filterStack = obj as FilterStackTextMeshPro;
				var component = filterStack.gameObject.GetComponent(TextAnimator2TypeName);
				if (component == null)
				{
					component = filterStack.gameObject.GetComponent(TextAnimator1TypeName);
				}

				result = component != null;
			}
			#endif
			return result;
		}

		bool IsTextureTooLarge()
		{
			bool result = false;
			foreach (var obj in this.targets)
			{
				var filterStack = obj as FilterStackTextMeshPro;
				if (filterStack.IsTextureTooLarge())
				{
					result = true;
					break;
				}
			}
			return result;
		}

		public override void OnInspectorGUI()
		{
			s_aboutToolbar.OnGUI();

			if (NeedsTextAnimatorSupportEnabled())
			{
				EditorGUILayout.HelpBox("Text Animator support must be enabled in the UIFX Settings for your build platforms.", MessageType.Error, true);
				if (GUILayout.Button("Open UIFX Settings"))
				{
					SettingsService.OpenProjectSettings(Preferences.SettingsPath);
				}
				EditorGUILayout.Space();
				return;
			}

			if (IsTextureTooLarge())
			{
				EditorGUILayout.HelpBox("Requested texture is larger than the supported size of " + ChocDino.UIFX.Filters.GetMaxiumumTextureSize() + ", rescaling texture to supported size, this can lead to lower texture quality. Consider invstigating why such a large texture is required.", MessageType.Error, true);
				EditorGUILayout.Space();
			}

			serializedObject.Update();

			EditorGUILayout.PropertyField(_propApplyToSprites);
			EditorGUILayout.PropertyField(_propRelativeToTransformScale);
			GUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(_propRelativeFontSize);
			if (_propRelativeFontSize.floatValue > 0f)
			{
				if (GUILayout.Button("Remove", GUILayout.ExpandWidth(false)))
				{
					_propRelativeFontSize.floatValue = 0f;
				}
			}
			else
			{
				var textMeshPro = (target as Behaviour).GetComponent<TextMeshProUGUI>();
				if (textMeshPro.fontSize != _propRelativeFontSize.floatValue)
				{
					if (GUILayout.Button("Set From Text", GUILayout.ExpandWidth(false)))
					{
						_propRelativeFontSize.floatValue = textMeshPro.fontSize;
					}
				}
			}
			GUILayout.EndHorizontal();
			EditorGUILayout.PropertyField(_propUpdateOnTransform);
			EnumAsToolbarCompact(_propRenderSpace);

			EditorGUILayout.Space();
			GUILayout.Label(Content_Filters, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			if (_propFilters.arraySize == 0)
			{
				EditorGUILayout.HelpBox("No filters currently added", MessageType.Error, true);
			}

			if (_unusedFilterComponents.Count > 0)
			{
				{
					var unusedList = string.Empty;
					foreach (var component in _unusedFilterComponents)
					{
						unusedList += "\n - " + component.GetType().Name;
					}
					EditorGUILayout.HelpBox("There are " + _unusedFilterComponents.Count + " unused filter components:" + unusedList, MessageType.Warning, true);
				}
				GUILayout.BeginHorizontal();
				if (GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), "Add " + _unusedFilterComponents.Count + " Unused Filters"))
				{
					AddUnusedFilters();
					UpdateFilterList();
				}
				if (GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), "Remove " + _unusedFilterComponents.Count + " Unused Filters"))
				{
					foreach (var component in _unusedFilterComponents)
					{
						ObjectHelper.Destroy(component);
					}
					_unusedFilterComponents.Clear();
					UpdateFilterList();
				}
				GUILayout.EndHorizontal();
			}

			EditorGUI.BeginChangeCheck();
			_reorderableList.DoLayoutList();
			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				serializedObject.SetIsDifferentCacheDirty();
				serializedObject.Update();
				UpdateFilterList();
			}

			if (_hasEmptyFilterSlots)
			{
				EditorGUILayout.HelpBox("There are empty slots in the filter list", MessageType.Info, true);
				if (GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), "Remove Empty Slots"))
				{
					RemoveEmptySlots();
					UpdateFilterList();
				}
				EditorGUILayout.Space();
			}

			serializedObject.ApplyModifiedProperties();

			if (_filterTypesNames != null && _filterTypesNames.Length > 0)
			{
				EditorGUILayout.PrefixLabel(Content_AddFilter);
				EditorGUILayout.BeginHorizontal();
				// Show dropdown list of filters that can be added
				_selectedTypeIndex = EditorGUILayout.Popup(_selectedTypeIndex, _filterTypesNames);

				// Show button to add selected filter
				if (GUILayout.Button(Content_Add))
				{
					serializedObject.Update();
					foreach (var obj in this.targets)
					{
						var so = new SerializedObject(obj);
						var filterStack = obj as FilterStackTextMeshPro;
						var prop = so.FindProperty("_filters");
						var component = ObjectFactory.AddComponent(filterStack.gameObject, _filterTypes[_selectedTypeIndex]);
						InsertIntoList(prop, component);
						so.ApplyModifiedProperties();
					}
					serializedObject.ApplyModifiedProperties();
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUI.indentLevel--;
		}

		private static string GetDisplayNameForComponentType(System.Type type)
		{
			string title = null;
			
			// Use the AddComponentMenu attribute to get the nice name for the component
			var attr = type.GetCustomAttributes(typeof(AddComponentMenu), false).FirstOrDefault() as AddComponentMenu;
			if (attr != null)
			{
				title = attr.componentMenu?.Trim();
				if (!string.IsNullOrEmpty(title))
				{
					var lastPathCharIndex = title.LastIndexOf('/');
					if (lastPathCharIndex >= 0)
					{
						if (lastPathCharIndex < title.Length - 1)
						{
							title = title.Substring(lastPathCharIndex + 1);
						}
					}
				}
			}
			// Fallback to just use the type name
			if (string.IsNullOrEmpty(title))
			{
				title = type.Name;
			}
			return title;
		}
	}
}
#endif