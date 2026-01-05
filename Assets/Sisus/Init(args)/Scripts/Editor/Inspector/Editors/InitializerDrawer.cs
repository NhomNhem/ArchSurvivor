using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Sisus.Init.Internal;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditorInternal;
using UnityEngine;
#if DEV_MODE
using UnityEngine.Profiling;
#endif
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace Sisus.Init.EditorOnly.Internal
{
	internal sealed class InitializerDrawer : IDisposable
	{
		public static bool ServicesShown
		{
			get => EditorPrefs.GetBool(ServiceVisibilityKey, true);
			set => EditorPrefs.SetBool(ServiceVisibilityKey, value);
		}

		public const string SetInitializerTargetOnScriptsReloadedKey = "InitArgs.SetInitializerTarget";
		public const string ServiceVisibilityKey = "InitArgs.InitializerServiceVisibility";
		private const string DefaultHeaderText = "Init";

		private readonly Object[] clients;
		private bool isResponsibleForInitializerEditorLifetime;
		private readonly GameObject[] gameObjects;
		private readonly Object[] rootObjects; // e.g. GameObject[] or ScriptableObject[]

		private GUIStyle initArgsFoldoutBackgroundStyle;
		private GUIStyle initArgsFoldoutStyle;
		private GUIStyle noInitArgsLabelStyle;
		private GUIContent addInitializerIcon;
		private GUIContent contextMenuIcon;
		private GUIContent nullGuardDisabledIcon;
		private GUIContent nullGuardPassedIcon;
		private GUIContent nullGuardFailedIcon;
		private readonly GUIContent servicesHiddenIcon = new GUIContent();
		private readonly GUIContent servicesShownIcon = new GUIContent();
		private readonly GUIContent headerLabel = new GUIContent(DefaultHeaderText);
		private GUIStyle initializerBackgroundStyle = null;
		private GUIStyle noInitializerBackgroundStyle = null;
		private readonly bool hasServiceParameters;
		private readonly bool allParametersAreServices;
		private readonly bool targetCanSelfInitialize;

		private Object[] initializers = new Object[1];
		private Editor initializerEditor;
		#if ODIN_INSPECTOR
		private PropertyTree odinPropertyTree;
		#endif

		[CanBeNull]
		private Object Target => clients[0];

		private bool lockInitializers = false;

		public Action<Rect> OnAddInitializerButtonPressedOverride { get; set; }

		public Object[] Initializers
		{
			get => initializers;

			set
			{
				#if DEV_MODE
				Debug.Assert(value != null && value.GetType() == typeof(Object[]));
				#endif

				initializers = value;
				lockInitializers = true;
				headerLabel.text = initializers.Length == 0 || initializers[0] == null ? DefaultHeaderText : "Init → " + ObjectNames.NicifyVariableName(Target.GetType().Name);
				GUI.changed = true;
			}
		}

		#if ODIN_INSPECTOR
		public PropertyTree OdinPropertyTree
		{
			get
			{
				if(odinPropertyTree is null)
				{
					odinPropertyTree = PropertyTree.Create(initializerEditor.serializedObject);
				}

				return odinPropertyTree;
			}
		}
		#endif

		public InitializerDrawer(Type clientType, Object[] clients, Type[] initParameterTypes, Editor initializerEditor = null, GameObject[] gameObjects = null)
		{
			#if DEV_MODE
			Profiler.BeginSample("InitializerDrawer.Ctr");
			#endif

			this.clients = clients;

			if(gameObjects is null)
			{
				int count = clients.Length;
				if(count > 0)
				{
					if(clients[0] is Component)
					{
						gameObjects = new GameObject[count];

						for(int i = 0; i < count; i++)
						{
							var component = clients[i] as Component;
							gameObjects[i] = component != null ? component.gameObject : null;
						}
					}
					else
					{
						gameObjects = Array.Empty<GameObject>();
					}
				}
			}

			bool[] initServiceParameters = initParameterTypes.Select(IsService).ToArray();
			hasServiceParameters = initServiceParameters.Any(isService => isService);
			allParametersAreServices = initServiceParameters.All(isService => isService);
			
			targetCanSelfInitialize = allParametersAreServices && TypeUtility.DerivesFromGenericBaseType(clientType);

			this.gameObjects = gameObjects;
			rootObjects = gameObjects.Length > 0 ? gameObjects : clients;

			isResponsibleForInitializerEditorLifetime = initializerEditor == null;
			this.initializerEditor = initializerEditor;

			GetInitializersOnTargets(out bool hasInitializers, out Object firstInitializer);

			headerLabel.text = DefaultHeaderText;
			if(TryGetCustomHeaderLabel(hasInitializers, firstInitializer, out string customHeaderText))
			{
				headerLabel.text = customHeaderText;
			}
			else
			{
				headerLabel.text = DefaultHeaderText;
			}

			headerLabel.tooltip = GetInitArgumentsTooltip(clientType, initParameterTypes, initServiceParameters);

			if(hasServiceParameters)
			{
				servicesShownIcon.tooltip = GetServiceVisibilityTooltip(initParameterTypes, initServiceParameters, true);
				servicesHiddenIcon.tooltip = GetServiceVisibilityTooltip(initParameterTypes, initServiceParameters, false);
			}
			else
			{
				servicesShownIcon.tooltip = "";
				servicesHiddenIcon.tooltip = "";
			}

			Setup();

			#if DEV_MODE
			Profiler.EndSample();
			#endif
		}

		private bool IsService(Type parameterType) => Target != null ? ServiceUtility.TryGetFor(Target, parameterType, out _, Context.MainThread) : ServiceUtility.TryGet(parameterType, out _);

		private static bool TryGetCustomHeaderLabel(bool hasInitializers, Object firstInitializer, out string customHeaderText)
		{
			if(hasInitializers && firstInitializer.GetType().GetNestedType(InitializerEditor.InitArgumentMetadataClassName, BindingFlags.Public | BindingFlags.NonPublic) is Type metadata && metadata.GetCustomAttributes<DisplayNameAttribute>().FirstOrDefault() is DisplayNameAttribute displayName)
			{
				customHeaderText = displayName.DisplayName;
				return true;
			}

			customHeaderText = null;
			return false;
		}

		private void Setup()
		{
			#if DEV_MODE
			Profiler.BeginSample("InitializerDrawer.Setup");
			#endif

			initializerBackgroundStyle = new GUIStyle(EditorStyles.helpBox);
			noInitializerBackgroundStyle = new GUIStyle(EditorStyles.label);

			var padding = initializerBackgroundStyle.padding;
			padding.left += 14;
			noInitializerBackgroundStyle.padding = padding;
			initializerBackgroundStyle.padding = padding;

			initArgsFoldoutStyle = new GUIStyle(EditorStyles.foldout);
			initArgsFoldoutStyle.fontStyle = FontStyle.Bold;

			noInitArgsLabelStyle = new GUIStyle(EditorStyles.label);
			noInitArgsLabelStyle.fontStyle = FontStyle.Bold;

			initArgsFoldoutBackgroundStyle = "RL Header";
			initArgsFoldoutBackgroundStyle.fixedHeight = 24f;

			addInitializerIcon = new GUIContent(EditorGUIUtility.TrIconContent("Toolbar Plus", "Add Initializer"));
			if(Target is StateMachineBehaviour)
			{
				addInitializerIcon.tooltip = "Add State Machine Behaviour Initializer";
			}

			contextMenuIcon = EditorGUIUtility.IconContent("_Menu");
			nullGuardDisabledIcon = EditorGUIUtility.IconContent("DebuggerDisabled");
			nullGuardPassedIcon = EditorGUIUtility.IconContent("Installed@2x");
			nullGuardFailedIcon = EditorGUIUtility.IconContent("DebuggerEnabled");

			if(hasServiceParameters)
			{
				servicesHiddenIcon.image = EditorGUIUtility.IconContent("animationvisibilitytoggleoff").image;
				servicesShownIcon.image = EditorGUIUtility.IconContent("animationvisibilitytoggleon").image;
			}
			else
			{
				servicesHiddenIcon.image = null;
				servicesShownIcon.image = null;
			}

			#if DEV_MODE
			Profiler.EndSample();
			#endif
		}

		public static void OnAssemblyCompilationStarted(ref InitializerDrawer drawer, string compilingAssemblyName)
		{
			if(drawer is null || drawer.initializerEditor == null)
			{
				return;
			}

			string initializerAssemblyName = Path.GetFileName(drawer.initializerEditor.GetType().Assembly.Location);
			if(string.Equals(compilingAssemblyName, initializerAssemblyName))
			{
				drawer.Dispose();
				drawer = null;
			}
		}

		public void OnInspectorGUI()
		{
			#if DEV_MODE
			Profiler.BeginSample("InitializerDrawer.OnInspectorGUI");
			#endif

			GetInitializersOnTargets(out bool hasInitializers, out Object firstInitializer);

			bool mixedInitializers = false;
			if(hasInitializers)
			{
				for(int i = 0, targetCount = clients.Length; i < targetCount; i++)
				{
					if(initializers[i] == null)
					{
						mixedInitializers = true;
						break;
					}
				}
			}
			// Don't draw Init GUI in play mode unless the target has an Initializer
			// (possible if the GameObject is inactive).
			else if(Application.isPlaying)
			{
				#if DEV_MODE
				Profiler.EndSample();
				#endif
				return;
			}

			if(initializerBackgroundStyle == null)
			{
				Setup();
			}

			EditorGUI.indentLevel = 0;

			bool drawFoldout = !string.IsNullOrEmpty(headerLabel.text);
			if(drawFoldout)
			{
				EditorGUILayout.BeginVertical(hasInitializers ? initializerBackgroundStyle : noInitializerBackgroundStyle);
			}

			var labelStyle = hasInitializers && !mixedInitializers ? initArgsFoldoutStyle : noInitArgsLabelStyle;
			var headerRect = EditorGUILayout.GetControlRect(false, 20f, labelStyle);

			bool initializerUnfolded = !drawFoldout ||
										(hasInitializers && !mixedInitializers && InternalEditorUtility.GetIsInspectorExpanded(firstInitializer));
			bool guiWasEnabled = GUI.enabled;
			if(!hasInitializers)
			{
				GUI.enabled = false;
			}

			const float ICON_WIDTH = 20f;
			var foldoutRect = headerRect;
			if(!isResponsibleForInitializerEditorLifetime)
			{
				foldoutRect.width = EditorGUIUtility.labelWidth - foldoutRect.x;
			}
			else
			{
				foldoutRect.width -= 38f;
				foldoutRect.x -= 12f;
			}

			foldoutRect.y -= 1f;
			foldoutRect.width -= ICON_WIDTH;

			var addInitializerOrContextMenuRect = headerRect;
			addInitializerOrContextMenuRect.x += addInitializerOrContextMenuRect.width - ICON_WIDTH;
			addInitializerOrContextMenuRect.width = ICON_WIDTH;

			bool setInitializerUnfolded = initializerUnfolded;
			var foldoutClickableRect = foldoutRect;
			foldoutClickableRect.x -= 5f;
			foldoutClickableRect.width +=  10f;

			if(!isResponsibleForInitializerEditorLifetime)
			{
				foldoutClickableRect.x -= 12f;
				foldoutClickableRect.width += 12f;
			}

			if(Event.current.type == EventType.MouseDown && foldoutClickableRect.Contains(Event.current.mousePosition))
			{
				if(Event.current.button == 0)
				{
					setInitializerUnfolded = !setInitializerUnfolded;
				}
				else if(Event.current.button == 1)
				{
					OnInitializerContextMenuButtonPressed(clients, firstInitializer, mixedInitializers, null);
				}

				Event.current.Use();
			}

			GUI.enabled = guiWasEnabled;

			if(setInitializerUnfolded != initializerUnfolded && hasInitializers)
			{
				for(int i = 0, targetCount = clients.Length; i < targetCount; i++)
				{
					var initializer = initializers[i];
					if(initializer != null)
					{
						InternalEditorUtility.SetIsInspectorExpanded(initializer, setInitializerUnfolded);
					}
				}

				initializerUnfolded = setInitializerUnfolded;
				GUIUtility.ExitGUI();
			}

			if(!hasInitializers)
			{
				if(drawFoldout)
				{
					DrawInitHeader();
				}

				if(GUI.Button(addInitializerOrContextMenuRect, addInitializerIcon, "RL FooterButton"))
				{
					if(OnAddInitializerButtonPressedOverride != null)
					{
						OnAddInitializerButtonPressedOverride.Invoke(addInitializerOrContextMenuRect);

						#if DEV_MODE
						Profiler.EndSample();
						#endif
						return;
					}

					AddInitializer(addInitializerOrContextMenuRect);
				}

				if(targetCanSelfInitialize)
				{
					var checkmarkIconRect = addInitializerOrContextMenuRect;
					checkmarkIconRect.y -= 1f;
					checkmarkIconRect.x -= addInitializerOrContextMenuRect.width;

					var guiColorWas = GUI.color;
					GUI.color = Color.yellow;
					nullGuardPassedIcon.tooltip = "All arguments are services.\n\nAdding an initializer is not required.";
					GUI.Label(checkmarkIconRect, nullGuardPassedIcon);
					GUI.color = guiColorWas;
				}
			}
			else
			{
				if(!isResponsibleForInitializerEditorLifetime)
				{
					addInitializerOrContextMenuRect.x += addInitializerOrContextMenuRect.width;
				}
				else if(GUI.Button(addInitializerOrContextMenuRect, GUIContent.none, EditorStyles.label))
				{
					OnInitializerContextMenuButtonPressed(clients, firstInitializer, mixedInitializers, addInitializerOrContextMenuRect);
				}

				var nullGuardIconRect = addInitializerOrContextMenuRect;
				nullGuardIconRect.y -= 1f;
				nullGuardIconRect.x -= addInitializerOrContextMenuRect.width;

				var initializerEditorOnly = firstInitializer as IInitializerEditorOnly;
				bool drawNullGuard = initializerEditorOnly != null && initializerEditorOnly.ShowNullArgumentGuard;
				bool hasNullArguments = !allParametersAreServices && (initializerEditorOnly == null || initializerEditorOnly.HasNullArguments);
				var nullGuard = drawNullGuard ? initializerEditorOnly.NullArgumentGuard : NullArgumentGuard.None;

				if(drawNullGuard && GUI.Button(nullGuardIconRect, GUIContent.none, EditorStyles.label))
				{
					OnInitializerNullGuardButtonPressed(nullGuard, nullGuardIconRect);
				}

				bool servicesShown = ServicesShown;
				var serviceVisibilityIconRect = nullGuardIconRect;
				if(drawNullGuard)
				{
					serviceVisibilityIconRect.x -= nullGuardIconRect.width;
				}

				if(hasServiceParameters && GUI.Button(serviceVisibilityIconRect, GUIContent.none, EditorStyles.label))
				{
					servicesShown = !servicesShown;
					ServicesShown = servicesShown;
					EditorPrefs.SetBool(ServiceVisibilityKey, servicesShown);
				}

				if(initializerUnfolded)
				{
					DrawInitializerArguments();
				}

				if(drawFoldout)
				{
					DrawInitHeader();
				}

				GUI.Label(addInitializerOrContextMenuRect, contextMenuIcon);

				var iconSizeWas = EditorGUIUtility.GetIconSize();
				EditorGUIUtility.SetIconSize(new Vector2(15f, 15f));

				if(drawNullGuard)
				{
					bool nullGuardPassedAndFullyEnabled;
					GUIContent nullGuardIcon;
					
					bool nullGuardDisabled =
						!nullGuard.IsEnabled(Application.isPlaying ? NullArgumentGuard.RuntimeException : NullArgumentGuard.EditModeWarning)
						|| (!nullGuard.IsEnabled(NullArgumentGuard.EnabledForPrefabs) && PrefabUtility.IsPartOfPrefabAsset(firstInitializer));

					if(nullGuardDisabled)
					{
						nullGuardIcon = nullGuardDisabledIcon;
						nullGuardIcon.tooltip = GetTooltip(nullGuard) + "\n\nNull argument guard is off.";
						nullGuardPassedAndFullyEnabled = false;
					}
					else if(hasNullArguments)
					{
						nullGuardIcon = nullGuardFailedIcon;
						nullGuardIcon.tooltip = "Missing argument detected.\n\nIf the argument is allowed to be null set the 'Null Argument Guard' option to 'None'.\n\nIf the missing argument is a service that only becomes available at runtime set the option to 'Runtime Exception'.";
						nullGuardPassedAndFullyEnabled = false;
					}
					else
					{
						if(!string.IsNullOrEmpty(initializerEditorOnly.NullGuardFailedMessage))
						{
							initializerEditorOnly.NullGuardFailedMessage = "";
						}

						nullGuardIcon = nullGuardPassedIcon;
						nullGuardIcon.tooltip = GetTooltip(nullGuard) + "\n\nAll arguments provided.";
						nullGuardPassedAndFullyEnabled = nullGuard.IsEnabled(NullArgumentGuard.EditModeWarning) && nullGuard.IsEnabled(NullArgumentGuard.RuntimeException);
					}

					var guiColorWas = GUI.color;
					if(nullGuardPassedAndFullyEnabled)
					{
						GUI.color = Color.yellow;
					}

					GUI.Label(nullGuardIconRect, nullGuardIcon);
					
					GUI.color = guiColorWas;
				}

				if(hasServiceParameters)
				{
					var serviceVisibilityIcon = servicesShown ? servicesShownIcon : servicesHiddenIcon;
					GUI.Label(serviceVisibilityIconRect, serviceVisibilityIcon);
				}

				EditorGUIUtility.SetIconSize(iconSizeWas);
			}

			if(drawFoldout)
			{
				EditorGUILayout.EndVertical();
			}

			#if DEV_MODE
			Profiler.EndSample();
			#endif

			void DrawInitHeader()
			{
				if(Event.current.type != EventType.Repaint)
				{
					return;
				}

				var backgroundRect = headerRect;
				backgroundRect.y -= 3f;
				backgroundRect.x -= 18f;
				backgroundRect.width += 22f;
				initArgsFoldoutBackgroundStyle.Draw(backgroundRect, false, false, false, false);
				foldoutRect.x = 22f;
				labelStyle.Draw(foldoutRect, headerLabel, 0, initializerUnfolded);
			}
		}

		private void DrawInitializerArguments()
		{
			if(initializerEditor == null)
			{
				isResponsibleForInitializerEditorLifetime = true;

				Editor.CreateCachedEditor(initializers, null, ref initializerEditor);

				if(initializerEditor is InitializerEditor setup)
				{
					setup.Setup(this);
				}
			}

			if(initializerEditor is InitializerEditor internalInitializerEditor)
			{
				internalInitializerEditor.serializedObject.Update();
				internalInitializerEditor.DrawArgumentFields();
				internalInitializerEditor.serializedObject.ApplyModifiedProperties();
			}
			else if(initializerEditor.GetType() == CustomEditorUtility.genericInspectorType)
			{
				HideScriptReferenceField();
				initializerEditor.OnInspectorGUI();
			}
			else
			{
				initializerEditor.OnInspectorGUI();
			}
		}

		private string GetInitArgumentsTooltip([NotNull] Type clientType, [NotNull] Type[] initParameterTypes, [NotNull] bool[] initServiceParameters)
		{
			var className = clientType.Name;
			var types = initParameterTypes;

			var sb = new StringBuilder();
			sb.Append(className);
			int count = types.Length;
			sb.Append(count switch
			{
				1 => " accepts one Init argument:",
				2 => " accepts two Init arguments:",
				3 => " accepts three Init arguments:",
				4 => " accepts four Init arguments:",
				5 => " accepts five Init arguments:",
				_ => $" accepts {count} Init arguments:"
			});

			for(int i = 0; i < count; i++)
			{
				sb.Append('\n');
				sb.Append(GetInitArgumentTooltipText(types[i], initServiceParameters[i]));
			}

			return sb.ToString();
		}

		private string GetServiceVisibilityTooltip([NotNull] Type[] initParameterTypes, [NotNull] bool[] initServiceParameters, bool servicesShown)
		{
			var sb = new StringBuilder();

			int serviceCount = initServiceParameters.Count(b => b);
			sb.Append(serviceCount switch
			{
				1 => "One service argument is",
				2 => "Two service arguments are",
				3 => "Three service arguments are",
				4 => "Four service arguments are",
				5 => "Five service arguments are",
				6 => "Six service arguments are",
				_ => serviceCount + " service arguments are"
			});

			sb.Append(servicesShown ? " shown:" : " hidden:");

			for(int i = 0, count = initParameterTypes.Length; i < count; i++)
			{
				if(initServiceParameters[i])
				{
					sb.Append("\n ");
					sb.Append(TypeUtility.ToString(initParameterTypes[i]));
				}
			}

			sb.Append("\n\nThese services will be provided automatically during initialization.");

			return sb.ToString();
		}

		private string GetInitArgumentTooltipText(Type type, bool isService) => isService ? TypeUtility.ToString(type) + " (Service)" : TypeUtility.ToString(type);

		private static string GetTooltip(NullArgumentGuard guard)
		{
			switch(guard)
			{
				default:
					return "Null Argument Guard:\n☐ Edit Mode Warning\n☐ Runtime Exception\n☐ Enabled For Prefabs";
				case NullArgumentGuard.EditModeWarning:
					return "Null Argument Guard:\n☑ Edit Mode Warning\n☐ Runtime Exception\n☐ Enabled For Prefabs";
				case NullArgumentGuard.RuntimeException:
					return "Null Argument Guard:\n☐ Edit Mode Warning\n☑ Runtime Exception\n☐ Enabled For Prefabs";
				case NullArgumentGuard.EnabledForPrefabs:
					return "Null Argument Guard:\n☐ Edit Mode Warning\n☐ Runtime Exception\n☑ Enabled For Prefabs";
				case NullArgumentGuard.EditModeWarning | NullArgumentGuard.RuntimeException:
					return "Null Argument Guard:\n☑ Edit Mode Warning\n☑ Runtime Exception\n☐ Enabled For Prefabs";
				case NullArgumentGuard.RuntimeException | NullArgumentGuard.EnabledForPrefabs:
					return "Null Argument Guard:\n☐ Edit Mode Warning\n☑ Runtime Exception\n☑ Enabled For Prefabs";
				case NullArgumentGuard.EditModeWarning | NullArgumentGuard.EnabledForPrefabs:
					return "Null Argument Guard:\n☑ Edit Mode Warning\n☑ Runtime Exception\n☐ Enabled For Prefabs";
				case NullArgumentGuard.EditModeWarning | NullArgumentGuard.RuntimeException | NullArgumentGuard.EnabledForPrefabs:
					return "Null Argument Guard:\n☑ Edit Mode Warning\n☑ Runtime Exception\n☑ Enabled For Prefabs";
			}
		}

		private void OnInitializerNullGuardButtonPressed(NullArgumentGuard nullGuard, Rect nullGuardIconRect)
		{
			var menu = new GenericMenu();

			menu.AddItem(new GUIContent("None"), nullGuard == NullArgumentGuard.None, () => ForEachInitializer("Disable Null Guard", (init) => init.NullArgumentGuard = NullArgumentGuard.None));
			menu.AddItem(new GUIContent("Edit Mode Warning"), nullGuard.IsEnabled(NullArgumentGuard.EditModeWarning), () => ForEachInitializer("Set Null Guard", (init) => init.NullArgumentGuard = nullGuard.WithFlagToggled(NullArgumentGuard.EditModeWarning)));
			menu.AddItem(new GUIContent("Runtime Exception"), nullGuard.IsEnabled(NullArgumentGuard.RuntimeException), () => ForEachInitializer("Set Null Guard", (init) => init.NullArgumentGuard = nullGuard.WithFlagToggled(NullArgumentGuard.RuntimeException)));
			menu.AddItem(new GUIContent("Enabled For Prefabs"), nullGuard.IsEnabled(NullArgumentGuard.EnabledForPrefabs), () => ForEachInitializer("Set Null Guard", (init) => init.NullArgumentGuard = nullGuard.WithFlagToggled(NullArgumentGuard.EnabledForPrefabs)));
			menu.AddItem(new GUIContent("All"), nullGuard == (NullArgumentGuard.EditModeWarning | NullArgumentGuard.RuntimeException | NullArgumentGuard.EnabledForPrefabs), () => ForEachInitializer("Enable Null Guard", (init) => init.NullArgumentGuard = NullArgumentGuard.EditModeWarning | NullArgumentGuard.RuntimeException | NullArgumentGuard.EnabledForPrefabs));

			menu.DropDown(nullGuardIconRect);
		}

		public void AddInitializer(Rect addButtonRect)
		{
			var target = clients[0];
			var targetType = target.GetType();
			var initializerTypes = InitializerEditorUtility.GetInitializerTypes(targetType).ToArray();
			
			int count = initializerTypes.Length;
			if(count > 0)
			{
				var menu = new GenericMenu();
				int activeOptions = 0;
				foreach(var initializerType in initializerTypes)
				{
					var label = new GUIContent(TypeUtility.ToString(initializerType));
					if(IsTargetedByInitializerOfType(initializerType))
					{
						menu.AddDisabledItem(label, true);
					}
					else
					{
						menu.AddItem(label, false, () => InitializerEditorUtility.AddInitializer(clients, initializerType));
						activeOptions++;
					}
				}

				if(activeOptions == 1)
				{
					InitializerEditorUtility.AddInitializer(clients, initializerTypes.First(initializerType => !IsTargetedByInitializerOfType(initializerType)));
				}
				else
				{
					menu.DropDown(addButtonRect);
				}
			}
			else if((target is MonoBehaviour monoBehaviour && MonoScript.FromMonoBehaviour(monoBehaviour) is MonoScript monoScript) || Find.Script(targetType, out monoScript))
			{
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Generate Initializer"), false, () => GenerateInitializer(monoScript));
				menu.DropDown(addButtonRect);
			}
			else
			{
				EditorUtility.DisplayDialog("Initializer Not Found", $"No Initializer class was found for {targetType.Name}.", "Ok");
			}

			GUI.changed = true;
			if(Event.current != null)
			{
				GUIUtility.ExitGUI();
			}
		}

		private void GenerateInitializer(MonoScript monoScript) => InitializerEditorUtility.GenerateAndAttachInitializer(clients, monoScript);

		private bool IsTargetedByInitializerOfType(Type initializerType)
		{
			for(int i = 0, count = gameObjects.Length; i < count; i++)
			{
				var gameObject = gameObjects[i];
				foreach(var initializer in gameObject.GetComponents<IInitializer>())
				{
					if(initializer.GetType() == initializerType && initializer.Target == clients[i])
					{
						return true;
					}
				}
			}

			return false;
		}

		private void OnInitializerContextMenuButtonPressed(Object[] targets, Object firstInitializer, bool mixedInitializers, Rect? toggleInitializerRect)
		{
			var menu = new GenericMenu();

			menu.AddItem(new GUIContent("Reset"), false, Reset);

			menu.AddSeparator("");

			menu.AddItem(new GUIContent("Remove"), false, () =>
			{
				ForEachInitializer("Remove Initializer", RemoveInitializer);
				isResponsibleForInitializerEditorLifetime = true;
				Dispose();
				initializerEditor = null;

				foreach(var target in clients)
				{
					using(var serializedObject = new SerializedObject(target))
					{
						serializedObject.FindProperty("initializer").objectReferenceValue = null;
						serializedObject.ApplyModifiedProperties();
					}

					string path = AssetDatabase.GetAssetPath(target);
					AssetDatabase.ImportAsset(path);
				}
			});

			menu.AddItem(new GUIContent("Copy"), false, Copy);

			menu.AddItem(new GUIContent("Paste"), false, Paste);

			menu.AddSeparator("");

			if(MonoScript.FromMonoBehaviour(firstInitializer as MonoBehaviour) is MonoScript scriptAsset)
			{
				menu.AddItem(new GUIContent("Edit Script"), false, () => AssetDatabase.OpenAsset(scriptAsset));
				menu.AddItem(new GUIContent("Ping Script"), false, () => EditorApplication.delayCall += () => EditorGUIUtility.PingObject(scriptAsset));
			}

			if(!mixedInitializers)
			{
				menu.AddItem(new GUIContent("Preset"), false, () => PresetSelector.ShowSelector(initializers, null, true));
			}

			if(toggleInitializerRect.HasValue)
			{
				menu.DropDown(toggleInitializerRect.Value);
			}
			else
			{
				menu.ShowAsContext();
			}

			void Reset()
			{
				EditorGUIUtility.editingTextField = false;

				Object destroyWhenDone;
				Object copySource;
				if(firstInitializer is Component)
				{
					var tempGameObject = new GameObject("");
					tempGameObject.SetActive(false);
					destroyWhenDone = tempGameObject;
					var tempComponent = tempGameObject.AddComponent(firstInitializer.GetType());
					tempComponent.hideFlags = HideFlags.HideInInspector;
					(tempComponent as IInitializer).Target = Target;
					copySource = tempComponent;
				}
				else if(firstInitializer is ScriptableObject)
				{
					var tempScriptableObject = ScriptableObject.CreateInstance(firstInitializer.GetType());
					(tempScriptableObject as IInitializer).Target = Target;
					copySource = tempScriptableObject;
					destroyWhenDone = tempScriptableObject;
				}
				else
				{
					return;
				}

				ForEachInitializer("", (index, initializer) =>
				{
					EditorUtility.CopySerialized(copySource, initializer);
					(initializer as IInitializer).Target = targets[index];
					initializer.hideFlags = gameObjects.Length >= index && targets[index] != null ? HideFlags.HideInInspector : HideFlags.None;
				});

				Object.DestroyImmediate(destroyWhenDone);
			}

			void Copy()
			{
				if(firstInitializer is Component component)
				{
					ComponentUtility.CopyComponent(component);
				}
				else
				{
					EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(firstInitializer, true);
				}
			}

			void Paste()
			{
				EditorGUIUtility.editingTextField = false;

				if(firstInitializer is Component)
				{
					ForEachInitializer("", (i)=> ComponentUtility.PasteComponentValues(i));
				}
				else if(!string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer))
				{
					ForEachInitializer("", (Object i)=> JsonUtility.FromJsonOverwrite(EditorGUIUtility.systemCopyBuffer, i));
				}
			}

			void RemoveInitializer(Object initializer)
			{
				if(AssetDatabase.IsSubAsset(initializer))
				{
					AssetDatabase.RemoveObjectFromAsset(initializer);
				}

				if(initializer != null)
				{
					Undo.DestroyObjectImmediate(initializer);
				}
			}
		}

		private void ForEachInitializer(string undoName, Action<IInitializerEditorOnly> action)
		{
			if(!string.IsNullOrEmpty(undoName))
			{
				Undo.RecordObjects(initializers, undoName);
			}

			for(int i = 0, count = initializers.Length; i < count; i++)
			{
				if(initializers[i] is IInitializerEditorOnly initializer)
				{
					action(initializer);
				}
			}

			if(initializerEditor != null && initializerEditor.serializedObject != null)
			{
				initializerEditor.serializedObject.Update();
				initializerEditor.Repaint();
			}
		}

		private void ForEachInitializer(string undoName, Action<int, Component> action)
		{
			if(!string.IsNullOrEmpty(undoName))
			{
				Undo.RecordObjects(initializers, undoName);
			}

			for(int i = 0, count = initializers.Length; i < count; i++)
			{
				var initializer = initializers[i] as Component;
				if(initializer != null)
				{
					action(i, initializer);
				}
			}

			if(initializerEditor != null && initializerEditor.serializedObject != null)
			{
				initializerEditor.serializedObject.Update();
				initializerEditor.Repaint();
			}
		}

		private void ForEachInitializer(string undoName, Action<Component> action)
		{
			if(!string.IsNullOrEmpty(undoName))
			{
				Undo.RecordObjects(initializers, undoName);
			}

			for(int i = 0, count = initializers.Length; i < count; i++)
			{
				var initializer = initializers[i] as Component;
				if(initializer != null)
				{
					action(initializer);
				}
			}

			if(initializerEditor != null && initializerEditor.serializedObject != null)
			{
				initializerEditor.serializedObject.Update();
				initializerEditor.Repaint();
			}
		}

		private void ForEachInitializer(string undoName, Action<Object> action)
		{
			if(!string.IsNullOrEmpty(undoName))
			{
				Undo.RecordObjects(initializers, undoName);
			}

			for(int i = 0, count = initializers.Length; i < count; i++)
			{
				var initializer = initializers[i];
				if(initializer != null)
				{
					action(initializer);
				}
			}

			if(initializerEditor != null && initializerEditor.serializedObject != null && initializerEditor.target != null)
			{
				initializerEditor.serializedObject.Update();
				initializerEditor.Repaint();
			}
		}

		public void Dispose()
		{
			if(isResponsibleForInitializerEditorLifetime && initializerEditor != null)
			{
				Object.DestroyImmediate(initializerEditor);
				initializerEditor = null;
				isResponsibleForInitializerEditorLifetime = false;
			}

			#if ODIN_INSPECTOR
			if(odinPropertyTree != null)
			{
				odinPropertyTree.Dispose();
				odinPropertyTree = null;
			}
			#endif
		}

		private void GetInitializersOnTargets(out bool hasInitializers, out Object firstInitializer)
		{
			if(initializerEditor != null)
			{
				initializers = initializerEditor.targets;
				firstInitializer = initializers[0];
				hasInitializers = firstInitializer != null;
				return;
			}

			if(lockInitializers)
			{
				firstInitializer = initializers.Length == 0 ? null : initializers[0];
				hasInitializers = firstInitializer != null;
				if(!hasInitializers && headerLabel.text != DefaultHeaderText)
				{
					headerLabel.text = DefaultHeaderText;
					GUI.changed = true;
				}
				return;
			}

			int targetCount = clients.Length;
			hasInitializers = false;
			Array.Resize(ref initializers, targetCount);

			firstInitializer = null;
			for(int i = 0; i < targetCount; i++)
			{
				initializers[i] = null;

				var rootObject = rootObjects[i];
				if(!InitializerEditors.InitializersOnInspectedObjects.TryGetValue(rootObject, out var initializersOnRootObject))
				{
					continue;
				}

				foreach(var initializer in initializersOnRootObject)
				{
					if(!ShouldDrawInitializerEmbedded(initializer))
					{
						continue;
					}

					var initializerAsObject = initializer as Object;
					if(initializerAsObject == null)
					{
						continue;
					}

					initializers[i] = initializerAsObject;
					hasInitializers = true;

					if(firstInitializer == null)
					{
						firstInitializer = initializerAsObject;
					}
				}
			}
		}

		private bool ShouldDrawInitializerEmbedded(IInitializer initializer)
		{
			if(initializer is Object initializerAsObject && initializerAsObject == null)
			{
				return false;
			}

			var initializerTarget = initializer.Target;
			if(initializerTarget == null)
			{
				return false;
			}

			foreach(var target in clients)
			{
				if(initializerTarget == target)
				{
					return true;
				}
			}

			Object firstClient = clients[0];
			if(firstClient == null)
			{
				return false;
			}

			Animator animator = initializerTarget as Animator;
			if(animator == null)
			{
				return false;
			}

			StateMachineBehaviour[] stateMachineBehaviours;
			try
			{
				stateMachineBehaviours = animator.GetBehaviours<StateMachineBehaviour>();
			}
			catch(Exception e)
			{
				Debug.LogException(e, initializer as Object);
				return false;
			}

			Type clientType = clients[0].GetType();
			foreach(var stateMachineBehaviour in stateMachineBehaviours)
			{
				if(stateMachineBehaviour != null && stateMachineBehaviour.GetType() == clientType)
				{
					return true;
				}
			}

			return false;
		}

		private static void HideScriptReferenceField() => GUILayout.Space(-EditorGUIUtility.singleLineHeight - 1f);
	}
}