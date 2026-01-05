using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;

namespace Sisus.Init
{
    [Preserve]
	internal sealed class ScopedServiceRemover
	{
        public delegate void RemoveFromHandler<TService>(Clients clients, TService service, Component container);

        [Preserve] private static readonly MethodInfo methodDefinition;
        [Preserve] private static readonly object[] arguments = new object[3];
        [Preserve] private readonly Delegate @delegate;

        static ScopedServiceRemover()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
            methodDefinition = typeof(Service).GetMethod(nameof(Service.RemoveFrom), flags);
            
            if(methodDefinition is null)
			{
                Debug.LogWarning("MethodInfo Service.RemoveFromClients<>() not found.");
                methodDefinition = typeof(ScopedServiceRemover).GetMethod(nameof(DoNothing), BindingFlags.NonPublic | BindingFlags.Instance);
			}
        }

        public ScopedServiceRemover([NotNull] Type definingType)
		{
            var method = methodDefinition.MakeGenericMethod(definingType);
            var delegateType = typeof(RemoveFromHandler<>).MakeGenericType(definingType);
            @delegate = Delegate.CreateDelegate(delegateType, method);
		}

        [Preserve]
        public void RemoveFrom([NotNull] object service, Clients clients, [NotNull] Component container)
		{
            arguments[0] = clients;
            arguments[1] = service;
            arguments[2] = container;
            @delegate.DynamicInvoke(arguments);
		}

        [Preserve] private void DoNothing<TService>(Clients clients, TService service, Component container) { }
	}
}