//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ChocDino.UIFX.Editor
{
	internal static class Preferences
	{
		internal static readonly string SettingsPath = "Project/Chocolate Dinosaur/UIFX";

		private const string KeyPrefix = "UIFX.";

		private const string BakedImageSubfolderKey = KeyPrefix + "BakedImageSubfolder";
		private const string ShowHeaderAboutButtonsKey = KeyPrefix + "ShowHeaderAboutButtons";
		private const string ShowFooterBakeButtonsKey = KeyPrefix + "ShowFooterBakeButtons";
		private const string CameraSourcePreviewExpandKey = KeyPrefix + "CameraSource.PreviewExpanded";

		internal const string DefaultBakedImageAssetsSubfolder = "Baked-Images";

		internal static string BakedImageSubfolder
		{
			get { return EditorPrefs.GetString(BakedImageSubfolderKey, DefaultBakedImageAssetsSubfolder); }
			set { EditorPrefs.SetString(BakedImageSubfolderKey, value); }
		}

		internal static bool ShowHeaderAboutButtons
		{
			get { return EditorPrefs.GetBool(ShowHeaderAboutButtonsKey, true); }
			set { EditorPrefs.SetBool(ShowHeaderAboutButtonsKey, value); }
		}

		internal static bool ShowFooterBakeButtons
		{
			get { return EditorPrefs.GetBool(ShowFooterBakeButtonsKey, true); }
			set { EditorPrefs.SetBool(ShowFooterBakeButtonsKey, value); }
		}

		internal static bool CameraSourcePreviewExpand
		{
			get { return EditorPrefs.GetBool(CameraSourcePreviewExpandKey, true); }
			set { EditorPrefs.SetBool(CameraSourcePreviewExpandKey, value); }
		}


		[SettingsProvider]
		static SettingsProvider CreateSettingsProvider()
		{
			return new UIFXSettingsProvider(SettingsPath, SettingsScope.Project);
		}

		private class UIFXSettingsProvider : SettingsProvider
		{
			public UIFXSettingsProvider(string path, SettingsScope scope) : base(path, scope)
			{
				this.keywords = new HashSet<string>(new[] { "UIFX", "Chocolate", "Dinosaur", "ChocDino", "UI", "GUI", "UGUI" });
			}

			private static readonly GUIContent Content_BakedImagesFolder = new GUIContent("Baked Images Folder", "Folder path for baked images. Path is relative to Assets folder.");
			private static readonly GUIContent Content_ShowHeaderAboutButtons = new GUIContent("Show Header About Buttons", "Show the about/help buttons that appear at the top of each UIFX component.");
			private static readonly GUIContent Content_ShowFooterBakeButtons = new GUIContent("Show Footer Bake Buttons", "Show the bake/save PNG buttons that appear at the bottom of each UIFX component.");

			private string _defines;
			private string _oldDefines;
			private bool _unappliedChanges;
			private BuildTargetGroup _buildTarget;

			public override void OnActivate(string searchContext, VisualElement rootElement)
			{
				_buildTarget = BuildTargetGroup.Unknown;
			}

			public override void OnDeactivate()
			{
			}

			private void CacheDefines()
			{
#if UNITY_2023_1_OR_NEWER
				var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(_buildTarget);
				_oldDefines = _defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
				_oldDefines = _defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(_buildTarget);
#endif
			}

			private void ApplyDefines()
			{
#if UNITY_2023_1_OR_NEWER
				var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(_buildTarget);
				PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, _defines);
#else
				PlayerSettings.SetScriptingDefineSymbolsForGroup(_buildTarget, _defines);
#endif
				CacheDefines();
			}

			private bool HasDefine(string define)
			{
				return (_defines.IndexOf(define) >= 0);
			}

			private void AddDefine(string define)
			{
				_defines = (_defines + ";" + define + ";").Replace(";;", ";");
			}

			private void RemoveDefine(string define)
			{
				_defines = _defines.Replace(define, "").Replace(";;", ";");
			}

			private bool HasDefineChanged(string define)
			{
				bool a = HasDefine(define);
				bool b = (_oldDefines.IndexOf(define) >= 0);
				return (a != b);
			}

			public override void OnTitleBarGUI()
			{
				if (_unappliedChanges)
				{
					GUI.color = Color.green;
					if (GUILayout.Button("Apply Changes"))
					{
						ApplyDefines();
					}
					GUI.color = Color.white;
				}
			}

			public override void OnFooterBarGUI()
			{
			}

			public override void OnGUI(string searchContext)
			{
				const string UIFX_SUPPORT_TEXT_ANIMATOR = "UIFX_SUPPORT_TEXT_ANIMATOR";
				const string UIFX_BETA = "UIFX_BETA";
				const string UIFX_UNRELEASED = "UIFX_UNRELEASED";
				const string UIFX_FILTER_HIDE_INSPECTOR_PREVIEW = "UIFX_FILTER_HIDE_INSPECTOR_PREVIEW";
				const string UIFX_LOG = "UIFX_LOG";
				const string UIFX_FILTER_DEBUG = "UIFX_FILTER_DEBUG";
				const string UIFX_FILTERS_FORCE_UPDATE_PLAYMODE = "UIFX_FILTERS_FORCE_UPDATE_PLAYMODE";
				const string UIFX_FILTERS_FORCE_UPDATE_EDITMODE = "UIFX_FILTERS_FORCE_UPDATE_EDITMODE";

				EditorGUILayout.Space();

				ShowEditorPref(Content_BakedImagesFolder, BakedImageSubfolderKey, DefaultBakedImageAssetsSubfolder);

				EditorGUI.BeginChangeCheck();
				ShowEditorPref(Content_ShowHeaderAboutButtons, ShowHeaderAboutButtonsKey, true);
				ShowEditorPref(Content_ShowFooterBakeButtons, ShowFooterBakeButtonsKey, true);
				if (EditorGUI.EndChangeCheck())
				{
					UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
				}

				EditorGUILayout.Space();

				GUILayout.Label("Active platform is: " + BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget).ToString());

				bool isActivePlatform = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) == _buildTarget;

				bool changes = false;
				{
					if (isActivePlatform)
					{
						GUI.color = new Color(1.1f, 1.1f, 1.1f, 1f);
					}
					BuildTargetGroup group = EditorGUILayout.BeginBuildTargetSelectionGrouping();
					if (_buildTarget != group)
					{
						_buildTarget = group;
						CacheDefines();
					}
					changes |= ShowDefineToggle("Text Animator Support (Requires Febucci.TextAnimator package)", UIFX_SUPPORT_TEXT_ANIMATOR);
					changes |= ShowDefineToggle("Hide Inspector Filter Preview", UIFX_FILTER_HIDE_INSPECTOR_PREVIEW);
					changes |= ShowDefineToggle("Beta Features", UIFX_BETA);
					EditorGUILayout.EndBuildTargetSelectionGrouping();
				}
				GUI.color = Color.white;
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

				if (SystemInfo.deviceName == "DESKTOP-ULTRA")
				{
					GUILayout.Label("Developer Mode:", EditorStyles.largeLabel);
					if (isActivePlatform)
					{
						GUI.color = new Color(1.1f, 1.1f, 1.1f, 1f);
					}
					BuildTargetGroup group = EditorGUILayout.BeginBuildTargetSelectionGrouping();
					if (_buildTarget != group)
					{
						_buildTarget = group;
						CacheDefines();
					}
					changes |= ShowDefineToggle("Debug Logging", UIFX_LOG);
					changes |= ShowDefineToggle("Filter Debugging", UIFX_FILTER_DEBUG);
					changes |= ShowDefineToggle("Filter Force Update in Play Mode", UIFX_FILTERS_FORCE_UPDATE_PLAYMODE);
					changes |= ShowDefineToggle("Filter Force Update in Edit Mode", UIFX_FILTERS_FORCE_UPDATE_EDITMODE);
					changes |= ShowDefineToggle("Unreleased Features", UIFX_UNRELEASED);
					EditorGUILayout.EndBuildTargetSelectionGrouping();
					GUI.color = Color.white;

					EditorGUILayout.Space();
					EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
				}

				_unappliedChanges = changes;

				Links();
			}

			private void Links()
			{
				const string DiscordCommunityUrl = "https://discord.gg/wKRzKAHVUE";
				const string DocumentationUrl = "https://www.chocdino.com/products/category/components/";
				const string AssetStoreUrl = "https://assetstore.unity.com/publishers/80225?aid=1100lSvNe";

				GUILayout.Label("Chocolate Dinosaur Links:", EditorStyles.largeLabel);

				if (GUILayout.Button("UIFX Documentation", EditorStyles.miniButton))
				{
					Application.OpenURL(DocumentationUrl);
				}
				if (GUILayout.Button("Discord Community", EditorStyles.miniButton))
				{
					Application.OpenURL(DiscordCommunityUrl);
				}
				if (GUILayout.Button("Our Assets", EditorStyles.miniButton))
				{
					Application.OpenURL(AssetStoreUrl);
				}
			}

			private bool ShowDefineToggle(string label, string define)
			{
				bool enabled = HasDefine(define);
				bool changed = HasDefineChanged(define);
				if (changed)
				{
					label += " *";
				}

				bool newState = GUILayout.Toggle(enabled, label);
				if (newState != enabled)
				{
					if (newState)
					{
						AddDefine(define);
					}
					else
					{
						RemoveDefine(define);
					}
				}

				return HasDefineChanged(define);
			}

			private static void ShowEditorPref(GUIContent label, string prefKey, string defaultValue)
			{
				var oldValue = EditorPrefs.GetString(prefKey, defaultValue);
				GUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(label, EditorStyles.textField);
				var newValue = EditorGUILayout.TextField(oldValue);
				if (newValue != oldValue)
				{
					EditorPrefs.SetString(prefKey, newValue);
				}
				GUILayout.EndHorizontal();
			}
			
			private static void ShowEditorPref(GUIContent label, string prefKey, bool defaultValue)
			{
				var oldValue = EditorPrefs.GetBool(prefKey, defaultValue);
				var newValue = GUILayout.Toggle(oldValue, label);
				if (newValue != oldValue)
				{
					EditorPrefs.SetBool(prefKey, newValue);
				}
			}
		}
	}
}