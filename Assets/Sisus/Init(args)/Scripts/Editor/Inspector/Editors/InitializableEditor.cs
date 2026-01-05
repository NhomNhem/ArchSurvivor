//#define DRAW_INIT_SECTION_WITHOUT_EDITOR

using System;
using System.IO;
#if !DRAW_INIT_SECTION_WITHOUT_EDITOR
using System.Linq;
#endif
using JetBrains.Annotations;
using Sisus.Init.EditorOnly.Internal;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;
#if DEV_MODE
using UnityEngine.Profiling;
#endif
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Sisus.Init.EditorOnly
{
	/// <summary>
	/// Base class for custom editors for initializable targets that visualize non-serialized fields of the target in play mode.
	/// </summary>
	public class InitializableEditor : Editor
    {
		private static readonly string[] scriptField = new string[] { "m_Script" };

        [NonSerialized]
        private RuntimeFieldsDrawer runtimeFieldsDrawer;
		[NonSerialized]
		private InitializerDrawer initializerDrawer;
		private bool drawInitializerGUI;
		private Editor internalEditor;
		[SerializeField]
		private bool internalEditorIsGenericInspector;

		protected bool ShowRuntimeFields { get; set; }

		/// <summary>
		/// The generic type definition of the Initializable base class from which the Editor target derives from,
		/// or if that is not known, then an IInitializable<T...> interface the target implements.
		/// <para>
		/// This is used to determine what the <see cref="BaseType"/> of the target's class is.
		/// </para>
		/// </summary>
		protected virtual Type BaseTypeDefinition => typeof(Object);

		/// <summary>
		/// The type of the Initializable base class from which the Editor target derives from.
		/// <para>
		/// Types of the Init parameters are determined from the <see cref="Type.GetGenericArguments"/>
		/// results for <see cref="BaseType"/>.
		/// </para>
		/// <para>
		/// Also when drawing runtime fields fields in the base type and types it derives from will be skipped.
		/// </para>
		/// </summary>
		protected Type BaseType
        {
            get
            {
                var baseTypeDefinition = BaseTypeDefinition;

                if(baseTypeDefinition.IsInterface)
                {
                    Type parentType = target.GetType();

                    if(baseTypeDefinition.IsGenericTypeDefinition)
					{
                        for(Type baseType = parentType.BaseType; baseType != null; baseType = baseType.BaseType)
                        {
                            bool implementsInterface = false;
                            foreach(var @interface in baseType.GetInterfaces())
                            {
                                if(@interface.IsGenericType && @interface.GetGenericTypeDefinition() == baseTypeDefinition)
						        {
                                    implementsInterface = true;
                                    break;
						        }
                            }

                            if(!implementsInterface)
					        {
                                return parentType;
					        }

                            parentType = baseType;
                        }
					}
                    else
                    {
                        for(Type baseType = parentType.BaseType; baseType != null; baseType = baseType.BaseType)
                        {
                            bool implementsInterface = false;
                            foreach(var @interface in baseType.GetInterfaces())
                            {
                                if(@interface == baseTypeDefinition)
						        {
                                    implementsInterface = true;
                                    break;
						        }
                            }

                            if(!implementsInterface)
					        {
                                return parentType;
					        }

                            parentType = baseType;
                        }
                    }

                    return typeof(MonoBehaviour).IsAssignableFrom(parentType) ? typeof(MonoBehaviour) : 
						   typeof(ScriptableObject).IsAssignableFrom(parentType) ? typeof(ScriptableObject) :
						   typeof(Object);
                }
                else if(baseTypeDefinition.IsGenericTypeDefinition)
                {
                    for(Type baseType = target.GetType().BaseType; baseType != null; baseType = baseType.BaseType)
                    {
                        if(baseType.IsGenericType && baseType.GetGenericTypeDefinition() == baseTypeDefinition)
                        {
                            return baseType;
                        }
                    }
                }

                return baseTypeDefinition;
            }
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
		{
			if(!EditorPrefs.HasKey(InitializerDrawer.SetInitializerTargetOnScriptsReloadedKey))
			{
				return;
			}

			var value = EditorPrefs.GetString(InitializerDrawer.SetInitializerTargetOnScriptsReloadedKey);
			EditorPrefs.DeleteKey(InitializerDrawer.SetInitializerTargetOnScriptsReloadedKey);

			int i = value.IndexOf(':');
			if(i <= 0)
			{
				return;
			}

			string initializerAssetGuid = value.Substring(0, i);
            string initializerAssetPath = AssetDatabase.GUIDToAssetPath(initializerAssetGuid);
            if(string.IsNullOrEmpty(initializerAssetPath))
			{
                return;
			}

            var initializerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(initializerAssetPath);
            if(initializerScript == null)
			{
                return;
			}

            var initializerType = initializerScript.GetClass();
            if(initializerType == null)
			{
                return;
			}
            
            var targetIds = value.Substring(i + 1).Split(';');
            foreach(var idString in targetIds)
			{
                if(!int.TryParse(idString, out int id))
				{
                    continue;
				}

				var target = EditorUtility.InstanceIDToObject(id);

                var component = target as Component;
                if(component != null)
				{
					var gameObject = component.gameObject;
					var initializerComponent = gameObject.GetComponent(initializerType);
					if(initializerComponent == null || !(initializerComponent is IInitializer initializer) || !initializer.TargetIsAssignableOrConvertibleToType(component.GetType()) || initializer.Target != null)
					{
						continue;
					}

					initializer.Target = component;
					continue;
				}

				var scriptableObject = target as ScriptableObject;
				if(scriptableObject != null && typeof(IInitializer).IsAssignableFrom(initializerType) && typeof(ScriptableObject).IsAssignableFrom(initializerType))
				{
					IInitializer initializer = CreateInstance(initializerType) as IInitializer;
					if(initializer == null || !initializer.TargetIsAssignableOrConvertibleToType(component.GetType()) || initializer.Target != null)
					{
						continue;
					}

					Undo.RegisterCreatedObjectUndo(initializer as Object, "Add Initializer");
					initializer.Target = target;

					if(target is IInitializableEditorOnly initializableEditorOnly)
					{
						Undo.RecordObject(target, "Add Initializer");
						initializableEditorOnly.Initializer = initializer;
					}
				}
            }
		}

		protected virtual void OnEnable()
        {
			#if DEV_MODE
			Profiler.enableAllocationCallstacks = true;
			Profiler.maxUsedMemory = 1024 * 1024 * 1024;
			#endif

			EditorApplication.update -= OnUpdate;
			EditorApplication.update += OnUpdate;
			EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
			EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
			CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
			CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
		}

		private void OnPlaymodeStateChanged(PlayModeStateChange stateChange)
		{
			switch(stateChange)
			{
				case PlayModeStateChange.ExitingEditMode:
				case PlayModeStateChange.EnteredPlayMode:
					DestroyWrappedEditors();
					break;
			}
		}

		private void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] compilerMessages)
		{
			string assemblyName = Path.GetFileName(assemblyPath);

			if(internalEditor != null && string.Equals(assemblyName, Path.GetFileName(internalEditor.GetType().Assembly.Location)))
			{
				DestroyImmediate(internalEditor);
			}

			InitializerDrawer.OnAssemblyCompilationStarted(ref initializerDrawer, assemblyName);
		}

		protected virtual void Setup()
		{
			#if DEV_MODE
			Profiler.BeginSample("InitializableEditor.Setup");
			#endif

			var baseType = BaseType;
			var initParameterTypes = baseType is null || !baseType.IsGenericType || baseType.IsGenericTypeDefinition ? Type.EmptyTypes : baseType.GetGenericArguments();
			var firstTarget = targets[0];
			var targetType = firstTarget.GetType();

			initializerDrawer = new InitializerDrawer(targetType, targets, initParameterTypes);

			ShowRuntimeFields = firstTarget is MonoBehaviour
							  && (firstTarget is IOneArgument || firstTarget is ITwoArguments || firstTarget is IThreeArguments
							  || firstTarget is IFourArguments || firstTarget is IFiveArguments || firstTarget is ISixArguments);

			if(ShowRuntimeFields)
            {
                runtimeFieldsDrawer = new RuntimeFieldsDrawer(target, baseType);
            }

			#if DRAW_INIT_SECTION_WITHOUT_EDITOR
			drawInitializerGUI = false;
			#else
			// Only draw the Init bar if an Initializer class exists for the target type
			// or if the Initializable has no visible serialized fields in the Inspector.
			if(InitializerEditorUtility.GetInitializerTypes(targetType).Any())
			{
				drawInitializerGUI = true;
			}
			else
			{
				var iterator = serializedObject.GetIterator();
				drawInitializerGUI = !iterator.NextVisible(true) || !iterator.NextVisible(false);
			}
			#endif

			#if DEV_MODE
			Profiler.EndSample();
			#endif
		}

		[Pure]
		protected virtual RuntimeFieldsDrawer CreateRuntimeFieldsDrawer() => new RuntimeFieldsDrawer(target, BaseType);

		public override VisualElement CreateInspectorGUI()
		{
			#if DEV_MODE
			Profiler.BeginSample("InitializableEditor.CreateInspectorGUI");
			#endif

			if(internalEditor == null)
			{
				var editorType = InitializableEditorInjector.GetCustomEditorType(target.GetType(), targets.Length > 0);
				CreateCachedEditor(targets, editorType, ref internalEditor);
				internalEditorIsGenericInspector = string.Equals(internalEditor.GetType().FullName, "UnityEditor.GenericInspector");
			}

			var internalGUI = internalEditor.CreateInspectorGUI();
			if(internalGUI == null)
			{
				#if DEV_MODE
				Profiler.EndSample();
				#endif
				return null;
			}

			var root = new VisualElement();
			root.Add(new IMGUIContainer(InitializerGUI));
			root.Add(internalGUI);
			if(ShowRuntimeFields)
			{
				root.Add(new IMGUIContainer(RuntimeFieldsGUI));
			}

			#if DEV_MODE
			Profiler.EndSample();
			#endif

			return root;
		}

		public override void OnInspectorGUI()
		{
			#if DEV_MODE
			Profiler.BeginSample("InitializableEditor.OnInspectorGUI");
			#endif

			InitializerGUI();
			BaseGUI();
			RuntimeFieldsGUI();

			#if DEV_MODE
			Profiler.EndSample();
			#endif
		}

		protected virtual void BaseGUI()
		{
			#if DEV_MODE
			Profiler.BeginSample("InitializableEditor.BaseGUI");
			#endif

			if(internalEditor == null)
			{
				var editorType = InitializableEditorInjector.GetCustomEditorType(target.GetType(), targets.Length > 0);
				CreateCachedEditor(targets, editorType, ref internalEditor);
				internalEditorIsGenericInspector = string.Equals(internalEditor.GetType().FullName, "UnityEditor.GenericInspector");
			}

			if(internalEditorIsGenericInspector)
			{
				serializedObject.Update();
				DrawPropertiesExcluding(serializedObject, scriptField);
				serializedObject.ApplyModifiedProperties();
			}
			else
			{
				serializedObject.ApplyModifiedProperties();
				internalEditor.serializedObject.Update();
				internalEditor.OnInspectorGUI();
				internalEditor.serializedObject.ApplyModifiedProperties();
				serializedObject.Update();
			}

			#if DEV_MODE
			Profiler.EndSample();
			#endif
		}

		protected virtual void InitializerGUI()
		{
			#if DEV_MODE
			Profiler.BeginSample("InitializableEditor.InitializerGUI");
			#endif

			if(initializerDrawer is null)
			{
				Setup();
			}

			if(drawInitializerGUI)
			{
				initializerDrawer.OnInspectorGUI();
			}

			#if DEV_MODE
			Profiler.EndSample();
			#endif
		}

		protected void RuntimeFieldsGUI()
		{
			#if DEV_MODE
			Profiler.BeginSample("InitializableEditor.RuntimeFieldsGUI");
			#endif

			if(!ShowRuntimeFields)
			{
				return;
			}

			if(runtimeFieldsDrawer is null)
			{
				runtimeFieldsDrawer = CreateRuntimeFieldsDrawer();
			}

			runtimeFieldsDrawer.Draw();

			#if DEV_MODE
			Profiler.EndSample();
			#endif
		}

        private void OnDisable()
		{
			EditorApplication.update -= OnUpdate;
			EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
			CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;

			DestroyWrappedEditors();
		}

		private void DestroyWrappedEditors()
		{
			if(initializerDrawer != null)
			{
				initializerDrawer.Dispose();
				initializerDrawer = null;
			}

			DestroyIfNotNull(ref internalEditor);
		}

		private void DestroyIfNotNull(ref Editor editor)
		{
			if(editor != null)
			{
				DestroyImmediate(editor);
			}

			editor = null;
		}

		private void OnUpdate()
		{
			if(Application.isPlaying && runtimeFieldsDrawer != null)
			{
                Repaint();
            }
		}
	}
}