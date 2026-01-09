//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(GlowFilter), true)]
	[CanEditMultipleObjects]
	internal class GlowFilterEditor : FilterBaseEditor
	{

		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Glow Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-glow-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/glow-filter/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/glow-filter/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/glow-filter/components/glow-filter/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/uifx/glow-filter/API/ChocDino.UIFX/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX - Glow Filter</b>", "https://assetstore.unity.com/packages/slug/274847?aid=1100lSvNe#reviews"),
								new AboutButton("Review <b>UIFX Bundle</b>", AssetStoreBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Discussions", ""),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, s_aboutInfo } );

		private static readonly GUIContent Content_Shape = new GUIContent("Shape");
		private static readonly GUIContent Content_Falloff = new GUIContent("Falloff");
		private static readonly GUIContent Content_Distance = new GUIContent("Distance");
		private static readonly GUIContent Content_Mode = new GUIContent("Mode");
		private static readonly GUIContent Content_Curve = new GUIContent("Curve");
		private static readonly GUIContent Content_Energy = new GUIContent("Energy");
		private static readonly GUIContent Content_Power = new GUIContent("Power");
		private static readonly GUIContent Content_Gamma = new GUIContent("Gamma");
		private static readonly GUIContent Content_Offset = new GUIContent("Offset");

		private SerializedProperty _propEdgeSide;
		private SerializedProperty _propDistanceShape;
		private SerializedProperty _propMaxDistance;
		private SerializedProperty _propReuseDistanceMap;
		private SerializedProperty _propFalloffMode;
		private SerializedProperty _propExpFalloffEnergy;
		private SerializedProperty _propExpFalloffPower;
		private SerializedProperty _propExpFalloffOffset;
		private SerializedProperty _propFalloffCurve;
		private SerializedProperty _propFalloffCurveGamma;
		private SerializedProperty _propFillMode;
		private SerializedProperty _propColor;
		private SerializedProperty _propGradient;
		private SerializedProperty _propGradientTexture;
		private SerializedProperty _propGradientOffset;
		private SerializedProperty _propGradientGamma;
		private SerializedProperty _propGradientReverse;
		private SerializedProperty _propBlur;
		private SerializedProperty _propSourceAlpha;
		private SerializedProperty _propAdditive;
		private SerializedProperty _propNoiseScale;
		private SerializedProperty _propStrength;
		private SerializedProperty _propRenderSpace;
		private SerializedProperty _propExpand;

		void OnEnable()
		{
			_propDistanceShape = VerifyFindProperty("_distanceShape");
			_propMaxDistance = VerifyFindProperty("_maxDistance");
			_propEdgeSide = VerifyFindProperty("_edgeSide");
			_propBlur = VerifyFindProperty("_blur");
			_propReuseDistanceMap = VerifyFindProperty("_reuseDistanceMap");

			_propFalloffMode = VerifyFindProperty("_falloffMode");
			_propExpFalloffEnergy = VerifyFindProperty("_expFalloffEnergy");
			_propExpFalloffPower = VerifyFindProperty("_expFalloffPower");
			_propExpFalloffOffset = VerifyFindProperty("_expFalloffOffset");
			_propFalloffCurve = VerifyFindProperty("_falloffCurve");
			_propFalloffCurveGamma = VerifyFindProperty("_falloffCurveGamma");

			_propFillMode = VerifyFindProperty("_fillMode");
			_propColor = VerifyFindProperty("_color");
			_propGradient = VerifyFindProperty("_gradient");
			_propGradientTexture = VerifyFindProperty("_gradientTexture");
			_propGradientOffset = VerifyFindProperty("_gradientOffset");
			_propGradientGamma = VerifyFindProperty("_gradientGamma");
			_propGradientReverse = VerifyFindProperty("_gradientReverse");

			_propAdditive = VerifyFindProperty("_additive");
			_propNoiseScale = VerifyFindProperty("_noiseScale");
			_propSourceAlpha = VerifyFindProperty("_sourceAlpha");
			_propStrength = VerifyFindProperty("_strength");
			_propRenderSpace = VerifyFindProperty("_renderSpace");
			_propExpand = VerifyFindProperty("_expand");
		}

		public override void OnInspectorGUI()
		{
			s_aboutToolbar.OnGUI();

			serializedObject.Update();

			var filter = this.target as FilterBase;

			if (OnInspectorGUI_Check(filter))
			{
				return;
			}

			GUILayout.Label(Content_Distance, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EnumAsToolbar(_propEdgeSide);
			EnumAsToolbar(_propDistanceShape, Content_Shape);
			EditorGUILayout.PropertyField(_propMaxDistance);
			EditorGUILayout.PropertyField(_propBlur);
			EditorGUILayout.PropertyField(_propReuseDistanceMap);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Falloff, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EnumAsToolbar(_propFalloffMode, Content_Mode);
			if (_propFalloffMode.enumValueIndex == (int)GlowFalloffMode.Exponential)
			{
				EditorGUILayout.PropertyField(_propExpFalloffEnergy, Content_Energy);
				EditorGUILayout.PropertyField(_propExpFalloffPower, Content_Power);
				EditorGUILayout.PropertyField(_propExpFalloffOffset, Content_Offset);
			}
			else
			{
				EditorGUILayout.PropertyField(_propFalloffCurve, Content_Curve);
				// Show a warning if curve key values of out of sensible range.
				// NOTE: We have to detect whether the curve window has focus currently, otherwise if the HelpBox() appears
				// while dragging curve keys, it can cause the keys to not update.
				if (!EditorHelper.IsEditingCurve())
				{
					if (_propFalloffCurve.animationCurveValue.HasOutOfRangeValues(0f, 1f, 0f))
					{
						EditorGUILayout.HelpBox("Some curve points are outside of the range [0..1]. This might be fine, or could lead to unexpected results.", MessageType.Warning, true);
					}
				}
				EditorGUILayout.PropertyField(_propFalloffCurveGamma, Content_Gamma);
			}
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Fill, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EnumAsToolbar(_propFillMode, Content_Mode);
			if (_propFillMode.enumValueIndex == (int)GlowFillMode.Color)
			{
				EditorGUILayout.PropertyField(_propColor);
			}
			if (_propFillMode.enumValueIndex == (int)GlowFillMode.Texture)
			{
				EditorGUILayout.PropertyField(_propGradientTexture, Content_Texture);
				EditorGUILayout.PropertyField(_propGradientOffset, Content_Offset);
				EditorGUILayout.PropertyField(_propGradientGamma, Content_Gamma);
				EditorGUILayout.PropertyField(_propGradientReverse, Content_Reverse);
			}
			else if (_propFillMode.enumValueIndex == (int)GlowFillMode.Gradient)
			{
				EditorGUILayout.PropertyField(_propGradient);
				EditorGUILayout.PropertyField(_propGradientOffset, Content_Offset);
				EditorGUILayout.PropertyField(_propGradientGamma, Content_Gamma);
				EditorGUILayout.PropertyField(_propGradientReverse, Content_Reverse);
			}
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Apply, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propNoiseScale);
			EditorGUILayout.PropertyField(_propAdditive);
			EditorGUILayout.PropertyField(_propSourceAlpha);
			EnumAsToolbarCompact(_propRenderSpace);
			EnumAsToolbarCompact(_propExpand);
			DrawStrengthProperty(_propStrength);
			EditorGUI.indentLevel--;

			if (OnInspectorGUI_Baking(filter))
			{
				return;
			}
			
			FilterBaseEditor.OnInspectorGUI_Debug(filter);

			serializedObject.ApplyModifiedProperties();
		}
	}
}