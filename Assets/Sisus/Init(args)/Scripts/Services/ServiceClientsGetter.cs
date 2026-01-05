using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace Sisus.Init
{
	[Preserve]
	internal sealed class ServiceClientsGetter
	{
        public delegate bool Delegate<TService>(TService service, out Clients clients);

        [Preserve] private static readonly MethodInfo methodDefinition;
        [Preserve] private static readonly object[] arguments = new object[2];
        [Preserve] private readonly Delegate @delegate;

        static ServiceClientsGetter()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            methodDefinition = typeof(Service).GetMethod(nameof(Service.TryGetClients), flags);

            if(methodDefinition is null)
			{
                Debug.LogWarning("MethodInfo Service.TryGetClients<TService>(TService service, Clients forClients) not found.");
                methodDefinition = typeof(ScopedServiceGetter).GetMethod(nameof(AlwaysFalse), BindingFlags.NonPublic | BindingFlags.Static);
			}
        }

        public ServiceClientsGetter([NotNull] Type definingType)
		{
            var method = methodDefinition.MakeGenericMethod(definingType);
            var delegateType = typeof(Delegate<>).MakeGenericType(definingType);
            @delegate = Delegate.CreateDelegate(delegateType, method);
		}

        [Preserve]
        public bool TryGetFor(object service, out Clients clients)
		{
            arguments[0] = service;
            arguments[1] = Clients.Everywhere;

            try
            {
                bool result = (bool)@delegate.DynamicInvoke(arguments);
                clients = (Clients)arguments[1];
                return result;
            }
            catch(TargetInvocationException ex)
			{
                Debug.LogWarning(ex);
                clients = (Clients)(-1);
                return false;
			}
		}

        [Preserve]
        private static bool AlwaysFalse<TService>(TService service, out Clients clients)
        {
            if(service is Object unityObject)
			{
                Debug.LogWarning($"Unable to get clients for service {typeof(TService).Name} on GameObject \"{unityObject.name}\".", unityObject);
			}
            else
            {
                Debug.LogWarning($"Unable to get clients for service {typeof(TService).Name}.");
            }

            clients = default;
            return false;
        }
	}
}