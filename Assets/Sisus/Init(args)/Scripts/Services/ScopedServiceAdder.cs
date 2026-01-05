using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;

namespace Sisus.Init
{
	[Preserve]
	internal sealed class ScopedServiceAdder
	{
        public delegate void AddForHandler<TService>(Clients clients, TService service, Component container);

        [Preserve] private static readonly MethodInfo methodDefinition;
        [Preserve] private static readonly object[] arguments = new object[3];
        [Preserve] private readonly Delegate @delegate;

        static ScopedServiceAdder()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
            methodDefinition = typeof(Service).GetMethod(nameof(Service.AddFor), flags);

            if(methodDefinition is null)
			{
                Debug.LogWarning("MethodInfo Service.AddInstance<>(object instance, Clients forClients) not found.");
                methodDefinition = typeof(ScopedServiceAdder).GetMethod(nameof(DoNothing), BindingFlags.NonPublic | BindingFlags.Static);
			}
        }

        public ScopedServiceAdder([NotNull] Type definingType)
		{
            var method = methodDefinition.MakeGenericMethod(definingType);
            var delegateType = typeof(AddForHandler<>).MakeGenericType(definingType);
            @delegate = Delegate.CreateDelegate(delegateType, method);
		}

        [Preserve]
        public void AddFor(object instance, Clients forClients, Component container)
		{
            arguments[0] = forClients;
            arguments[1] = instance;
            arguments[2] = container;
            @delegate.DynamicInvoke(arguments);
		}

        [Preserve] private static void DoNothing<TService>(Clients clients, TService service, Component container)
            => Debug.LogWarning($"Unable to register service {service.GetType().Name}.", container);
	}
}