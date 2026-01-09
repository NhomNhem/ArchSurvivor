//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	public class BaseEditor : UnityEditor.Editor
	{
		protected const string s_aboutHelp = "About & Help";

		protected static readonly string DiscordUrl = "https://discord.gg/wKRzKAHVUE";
		protected static readonly string ForumBundleUrl = "https://discussions.unity.com/t/released-uifx-bundle-advanced-effects-for-unity-ui/940575";
		protected static readonly string GithubUrl = "https://github.com/Chocolate-Dinosaur/UIFX/issues";
		protected static readonly string SupportEmailUrl = "mailto:support@chocdino.com";
		protected static readonly string UIFXBundleWebsiteUrl = "https://www.chocdino.com/products/uifx/bundle/about/";
		protected static readonly string AssetStoreBundleUrl = "https://assetstore.unity.com/packages/slug/266945?aid=1100lSvNe";
		protected static readonly string AssetStoreBundleReviewUrl = "https://assetstore.unity.com/packages/slug/266945?aid=1100lSvNe#reviews";

		internal static readonly AboutInfo s_upgradeToBundle =
				new AboutInfo("Upgrade ★", "This asset is part of the <b>UIFX Bundle</b> asset.\r\n\r\nAs an existing customer you are entitled to a discounted upgrade!", "uifx-logo-bundle", BaseEditor.ShowUpgradeBundleButton)
				{
					sections = new AboutSection[]
					{
						new AboutSection("Upgrade")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("<color=#ffd700>★ </color>Upgrade to UIFX Bundle<color=#ffd700> ★</color>", AssetStoreBundleUrl),
							}
						},
						new AboutSection("Read More")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("About UIFX Bundle", UIFXBundleWebsiteUrl),
							}
						},
					}
				};

		internal static bool ShowUpgradeBundleButton(bool dummy)
		{
			return !DetectUIFXBundle();
		}

		internal static bool DetectUIFXBundle()
		{
			// This GUID is from FillGradientFilterEditor.cs.meta which is currently only included in the UIFX Bundle.
			const string fileGUID = "df03afa3ecd8c6941a0462c7c870ae83";
			return !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(fileGUID));
		}

		// <summary>
		// Creates a button that toggles between two texts but maintains the same size by using the size of the largest.
		// This is useful so that the button doesn't change size resulting in the mouse cursor no longer being over the button.
		// </summary>
		protected bool ToggleButton(bool value, GUIContent labelTrue, GUIContent labelFalse)
		{
			float maxWidth = Mathf.Max(GUI.skin.button.CalcSize(labelTrue).x, GUI.skin.button.CalcSize(labelFalse).x);
			GUIContent content = value ? labelTrue : labelFalse;
			return GUILayout.Button(content, GUILayout.Width(maxWidth));
		}

		protected void EnumAsToolbarCompact(SerializedProperty prop, GUIContent displayName = null)
		{
			if (displayName == null)
			{
				displayName = new GUIContent(prop.displayName);
			}

			if (prop.hasMultipleDifferentValues)
			{
				EditorGUILayout.PropertyField(prop, displayName);
				return;
			}

			Rect rect = EditorGUILayout.GetControlRect();
			EditorGUI.BeginProperty(rect, displayName, prop);

			EditorGUI.PrefixLabel(rect, displayName);
			rect.xMin += EditorGUIUtility.labelWidth;
			
			EditorGUI.BeginChangeCheck();
			int newIndex = prop.enumValueIndex;
			newIndex = GUI.Toolbar(rect, newIndex, prop.enumDisplayNames);

			// Only assign the value back if it was actually changed by the user.
			// Otherwise a single value will be assigned to all objects when multi-object editing,
			// even when the user didn't touch the control.
			if (EditorGUI.EndChangeCheck())
			{
				prop.enumValueIndex = newIndex;
			}

			EditorGUI.EndProperty();
		}

		protected void EnumAsToolbar(SerializedProperty prop, GUIContent displayName = null)
		{
			if (displayName == null)
			{
				displayName = new GUIContent(prop.displayName);
			}

			if (prop.hasMultipleDifferentValues)
			{
				EditorGUILayout.PropertyField(prop, displayName);
				return;
			}

			if (!EditorGUIUtility.wideMode)
			{
				EditorGUILayout.PropertyField(prop, displayName);
			}
			else
			{
				Rect rect = EditorGUILayout.GetControlRect(true);
				EditorGUI.BeginProperty(rect, displayName, prop);

				EditorGUI.PrefixLabel(rect, displayName);
				rect.xMin += EditorGUIUtility.labelWidth;
				
				EditorGUI.BeginChangeCheck();

				int newIndex = GUI.Toolbar(rect, prop.enumValueIndex, prop.enumDisplayNames);

				// Only assign the value back if it was actually changed by the user.
				// Otherwise a single value will be assigned to all objects when multi-object editing,
				// even when the user didn't touch the control.
				if (EditorGUI.EndChangeCheck())
				{
					prop.enumValueIndex = newIndex;
				}
				EditorGUI.EndProperty();
			}
		}

		protected void TextureScaleOffset(SerializedProperty propTexture, SerializedProperty propScale, SerializedProperty propOffset, GUIContent displayName)
		{
			EditorGUILayout.PropertyField(propTexture);
			//EditorGUILayout.PropertyField(_propTextureOffset, Content_Offset);
			//EditorGUILayout.PropertyField(_propTextureScale, Content_Scale);

			Rect rect = EditorGUILayout.GetControlRect(true, 2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), EditorStyles.layerMaskField);
			EditorGUI.BeginChangeCheck();
			Vector4 scaleOffset = new Vector4(propScale.vector2Value.x, propScale.vector2Value.y, propOffset.vector2Value.x, propOffset.vector2Value.y);
			EditorGUI.indentLevel++;
			scaleOffset = MaterialEditor.TextureScaleOffsetProperty(rect, scaleOffset, false);
			EditorGUI.indentLevel--;
			if (EditorGUI.EndChangeCheck())
			{
				propScale.vector2Value = new Vector2(scaleOffset.x, scaleOffset.y);
				propOffset.vector2Value = new Vector2(scaleOffset.z, scaleOffset.w);
			}
		}
		
		protected SerializedProperty VerifyFindProperty(string propertyPath)
		{
			SerializedProperty result = serializedObject.FindProperty(propertyPath);
			Debug.Assert(result != null);
			if (result == null)
			{
				Debug.LogError("Failed to find property '" + propertyPath + "' in object '" + serializedObject.ToString()+ "'");
			}
			return result;
		}

		internal static SerializedProperty VerifyFindPropertyRelative(SerializedProperty property, string relativePropertyPath)
		{
			SerializedProperty result = null;
			Debug.Assert(property != null);
			if (property == null)
			{
				Debug.LogError("Property is null while finding relative property '"+ relativePropertyPath + "'");
			}
			else
			{
				result = property.FindPropertyRelative(relativePropertyPath);
				Debug.Assert(result != null);
				if (result == null)
				{
					Debug.LogError("Failed to find relative property '" + relativePropertyPath + "' in property '" + property.name + "'");
				}
			}
			return result;
		}

		#if false
		void ShowAlignmentSelector()
		{
			GUILayoutOption layout = GUILayout.ExpandWidth(false);
			GUIStyle style = UnityEditor.EditorStyles.toolbarButton;
			//style.margin = new RectOffset(0, 0, 0, 0);
			//style.border = new RectOffset(0, 0, 0, 0);
		
			bool toggle;
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			toggle = _propGradientCenterX.floatValue == -1f;
			toggle = GUILayout.Toggle(toggle, "┏", style, layout);
			GUILayout.Toggle(false, "┳", style, layout);
			GUILayout.Toggle(false, "┓", style, layout);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Toggle(false, "┣", style, layout);
			GUILayout.Toggle(false, "╋", style, layout);
			GUILayout.Toggle(false, "┫", style, layout);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Toggle(false, "┗", style, layout);
			GUILayout.Toggle(false, "┻", style, layout);
			GUILayout.Toggle(false, "┛", style, layout);
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}
		#endif

		internal static Gradient GetGradient(SerializedProperty gradientProperty)
		{
			#if UNITY_2022_1_OR_NEWER
			return gradientProperty.gradientValue;
			#else
			System.Reflection.PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty("gradientValue", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (propertyInfo == null) { return null; }
			else { return propertyInfo.GetValue(gradientProperty, null) as Gradient; }
			#endif
		}
		internal static void SetGradient(SerializedProperty gradientProperty, Gradient value)
		{
			#if UNITY_2022_1_OR_NEWER
			gradientProperty.gradientValue = value;
			#else
			System.Reflection.PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty("gradientValue", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (propertyInfo != null) { propertyInfo.SetValue(gradientProperty, value); }
			#endif
		}
	}
}