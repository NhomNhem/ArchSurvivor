//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.UI;
using UnityEditor; 

namespace ChocDino.UIFX.Editor
{
	internal class FilterBaseEditor : BaseEditor
	{
		protected static readonly string TextMeshProGraphicTypeName = "TMPro.TextMeshProUGUI";
		protected static readonly string FilterStackTextMeshProFullTypeName = "ChocDino.UIFX.FilterStackTextMeshPro, ChocDino.UIFX.TMP";
		protected static readonly string FilterStackTextMeshProComponentName = "FilterStackTextMeshPro";

		protected static readonly GUIContent Content_R = new GUIContent("R");
		protected static readonly GUIContent Content_SpaceCharacter = new GUIContent(" ");
		protected static readonly GUIContent Content_Size = new GUIContent("Size");
		protected static readonly GUIContent Content_Color = new GUIContent("Color");
		protected static readonly GUIContent Content_Colors = new GUIContent("Colors");
		protected static readonly GUIContent Content_Apply = new GUIContent("Apply");
		protected static readonly GUIContent Content_Border = new GUIContent("Border");
		protected static readonly GUIContent Content_Fill = new GUIContent("Fill");
		protected static readonly GUIContent Content_Preview = new GUIContent("Preview");
		protected static readonly GUIContent Content_Stop = new GUIContent("Stop");
		protected static readonly GUIContent Content_Texture = new GUIContent("Texture");
		protected static readonly GUIContent Content_Gradient = new GUIContent("Gradient");
		protected static readonly GUIContent Content_Reverse = new GUIContent("Reverse");

		private static readonly GUIContent Content_Debug = new GUIContent("Debug");
		private static readonly GUIContent Content_Strength = new GUIContent("Strength");
		private static readonly GUIContent Content_SaveToPNG = new GUIContent("Save to PNG");
		private static readonly GUIContent Content_BakeToImage = new GUIContent("Bake To Image", "Bake all filters above to an Image component. Baking currently doesn't support: world-space canvas, rotation or scale, and unmatching anchor min/max values, TextMeshPro.");
		private static readonly GUIContent Content_PreviewTitle= new GUIContent("UIFX - Filters");

		private void ShowSaveToPngButton()
		{
			// TODO: Add baking to Image option that bakes to a new GameObject and doesn't destroy the old object
			// TODO: Convert the image baking buttons into a EditorGUILayout.DropDownToggle()
			if (GUILayout.Button(Content_SaveToPNG))
			{
				foreach (FilterBase filter in this.targets)
				{
					System.IO.Directory.CreateDirectory(Application.dataPath + "/../Captures/");
					string timestamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
					string filterName = filter.GetType().Name;
					string path = System.IO.Path.GetFullPath(Application.dataPath + string.Format("/../Captures/Image-{0}-{1}-{2}.png", timestamp, filter.gameObject.name, filterName));
					if (filter.SaveToPNG(path))
					{
						Debug.Log("Saving image to: <b>" + path  + "</b>");
					}
					else
					{
						Debug.LogError("Failed to save image to: <b>" + path  + "</b>");
					}
				}
			}
		}

		#if !UIFX_FILTER_HIDE_INSPECTOR_PREVIEW

		private string GetFilterShortName()
		{
			return this.target.GetType().Name;
		}

		public override GUIContent GetPreviewTitle()
		{
			return Content_PreviewTitle;
		}

		public override bool HasPreviewGUI()
		{
			// We can't support multiple targets because the below logic to only show the last item doesn't work
			if (targets.Length > 1) return false;

			var filter = target as FilterBase;
			if (!filter.isActiveAndEnabled) return false;

			// Only allow the last ENABLED filter in the stack to preview
			var filters = filter.gameObject.GetComponents<FilterBase>();
			FilterBase lastEnabledFilter = null;
			for (int i = 0; i < filters.Length; i++)
			{
				if (filters[i].isActiveAndEnabled && filters[i].IsFilterEnabled() && filters[i].CanSelfRender())
				{
					lastEnabledFilter = filters[i];
				}
			}
			return (lastEnabledFilter == filter);
		}

		public override string GetInfoString()
		{
			var filter = target as FilterBase;
			return filter.GetDebugString();
		}

		public override void OnPreviewSettings()
		{
			ShowSaveToPngButton();
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			if (Event.current.type == EventType.Repaint)
			{
				var filter = target as FilterBase;
				EditorGUI.DrawTextureTransparent(r, Texture2D.blackTexture, ScaleMode.StretchToFill);
				var texture = filter.ResolveToTexture();
				if (texture)
				{
					GUI.DrawTexture(r, texture, ScaleMode.ScaleToFit, true);
				}
			}
		}
		#endif

		private static void DrawTexture(Texture texture, bool showAlphaBlended)
		{
			if (texture)
			{
				GUILayout.Label(string.Format("{0} {1}x{2}:{3}", texture.name, texture.width, texture.height, texture.updateCount));

				if (showAlphaBlended)
				{
					{
						Rect r = GUILayoutUtility.GetRect(256f, 256f);
						EditorGUI.DrawTextureTransparent(r, Texture2D.blackTexture, ScaleMode.StretchToFill);
						GUI.DrawTexture(r, texture, ScaleMode.ScaleToFit, true);
					}
				}
				else
				{
					GUILayout.BeginHorizontal();
					{
						float aspect = (float)texture.width / (float)texture.height;
						Rect r = GUILayoutUtility.GetAspectRect(aspect * 2f, GUILayout.ExpandWidth(true));
						Rect rc = r;
						rc.width /= 2f;
						GUI.DrawTexture(rc, texture, ScaleMode.ScaleToFit, false);
						rc.x += rc.width;
						EditorGUI.DrawTextureAlpha(rc, texture, ScaleMode.ScaleToFit);
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Select"))
				{
					UnityEditor.Selection.activeObject = texture;
				}
				if (GUILayout.Button("Save"))
				{
					SaveTexture(texture as RenderTexture);
				}
				GUILayout.EndHorizontal();
			}
		}

		private bool CanBakeFiltersToImage(FilterBase filter)
		{
			if (!filter.CanApplyFilter()) { return false; }
			var graphic = filter.GetComponent<Graphic>();
			if (!graphic) { return false; }

			var graphicTypeName = graphic.GetType().ToString();
			if (graphicTypeName.Contains(TextMeshProGraphicTypeName)) { return false; }

			var canvas = graphic.canvas;
			if (!canvas) { return false; }

			// NOTE: In the future when working in canvas space we'll be able to support these
			if (canvas.renderMode == RenderMode.WorldSpace) { return false; }

		 	var xform = filter.GetComponent<RectTransform>();
			if (!xform) { return false; }

			// NOTE: In the future when working in canvas space we'll be able to support these
			if (xform.localRotation != Quaternion.identity) { return false;}
			if (xform.localScale != Vector3.one) { return false;}

			// NOTE: Currently just haven't figured out the math for this case where anchor min/max are different
			if (xform.anchorMin.x != xform.anchorMax.x) { return false; }
			if (xform.anchorMin.y != xform.anchorMax.y) { return false; }

			return true;
		}

		private bool CanSaveFilterToPng(FilterBase filter)
		{
			if (!filter.CanApplyFilter()) { return false; }
			var graphic = filter.GetComponent<Graphic>();
			if (!graphic) { return false; }

			var graphicTypeName = graphic.GetType().ToString();
			if (graphicTypeName.Contains(TextMeshProGraphicTypeName)) { return false; }

			var canvas = graphic.canvas;
			if (!canvas) { return false; }

			// NOTE: In the future when working in canvas space we'll be able to support these
			if (canvas.renderMode == RenderMode.WorldSpace) { return false; }

		 	var xform = filter.GetComponent<RectTransform>();
			if (!xform) { return false; }

			return true;
		}

		private bool BakeFiltersToImage(FilterBase filter, bool makeCopy)
		{
			Vector2 anchorOffset = Vector2.zero;
			{
				var graphic = filter.GetComponent<Graphic>();
				if (graphic)
				{
				 	var xform = graphic.GetComponent<RectTransform>();
					Rect r = filter.GetLocalRect();
					r.max += filter._lastRenderAdjustRightUp;
					r.min -= filter._lastRenderAdjustLeftDown;
					anchorOffset.x = r.x + r.width * xform.pivot.x + xform.anchoredPosition.x;
					anchorOffset.y = r.y + r.height * xform.pivot.y + xform.anchoredPosition.y;
				}
			}

			// Bake to new texture asset
			string uniqueFileName;
			{
				string timestamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
				string subFolder = Preferences.BakedImageSubfolder;
				subFolder = System.IO.Path.Combine("Assets/", subFolder);
				if (!subFolder.EndsWith("/")) {  subFolder += "/"; }
				subFolder = System.IO.Path.Combine(subFolder, string.Format("Baked-{0}-{1}.png", timestamp, filter.gameObject.name));
				var fullPath = System.IO.Path.GetFullPath(Application.dataPath + "/../" + subFolder);
				string directoryPath = System.IO.Path.GetDirectoryName(fullPath);
				if (!System.IO.Directory.Exists(directoryPath))
				{
					System.IO.Directory.CreateDirectory(directoryPath);
				}
				uniqueFileName = AssetDatabase.GenerateUniqueAssetPath(subFolder);
				fullPath = System.IO.Path.GetFullPath(Application.dataPath + "/../" + uniqueFileName);
				AssetDatabase.StartAssetEditing();
				filter.SaveToPNG(fullPath);
				AssetDatabase.StopAssetEditing();
				AssetDatabase.Refresh();
			}

			// Modify importer settings for new texture asset
			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(uniqueFileName);
			if (importer)
			{
				importer.textureType = TextureImporterType.Sprite;
				importer.textureCompression = TextureImporterCompression.Uncompressed;
				importer.npotScale = TextureImporterNPOTScale.None;
				importer.alphaIsTransparency = true;
				importer.mipmapEnabled = false;
				importer.sRGBTexture = true;

				TextureImporterSettings settings = new TextureImporterSettings();
				importer.ReadTextureSettings(settings); 
				settings.spriteMode = (int)SpriteImportMode.Single;
				settings.spriteMeshType = SpriteMeshType.FullRect;
				settings.spriteGenerateFallbackPhysicsShape = false;
				importer.SetTextureSettings(settings);	

				EditorUtility.SetDirty(importer);
				importer.SaveAndReimport();
				AssetDatabase.WriteImportSettingsIfDirty(uniqueFileName);
			}

			// Get the asset
			var textureAsset = (Texture2D)AssetDatabase.LoadAssetAtPath(uniqueFileName, typeof(Texture2D));
			Sprite spriteAsset = null;
			{
				Object[] objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(uniqueFileName);
				if (objs != null)
				{
					foreach (Object obj in objs)
					{
						if (obj.GetType() == typeof(Sprite))
						{
							spriteAsset = obj as Sprite;
							break;
						}
					}
				}
			}

			GameObject targetGo = filter.gameObject;

			// Make a copy of the GameObject (optional)
			if (makeCopy)
			{
				targetGo = (GameObject)GameObject.Instantiate(filter.gameObject, filter.transform.parent, instantiateInWorldSpace:false);
				targetGo.name = filter.gameObject.name + "-Baked";
			}

			// Get the component index of the original filter
			int filterComponentIndex = 0;
			{
				Component[] sourceComponents = filter.gameObject.GetComponents<Component>();
				filterComponentIndex = System.Array.IndexOf(sourceComponents, filter);
			}

			Component[] components = targetGo.GetComponents<Component>();
			
			int oldGraphicIndex = 0;
			bool isRaycastTarget = false;
			// Remove the previous Graphic
			{
				var graphic = targetGo.GetComponent<Graphic>();
				if (graphic)
				{
					isRaycastTarget = graphic.raycastTarget;
					oldGraphicIndex = System.Array.IndexOf(components, graphic);
					Undo.DestroyObjectImmediate(graphic);
				}
			}

			// Add RawImage component and assign texture
			var image = Undo.AddComponent<RawImage>(targetGo);
			//image.sprite = spriteAsset;
			image.texture = textureAsset;
			image.raycastTarget = isRaycastTarget;
			//image.preserveAspect = true;
			image.maskable = false;
			float scale = image.canvas.scaleFactor;

			// Move RawImage component to order position of old Graphic
			int newGraphicIndex = (components.Length - 1);
			int moveDelta = Mathf.Abs(oldGraphicIndex - newGraphicIndex);
			for (int i = 0; i < moveDelta; i++)
			{
				if (newGraphicIndex > oldGraphicIndex) { UnityEditorInternal.ComponentUtility.MoveComponentUp(image); }
				else { UnityEditorInternal.ComponentUtility.MoveComponentDown(image); }
			}

			// Change the RectTransform
			{
				var xform = image.rectTransform;
 				xform.sizeDelta = new Vector2(textureAsset.width / scale, textureAsset.height / scale);
				xform.anchoredPosition = anchorOffset;
			}

			// Remove all FilterBase and IMeshModifiers that affect this render
			{
				int componentIndex = 0;
				foreach (var component in components)
				{
					if (componentIndex <= filterComponentIndex)
					{
						if (component is FilterBase || component is IMeshModifier)
						{
							Undo.DestroyObjectImmediate(component);
						}
					}
					componentIndex++;
				}
			}

			// Select the texture so the user knows where it exists
			//Selection.activeObject = textureAsset;

			//Object prevSelection  = Selection.activeObject;
			// Select the old or new GameObject
			if (makeCopy)
			{
				Selection.activeGameObject = targetGo;
			}
			//else
			{
			//	Selection.activeObject = prevSelection;
			}

			//Undo.DestroyObjectImmediate(filter);

			return true;
		}

		private bool ShowBakeFiltersToImageDialog(FilterBase filter)
		{
			bool result = false;
			int option = EditorUtility.DisplayDialogComplex("Bake To Image", "Do you want to override the current GameObject or bake to a copy of it?", "Override", "Cancel", "Make Copy"); 
			switch (option)
			{
				// Override.
				case 0:
					result = BakeFiltersToImage(filter, makeCopy:false);
					break;
				// Make Copy.
				case 2:
					result = BakeFiltersToImage(filter, makeCopy:true);
					break;
				// Cancel.
				case 1:
					break;
				default:
					Debug.LogError("Unrecognized option.");
					break;
			}
			return result;
		}

		internal bool OnInspectorGUI_Baking(FilterBase filter)
		{
			if (!Preferences.ShowFooterBakeButtons)
			{
				return false;
			}

			bool result = false;
			
			EditorGUILayout.Space();
			GUILayout.BeginHorizontal();
			bool wasEnabled = GUI.enabled;
			GUI.enabled = wasEnabled & CanBakeFiltersToImage(filter);
			if (GUILayout.Button(Content_BakeToImage))
			{
				// Use a delay because we're showing a dialog which breaks IMGUI flow
				EditorApplication.delayCall += () =>
				{
					ShowBakeFiltersToImageDialog(filter);
				};
			}
			GUI.enabled = wasEnabled & CanSaveFilterToPng(filter);
			ShowSaveToPngButton();
			GUI.enabled = wasEnabled;
			GUILayout.EndHorizontal();

			return result;
		}

		// Returns true if checks fail and the rest of the inspector shouldn't be shown
		internal bool OnInspectorGUI_Check(FilterBase filter)
		{
			bool result = false;
			var graphic = filter.GraphicComponent;
			if (graphic)
			{
				// Check for TextMeshPro in the selection
				bool selectionHasTextMeshPro = false;
				foreach (var obj in this.targets)
				{
					var filterInstance = obj as FilterBase;
					if (filterInstance.GraphicComponent)
					{
						if (filterInstance.GraphicComponent.GetType().ToString().Contains(TextMeshProGraphicTypeName))
						{
							selectionHasTextMeshPro = true;
							break;
						}
					}
				}

				if (selectionHasTextMeshPro)
				{
				#if !UIFX_TMPRO
					result = true;
					EditorGUILayout.HelpBox("TextMeshPro not detected...", MessageType.Error, true);
					EditorGUILayout.Space();
				#else
					bool needsFilterStackTMP = false;
					foreach (var obj in this.targets)
					{
						var filterInstance = obj as FilterBase;
						if (filterInstance.GraphicComponent)
						{
							if (filterInstance.GraphicComponent.GetType().ToString().Contains(TextMeshProGraphicTypeName))
							{
								if (null == filterInstance.gameObject.GetComponent(FilterStackTextMeshProComponentName))
								{
									needsFilterStackTMP = true;
									break;
								}
							}
						}
					}
					if (needsFilterStackTMP)
					{
						result = true;
						EditorGUILayout.HelpBox("The FilterStackTextMeshPro component is required to apply filters to TextMeshPro", MessageType.Error, true);
						if (GUILayout.Button("Add FilterStackTextMeshPro"))
						{
							var filterStackType = System.Type.GetType(FilterStackTextMeshProFullTypeName, false);
							if (filterStackType != null)
							{
								foreach (var obj in this.targets)
								{
									var filterInstance = obj as FilterBase;
									if (filterInstance.GraphicComponent)
									{
										if (filterInstance.GraphicComponent.GetType().ToString().Contains(TextMeshProGraphicTypeName))
										{
											if (null == filterInstance.gameObject.GetComponent(FilterStackTextMeshProComponentName))
											{
												filterInstance.gameObject.AddComponent(filterStackType);
											}
										}
									}
								}
							}
							else
							{
								Debug.LogError("Couldn't find " + FilterStackTextMeshProFullTypeName);
							}
						}
						EditorGUILayout.Space();
					}
				#endif
				}

				// Warn user about out of range Canvas.planeDistance
				if (filter.IsCanvasPlaneDistanceOutOfRange())
				{
					EditorGUILayout.HelpBox(FilterBase.s_warnCanvasPlaneDistanceCullingMessage, MessageType.Warning, true);
					EditorGUILayout.Space();
				}

				if (filter.IsTextureTooLarge())
				{
					EditorGUILayout.HelpBox("Requested texture is larger than the supported size of " + Filters.GetMaxiumumTextureSize() + ", rescaling texture to supported size, this can lead to lower texture quality. Consider invstigating why such a large texture is required.", MessageType.Error, true);
					EditorGUILayout.Space();
				}
			}
			return result;
		}

		internal static void OnInspectorGUI_Debug(FilterBase filter)
		{
#if UIFX_FILTER_DEBUG
			{
				EditorGUILayout.Space();
				GUILayout.Label(Content_Debug, EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				EditorGUILayout.TextArea(filter.GetDebugString());
				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
				Texture[] textures = filter.GetDebugTextures();
				foreach (var texture in textures)
				{
					DrawTexture(texture, false);
					EditorGUILayout.Space();
				}

				if (filter.isActiveAndEnabled)
				{
					DrawTexture(filter.ResolveToTexture(), true);
					if (GUILayout.Button("Save Final"))
					{
						filter.SaveToPNG(Application.dataPath + "/../final.png");
					}
				}
			}
#endif
		}

		internal static void PropertyReset_Slider(SerializedProperty prop, GUIContent label, float min, float max, float resetValue)
		{
			float buttonWidth = 0f;
			if (EditorGUIUtility.wideMode)
			{
				buttonWidth = GUI.skin.button.CalcSize(Content_R).x;
			}

			Rect rect = EditorGUILayout.GetControlRect(true);
			EditorGUI.BeginProperty(rect, label, prop);

			rect.xMax -= buttonWidth;
			EditorGUI.BeginChangeCheck();
			float newValue = EditorGUI.Slider(rect, label, prop.floatValue, min, max);
			if (EditorGUI.EndChangeCheck())
			{
				prop.floatValue = newValue;
			}

			if (EditorGUIUtility.wideMode)
			{
				rect.xMin += rect.width;
				rect.xMax += buttonWidth;
				
				{
					bool wasEnabled = GUI.enabled;
					GUI.enabled = (prop.floatValue != resetValue);
					if (GUI.Button(rect, Content_R))
					{
						prop.floatValue = resetValue;
					}
					GUI.enabled = wasEnabled;
				}
			}

			EditorGUI.EndProperty();
		}

		internal static void PropertyReset_Float(SerializedProperty prop, float resetValue)
		{
			if (!EditorGUIUtility.wideMode)
			{
				EditorGUILayout.PropertyField(prop);
			}
			else
			{
				GUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(prop);
				bool wasEnabled = GUI.enabled;
				GUI.enabled = (prop.floatValue != resetValue);
				if (GUILayout.Button(Content_R, GUILayout.ExpandWidth(false)))
				{
					prop.floatValue = resetValue;
				}
				GUI.enabled = wasEnabled;
				GUILayout.EndHorizontal();
			}
		}

		internal static void DrawStrengthProperty(SerializedProperty prop)
		{
			Rect rect = EditorGUILayout.GetControlRect(true);
			EditorGUI.BeginProperty(rect, Content_Strength, prop);

			float strength = prop.floatValue;
			if (DrawStrengthProperty(rect, ref strength))
			{
				prop.floatValue = strength;
			}

			EditorGUI.EndProperty();
		}

		internal static bool DrawStrengthProperty(ref float value)
		{
			bool hasChanged = false;
			Rect rect = EditorGUILayout.GetControlRect(true);

			if (DrawStrengthProperty(rect, ref value))
			{
				hasChanged = true;
			}

			return hasChanged;
		}

		internal static bool DrawStrengthProperty(Rect rect, ref float value)
		{
			bool changed = false;
			const float resetValue = 1f;

			float buttonWidth = 0f;
			if (EditorGUIUtility.wideMode)
			{
				buttonWidth = GUI.skin.button.CalcSize(Content_R).x;
			}

			bool isColored = false;
			if (value < 1f)
			{
				isColored = true;
				if (value > 0f)
				{
					GUI.backgroundColor = Color.yellow;
				}
				else
				{
					GUI.backgroundColor = Color.red;
				}
			}

			EditorGUI.BeginChangeCheck();

			rect.xMax -= buttonWidth;
			float newValue = EditorGUI.Slider(rect, Content_Strength, value, 0f, 1f);

			if (EditorGUIUtility.wideMode)
			{
				rect.xMin += rect.width;
				rect.xMax += buttonWidth;
				bool wasEnabled = GUI.enabled;
				GUI.enabled = (value != resetValue);
				if (GUI.Button(rect, Content_R))
				{
					value = newValue = resetValue;
					changed = true;
				}
				GUI.enabled = wasEnabled;
			}

			if (EditorGUI.EndChangeCheck())
			{
				value = newValue;
				changed = true;
			}

			if (isColored)
			{
				GUI.backgroundColor = Color.white;
			}

			return changed;
		}

		internal static void DrawDualColors(SerializedProperty col1, SerializedProperty col2, GUIContent label)
		{
			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(label, EditorStyles.colorField);
			EditorGUI.indentLevel-=1;		// NOTE: This seems to be a bug in Unity where it shows the indentation with ColorFields, so we have to remove it manually...
			EditorGUILayout.PropertyField(col1, GUIContent.none);
			EditorGUILayout.PropertyField(col2, GUIContent.none);
			EditorGUI.indentLevel+=1;
			GUILayout.EndHorizontal();
		}

		protected static void SaveTexture(RenderTexture texture)
		{
			#if UNITY_2022_1_OR_NEWER
			Texture2D rwTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, mipChain: false, linear: false, createUninitialized: true);
			#else
			Texture2D rwTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, mipChain: false);
			#endif

			TextureUtils.WriteToPNG(texture, rwTexture, Application.dataPath + "/../SavedScreen.png");
			ObjectHelper.Destroy(ref rwTexture);
		}
	}
}