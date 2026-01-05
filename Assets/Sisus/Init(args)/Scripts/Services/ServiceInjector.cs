//#define INIT_ARGS_DISABLE_SERVICE_CONSTRUCTOR_SUPPORT

//#define DEBUG_INIT_SERVICES
//#define DEBUG_CREATE_SERVICES
//#define DEBUG_LAZY_INIT

#if !INIT_ARGS_DISABLE_SERVICE_INJECTION
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using ServiceDefinitions = System.Collections.Generic.Dictionary<System.Type, Sisus.Init.ServiceAttribute.Info>.ValueCollection;

#if UNITY_ADDRESSABLES_1_17_4_OR_NEWER
using UnityEngine.AddressableAssets;
#endif

#if UNITY_EDITOR
using UnityEditor;
using Sisus.Init.EditorOnly;
using TypeCollection = UnityEditor.TypeCache.TypeCollection;
using Debug = UnityEngine.Debug;
#else
using TypeCollection = System.Collections.Generic.IEnumerable<System.Type>;
#endif

namespace Sisus.Init.Internal
{
	/// <summary>
	/// Class responsible for caching instances of all classes that have the <see cref="ServiceAttribute"/>,
	/// injecting dependencies for services that implement an <see cref="IInitializable{}"/>
	/// interface targeting only other services,
	/// and using <see cref="InitArgs.Set"/> to assign references to services ready to be retrieved
	/// for any other classes that implement an <see cref="IArgs{}"/> interface targeting only services.
	/// </summary>
	internal static class ServiceInjector
    {
        /// <summary>
        /// <see langword="true"/> if all services  have been created, initialized and are ready
        /// to be used by clients; otherwise, <see langword="false"/>.
        /// <para>
        /// This only takes into consideration services that are initialized synchronously and non-lazily
        /// during game initialization. To determine if all asynchronously initialized services are also
        /// ready to be used, use <see cref="AsyncServicesAreReady"/> instead.
        /// </para>
        /// <para>
        /// This only takes into consideration services defined using the <see cref="ServiceAttribute"/>.
        /// Services set up in scenes and prefabs using Service tags and <see cref="Services"/> components
        /// are not guaranteed to be yet loaded even if this is <see langword="true"/>.
        /// Services that are <see cref="Service.SetInstance">registered manually</see> are also not
        /// guaranteed to be loaded even if this is <see langword="true"/>.
        /// </para>
        /// </summary>
        public static bool ServicesAreReady { get; private set; }

        /// <summary>
        /// <see langword="true"/> if all services that are initialized asynchrously
        /// have been created, initialized and are ready to be used by clients.
        /// </para>
        /// This only takes into consideration services defined using the <see cref="ServiceAttribute"/>.
        /// Services set up in scenes and prefabs using Service tags and <see cref="Services"/> components
        /// are not guaranteed to be yet loaded even if this is <see langword="true"/>.
        /// Services that are <see cref="Service.SetInstance">registered manually</see> are also not
        /// guaranteed to be loaded even if this is <see langword="true"/>.
        /// </para>
        /// </summary>
        public static bool AsyncServicesAreReady { get; private set; }

        /// <summary>
        /// Called when all services have been created,
        /// initialized and are ready to be used by clients.
        /// <para>
        /// This only takes into consideration services that are initialized synchronously and non-lazily
        /// during game initialization. To get a callback when all asynchronously initialized services are also
        /// ready to be used, use <see cref="AsyncServicesBecameReady"/> instead.
        /// </para>
        /// </summary>
        public static event Action ServicesBecameReady;

        /// <summary>
        /// Called when all services that are initialized asynchrously have been created,
        /// initialized and are ready to be used by clients.
        /// </summary>
        public static event Action AsyncServicesBecameReady;

        internal static Dictionary<Type, object> services = new Dictionary<Type, object>();
        internal static readonly Dictionary<Type, ServiceAttribute.Info> unitializedServices = new Dictionary<Type, ServiceAttribute.Info>();
        private static GameObject container;

        #if UNITY_EDITOR
        /// <summary>
        /// Reset state when entering play mode in the editor to support Enter Play Mode Settings.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnEnterPlayMode()
		{
            ServicesAreReady = false;
            AsyncServicesAreReady = false;
            services.Clear();
            unitializedServices.Clear();

            static void OnExitingPlayMode() => ServicesAreReady = false;
            ThreadSafe.Application.ExitingPlayMode -= OnExitingPlayMode;
            ThreadSafe.Application.ExitingPlayMode += OnExitingPlayMode;
		}
        #endif

        /// <summary>
        /// Creates instances of all services,
        /// injects dependencies for servives that implement an <see cref="IInitializable{}"/>
        /// interface targeting only other services,
        /// and uses <see cref="InitArgs.Set"/> to assign references to services ready to be retrieved
        /// for any other classes that implement an <see cref="IArgs{}"/> interface targeting only services.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static async void CreateAndInjectServices()
        {
            #if DEV_MODE
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            #endif

            CreateInstancesOfAllServices();

            ServicesAreReady = true;

            #if UNITY_EDITOR
            var scriptableObjects = Resources.FindObjectsOfTypeAll<ScriptableObject>();
            var uninitializedScriptableObjects = new Dictionary<Type, List<ScriptableObject>>(scriptableObjects.Length);
            foreach(var uninitializedScriptableObject in scriptableObjects)
			{
                var type = uninitializedScriptableObject.GetType();
                if(!uninitializedScriptableObjects.TryGetValue(type, out List<ScriptableObject> instances))
				{
                    instances = new List<ScriptableObject>(1);
                    uninitializedScriptableObjects.Add(type, instances);
				}

                instances.Add(uninitializedScriptableObject);
			}
            #endif

            InjectServiceDependenciesForTypesThatRequireOnlyThem(out List<Task> injectAsyncServices
            #if UNITY_EDITOR
            , uninitializedScriptableObjects    
            #endif
            );

            #if UNITY_EDITOR
            InitializeAlreadyLoadedScriptableObjectsInTheEditor(uninitializedScriptableObjects);
            #endif

            ServicesBecameReady?.Invoke();
            ServicesBecameReady = null;

            #if DEV_MODE
            Debug.Log($"Initialization of {services.Count} service took {timer.Elapsed.TotalSeconds} seconds.");
            #endif

            await Task.WhenAll(injectAsyncServices);

            #if DEV_MODE
            timer.Stop();
            Debug.Log($"Injection of {injectAsyncServices.Count} async service took {timer.Elapsed.TotalSeconds} seconds.");
            #endif

            AsyncServicesAreReady = true;
            AsyncServicesBecameReady?.Invoke();
            AsyncServicesBecameReady = null;
        }

		private static void CreateInstancesOfAllServices()
        {
            var serviceDefinitions = GetServiceDefinitions();
            int count = serviceDefinitions.Count();
            if(count == 0)
            {
                services = null;
                return;
            }

            services = new Dictionary<Type, object>(count);
            HashSet<Type> initialized = new HashSet<Type>();
			CreateServices(serviceDefinitions, initialized);
            
            InjectCrossServiceDependencies(serviceDefinitions, initialized);

            #if UNITY_EDITOR
            _ = CreateServicesDebugger();
            #endif

            if(container != null)
            {
                container.SetActive(true);
            }

            HandleBroadcastingUnityEvents();
        }

        #if UNITY_EDITOR
        private static async Task CreateServicesDebugger()
        {
            if(!Application.isPlaying)
            {
                return;
            }

            if(container == null)
            {
                CreateServicesContainer();
            }

            var debugger = container.AddComponent<ServicesDebugger>();
            await debugger.SetServices(services.Values.Distinct());
        }
        #endif

        private static void ForEachService(Action<object> action)
		{
            foreach(var service in services.Values)
			{
                if(service is Task task)
				{
                    task.ContinueWith(InvokeOnResult);
                }
                else
                {
                    action(service);
                }
			}

            void InvokeOnResult(Task task) => action(task.GetResult());
		}

		private static void CreateServicesContainer()
        {
            container = new GameObject("Services");
            container.SetActive(false);
            container.hideFlags = HideFlags.DontSave;
            Object.DontDestroyOnLoad(container);
        }

        private static void CreateServices(ServiceDefinitions serviceDefinitions, HashSet<Type> initialized)
        {
            foreach(var serviceDefinition in serviceDefinitions)
			{
				if(serviceDefinition.LazyInit)
				{
                    #if DEV_MODE && DEBUG_LAZY_INIT
                    Debug.Log($"Will not initialize {classWithAttribute.FullName} yet because it has LazyInit set to true");
                    #endif

                    unitializedServices[serviceDefinition.concreteType] = serviceDefinition;
                    foreach(var definingType in serviceDefinition.definingTypes)
					{
                        unitializedServices[definingType] = serviceDefinition;
					}

					continue;
				}

				_ = GetOrCreateInstance(serviceDefinition, initialized);
			}
		}

        internal static async Task LazyInit(ServiceAttribute.Info serviceDefinition)
        {
            Type concreteType = serviceDefinition.concreteType;
            if(services.TryGetValue(concreteType, out object service))
            {
                return;
            }

            if(!unitializedServices.Remove(concreteType))
			{
                return;
			}

            var definingTypes = serviceDefinition.definingTypes;
            foreach(var definingType in definingTypes)
            {
                unitializedServices.Remove(definingType);
            }

            #if DEV_MODE && DEBUG_LAZY_INIT
            Debug.Log($"LazyInit({definingType.Name} => {concreteType.FullName})");
            #endif

            #if UNITY_2021_3_OR_NEWER
            var initialized = new HashSet<Type>(256);
            #else
            var initialized = new HashSet<Type>();
            #endif

            service = await GetOrCreateInstance(serviceDefinition, initialized);

            #if UNITY_EDITOR
            if(container != null && container.TryGetComponent(out ServicesDebugger debugger))
            {
                _ = debugger.SetServices(services.Values.Distinct());
            }
            #endif

            await InjectCrossServiceDependencies(service, initialized);
        }

        private static async Task<object> GetOrCreateInstance(ServiceAttribute.Info serviceDefinition, HashSet<Type> initialized)
        {
            var concreteType = serviceDefinition.concreteType;

            // If one class contains multiple Service attributes still create only one shared instance.
            if(services.TryGetValue(concreteType, out var existingInstance))
            {
                if(existingInstance is Task existingTask)
				{
                    existingInstance = await existingTask.GetResult();
				}

                // needed?
                //SetInstanceSync(serviceDefinition, existingInstance);

                return existingInstance;
            }

            Task<object> task;
            try
			{
				task = LoadInstance(serviceDefinition, initialized);
			}
            catch(MissingMethodException e)
			{
                Debug.LogWarning($"Failed to initialize service {concreteType.Name} with defining types {string.Join(", ", serviceDefinition.definingTypes.Select(t => t?.Name))}.\nThis can happen when constructor arguments contain circular references (for example: object A requires object B, but object B also requires object A, so neither object can be constructed).\n{e}");
                return null;
			}
			catch(Exception e)
			{
				Debug.LogWarning($"Failed to initialize service {concreteType.Name} with defining types {string.Join(", ", serviceDefinition.definingTypes.Select(t => t?.Name))}.\n{e}");
                return null;
			}
            
            services[concreteType] = task;
            foreach(var definingType in serviceDefinition.definingTypes)
			{
                services[definingType] = task;
			}

            object result = await task;
            if(result is Task chainedTask)
            {
                result = await chainedTask.GetResult();
			}

            if(result is null)
            {
                #if DEV_MODE
                Debug.LogWarning($"GetOrCreateInstance(concreteType:{concreteType.Name}, definingTypes:{string.Join(", ", serviceDefinition.definingTypes.Select(t => t?.Name))}) returned instance was null.");
                #endif
                return null;
            }

            SetInstanceSync(serviceDefinition, result);
            return result;
        }

        private static async Task<object> LoadInstance(ServiceAttribute.Info serviceDefinition, HashSet<Type> initialized)
        {
            Type concreteType = serviceDefinition.concreteType;
            if(typeof(IServiceInitializer).IsAssignableFrom(serviceDefinition.classWithAttribute))
			{
                if(initialized.Contains(concreteType))
			    {
                    return null;
			    }

                Type initializerType = serviceDefinition.classWithAttribute;
                var serviceInitializer = Activator.CreateInstance(initializerType) as IServiceInitializer;
                var interfaceTypes = initializerType.GetInterfaces();
                for(int i = interfaceTypes.Length - 1; i >= 0; i--)
                {
                    var interfaceType = interfaceTypes[i];
                    if(!interfaceType.IsGenericType)
                    {
                        continue;
                    }

                    var typeDefinition = interfaceType.GetGenericTypeDefinition();

                    if(typeDefinition == typeof(IServiceInitializer<,,,,,,>))
                    {
                        var argumentTypes = interfaceType.GetGenericArguments();
                        var firstArgumentType = argumentTypes[1];
                        var secondArgumentType = argumentTypes[2];
                        var thirdArgumentType = argumentTypes[3];
                        var fourthArgumentType = argumentTypes[4];
                        var fifthArgumentType = argumentTypes[5];
                        var sixthArgumentType = argumentTypes[6];
                        initialized.Add(concreteType);
                        if(TryGetOrCreateService(firstArgumentType, out object firstArgument, initialized)
                        && TryGetOrCreateService(secondArgumentType, out object secondArgument, initialized)
                        && TryGetOrCreateService(thirdArgumentType, out object thirdArgument, initialized)
                        && TryGetOrCreateService(fourthArgumentType, out object fourthArgument, initialized)
                        && TryGetOrCreateService(fifthArgumentType, out object fifthArgument, initialized)
                        && TryGetOrCreateService(sixthArgumentType, out object sixthArgument, initialized))
                        {
                            Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                            Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                            Task thirdArgumentTask = !thirdArgumentType.IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                            Task fourthArgumentTask = !fourthArgumentType.IsInstanceOfType(fourthArgument) ? fourthArgument as Task : null;
                            Task fifthArgumentTask = !fifthArgumentType.IsInstanceOfType(fifthArgument) ? fifthArgument as Task : null;
                            Task sixthArgumentTask = !sixthArgumentType.IsInstanceOfType(thirdArgument) ? sixthArgument as Task : null;

                            var loadArgumentTasks = Enumerable.Empty<Task>();

                            if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                            if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                            if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);
                            if(fourthArgumentTask != null) loadArgumentTasks.Append(fourthArgumentTask);
                            if(fifthArgumentTask != null) loadArgumentTasks.Append(fifthArgumentTask);
                            if(sixthArgumentTask != null) loadArgumentTasks.Append(sixthArgumentTask);

                            await Task.WhenAll(loadArgumentTasks);

                            if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                            if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                            if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();
                            if(fourthArgumentTask != null) fourthArgument = await fourthArgumentTask.GetResult();
                            if(fifthArgumentTask != null) fifthArgument  = await fifthArgumentTask.GetResult();
                            if(sixthArgumentTask != null) sixthArgument  = await sixthArgumentTask.GetResult();

                            await InjectCrossServiceDependencies(firstArgument, initialized);
                            await InjectCrossServiceDependencies(secondArgument, initialized);
                            await InjectCrossServiceDependencies(thirdArgument, initialized);
                            await InjectCrossServiceDependencies(fourthArgument, initialized);
                            await InjectCrossServiceDependencies(fifthArgument, initialized);
                            await InjectCrossServiceDependencies(sixthArgument, initialized);

                            #if DEBUG_CREATE_SERVICES
                            Debug.Log($"Service {concreteType.Name} created via service initializer {serviceInitializer.GetType().Name} successfully.");
                            #endif

                            var result = interfaceType.GetMethod(nameof(IServiceInitializer.InitTarget), new Type[] { firstArgumentType, secondArgumentType, thirdArgumentType, fourthArgumentType, fifthArgumentType, sixthArgumentType }).Invoke(serviceInitializer, new object[] { firstArgument, secondArgument, thirdArgument, fourthArgument, fifthArgument, sixthArgument });

							return result is Task task ? await task.GetResult() : result;
                        }
                        #if DEBUG_INIT_SERVICES
                        else { Debug.Log($"Service {concreteType.Name} requires 6 arguments but instances not found among {services.Count} services..."); }
                        #endif

                        initialized.Remove(concreteType);
                    }

                    if(typeDefinition == typeof(IServiceInitializer<,,,,,>))
                    {
                        var argumentTypes = interfaceType.GetGenericArguments();
                        var firstArgumentType = argumentTypes[1];
                        var secondArgumentType = argumentTypes[2];
                        var thirdArgumentType = argumentTypes[3];
                        var fourthArgumentType = argumentTypes[4];
                        var fifthArgumentType = argumentTypes[5];
                        initialized.Add(concreteType);
                        if(TryGetOrCreateService(firstArgumentType, out object firstArgument, initialized)
                        && TryGetOrCreateService(secondArgumentType, out object secondArgument, initialized)
                        && TryGetOrCreateService(thirdArgumentType, out object thirdArgument, initialized)
                        && TryGetOrCreateService(fourthArgumentType, out object fourthArgument, initialized)
                        && TryGetOrCreateService(fifthArgumentType, out object fifthArgument, initialized))
                        {
                            Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                            Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                            Task thirdArgumentTask = !thirdArgumentType.IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                            Task fourthArgumentTask = !fourthArgumentType.IsInstanceOfType(fourthArgument) ? fourthArgument as Task : null;
                            Task fifthArgumentTask = !fifthArgumentType.IsInstanceOfType(fifthArgument) ? fifthArgument as Task : null;

                            var loadArgumentTasks = Enumerable.Empty<Task>();

                            if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                            if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                            if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);
                            if(fourthArgumentTask != null) loadArgumentTasks.Append(fourthArgumentTask);
                            if(fifthArgumentTask != null) loadArgumentTasks.Append(fifthArgumentTask);

                            await Task.WhenAll(loadArgumentTasks);

                            if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                            if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                            if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();
                            if(fourthArgumentTask != null) fourthArgument = await fourthArgumentTask.GetResult();
                            if(fifthArgumentTask != null) fifthArgument  = await fifthArgumentTask.GetResult();

                            await InjectCrossServiceDependencies(firstArgument, initialized);
                            await InjectCrossServiceDependencies(secondArgument, initialized);
                            await InjectCrossServiceDependencies(thirdArgument, initialized);
                            await InjectCrossServiceDependencies(fourthArgument, initialized);
                            await InjectCrossServiceDependencies(fifthArgument, initialized);

                            #if DEBUG_CREATE_SERVICES
                            Debug.Log($"Service {concreteType.Name} created via service initializer {serviceInitializer.GetType().Name} successfully.");
                            #endif

                            var result = interfaceType.GetMethod(nameof(IServiceInitializer.InitTarget), new Type[] { firstArgumentType, secondArgumentType, thirdArgumentType, fourthArgumentType, fifthArgumentType }).Invoke(serviceInitializer, new object[] { firstArgument, secondArgument, thirdArgument, fourthArgument, fifthArgument });

                            return result is Task task ? await task.GetResult() : result;
                        }
                        #if DEBUG_INIT_SERVICES
                        else { Debug.Log($"Service {concreteType.Name} requires 5 arguments but instances not found among {services.Count} services..."); }
                        #endif

                        initialized.Remove(concreteType);
                    }

                    if(typeDefinition == typeof(IServiceInitializer<,,,,>))
                    {
                        var argumentTypes = interfaceType.GetGenericArguments();
                        var firstArgumentType = argumentTypes[1];
                        var secondArgumentType = argumentTypes[2];
                        var thirdArgumentType = argumentTypes[3];
                        var fourthArgumentType = argumentTypes[4];
                        initialized.Add(concreteType);
                        if(TryGetOrCreateService(firstArgumentType, out object firstArgument, initialized)
                        && TryGetOrCreateService(secondArgumentType, out object secondArgument, initialized)
                        && TryGetOrCreateService(thirdArgumentType, out object thirdArgument, initialized)
                        && TryGetOrCreateService(fourthArgumentType, out object fourthArgument, initialized))
                        {
                            Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                            Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                            Task thirdArgumentTask = !thirdArgumentType.IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                            Task fourthArgumentTask = !fourthArgumentType.IsInstanceOfType(fourthArgument) ? fourthArgument as Task : null;

                            var loadArgumentTasks = Enumerable.Empty<Task>();

                            if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                            if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                            if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);
                            if(fourthArgumentTask != null) loadArgumentTasks.Append(fourthArgumentTask);

                            await Task.WhenAll(loadArgumentTasks);

                            if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                            if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                            if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();
                            if(fourthArgumentTask != null) fourthArgument = await fourthArgumentTask.GetResult();

                            await InjectCrossServiceDependencies(firstArgument, initialized);
                            await InjectCrossServiceDependencies(secondArgument, initialized);
                            await InjectCrossServiceDependencies(thirdArgument, initialized);
                            await InjectCrossServiceDependencies(fourthArgument, initialized);

                            #if DEBUG_CREATE_SERVICES
                            Debug.Log($"Service {concreteType.Name} created via service initializer {serviceInitializer.GetType().Name} successfully.");
                            #endif

                            var result = interfaceType.GetMethod(nameof(IServiceInitializer.InitTarget), new Type[] { firstArgumentType, secondArgumentType, thirdArgumentType, fourthArgumentType }).Invoke(serviceInitializer, new object[] { firstArgument, secondArgument, thirdArgument, fourthArgument });

                            return result is Task task ? await task.GetResult() : result;
                        }
                        #if DEBUG_INIT_SERVICES
                        else { Debug.Log($"Service {concreteType.Name} requires 4 arguments but instances not found among {services.Count} services..."); }
                        #endif

                        initialized.Remove(concreteType);
                    }

                    if(typeDefinition == typeof(IServiceInitializer<,,,>))
                    {
                        var argumentTypes = interfaceType.GetGenericArguments();
                        var firstArgumentType = argumentTypes[1];
                        var secondArgumentType = argumentTypes[2];
                        var thirdArgumentType = argumentTypes[3];
                        initialized.Add(concreteType);
                        if(TryGetOrCreateService(firstArgumentType, out object firstArgument, initialized)
                        && TryGetOrCreateService(secondArgumentType, out object secondArgument, initialized)
                        && TryGetOrCreateService(thirdArgumentType, out object thirdArgument, initialized))
                        {
                            Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                            Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                            Task thirdArgumentTask = !thirdArgumentType.IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;

                            var loadArgumentTasks = Enumerable.Empty<Task>();

                            if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                            if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                            if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);

                            await Task.WhenAll(loadArgumentTasks);

                            if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                            if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                            if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();

                            await InjectCrossServiceDependencies(firstArgument, initialized);
                            await InjectCrossServiceDependencies(secondArgument, initialized);
                            await InjectCrossServiceDependencies(thirdArgument, initialized);

                            #if DEBUG_CREATE_SERVICES
                            Debug.Log($"Service {concreteType.Name} created via service initializer {serviceInitializer.GetType().Name} successfully.");
                            #endif

                            var result = interfaceType.GetMethod(nameof(IServiceInitializer.InitTarget), new Type[] { firstArgumentType, secondArgumentType, thirdArgumentType }).Invoke(serviceInitializer, new object[] { firstArgument, secondArgument, thirdArgument });

                            return result is Task task ? await task.GetResult() : result;
                        }
                        #if DEBUG_INIT_SERVICES
                        else { Debug.Log($"Service {concreteType.Name} requires 3 arguments but instances not found among {services.Count} services..."); }
                        #endif

                        initialized.Remove(concreteType);
                    }

                    if(typeDefinition == typeof(IServiceInitializer<,,>))
                    {
                        var argumentTypes = interfaceType.GetGenericArguments();
                        var firstArgumentType = argumentTypes[1];
                        var secondArgumentType = argumentTypes[2];
                        initialized.Add(concreteType);

                        if(TryGetOrCreateService(firstArgumentType, out object firstArgument, initialized)
                        && TryGetOrCreateService(secondArgumentType, out object secondArgument, initialized))
                        {
                            Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                            Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;

                            var loadArgumentTasks = Enumerable.Empty<Task>();

                            if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                            if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);

                            await Task.WhenAll(loadArgumentTasks);

                            if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();

                            if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();

                            await InjectCrossServiceDependencies(firstArgument, initialized);
                            await InjectCrossServiceDependencies(secondArgument, initialized);

                            #if DEBUG_CREATE_SERVICES
                            Debug.Log($"Service {concreteType.Name} created via service initializer {serviceInitializer.GetType().Name} successfully.");
                            #endif

                            var result = interfaceType.GetMethod(nameof(IServiceInitializer.InitTarget), new Type[] { firstArgumentType, secondArgumentType }).Invoke(serviceInitializer, new object[] { firstArgument, secondArgument });

                            return result is Task task ? await task.GetResult() : result;
                        }
                        #if DEBUG_INIT_SERVICES
                        else { Debug.Log($"Service {concreteType.Name} requires 2 arguments but instances not found among {services.Count} services..."); }
                        #endif

                        initialized.Remove(concreteType);
                    }

                    if(typeDefinition == typeof(IServiceInitializer<,>))
                    {
                        var argumentType = interfaceType.GetGenericArguments()[1];
                        initialized.Add(concreteType);

                        if(TryGetOrCreateService(argumentType, out object argument, initialized))
                        {
                            if(!argumentType.IsInstanceOfType(argument) && argument is Task argumentTask)
							{
                                argument = await argumentTask.GetResult();
							}

                            await InjectCrossServiceDependencies(argument, initialized);

                            #if DEBUG_CREATE_SERVICES
                            Debug.Log($"Service {concreteType.Name} created via service initializer {serviceInitializer.GetType().Name} successfully.");
                            #endif

                            var result = interfaceType.GetMethod(nameof(IServiceInitializer.InitTarget), new Type[] { argumentType }).Invoke(serviceInitializer, new object[] { argument });

							return result is Task task ? await task.GetResult() : result;
						}
                        #if DEBUG_INIT_SERVICES
						else { Debug.Log($"Service {concreteType.Name} requires argument {interfaceType.GetGenericArguments()[0].Name} but instance not found among {services.Count} services..."); }
                        #endif

                        initialized.Remove(concreteType);
                    }
                }

                var instance = serviceInitializer.InitTarget();
                if(instance != null)
				{
                    initialized.Add(concreteType);
                    return instance;
				}
			}

            if(serviceDefinition.FindFromScene)
            {
                object instance;
                if(typeof(Component).IsAssignableFrom(concreteType))
                {
                    instance = 
                    #if UNITY_2023_1_OR_NEWER
                    Object.FindAnyObjectByType(concreteType, FindObjectsInactive.Exclude);
                    #else
                    Object.FindObjectOfType(concreteType);
                    #endif
                }
                else if(typeof(Component).IsAssignableFrom(serviceDefinition.classWithAttribute))
                {
                    instance = 
                    #if UNITY_2023_1_OR_NEWER
                    Object.FindAnyObjectByType(serviceDefinition.classWithAttribute, FindObjectsInactive.Exclude);
                    #else
                    Object.FindObjectOfType(serviceDefinition.classWithAttribute);
                    #endif
                }
                else
				{
                    instance = Find.Any(concreteType, false);
				}

                if(instance != null)
                {
                    #if DEBUG_CREATE_SERVICES
                    Debug.Log($"Service {concreteType.Name} retrieved from scene successfully.", instance);
                    #endif

                    if(IsInstanceOf(serviceDefinition, instance))
					{
                        return instance;
					}

                    if(instance is IInitializer initializerWithAttribute)
					{
                        instance = initializerWithAttribute.InitTarget();
                        if(IsInstanceOf(serviceDefinition, instance))
					    {
                            return instance;
					    }
                    }

                    if(instance is IValueProvider valueProvider && valueProvider.Value is object value)
                    {
                        #if DEBUG || INIT_ARGS_SAFE_MODE
                        if(!IsInstanceOf(serviceDefinition, value))
					    {
                            throw new Exception($"Failed to convert service instance from {value.GetType().Name} to {serviceDefinition.definingTypes.FirstOrDefault()?.Name}.");
					    }
                        #endif

                        return value;
                    }
                }

                var allInitializerInLoadedScenes = Find.All<IInitializer>();
                foreach(var initializer in allInitializerInLoadedScenes)
                {
                    if(TargetIsAssignableOrConvertibleToType(initializer, serviceDefinition) && initializer.InitTarget() is object initializedTarget)
                    {
                        #if DEBUG_CREATE_SERVICES
                        Debug.Log($"Service {initializedTarget.GetType().Name} retrieved from scene successfully.", instance);
                        #endif

                        // Support Initializer -> Initialized
                        if(IsInstanceOf(serviceDefinition, instance))
					    {
                            return initializedTarget;
						}

                        // Support WrapperInitializer -> Wrapper -> Wrapped Object
                        if(initializedTarget is IValueProvider valueProvider)
					    {
                            return valueProvider.Value;
                        }
                    }
                }

                foreach(var servicesComponent in Find.All<Services>())
				{
					if(!servicesComponent.AreAvailableToAnyClient())
					{
						continue;
					}

					foreach(var serviceComponent in servicesComponent.providesServices)
					{
						if(serviceDefinition.HasDefiningType(serviceComponent.definingType.Value) && TryGetService(serviceComponent.service, serviceDefinition, out var result))
						{
							return result;
						}
					}
				}

				if(typeof(ScriptableObject).IsAssignableFrom(concreteType))
                {
                    Debug.LogWarning($"Invalid Service Definition: Service '{concreteType.Name}' is a ScriptableObject type but has the {nameof(ServiceAttribute)} with {nameof(ServiceAttribute.FindFromScene)} set to true. ScriptableObjects can not exist in scenes and as such can't be retrived using this method.");
                    return null;
                }

                #if UNITY_EDITOR
                if(!IsFirstSceneInBuildSettingsLoaded()) { return null; }
                #endif

                Debug.LogWarning($"Service Not Found: There is no '{concreteType.Name}' found in the active scene {SceneManager.GetActiveScene().name}, but the service class has the {nameof(ServiceAttribute)} with {nameof(ServiceAttribute.FindFromScene)} set to true. Either add an instance to the scene or don't set {nameof(ServiceAttribute.FindFromScene)} true to have a new instance be created automatically.");
                return null;
            }

            if(serviceDefinition.ResourcePath is string resourcePath)
            {
                Object asset;
                if(serviceDefinition.LoadAsync)
				{
  					ResourceRequest resourceRequest = Resources.LoadAsync(resourcePath, typeof(Object));

                    while(!resourceRequest.isDone)
					{
                        await Task.Yield();
					}

                    asset = resourceRequest.asset;
				}
                else
				{
                    asset = Resources.Load(resourcePath, typeof(Object));
				}

                if(asset == null)
                {
                    Debug.LogWarning($"Service Not Found: There is no '{concreteType.Name}' found at resource path 'Resources/{resourcePath}', but the service class has the {nameof(ServiceAttribute)} {nameof(ServiceAttribute.ResourcePath)} set to '{resourcePath}'. Either make sure an asset exists in the project at the expected path or don't specify a {nameof(ServiceAttribute.ResourcePath)} to have a new instance be created automatically.");
                    return null;
                }

                if(TryGetService(asset, serviceDefinition, out object result))
                {
                    #if DEBUG_CREATE_SERVICES
                    Debug.Log($"Service {concreteType.Name} loaded from resources successfully.", asset);
                    #endif

                    return result;
                }

                Debug.LogWarning($"Service Not Found: Resource at path 'Resources/{resourcePath}' could not be converted to type {serviceDefinition.definingTypes.FirstOrDefault()?.Name}.");
                return null;
            }

            #if UNITY_ADDRESSABLES_1_17_4_OR_NEWER
            if(serviceDefinition.AddressableKey is string addressableKey)
            {
                var asyncOperation = Addressables.LoadAssetAsync<Object>(addressableKey);
                Object asset;

                if(serviceDefinition.LoadAsync)
				{
                    asset = await asyncOperation.Task;
				}
                else
                {
                    asset = asyncOperation.WaitForCompletion();
                }

                if(asset == null)
                {
                    Debug.LogWarning($"Service Not Found: There is no '{concreteType.Name}' found in the Addressable registry under the address {addressableKey}, but the service class has the {nameof(ServiceAttribute)} with {nameof(ServiceAttribute.AddressableKey)} set to '{addressableKey}'. Either make sure an instance with the address exists in the project or don't specify a {nameof(ServiceAttribute.ResourcePath)} to have a new instance be created automatically.");
                    return null;
                }

                if(TryGetService(asset, serviceDefinition, out object result))
                {
                    #if DEBUG_CREATE_SERVICES
                    Debug.Log($"Service {concreteType.Name} loaded using Addressables successfully.", asset);
                    Debug.Assert(asset != null, addressableKey, asset);
                    Debug.Assert(result != null, addressableKey, asset);
                    #endif

                    return result;
                }

                Debug.LogWarning($"Service Not Found: Addressable in the Addressable registry under the address {addressableKey} could not be converted to type {serviceDefinition.definingTypes.FirstOrDefault()?.Name}.");
                return null;
            }
            #endif

            if(typeof(Component).IsAssignableFrom(concreteType))
            {
                if(container == null)
                {
                    CreateServicesContainer();
                }

                #if DEBUG_CREATE_SERVICES
                Debug.Log($"Service {concreteType.Name} added to Services container.", container);
                #endif

                return container.AddComponent(concreteType);
            }

            if(typeof(ScriptableObject).IsAssignableFrom(concreteType))
            {
                #if DEBUG_CREATE_SERVICES
                Debug.Log($"Service {concreteType.Name} created successfully.");
                #endif

                return ScriptableObject.CreateInstance(concreteType);
            }

            #if !INIT_ARGS_DISABLE_SERVICE_CONSTRUCTOR_SUPPORT
            if(initialized.Contains(concreteType))
			{
                return null;
			}

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            var constructors = concreteType.GetConstructors(flags);
            IEnumerable<ConstructorInfo> constructorsByParameterCount = constructors.Length <= 1 ? constructors as IEnumerable<ConstructorInfo> : constructors.OrderByDescending(c => c.GetParameters().Length);
            foreach(var constructor in constructorsByParameterCount)
            {
                var parameters = constructor.GetParameters();

                switch(parameters.Length)
				{
                    case 1:
                        initialized.Add(concreteType);
                        Type firstArgumentType = parameters[0].ParameterType;
                        if(TryGetOrCreateService(firstArgumentType, out object firstArgument, initialized))
                        {
                            Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                            if(firstArgumentTask != null)
                            {
                                await firstArgumentTask;
                                firstArgument = await firstArgumentTask.GetResult();
                            }

                            await InjectCrossServiceDependencies(firstArgument, initialized);
                            return constructor.Invoke(new object[] { firstArgument });
                        }
                        initialized.Remove(concreteType);
                        break;
                    case 2:
                        initialized.Add(concreteType);
                        firstArgumentType = parameters[0].ParameterType;
                        Type secondArgumentType = parameters[1].ParameterType;
                        if(TryGetOrCreateService(firstArgumentType, out firstArgument, initialized)
                        && TryGetOrCreateService(secondArgumentType, out object secondArgument, initialized))
                        {
                            Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                            Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;

                            var loadArgumentTasks = Enumerable.Empty<Task>();

                            if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                            if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);

                            await Task.WhenAll(loadArgumentTasks);

                            if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                            if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();

                            await InjectCrossServiceDependencies(firstArgument, initialized);
                            await InjectCrossServiceDependencies(secondArgument, initialized);
                            return constructor.Invoke(new object[] { firstArgument, secondArgument });
                        }
                        initialized.Remove(concreteType);
                        break;
                    case 3:
                        initialized.Add(concreteType);
                        firstArgumentType = parameters[0].ParameterType;
                        secondArgumentType = parameters[1].ParameterType;
                        Type thirdArgumentType = parameters[2].ParameterType;
                        if(TryGetOrCreateService(firstArgumentType, out firstArgument, initialized)
                        && TryGetOrCreateService(secondArgumentType, out secondArgument, initialized)
                        && TryGetOrCreateService(thirdArgumentType, out object thirdArgument, initialized))
                        {
                            Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                            Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                            Task thirdArgumentTask = !thirdArgumentType.IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;

                            var loadArgumentTasks = Enumerable.Empty<Task>();

                            if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                            if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                            if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);

                            await Task.WhenAll(loadArgumentTasks);

                            if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                            if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                            if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();

                            await InjectCrossServiceDependencies(firstArgument, initialized);
                            await InjectCrossServiceDependencies(secondArgument, initialized);
                            await InjectCrossServiceDependencies(thirdArgument, initialized);
                            return constructor.Invoke(new object[] { firstArgument, secondArgument, thirdArgument });
                        }
                        initialized.Remove(concreteType);
                        break;
                    case 4:
                        initialized.Add(concreteType);
                        firstArgumentType = parameters[0].ParameterType;
                        secondArgumentType = parameters[1].ParameterType;
                        thirdArgumentType = parameters[2].ParameterType;
                        Type fourthArgumentType = parameters[3].ParameterType;
                        if(TryGetOrCreateService(firstArgumentType, out firstArgument, initialized)
                        && TryGetOrCreateService(secondArgumentType, out secondArgument, initialized)
                        && TryGetOrCreateService(thirdArgumentType, out thirdArgument, initialized)
                        && TryGetOrCreateService(fourthArgumentType, out object fourthArgument, initialized))
                        {
                            Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                            Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                            Task thirdArgumentTask = !thirdArgumentType.IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                            Task fourthArgumentTask = !fourthArgumentType.IsInstanceOfType(fourthArgument) ? fourthArgument as Task : null;

                            var loadArgumentTasks = Enumerable.Empty<Task>();

                            if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                            if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                            if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);
                            if(fourthArgumentTask != null) loadArgumentTasks.Append(fourthArgumentTask);

                            await Task.WhenAll(loadArgumentTasks);

                            if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                            if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                            if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();
                            if(fourthArgumentTask != null) fourthArgument = await fourthArgumentTask.GetResult();

                            await InjectCrossServiceDependencies(firstArgument, initialized);
                            await InjectCrossServiceDependencies(secondArgument, initialized);
                            await InjectCrossServiceDependencies(thirdArgument, initialized);
                            await InjectCrossServiceDependencies(fourthArgument, initialized);
                            return constructor.Invoke(new object[] { firstArgument, secondArgument, thirdArgument, fourthArgument });
                        }
                        initialized.Remove(concreteType);
                        break;
                    case 5:
                        initialized.Add(concreteType);
                        firstArgumentType = parameters[0].ParameterType;
                        secondArgumentType = parameters[1].ParameterType;
                        thirdArgumentType = parameters[2].ParameterType;
                        fourthArgumentType = parameters[3].ParameterType;
                        Type fifthArgumentType = parameters[4].ParameterType;
                        if(TryGetOrCreateService(firstArgumentType, out firstArgument, initialized)
                        && TryGetOrCreateService(secondArgumentType, out secondArgument, initialized)
                        && TryGetOrCreateService(thirdArgumentType, out thirdArgument, initialized)
                        && TryGetOrCreateService(fourthArgumentType, out fourthArgument, initialized)
                        && TryGetOrCreateService(fifthArgumentType, out object fifthArgument, initialized))
                        {
                            Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                            Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                            Task thirdArgumentTask = !thirdArgumentType.IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                            Task fourthArgumentTask = !fourthArgumentType.IsInstanceOfType(fourthArgument) ? fourthArgument as Task : null;
                            Task fifthArgumentTask = !fifthArgumentType.IsInstanceOfType(fifthArgument) ? fifthArgument as Task : null;
                            
                            var loadArgumentTasks = Enumerable.Empty<Task>();

                            if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                            if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                            if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);
                            if(fourthArgumentTask != null) loadArgumentTasks.Append(fourthArgumentTask);
                            if(fifthArgumentTask != null) loadArgumentTasks.Append(fifthArgumentTask);

                            await Task.WhenAll(loadArgumentTasks);

                            if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                            if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                            if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();
                            if(fourthArgumentTask != null) fourthArgument = await fourthArgumentTask.GetResult();
                            if(fifthArgumentTask != null) fifthArgument  = await fifthArgumentTask.GetResult();

                            await InjectCrossServiceDependencies(firstArgument, initialized);
                            await InjectCrossServiceDependencies(secondArgument, initialized);
                            await InjectCrossServiceDependencies(thirdArgument, initialized);
                            await InjectCrossServiceDependencies(fourthArgument, initialized);
                            await InjectCrossServiceDependencies(fifthArgument, initialized);
                            return constructor.Invoke(new object[] { firstArgument, secondArgument, thirdArgument, fourthArgument, fifthArgument });
                        }
                        initialized.Remove(concreteType);
                        break;
                    case 6:
                        initialized.Add(concreteType);
                        firstArgumentType = parameters[0].ParameterType;
                        secondArgumentType = parameters[1].ParameterType;
                        thirdArgumentType = parameters[2].ParameterType;
                        fourthArgumentType = parameters[3].ParameterType;
                        fifthArgumentType = parameters[4].ParameterType;
                        Type sixthArgumentType = parameters[5].ParameterType;
                        if(TryGetOrCreateService(firstArgumentType, out firstArgument, initialized)
                        && TryGetOrCreateService(secondArgumentType, out secondArgument, initialized)
                        && TryGetOrCreateService(thirdArgumentType, out thirdArgument, initialized)
                        && TryGetOrCreateService(fourthArgumentType, out fourthArgument, initialized)
                        && TryGetOrCreateService(fifthArgumentType, out fifthArgument, initialized)
                        && TryGetOrCreateService(sixthArgumentType, out object sixthArgument, initialized))
                        {
                            Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                            Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                            Task thirdArgumentTask = !thirdArgumentType.IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                            Task fourthArgumentTask = !fourthArgumentType.IsInstanceOfType(fourthArgument) ? fourthArgument as Task : null;
                            Task fifthArgumentTask = !fifthArgumentType.IsInstanceOfType(fifthArgument) ? fifthArgument as Task : null;
                            Task sixthArgumentTask = !sixthArgumentType.IsInstanceOfType(thirdArgument) ? sixthArgument as Task : null;

                            var loadArgumentTasks = Enumerable.Empty<Task>();

                            if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                            if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                            if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);
                            if(fourthArgumentTask != null) loadArgumentTasks.Append(fourthArgumentTask);
                            if(fifthArgumentTask != null) loadArgumentTasks.Append(fifthArgumentTask);
                            if(sixthArgumentTask != null) loadArgumentTasks.Append(sixthArgumentTask);

                            await Task.WhenAll(loadArgumentTasks);

                            if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                            if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                            if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();
                            if(fourthArgumentTask != null) fourthArgument = await fourthArgumentTask.GetResult();
                            if(fifthArgumentTask != null) fifthArgument  = await fifthArgumentTask.GetResult();
                            if(sixthArgumentTask != null) sixthArgument  = await sixthArgumentTask.GetResult();

                            await InjectCrossServiceDependencies(firstArgument, initialized);
                            await InjectCrossServiceDependencies(secondArgument, initialized);
                            await InjectCrossServiceDependencies(thirdArgument, initialized);
                            await InjectCrossServiceDependencies(fourthArgument, initialized);
                            await InjectCrossServiceDependencies(fifthArgument, initialized);
                            await InjectCrossServiceDependencies(sixthArgument, initialized);
                            return constructor.Invoke(new object[] { firstArgument, secondArgument, thirdArgument, fourthArgument, fifthArgument, sixthArgument });
                        }
                        initialized.Remove(concreteType);
                        break;
                }
			}
            #endif

            #if DEBUG_CREATE_SERVICES
            Debug.Log($"Service {concreteType} created successfully via default constructor.");
            #endif

            return Activator.CreateInstance(concreteType);
        }

        private static bool TargetIsAssignableOrConvertibleToType(IInitializer initializer, ServiceAttribute.Info serviceDefinition)
		{
            foreach(var definingType in serviceDefinition.definingTypes)
			{
                if(initializer.TargetIsAssignableOrConvertibleToType(definingType))
				{
                    return true;
				}
			}

            return false;
		}

        #if !INIT_ARGS_DISABLE_SERVICE_CONSTRUCTOR_SUPPORT
        private static bool TryGetOrCreateService(Type definingType, out object service, HashSet<Type> initialized)
        {
            if(TryGetService(definingType, out service, initialized))
			{
                return true;
			}

            var serviceDefinition = GetServiceDefinition(definingType);
            if(serviceDefinition.classWithAttribute != null)
			{
                service = GetOrCreateInstance(serviceDefinition, initialized);
                return service != null;
			}

            return false;
        }
        #endif

        private static bool TryGetService([NotNull] GameObject gameObject, ServiceAttribute.Info serviceDefinition, out object instance)
        {
            if(typeof(Component).IsAssignableFrom(serviceDefinition.classWithAttribute) && gameObject.TryGetComponent(serviceDefinition.classWithAttribute, out var component))
            {
                if(IsInstanceOf(serviceDefinition, component))
                {
                    instance = component;
                    return true;
                }

                if(component is IValueProvider valueProvider && valueProvider.Value is object value && IsInstanceOf(serviceDefinition, value))
                {
                    instance = value;
                    return true;
                }

                if(component is IInitializer initializer && TargetIsAssignableOrConvertibleToType(initializer, serviceDefinition))
                {
                    instance = initializer.InitTarget();
                    if(IsInstanceOf(serviceDefinition, instance))
					{
                        return true;
					}

                    valueProvider = instance as IValueProvider;
                    if(valueProvider != null)
					{
                        instance = valueProvider.Value;
                    }

                    return instance != null;
                }

                instance = null;
                return false;
            }

            var valueProviders = gameObject.GetComponents<IValueProvider>();
            foreach(var valueProvider in valueProviders)
            {
                var value = valueProvider.Value;
                if(value != null && IsInstanceOf(serviceDefinition, value))
                {
                    instance = value;
                    return true;
                }
            }

            foreach(var valueProvider in valueProviders)
            {
                if(valueProvider is IInitializer initializer && TargetIsAssignableOrConvertibleToType(initializer, serviceDefinition))
                {
                    instance = initializer.InitTarget();
                    return instance != null;
                }
            }

            instance = null;
            return false;
        }

        private static bool TryGetService([NotNull] Object unityObject, [NotNull] ServiceAttribute.Info serviceDefinition, out object result)
        {
            if(unityObject is GameObject gameObject)
            {
                return TryGetService(gameObject, serviceDefinition, out result);
            }

            if(IsInstanceOf(serviceDefinition, unityObject))
            {
                result = unityObject;
                return true;
            }

            if(unityObject is IWrapper wrapper && wrapper.WrappedObject is object wrappedObject && IsInstanceOf(serviceDefinition, wrappedObject))
            {
                result = wrappedObject;
                return true;
            }

            if(unityObject is IInitializer initializer && TargetIsAssignableOrConvertibleToType(initializer, serviceDefinition))
            {
                result = initializer.InitTarget();
                return result != null;
            }

            result = null;
            return false;
        }

        private static bool IsInstanceOf(ServiceAttribute.Info serviceDefinition, object instance)
		{
            foreach(var definingType in serviceDefinition.definingTypes)
			{
                if(definingType.IsInstanceOfType(instance))
                {
                    return true;
                }
			}

            return false;
		}

        private static bool TryGetService(Type definingType, out object service, HashSet<Type> initialized)
        {
            if(services.TryGetValue(definingType, out service))
            {
                return true;
            }

            if(!unitializedServices.TryGetValue(definingType, out var definition)
                || !ServiceAttributeUtility.definingTypes.TryGetValue(definingType, out var serviceDefinition))
            {
                return false;
            }

            #if DEV_MODE && DEBUG_LAZY_INIT
            Debug.Log($"Initializing service {definingType.Name} with LazyInit=true because it is a dependency of another service.");
            #endif

            unitializedServices.Remove(serviceDefinition.concreteType);
			foreach(var singleDefiningType in serviceDefinition.definingTypes)
			{
                unitializedServices.Remove(singleDefiningType);
			}

            service = LoadInstance(serviceDefinition, initialized);
            if(service is null)
            {
                return false;
            }

            if(service is Task task)
			{
                services[serviceDefinition.concreteType] = task;
                foreach(var singleDefiningType in serviceDefinition.definingTypes)
				{
                    services[singleDefiningType] = task;
				}
                
                _ = SetInstanceAsync(serviceDefinition, task);
                return true;
			}

            SetInstanceSync(serviceDefinition, service);
            return service != null;
        }

        private static async Task SetInstanceAsync(ServiceAttribute.Info serviceDefinition, Task loadServiceTask)
		{
			var service = await loadServiceTask.GetResult();

			SetInstanceSync(serviceDefinition, service);

            SubscribeToUpdateEvents(service);
            SubscribeToAwake(service);
            SubscribeToOnEnable(service);
            SubscribeToStart(service);
		}

		private static void SetInstanceSync(ServiceAttribute.Info serviceDefinition, object service)
		{
			services[serviceDefinition.concreteType] = service;
            foreach(var definingType in serviceDefinition.definingTypes)
            {
                services[definingType] = service;
                ServiceUtility.SetInstance(definingType, service);
            }
		}

        #if UNITY_EDITOR
		/// <summary>
		/// Warnings about missing Services should be suppressed when entering Play Mode from a scene
		/// which is not the first enabled one in build settings.
		/// </summary>
		/// <returns></returns>
		private static bool IsFirstSceneInBuildSettingsLoaded()
        {
            string firstSceneInBuildsPath = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled)?.path ?? "";
            Scene firstSceneInBuilds = SceneManager.GetSceneByPath(firstSceneInBuildsPath);
            return firstSceneInBuilds.IsValid() && firstSceneInBuilds.isLoaded;
        }
        #endif

        private static void InjectCrossServiceDependencies(ServiceDefinitions serviceDefinitions, HashSet<Type> initialized)
        {
            foreach(var serviceDefinition in serviceDefinitions)
            {
                if(!unitializedServices.ContainsKey(serviceDefinition.concreteType)
                    && services.TryGetValue(serviceDefinition.concreteType, out var service))
                {
                    _ = InjectCrossServiceDependencies(service, initialized);
                }
            }
        }

        private static async Task InjectCrossServiceDependencies(object client, HashSet<Type> initialized)
        {
            if(client is Task clientTask)
			{
                client = await clientTask.GetResult();
			}

            var concreteType = client.GetType();

            if(!initialized.Add(concreteType))
            {
                return;
            }

            var interfaceTypes = concreteType.GetInterfaces();
            for(int i = interfaceTypes.Length - 1; i >= 0; i--)
            {
                var interfaceType = interfaceTypes[i];

                if(!interfaceType.IsGenericType)
                {
                    continue;
                }

                var typeDefinition = interfaceType.GetGenericTypeDefinition();
                if(typeDefinition == typeof(IInitializable<,,,,,>))
                {
                    var argumentTypes = interfaceType.GetGenericArguments();
                    var firstArgumentType = argumentTypes[0];
                    var secondArgumentType = argumentTypes[1];
                    var thirdArgumentType = argumentTypes[2];
                    var fourthArgumentType = argumentTypes[3];
                    var fifthArgumentType = argumentTypes[4];
                    var sixthArgumentType = argumentTypes[5];
                    if(TryGetService(firstArgumentType, out object firstArgument, initialized)
                    && TryGetService(secondArgumentType, out object secondArgument, initialized)
                    && TryGetService(thirdArgumentType, out object thirdArgument, initialized)
                    && TryGetService(fourthArgumentType, out object fourthArgument, initialized)
                    && TryGetService(fifthArgumentType, out object fifthArgument, initialized)
                    && TryGetService(sixthArgumentType, out object sixthArgument, initialized))
                    {
                        Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                        Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                        Task thirdArgumentTask = !thirdArgumentType.IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                        Task fourthArgumentTask = !fourthArgumentType.IsInstanceOfType(fourthArgument) ? fourthArgument as Task : null;
                        Task fifthArgumentTask = !fifthArgumentType.IsInstanceOfType(fifthArgument) ? fifthArgument as Task : null;
                        Task sixthArgumentTask = !sixthArgumentType.IsInstanceOfType(thirdArgument) ? sixthArgument as Task : null;

                        var loadArgumentTasks = Enumerable.Empty<Task>();

                        if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                        if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                        if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);
                        if(fourthArgumentTask != null) loadArgumentTasks.Append(fourthArgumentTask);
                        if(fifthArgumentTask != null) loadArgumentTasks.Append(fifthArgumentTask);
                        if(sixthArgumentTask != null) loadArgumentTasks.Append(sixthArgumentTask);

                        await Task.WhenAll(loadArgumentTasks);

                        if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                        if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                        if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();
                        if(fourthArgumentTask != null) fourthArgument = await fourthArgumentTask.GetResult();
                        if(fifthArgumentTask != null) fifthArgument  = await fifthArgumentTask.GetResult();
                        if(sixthArgumentTask != null) sixthArgument  = await sixthArgumentTask.GetResult();

                        await InjectCrossServiceDependencies(firstArgument, initialized);
                        await InjectCrossServiceDependencies(secondArgument, initialized);
                        await InjectCrossServiceDependencies(thirdArgument, initialized);
                        await InjectCrossServiceDependencies(fourthArgument, initialized);
                        await InjectCrossServiceDependencies(fifthArgument, initialized);
                        await InjectCrossServiceDependencies(sixthArgument, initialized);

                        interfaceType.GetMethod(nameof(IInitializable<object>.Init)).Invoke(client, new object[] { firstArgument, secondArgument, thirdArgument, fourthArgument, fifthArgument, sixthArgument });
                        return;
                    }
                    #if DEBUG_INIT_SERVICES
                    else { Debug.Log($"Service {concreteType.Name} requires 6 arguments but instances not found among {services.Count} services..."); }
                    #endif
                }

                if(typeDefinition == typeof(IInitializable<,,,,>))
                {
                    var argumentTypes = interfaceType.GetGenericArguments();
                    var firstArgumentType = argumentTypes[0];
                    var secondArgumentType = argumentTypes[1];
                    var thirdArgumentType = argumentTypes[2];
                    var fourthArgumentType = argumentTypes[3];
                    var fifthArgumentType = argumentTypes[4];
                    if(TryGetService(firstArgumentType, out object firstArgument, initialized)
                    && TryGetService(secondArgumentType, out object secondArgument, initialized)
                    && TryGetService(thirdArgumentType, out object thirdArgument, initialized)
                    && TryGetService(fourthArgumentType, out object fourthArgument, initialized)
                    && TryGetService(fifthArgumentType, out object fifthArgument, initialized))
                    {
                        Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                        Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                        Task thirdArgumentTask = !thirdArgumentType.IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                        Task fourthArgumentTask = !fourthArgumentType.IsInstanceOfType(fourthArgument) ? fourthArgument as Task : null;
                        Task fifthArgumentTask = !fifthArgumentType.IsInstanceOfType(fifthArgument) ? fifthArgument as Task : null;

                        var loadArgumentTasks = Enumerable.Empty<Task>();

                        if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                        if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                        if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);
                        if(fourthArgumentTask != null) loadArgumentTasks.Append(fourthArgumentTask);
                        if(fifthArgumentTask != null) loadArgumentTasks.Append(fifthArgumentTask);

                        await Task.WhenAll(loadArgumentTasks);

                        if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                        if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                        if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();
                        if(fourthArgumentTask != null) fourthArgument = await fourthArgumentTask.GetResult();
                        if(fifthArgumentTask != null) fifthArgument  = await fifthArgumentTask.GetResult();

                        await InjectCrossServiceDependencies(firstArgument, initialized);
                        await InjectCrossServiceDependencies(secondArgument, initialized);
                        await InjectCrossServiceDependencies(thirdArgument, initialized);
                        await InjectCrossServiceDependencies(fourthArgument, initialized);
                        await InjectCrossServiceDependencies(fifthArgument, initialized);

                        interfaceType.GetMethod(nameof(IInitializable<object>.Init)).Invoke(client, new object[] { firstArgument, secondArgument, thirdArgument, fourthArgument, fifthArgument });
                        return;
                    }
                    #if DEBUG_INIT_SERVICES
                    else { Debug.Log($"Service {concreteType.Name} requires 5 arguments but instances not found among {services.Count} services..."); }
                    #endif
                }

                if(typeDefinition == typeof(IInitializable<,,,>))
                {
                    var argumentTypes = interfaceType.GetGenericArguments();
                    var firstArgumentType = argumentTypes[0];
                    var secondArgumentType = argumentTypes[1];
                    var thirdArgumentType = argumentTypes[2];
                    var fourthArgumentType = argumentTypes[3];
                    if(TryGetService(firstArgumentType, out object firstArgument, initialized)
                    && TryGetService(secondArgumentType, out object secondArgument, initialized)
                    && TryGetService(thirdArgumentType, out object thirdArgument, initialized)
                    && TryGetService(fourthArgumentType, out object fourthArgument, initialized))
                    {
                        Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                        Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                        Task thirdArgumentTask = !thirdArgumentType.IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                        Task fourthArgumentTask = !fourthArgumentType.IsInstanceOfType(fourthArgument) ? fourthArgument as Task : null;

                        var loadArgumentTasks = Enumerable.Empty<Task>();

                        if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                        if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                        if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);
                        if(fourthArgumentTask != null) loadArgumentTasks.Append(fourthArgumentTask);

                        await Task.WhenAll(loadArgumentTasks);

                        if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                        if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                        if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();
                        if(fourthArgumentTask != null) fourthArgument = await fourthArgumentTask.GetResult();

                        await InjectCrossServiceDependencies(firstArgument, initialized);
                        await InjectCrossServiceDependencies(secondArgument, initialized);
                        await InjectCrossServiceDependencies(thirdArgument, initialized);
                        await InjectCrossServiceDependencies(fourthArgument, initialized);

                        interfaceType.GetMethod(nameof(IInitializable<object>.Init)).Invoke(client, new object[] { firstArgument, secondArgument, thirdArgument, fourthArgument });
                        return;
                    }
                    #if DEBUG_INIT_SERVICES
                    else { Debug.Log($"Service {concreteType.Name} requires 4 arguments but instances not found among {services.Count} services..."); }
                    #endif
                }

                if(typeDefinition == typeof(IInitializable<,,>))
                {
                    var argumentTypes = interfaceType.GetGenericArguments();
                    var firstArgumentType = argumentTypes[0];
                    var secondArgumentType = argumentTypes[1];
                    var thirdArgumentType = argumentTypes[2];
                    if(TryGetService(firstArgumentType, out object firstArgument, initialized)
                    && TryGetService(secondArgumentType, out object secondArgument, initialized)
                    && TryGetService(thirdArgumentType, out object thirdArgument, initialized))
                    {
                        Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                        Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                        Task thirdArgumentTask = !thirdArgumentType.IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;

                        var loadArgumentTasks = Enumerable.Empty<Task>();

                        if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                        if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                        if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);

                        await Task.WhenAll(loadArgumentTasks);

                        if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                        if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                        if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();

                        await InjectCrossServiceDependencies(firstArgument, initialized);
                        await InjectCrossServiceDependencies(secondArgument, initialized);
                        await InjectCrossServiceDependencies(thirdArgument, initialized);

                        interfaceType.GetMethod(nameof(IInitializable<object>.Init)).Invoke(client, new object[] { firstArgument, secondArgument, thirdArgument });
                        return;
                    }
                    #if DEBUG_INIT_SERVICES
                    else { Debug.Log($"Service {concreteType.Name} requires 3 arguments but instances not found among {services.Count} services..."); }
                    #endif
                }

                if(typeDefinition == typeof(IInitializable<,>))
                {
                    var argumentTypes = interfaceType.GetGenericArguments();
                    var firstArgumentType = argumentTypes[0];
                    var secondArgumentType = argumentTypes[1];
                    if(TryGetService(firstArgumentType, out object firstArgument, initialized)
                    && TryGetService(secondArgumentType, out object secondArgument, initialized))
                    {
                        Task firstArgumentTask = !firstArgumentType.IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                        Task secondArgumentTask = !secondArgumentType.IsInstanceOfType(secondArgument) ? secondArgument as Task : null;

                        var loadArgumentTasks = Enumerable.Empty<Task>();

                        if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                        if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);

                        await Task.WhenAll(loadArgumentTasks);

                        if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                        if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();

                        await InjectCrossServiceDependencies(firstArgument, initialized);
                        await InjectCrossServiceDependencies(secondArgument, initialized);

                        interfaceType.GetMethod(nameof(IInitializable<object>.Init)).Invoke(client, new object[] { firstArgument, secondArgument });
                        return;
                    }
                    #if DEBUG_INIT_SERVICES
                    else { Debug.Log($"Service {concreteType.Name} requires 2 arguments but instances not found among {services.Count} services..."); }
                    #endif
                }

                if(typeDefinition == typeof(IInitializable<>))
                {
                    var argumentType = interfaceType.GetGenericArguments()[0];
                    if(TryGetService(argumentType, out object argument, initialized))
                    {
                        await InjectCrossServiceDependencies(argument, initialized);

                        interfaceType.GetMethod(nameof(IInitializable<object>.Init)).Invoke(client, new object[] { argument });
                        return;
                    }
                    #if DEBUG_INIT_SERVICES
                    else { Debug.Log($"Service {concreteType.Name} requires argument {interfaceType.GetGenericArguments()[0].Name} but instance not found among {services.Count} services..."); }
                    #endif
                }
            }
        }

        private static void InjectServiceDependenciesForTypesThatRequireOnlyThem(out List<Task> injectAsyncServices
        #if UNITY_EDITOR
        , Dictionary<Type, List<ScriptableObject>> uninitializedScriptableObjects    
        #endif
        )
        {
            injectAsyncServices = new List<Task>(32);

            if(services is null)
            {
                return;
            }

            var setMethodsByArgumentCount = GetInitArgsSetMethods();

            var setMethodArgumentTypes = new Type[] { typeof(Type), null };
            var setMethodArguments = new object[2];
            foreach(var clientType in GetImplementingTypes<IOneArgument>())
            {
                if(!clientType.IsAbstract)
                {
                    var task = TrySetOneServiceDependency(clientType, clientType.GetInterfaces(), setMethodsByArgumentCount[1], setMethodArgumentTypes, setMethodArguments
                    #if UNITY_EDITOR
                    , uninitializedScriptableObjects
                    #endif
                    );
                        
                    if(!task.IsCompleted)
					{
                        injectAsyncServices.Add(task);
					}
                }
            }

            setMethodArgumentTypes = new Type[] { typeof(Type), null, null };
            setMethodArguments = new object[3];
            foreach(var clientType in GetImplementingTypes<ITwoArguments>())
            {
                if(!clientType.IsAbstract)
                {
                    var task = TrySetTwoServiceDependencies(clientType, clientType.GetInterfaces(), setMethodsByArgumentCount[2], setMethodArgumentTypes, setMethodArguments
                    #if UNITY_EDITOR
                    , uninitializedScriptableObjects
                    #endif
                    );

                    if(!task.IsCompleted)
					{
                        injectAsyncServices.Add(task);
					}
                }
            }

            setMethodArgumentTypes = new Type[] { typeof(Type), null, null, null };
            setMethodArguments = new object[4];
            foreach(var clientType in GetImplementingTypes<IThreeArguments>())
            {
                if(!clientType.IsAbstract)
                {
                    var task = TrySetThreeServiceDependencies(clientType, clientType.GetInterfaces(), setMethodsByArgumentCount[3], setMethodArgumentTypes, setMethodArguments
                    #if UNITY_EDITOR
                    , uninitializedScriptableObjects
                    #endif
                    );

                    if(!task.IsCompleted)
					{
                        injectAsyncServices.Add(task);
					}
                }
            }

            setMethodArgumentTypes = new Type[] { typeof(Type), null, null, null, null };
            setMethodArguments = new object[5];
            foreach(var clientType in GetImplementingTypes<IFourArguments>())
            {
                if(!clientType.IsAbstract)
                {
                    var task = TrySetFourServiceDependencies(clientType, clientType.GetInterfaces(), setMethodsByArgumentCount[4], setMethodArgumentTypes, setMethodArguments
                    #if UNITY_EDITOR
                    , uninitializedScriptableObjects
                    #endif
                    );

                    if(!task.IsCompleted)
					{
                        injectAsyncServices.Add(task);
					}
                }
            }

            setMethodArgumentTypes = new Type[] { typeof(Type), null, null, null, null, null };
            setMethodArguments = new object[6];
            foreach(var clientType in GetImplementingTypes<IFiveArguments>())
            {
                if(!clientType.IsAbstract)
                {
                    var task = TrySetFiveServiceDependencies(clientType, clientType.GetInterfaces(), setMethodsByArgumentCount[5], setMethodArgumentTypes, setMethodArguments
                    #if UNITY_EDITOR
                    , uninitializedScriptableObjects
                    #endif
                    );

                    if(!task.IsCompleted)
					{
                        injectAsyncServices.Add(task);
					}
                }
            }

            setMethodArgumentTypes = new Type[] { typeof(Type), null, null, null, null, null, null };
            setMethodArguments = new object[7];
            foreach(var clientType in GetImplementingTypes<ISixArguments>())
            {
                if(!clientType.IsAbstract)
                {
                    var task = TrySetSixServiceDependencies(clientType, clientType.GetInterfaces(), setMethodsByArgumentCount[6], setMethodArgumentTypes, setMethodArguments
                    #if UNITY_EDITOR
                    , uninitializedScriptableObjects
                    #endif
                    );

                    if(!task.IsCompleted)
					{
                        injectAsyncServices.Add(task);
					}
                }
            }
        }

        private static Dictionary<int, MethodInfo> GetInitArgsSetMethods()
        {
            Dictionary<int, MethodInfo> setMethodsByArgumentCount = new Dictionary<int, MethodInfo>(6);

            foreach(MethodInfo method in typeof(InitArgs).GetMember(nameof(InitArgs.Set), MemberTypes.Method, BindingFlags.Static | BindingFlags.NonPublic))
            {
                var genericArguments = method.GetGenericArguments();
                var parameters = method.GetParameters();
                int genericArgumentCount = genericArguments.Length;
                if(genericArgumentCount == parameters.Length - 1)
                {
                    setMethodsByArgumentCount.Add(genericArgumentCount, method);
                }
            }

            #if DEV_MODE
            Debug.Assert(setMethodsByArgumentCount.Count == 6);
            #endif

            return setMethodsByArgumentCount;
        }

        private static async Task TrySetOneServiceDependency(Type clientType, Type[] interfaceTypes, MethodInfo setMethod, Type[] setMethodArgumentTypes, object[] setMethodArguments
        #if UNITY_EDITOR
        , Dictionary<Type, List<ScriptableObject>> uninitializedScriptableObjects
        #endif
        )
        {
            for(int i = interfaceTypes.Length - 1; i >= 0; i--)
            {
                var interfaceType = interfaceTypes[i];

                if(interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IArgs<>))
                {
                    var argumentTypes = interfaceType.GetGenericArguments();
                    var argumentType = argumentTypes[0];
                    if(services.TryGetValue(argumentType, out object argument))
                    {
                        if(!argumentType.IsInstanceOfType(argument) && argument is Task task)
						{
                            #if DEBUG || INIT_ARGS_SAFE_MODE
                            try
                            {
                            #endif

                            argument = await task.GetResult();

                            #if DEBUG || INIT_ARGS_SAFE_MODE
                            }
                            catch(Exception e)
							{
                                Debug.LogWarning(e.ToString());
							}
                            #endif
						}

                        setMethodArgumentTypes[1] = argumentType;
                        setMethodArguments[0] = clientType;
                        setMethodArguments[1] = argument;

                        setMethod = setMethod.MakeGenericMethod(argumentTypes);

                        #if DEV_MODE && DEBUG_INIT_SERVICES
                        Debug.Log($"Providing 1 service for client {clientType.Name}: {argument.GetType().Name}.");
                        #endif

                        setMethod.Invoke(null, setMethodArguments);

                        #if UNITY_EDITOR
                        // In builds ScriptableObjects' Awake method gets called at runtime when a scene or prefab with a component referencing said ScriptableObject gets loaded.
                        // Because of this we can rely on ServiceInjector to execute via the RuntimeInitializeOnLoadMethod attribute before the Awake methods of ScriptableObjects are executed.
                        // In the editor however, ScriptableObject can already get loaded in edit mode, in which case Awake gets executed before service injection has taken place.
                        // For this reason we need to manually initialize these ScriptableObjects.
                        if(uninitializedScriptableObjects.TryGetValue(clientType, out var scriptableObjects))
						{
							for(int s = scriptableObjects.Count - 1; s >= 0; s--)
		                    {
								ScriptableObject scriptableObject = scriptableObjects[s];
								if(scriptableObject is IInitializableEditorOnly initializableEditorOnly
                                    && initializableEditorOnly.Initializer is IInitializer initializer)
								{
                                    try
                                    {
                                        initializer.InitTarget();
                                    }
                                    catch(Exception ex)
								    {
                                        Debug.LogError(ex);
								    }
                                    scriptableObjects.RemoveAt(s);
                                    continue;
								}

                                try
                                {
                                    var initMethod = typeof(IInitializable<>).MakeGenericType(argumentTypes).GetMethod(nameof(IInitializable<object>.Init));
                                    initMethod.Invoke(scriptableObject, new object[] { argument });
                                }
                                catch(Exception ex)
								{
                                    Debug.LogError(ex);
								}
		                    }
						}
                        #endif
                    }
                    return;
                }
            }
        }

        private static async Task TrySetTwoServiceDependencies(Type clientType, Type[] interfaceTypes, MethodInfo setMethod, Type[] setMethodArgumentTypes, object[] setMethodArguments
        #if UNITY_EDITOR
        , Dictionary<Type, List<ScriptableObject>> uninitializedScriptableObjects
        #endif
        )
        {
            for(int i = interfaceTypes.Length - 1; i >= 0; i--)
            {
                var interfaceType = interfaceTypes[i];
                if(interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IArgs<,>))
                {
                    var argumentTypes = interfaceType.GetGenericArguments();
                    if(services.TryGetValue(argumentTypes[0], out object firstArgument) && services.TryGetValue(argumentTypes[1], out object secondArgument))
                    {
                        Task firstArgumentTask = !argumentTypes[0].IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                        Task secondArgumentTask = !argumentTypes[1].IsInstanceOfType(secondArgument) ? secondArgument as Task : null;

                        if(firstArgumentTask != null || secondArgumentTask != null)
						{
                            var loadArgumentTasks = Enumerable.Empty<Task>();

                            if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                            if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);

                            #if DEBUG || INIT_ARGS_SAFE_MODE
                            try
                            {
                            #endif

                            await Task.WhenAll(loadArgumentTasks);

                            #if DEBUG || INIT_ARGS_SAFE_MODE
                            }
                            catch(Exception e)
							{
                                Debug.LogWarning(e.ToString());
							}
                            #endif

                            if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                            if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                        }

                        setMethodArgumentTypes[1] = argumentTypes[0];
                        setMethodArgumentTypes[2] = argumentTypes[1];
                        setMethodArguments[0] = clientType;
                        setMethodArguments[1] = firstArgument;
                        setMethodArguments[2] = secondArgument;

                        setMethod = setMethod.MakeGenericMethod(argumentTypes);

                        #if DEV_MODE && DEBUG_INIT_SERVICES
                        Debug.Log($"Providing 2 services for client {clientType.Name}: {firstArgument.GetType().Name}, {secondArgument.GetType().Name}.");
                        #endif

                        setMethod.Invoke(null, setMethodArguments);

                        #if UNITY_EDITOR
                        // In builds ScriptableObjects' Awake method gets called at runtime when a scene or prefab with a component referencing said ScriptableObject gets loaded.
                        // Because of this we can rely on ServiceInjector to execute via the RuntimeInitializeOnLoadMethod attribute before the Awake methods of ScriptableObjects are executed.
                        // In the editor however, ScriptableObject can already get loaded in edit mode, in which case Awake gets executed before service injection has taken place.
                        // For this reason we need to manually initialize these ScriptableObjects.
                        if(uninitializedScriptableObjects.TryGetValue(clientType, out var scriptableObjects))
						{
                            for(int s = scriptableObjects.Count - 1; s >= 0; s--)
		                    {
								ScriptableObject scriptableObject = scriptableObjects[s];
								if(scriptableObject is IInitializableEditorOnly initializableEditorOnly
                                    && initializableEditorOnly.Initializer is IInitializer initializer)
								{
                                    try
                                    {
                                        initializer.InitTarget();
                                    }
                                    catch(Exception ex)
								    {
                                        Debug.LogError(ex);
								    }
                                    scriptableObjects.RemoveAt(s);
                                    continue;
								}

                                try
                                {
                                    var initMethod = typeof(IInitializable<,>).MakeGenericType(argumentTypes).GetMethod(nameof(IInitializable<object>.Init));
                                    initMethod.Invoke(scriptableObject, new object[] { firstArgument, secondArgument });
                                }
                                catch(Exception ex)
								{
                                    Debug.LogError(ex);
								}
		                    }

                            uninitializedScriptableObjects.Remove(clientType);
						}
                        #endif
                    }
                    return;
                }
            }
        }

        private static async Task TrySetThreeServiceDependencies(Type clientType, Type[] interfaceTypes, MethodInfo setMethod, Type[] setMethodArgumentTypes, object[] setMethodArguments
        #if UNITY_EDITOR
        , Dictionary<Type, List<ScriptableObject>> uninitializedScriptableObjects
        #endif
        )
        {
            for(int i = interfaceTypes.Length - 1; i >= 0; i--)
            {
                var interfaceType = interfaceTypes[i];
                if(interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IArgs<,,>))
                {
                    var argumentTypes = interfaceType.GetGenericArguments();
                    if(services.TryGetValue(argumentTypes[0], out object firstArgument) && services.TryGetValue(argumentTypes[1], out object secondArgument)
                        && services.TryGetValue(argumentTypes[2], out object thirdArgument))
                    {
                        Task firstArgumentTask = !argumentTypes[0].IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                        Task secondArgumentTask = !argumentTypes[1].IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                        Task thirdArgumentTask = !argumentTypes[2].IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                        
                        var loadArgumentTasks = Enumerable.Empty<Task>();

                        if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                        if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                        if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);

                        await Task.WhenAll(loadArgumentTasks);

                        if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                        if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                        if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();

                        setMethodArgumentTypes[1] = argumentTypes[0];
                        setMethodArgumentTypes[2] = argumentTypes[1];
                        setMethodArgumentTypes[3] = argumentTypes[2];
                        setMethodArguments[0] = clientType;
                        setMethodArguments[1] = firstArgument;
                        setMethodArguments[2] = secondArgument;
                        setMethodArguments[3] = thirdArgument;

                        setMethod = setMethod.MakeGenericMethod(argumentTypes);

                        #if DEV_MODE && DEBUG_INIT_SERVICES
                        Debug.Log($"Providing 3 services for client {clientType.Name}: {firstArgument.GetType().Name}, {secondArgument.GetType().Name}, {thirdArgument.GetType().Name}.");
                        #endif

                        setMethod.Invoke(null, setMethodArguments);

                        #if UNITY_EDITOR
                        // In builds ScriptableObjects' Awake method gets called at runtime when a scene or prefab with a component referencing said ScriptableObject gets loaded.
                        // Because of this we can rely on ServiceInjector to execute via the RuntimeInitializeOnLoadMethod attribute before the Awake methods of ScriptableObjects are executed.
                        // In the editor however, ScriptableObject can already get loaded in edit mode, in which case Awake gets executed before service injection has taken place.
                        // For this reason we need to manually initialize these ScriptableObjects.
                        if(uninitializedScriptableObjects.TryGetValue(clientType, out var scriptableObjects))
						{
                            for(int s = scriptableObjects.Count - 1; s >= 0; s--)
		                    {
								ScriptableObject scriptableObject = scriptableObjects[s];
								if(scriptableObject is IInitializableEditorOnly initializableEditorOnly
                                    && initializableEditorOnly.Initializer is IInitializer initializer)
								{
                                    try
                                    {
                                        initializer.InitTarget();
                                    }
                                    catch(Exception ex)
								    {
                                        Debug.LogError(ex);
								    }
                                    scriptableObjects.RemoveAt(s);
                                    continue;
								}

                                try
                                {
                                    var initMethod = typeof(IInitializable<,,>).MakeGenericType(argumentTypes).GetMethod(nameof(IInitializable<object>.Init));
                                    initMethod.Invoke(scriptableObject, new object[] { firstArgument, secondArgument, thirdArgument });
                                }
                                catch(Exception ex)
								{
                                    Debug.LogError(ex);
								}
		                    }

                            uninitializedScriptableObjects.Remove(clientType);
						}
                        #endif
                    }
                    return;
                }
            }
        }

        private static async Task TrySetFourServiceDependencies(Type clientType, Type[] interfaceTypes, MethodInfo setMethod, Type[] setMethodArgumentTypes, object[] setMethodArguments
        #if UNITY_EDITOR
        , Dictionary<Type, List<ScriptableObject>> uninitializedScriptableObjects
        #endif
        )
        {
            for(int i = interfaceTypes.Length - 1; i >= 0; i--)
            {
                var interfaceType = interfaceTypes[i];
                if(interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IArgs<,,,>))
                {
                    var argumentTypes = interfaceType.GetGenericArguments();
                    if(services.TryGetValue(argumentTypes[0], out object firstArgument) && services.TryGetValue(argumentTypes[1], out object secondArgument)
                        && services.TryGetValue(argumentTypes[2], out object thirdArgument) && services.TryGetValue(argumentTypes[3], out object fourthArgument))
                    {
                        Task firstArgumentTask = !argumentTypes[0].IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                        Task secondArgumentTask = !argumentTypes[1].IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                        Task thirdArgumentTask = !argumentTypes[2].IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                        Task fourthArgumentTask = !argumentTypes[3].IsInstanceOfType(fourthArgument) ? fourthArgument as Task : null;

                        var loadArgumentTasks = Enumerable.Empty<Task>();

                        if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                        if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                        if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);
                        if(fourthArgumentTask != null) loadArgumentTasks.Append(fourthArgumentTask);

                        await Task.WhenAll(loadArgumentTasks);

                        if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                        if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                        if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();
                        if(fourthArgumentTask != null) fourthArgument = await fourthArgumentTask.GetResult();

                        setMethodArgumentTypes[1] = argumentTypes[0];
                        setMethodArgumentTypes[2] = argumentTypes[1];
                        setMethodArgumentTypes[3] = argumentTypes[2];
                        setMethodArgumentTypes[4] = argumentTypes[3];
                        setMethodArguments[0] = clientType;
                        setMethodArguments[1] = firstArgument;
                        setMethodArguments[2] = secondArgument;
                        setMethodArguments[3] = thirdArgument;
                        setMethodArguments[4] = fourthArgument;

                        setMethod = setMethod.MakeGenericMethod(argumentTypes);

                        #if DEV_MODE && DEBUG_INIT_SERVICES
                        Debug.Log($"Providing 4 services for client {clientType.Name}: {firstArgument.GetType().Name}, {secondArgument.GetType().Name}, {thirdArgument.GetType().Name}, {fourthArgument.GetType().Name}.");
                        #endif

                        setMethod.Invoke(null, setMethodArguments);

                        #if UNITY_EDITOR
                        // In builds ScriptableObjects' Awake method gets called at runtime when a scene or prefab with a component referencing said ScriptableObject gets loaded.
                        // Because of this we can rely on ServiceInjector to execute via the RuntimeInitializeOnLoadMethod attribute before the Awake methods of ScriptableObjects are executed.
                        // In the editor however, ScriptableObject can already get loaded in edit mode, in which case Awake gets executed before service injection has taken place.
                        // For this reason we need to manually initialize these ScriptableObjects.
                        if(uninitializedScriptableObjects.TryGetValue(clientType, out var scriptableObjects))
						{
                            for(int s = scriptableObjects.Count - 1; s >= 0; s--)
		                    {
								ScriptableObject scriptableObject = scriptableObjects[s];
								if(scriptableObject is IInitializableEditorOnly initializableEditorOnly
                                    && initializableEditorOnly.Initializer is IInitializer initializer)
								{
                                    try
                                    {
                                        initializer.InitTarget();
                                    }
                                    catch(Exception ex)
								    {
                                        Debug.LogError(ex);
								    }
                                    scriptableObjects.RemoveAt(s);
                                    continue;
								}

                                try
                                {
                                    var initMethod = typeof(IInitializable<,,,>).MakeGenericType(argumentTypes).GetMethod(nameof(IInitializable<object>.Init));
                                    initMethod.Invoke(scriptableObject, new object[] { firstArgument, secondArgument, thirdArgument, fourthArgument });
                                }
                                catch(Exception ex)
								{
                                    Debug.LogError(ex);
								}
		                    }

                            uninitializedScriptableObjects.Remove(clientType);
						}
                        #endif
                    }
                    return;
                }
            }
        }

        private static async Task TrySetFiveServiceDependencies(Type clientType, Type[] interfaceTypes, MethodInfo setMethod, Type[] setMethodArgumentTypes, object[] setMethodArguments
        #if UNITY_EDITOR
        , Dictionary<Type, List<ScriptableObject>> uninitializedScriptableObjects
        #endif
        )
        {
            for(int i = interfaceTypes.Length - 1; i >= 0; i--)
            {
                var interfaceType = interfaceTypes[i];
                if(interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IArgs<,,,,>))
                {
                    var argumentTypes = interfaceType.GetGenericArguments();
                    if(services.TryGetValue(argumentTypes[0], out object firstArgument) && services.TryGetValue(argumentTypes[1], out object secondArgument)
                        && services.TryGetValue(argumentTypes[2], out object thirdArgument) && services.TryGetValue(argumentTypes[3], out object fourthArgument)
                         && services.TryGetValue(argumentTypes[4], out object fifthArgument))
                    {
                        Task firstArgumentTask = !argumentTypes[0].IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                        Task secondArgumentTask = !argumentTypes[1].IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                        Task thirdArgumentTask = !argumentTypes[2].IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                        Task fourthArgumentTask = !argumentTypes[3].IsInstanceOfType(fourthArgument) ? fourthArgument as Task : null;
                        Task fifthArgumentTask = !argumentTypes[4].IsInstanceOfType(fifthArgument) ? fifthArgument as Task : null;

                        var loadArgumentTasks = Enumerable.Empty<Task>();

                        if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                        if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                        if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);
                        if(fourthArgumentTask != null) loadArgumentTasks.Append(fourthArgumentTask);
                        if(fifthArgumentTask != null) loadArgumentTasks.Append(fifthArgumentTask);

                        await Task.WhenAll(loadArgumentTasks);

                        if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                        if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                        if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();
                        if(fourthArgumentTask != null) fourthArgument = await fourthArgumentTask.GetResult();
                        if(fifthArgumentTask != null) fifthArgument  = await fifthArgumentTask.GetResult();

                        setMethodArgumentTypes[1] = argumentTypes[0];
                        setMethodArgumentTypes[2] = argumentTypes[1];
                        setMethodArgumentTypes[3] = argumentTypes[2];
                        setMethodArgumentTypes[4] = argumentTypes[3];
                        setMethodArgumentTypes[5] = argumentTypes[4];
                        setMethodArguments[0] = clientType;
                        setMethodArguments[1] = firstArgument;
                        setMethodArguments[2] = secondArgument;
                        setMethodArguments[3] = thirdArgument;
                        setMethodArguments[4] = fourthArgument;
                        setMethodArguments[5] = fifthArgument;

                        setMethod = setMethod.MakeGenericMethod(argumentTypes);

                        #if DEV_MODE && DEBUG_INIT_SERVICES
                        Debug.Log($"Providing 5 services for client {clientType.Name}: {firstArgument.GetType().Name}, {secondArgument.GetType().Name}, {thirdArgument.GetType().Name}, {fourthArgument.GetType().Name}, {fifthArgument.GetType().Name}.");
                        #endif

                        setMethod.Invoke(null, setMethodArguments);

                        #if UNITY_EDITOR
                        // In builds ScriptableObjects' Awake method gets called at runtime when a scene or prefab with a component referencing said ScriptableObject gets loaded.
                        // Because of this we can rely on ServiceInjector to execute via the RuntimeInitializeOnLoadMethod attribute before the Awake methods of ScriptableObjects are executed.
                        // In the editor however, ScriptableObject can already get loaded in edit mode, in which case Awake gets executed before service injection has taken place.
                        // For this reason we need to manually initialize these ScriptableObjects.
                        if(uninitializedScriptableObjects.TryGetValue(clientType, out var scriptableObjects))
						{
                            for(int s = scriptableObjects.Count - 1; s >= 0; s--)
		                    {
								ScriptableObject scriptableObject = scriptableObjects[s];
								if(scriptableObject is IInitializableEditorOnly initializableEditorOnly
                                    && initializableEditorOnly.Initializer is IInitializer initializer)
								{
                                    try
                                    {
                                        initializer.InitTarget();
                                    }
                                    catch(Exception ex)
								    {
                                        Debug.LogError(ex);
								    }
                                    scriptableObjects.RemoveAt(s);
                                    continue;
								}

                                try
                                {
                                    var initMethod = typeof(IInitializable<,,,,>).MakeGenericType(argumentTypes).GetMethod(nameof(IInitializable<object>.Init));
                                    initMethod.Invoke(scriptableObject, new object[] { firstArgument, secondArgument, thirdArgument, fourthArgument, fifthArgument });
                                }
                                catch(Exception ex)
								{
                                    Debug.LogError(ex);
								}
		                    }

                            uninitializedScriptableObjects.Remove(clientType);
						}
                        #endif
                    }
                }
            }
        }

        private static async Task TrySetSixServiceDependencies(Type clientType, Type[] interfaceTypes, MethodInfo setMethod, Type[] setMethodArgumentTypes, object[] setMethodArguments
        #if UNITY_EDITOR
        , Dictionary<Type, List<ScriptableObject>> uninitializedScriptableObjects
        #endif
        )
        {
            for(int i = interfaceTypes.Length - 1; i >= 0; i--)
            {
                var interfaceType = interfaceTypes[i];
                if(interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IArgs<,,,,,>))
                {
                    var argumentTypes = interfaceType.GetGenericArguments();
                    if(services.TryGetValue(argumentTypes[0], out object firstArgument) && services.TryGetValue(argumentTypes[1], out object secondArgument)
                        && services.TryGetValue(argumentTypes[2], out object thirdArgument) && services.TryGetValue(argumentTypes[3], out object fourthArgument)
                         && services.TryGetValue(argumentTypes[4], out object fifthArgument)&& services.TryGetValue(argumentTypes[4], out object sixthArgument))
                    {
                        Task firstArgumentTask = !argumentTypes[0].IsInstanceOfType(firstArgument) ? firstArgument as Task : null;
                        Task secondArgumentTask = !argumentTypes[1].IsInstanceOfType(secondArgument) ? secondArgument as Task : null;
                        Task thirdArgumentTask = !argumentTypes[2].IsInstanceOfType(thirdArgument) ? thirdArgument as Task : null;
                        Task fourthArgumentTask = !argumentTypes[3].IsInstanceOfType(fourthArgument) ? fourthArgument as Task : null;
                        Task fifthArgumentTask = !argumentTypes[4].IsInstanceOfType(fifthArgument) ? fifthArgument as Task : null;
                        Task sixthArgumentTask = !argumentTypes[5].IsInstanceOfType(sixthArgument) ? sixthArgument as Task : null;

                        var loadArgumentTasks = Enumerable.Empty<Task>();

                        if(firstArgumentTask != null) loadArgumentTasks.Append(firstArgumentTask);
                        if(secondArgumentTask != null) loadArgumentTasks.Append(secondArgumentTask);
                        if(thirdArgumentTask != null) loadArgumentTasks.Append(thirdArgumentTask);
                        if(fourthArgumentTask != null) loadArgumentTasks.Append(fourthArgumentTask);
                        if(fifthArgumentTask != null) loadArgumentTasks.Append(fifthArgumentTask);
                        if(sixthArgumentTask != null) loadArgumentTasks.Append(sixthArgumentTask);

                        await Task.WhenAll(loadArgumentTasks);

                        if(firstArgumentTask != null) firstArgument = await firstArgumentTask.GetResult();
                        if(secondArgumentTask != null) secondArgument = await secondArgumentTask.GetResult();
                        if(thirdArgumentTask != null) thirdArgument = await thirdArgumentTask.GetResult();
                        if(fourthArgumentTask != null) fourthArgument = await fourthArgumentTask.GetResult();
                        if(fifthArgumentTask != null) fifthArgument  = await fifthArgumentTask.GetResult();
                        if(sixthArgumentTask != null) sixthArgument  = await sixthArgumentTask.GetResult();

                        setMethodArgumentTypes[1] = argumentTypes[0];
                        setMethodArgumentTypes[2] = argumentTypes[1];
                        setMethodArgumentTypes[3] = argumentTypes[2];
                        setMethodArgumentTypes[4] = argumentTypes[3];
                        setMethodArgumentTypes[5] = argumentTypes[4];
                        setMethodArgumentTypes[6] = argumentTypes[5];
                        setMethodArguments[0] = clientType;
                        setMethodArguments[1] = firstArgument;
                        setMethodArguments[2] = secondArgument;
                        setMethodArguments[3] = thirdArgument;
                        setMethodArguments[4] = fourthArgument;
                        setMethodArguments[5] = fifthArgument;
                        setMethodArguments[6] = sixthArgument;

                        setMethod = setMethod.MakeGenericMethod(argumentTypes);

                        #if DEV_MODE && DEBUG_INIT_SERVICES
                        Debug.Log($"Providing 6 services for client {clientType.Name}: {firstArgument.GetType().Name}, {secondArgument.GetType().Name}, {thirdArgument.GetType().Name}, {fourthArgument.GetType().Name}, {fifthArgument.GetType().Name}, {sixthArgument.GetType().Name}.");
                        #endif

                        setMethod.Invoke(null, setMethodArguments);

                        #if UNITY_EDITOR
                        // In builds ScriptableObjects' Awake method gets called at runtime when a scene or prefab with a component referencing said ScriptableObject gets loaded.
                        // Because of this we can rely on ServiceInjector to execute via the RuntimeInitializeOnLoadMethod attribute before the Awake methods of ScriptableObjects are executed.
                        // In the editor however, ScriptableObject can already get loaded in edit mode, in which case Awake gets executed before service injection has taken place.
                        // For this reason we need to manually initialize these ScriptableObjects.
                        if(uninitializedScriptableObjects.TryGetValue(clientType, out var scriptableObjects))
						{
                            for(int s = scriptableObjects.Count - 1; s >= 0; s--)
		                    {
								ScriptableObject scriptableObject = scriptableObjects[s];
								if(scriptableObject is IInitializableEditorOnly initializableEditorOnly
                                    && initializableEditorOnly.Initializer is IInitializer initializer)
								{
                                    try
                                    {
                                        initializer.InitTarget();
                                    }
                                    catch(Exception ex)
								    {
                                        Debug.LogError(ex);
								    }
                                    scriptableObjects.RemoveAt(s);
                                    continue;
								}

                                try
                                {
                                    var initMethod = typeof(IInitializable<,,,,,>).MakeGenericType(argumentTypes).GetMethod(nameof(IInitializable<object>.Init));
                                    initMethod.Invoke(scriptableObject, new object[] { firstArgument, secondArgument, thirdArgument, fourthArgument, fifthArgument, sixthArgument });
                                }
                                catch(Exception ex)
								{
                                    Debug.LogError(ex);
								}
		                    }

                            uninitializedScriptableObjects.Remove(clientType);
						}
                        #endif
                    }
                }
            }
        }

        #if UNITY_EDITOR
        // In builds ScriptableObjects' Awake method gets called at runtime when a scene or prefab with a component
        // referencing said ScriptableObject gets loaded.
        // Because of this we can rely on ServiceInjector to execute via the RuntimeInitializeOnLoadMethod attribute
        // before the Awake methods of ScriptableObjects are executed.
        // In the editor however, ScriptableObject can already get loaded in edit mode, in which case Awake gets
        // executed before service injection has taken place.
        // For this reason we need to manually initialize these ScriptableObjects.
		private static void InitializeAlreadyLoadedScriptableObjectsInTheEditor(Dictionary<Type, List<ScriptableObject>> uninitializedScriptableObjects)
		{
            foreach(var scriptableObjects in uninitializedScriptableObjects.Values)
            {
                foreach(var scriptableObject in scriptableObjects)
                {
                    if(scriptableObject is IInitializableEditorOnly initializableEditorOnly
                    && initializableEditorOnly.Initializer is IInitializer initializer)
	                {
                        initializer.InitTarget();
	                }
                }
            }
		}
        #endif

        private static void HandleBroadcastingUnityEvents()
        {
            ForEachService(SubscribeToUpdateEvents);
            ForEachService(SubscribeToAwake);
            ForEachService(SubscribeToOnEnable);
            ForEachService(SubscribeToStart);
		}

        private static void SubscribeToUpdateEvents(object service)
		{
			if(service is IUpdate update)
			{
				Updater.Subscribe(update);
			}

			if(service is ILateUpdate lateUpdate)
			{
				Updater.Subscribe(lateUpdate);
			}

			if(service is IFixedUpdate fixedUpdate)
			{
				Updater.Subscribe(fixedUpdate);
			}
		}

		private static void SubscribeToAwake(object service)
		{
			if(service is IAwake onEnable)
			{
				onEnable.Awake();
			}
		}

		private static void SubscribeToOnEnable(object service)
		{
			if(service is IOnEnable onEnable)
			{
				onEnable.OnEnable();
			}
		}

		private static void SubscribeToStart(object service)
		{
			if(service is IStart start)
			{
                Updater.InvokeAtEndOfFrame(start.Start);
			}
		}

		[CanBeNull]
		internal static Type GetClassWithServiceAttribute(Type definingType)
            => ServiceAttributeUtility.definingTypes.TryGetValue(definingType, out var serviceDefinition)
				? serviceDefinition.classWithAttribute
				: null;

		[CanBeNull]
        internal static ServiceAttribute.Info GetServiceDefinition(Type definingType) => ServiceAttributeUtility.definingTypes.TryGetValue(definingType, out var result) ? result : null;

        private static ServiceDefinitions GetServiceDefinitions() => ServiceAttributeUtility.concreteTypes.Values;

        private static TypeCollection GetImplementingTypes<TInterface>() where TInterface : class
        {
            #if UNITY_EDITOR
			return TypeCache.GetTypesDerivedFrom<TInterface>();
            #else
            return TypeUtility.GetImplementingTypes<TInterface>(typeof(object).Assembly, typeof(InitArgs).Assembly);
            #endif
        }
    }
}
#endif