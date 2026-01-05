using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;

namespace Sisus.Init
{
	internal sealed class GlobalServiceGetter
	{
        public delegate bool TryGetHandler<TService>(out TService service);

        [Preserve] private static readonly MethodInfo methodDefinition;
        [Preserve] private static readonly object[] arguments = new object[1];
        [Preserve] private readonly Delegate @delegate;

        static GlobalServiceGetter()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
            methodDefinition = typeof(Service).GetMethod(nameof(Service.TryGet), flags);
            
            if(methodDefinition is null)
			{
                Debug.LogWarning("MethodInfo Service.TryGet<TService>(out TService service) not found.");
                methodDefinition = typeof(GlobalServiceGetter).GetMethod(nameof(AlwaysFalse), BindingFlags.NonPublic | BindingFlags.Static);
			}
        }

        public GlobalServiceGetter([NotNull] Type definingType)
		{
            var method = methodDefinition.MakeGenericMethod(definingType);
            var delegateType = typeof(TryGetHandler<>).MakeGenericType(definingType);
            @delegate = Delegate.CreateDelegate(delegateType, method);
		}

        public bool TryGet(out object service)
		{
            arguments[0] = null;

            try
            {
    		    bool result = (bool)@delegate.DynamicInvoke(arguments);
                service = arguments[0];
                return result;
            }
            catch(TargetInvocationException ex)
			{
                Debug.LogWarning(ex);
                service = null;
                return false;
			}
        }

        [Preserve]
        private static bool AlwaysFalse<TService>(out TService service)
		{
            Debug.LogWarning($"Unable to get service {typeof(TService).Name}.");
            service = default;
            return false;
		}
	}
}