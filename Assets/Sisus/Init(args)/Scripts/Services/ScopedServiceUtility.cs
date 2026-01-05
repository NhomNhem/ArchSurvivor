/*
#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sisus.Init.Internal;
using UnityEngine;

namespace Sisus.Init.EditorOnly.Internal
{
	using static ConversionExtensions;
	using static NullExtensions;
	using Object = UnityEngine.Object;

	internal static class ScopedServiceUtility
	{
		private static readonly ConcurrentDictionary<Type, ConcurrentBag<Instance>> instancesByDefiningTypeInPlayMode = new ConcurrentDictionary<Type, ConcurrentBag<Instance>>();
		
		// Separate caches for edit and play modes to avoid issues with Register / Deregister not being called in pairs
		// when entering / exiting play mode etc.
		private static readonly ConcurrentDictionary<Type, ConcurrentBag<Instance>> instancesByDefiningTypeInEditMode = new ConcurrentDictionary<Type, ConcurrentBag<Instance>>();

		internal static ConcurrentDictionary<Type, ConcurrentBag<Instance>> InfosByDefiningType => ThreadSafe.Application.IsPlaying ? instancesByDefiningTypeInPlayMode : instancesByDefiningTypeInEditMode;

		/// <summary>
		/// Reset state when entering play mode in the editor to support Enter Play Mode Settings.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void OnEnterPlayMode()
		{
			instancesByDefiningTypeInPlayMode.Clear();
			instancesByDefiningTypeInEditMode.Clear();
		}

		internal static bool TryGetServiceToClientVisibility(Component service, Type definingType, out Clients toClients)
		{
			if(!InfosByDefiningType.TryGetValue(definingType, out var infos))
			{
				toClients = default;
				return false;
			}
	
			foreach(var info in infos)
			{
				if(info.service as Object == service && info.registerer != null)
				{
					toClients = info.clients;
					return true;
				}
			}

			toClients = default;
			return false;
		}

		internal static bool TryGetForAnyClient<TService>(out TService service)
		{
			if(!InfosByDefiningType.TryGetValue(typeof(TService), out var infos))
			{
				service = default;
				return false;
			}

			Instance nearest = default;

			foreach(var info in infos)
			{
				if(info.registerer == null)
				{
					continue;
				}

				if(info.clients != Clients.Everywhere)
				{
					continue;
				}

				nearest = info;
			}

			if(nearest.service != Null && TryConvert<TService>(nearest.service, out var asService))
			{
				service = asService;
				return true;
			}

			service = default;
			return false;
		}

		internal static bool TryGetFor<TService>([NotNull] GameObject gameObject, out TService service)
		{
			Debug.Assert(gameObject != null);

			if(InfosByDefiningType.TryGetValue(typeof(TService), out var infos))
			{
				Instance? nearest = default;

				foreach(var info in infos)
				{
					if(info.registerer == null)
					{
						continue;
					}

					if(!IsAvailableToClient(info, gameObject.transform))
					{
						continue;
					}

					nearest = GetNearest(gameObject, info, nearest);
				}

				if(nearest.HasValue && nearest.Value.service is object obj && TryConvert<TService>(obj, out var asService))
				{
					service = asService;
					return true;
				}
			}

			service = default;
			return false;
		}

		private static bool IsAvailableToClient(Instance service, [NotNull] Transform client)
		{
			Debug.Assert(client != null);
			Debug.Assert(service.registerer != null);

			switch(service.clients)
			{
				case Clients.InGameObject:
					return service.transform == client;
				case Clients.InChildren:
					for(var parent = client.transform; parent != null; parent = parent.parent)
					{
						if(parent == service.transform)
						{
							return true;
						}
					}
					return false;
				case Clients.InParents:
					for(var parent = service.transform; parent != null; parent = parent.parent)
					{
						if(parent == client)
						{
							return true;
						}
					}
					return false;
				case Clients.InHierarchyRootChildren:
					return service.transform.root == client.root;
				case Clients.InScene:
					return client.gameObject.scene == service.scene;
				case Clients.InAllScenes:
				case Clients.Everywhere:
					return true;
				default:
					Debug.LogError($"Unrecognized {nameof(Clients)} value: {service.clients}.", service.transform);
					return false;
			}
		}

		private static Instance GetNearest([NotNull] GameObject client, Instance firstOption, Instance? secondOptionOrNull)
		{
			if(!secondOptionOrNull.HasValue)
			{
				return firstOption;
			}

			var firstService = firstOption.service;
			var secondOption = secondOptionOrNull.Value;
			var secondService = secondOption.service;

			if(firstService is null)
			{
				return secondOption;
			}

			if(secondService is null)
			{
				return firstOption;
			}

			if(ReferenceEquals(firstService, secondService))
			{
				return firstOption;
			}

			var scene = client.scene;
			if(firstOption.scene != scene)
			{
				return secondOption;
			}

			if(secondOption.scene != scene)
			{
				return firstOption;
			}

			var firstTransform = firstOption.transform;
			var secondTransform = secondOption.transform;

			for(var parent = client.transform; parent != null; parent = parent.parent)
			{
				if(parent == firstTransform)
				{
					if(parent == secondTransform)
					{
						break;
					}

					return firstOption;
				}

				if(parent == secondTransform)
				{
					return secondOption;
				}
			}

			return firstOption;
		}

		internal readonly struct Instance
		{
			public readonly object service;
            public readonly Clients clients;
			public readonly Transform transform;
			public readonly UnityEngine.SceneManagement.Scene scene;
            public readonly object registerer;

			public Instance([NotNull] object service, Clients clients, [NotNull] Component container)
			{
				this.service = service;
                this.clients = clients;
				transform = container.transform;
				scene = container.gameObject.scene;
                registerer = container;
			}
		}
	}
}
#endif
*/