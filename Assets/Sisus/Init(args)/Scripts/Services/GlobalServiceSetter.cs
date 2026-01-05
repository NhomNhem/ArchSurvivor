using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;

namespace Sisus.Init
{

	[Preserve]
	internal sealed class GlobalServiceSetter
	{
        public delegate void SetInstanceHandler<TService>(TService instance);

        [Preserve] private static readonly MethodInfo methodDefinition;
        [Preserve] private static readonly object[] arguments = new object[1];

        private readonly Delegate @delegate;

        static GlobalServiceSetter()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
            methodDefinition = typeof(Service).GetMethod(nameof(Service.SetInstance), flags);

            if(methodDefinition is null)
			{
                Debug.LogWarning("MethodInfo Service.SetInstance<> not found.");
                methodDefinition = typeof(GlobalServiceSetter).GetMethod(nameof(DoNothing), BindingFlags.NonPublic | BindingFlags.Instance);
			}
        }

        public GlobalServiceSetter([NotNull] Type definingType)
		{
            var method = methodDefinition.MakeGenericMethod(definingType);
            var delegateType = typeof(SetInstanceHandler<>).MakeGenericType(definingType);
            @delegate = Delegate.CreateDelegate(delegateType, method);
		}

        public void SetInstance(object instance)
		{
            try
            {
                arguments[0] = instance;
                @delegate.DynamicInvoke(arguments);
            }
            catch(TargetInvocationException ex)
			{
                Debug.LogWarning(ex);
			}
		}

        [Preserve] private void DoNothing<TService>(TService instance) { }
	}
}