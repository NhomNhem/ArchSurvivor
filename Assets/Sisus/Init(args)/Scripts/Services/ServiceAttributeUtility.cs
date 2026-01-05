using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Sisus.Init.Internal;

namespace Sisus.Init
{
	internal static class ServiceAttributeUtility
	{
        internal static readonly Dictionary<Type, ServiceAttribute.Info> concreteTypes;
        internal static readonly Dictionary<Type, ServiceAttribute.Info> definingTypes;

        static ServiceAttributeUtility()
        {
            #if UNITY_EDITOR
            var typesWithAttribute = UnityEditor.TypeCache.GetTypesWithAttribute<ServiceAttribute>();
            int count = typesWithAttribute.Count;
            #else
			var typesWithAttribute = TypeUtility.GetTypesWithAttribute<ServiceAttribute>(typeof(ServiceAttribute).Assembly, null);
            int count = typesWithAttribute.Count();
			#endif

            concreteTypes = new Dictionary<Type, ServiceAttribute.Info>(count);
            definingTypes = new Dictionary<Type, ServiceAttribute.Info>(count);

			#if UNITY_EDITOR
			foreach(var typeWithAttribute in typesWithAttribute)
			{
                var attributes = typeWithAttribute.GetCustomAttributes<ServiceAttribute>().ToArray();
			#else
			foreach((var typeWithAttribute, var attributes) in typesWithAttribute)
			{
			#endif
                var info = BuildInfo(typeWithAttribute, attributes);
				if(info.concreteType is null)
				{
					#if DEV_MODE
					UnityEngine.Debug.Log(typeWithAttribute.Name + " concrete type is null.");
					#endif
					continue;
				}

                concreteTypes[info.concreteType] = info;
                foreach(var definingType in info.definingTypes)
				{
                    definingTypes[definingType] = info;
				}
			}

            static ServiceAttribute.Info BuildInfo(Type classWithAttribute, ServiceAttribute[] attributes)
		    {
                var concreteType = GetConcreteType(classWithAttribute);
			    var definingTypesOfService = new Type[attributes.Length];
			    for(int i = attributes.Length - 1; i >= 0; i--)
			    {
                    definingTypesOfService[i] = attributes[i].definingType ?? concreteType;
			    }

                return new ServiceAttribute.Info(classWithAttribute, concreteType, definingTypesOfService, attributes);

                static Type GetConcreteType(Type classWithAttribute)
		        {
                    bool isInitializer = typeof(IInitializer).IsAssignableFrom(classWithAttribute);
                    bool isWrapper = typeof(IWrapper).IsAssignableFrom(classWithAttribute);
                    if(isInitializer || isWrapper)
			        {
                        var match = isInitializer ? typeof(IInitializer<>) : typeof(IWrapper<>);
                        var interfaceTypes = classWithAttribute.GetInterfaces();
                        for(int i = interfaceTypes.Length - 1; i >= 0; i--)
				        {
					        var interfaceType = interfaceTypes[i];
					        if(!interfaceType.IsGenericType)
					        {
						        continue;
					        }

					        var genericType = interfaceType.GetGenericTypeDefinition();
					        if(genericType != match)
					        {
						        continue;
					        }

					        var clientType = interfaceType.GetGenericArguments()[0];

                            if(TypeUtility.IsBaseType(clientType))
						    {
                                return null;
						    }

                            if(clientType.IsAbstract)
                            {
						        return GetSingleDerivedConcreteTypeOrNull(clientType);
                            }

                            if(clientType.IsGenericType && typeof(Task).IsAssignableFrom(clientType))
					        {
                                return clientType.GetGenericArguments()[0];
					        }
					
					        return clientType;
				        }
			        }

                    if(TypeUtility.IsBaseType(classWithAttribute))
				    {
                        return null;
				    }

				    return !classWithAttribute.IsAbstract ? classWithAttribute : GetSingleDerivedConcreteTypeOrNull(classWithAttribute);
			    }

			    static Type GetSingleDerivedConcreteTypeOrNull(Type baseOrInterfaceType)
                {
                    Type result = null;
			        #if UNITY_EDITOR
			        foreach(Type derivedType in UnityEditor.TypeCache.GetTypesDerivedFrom(baseOrInterfaceType))
                    {
			        #else
                    foreach(Type derivedType in TypeUtility.GetAllTypesThreadSafe(baseOrInterfaceType.Assembly, null))
			        {
				        if(!baseOrInterfaceType.IsAssignableFrom(derivedType))
				        {
					        continue;
				        }
			        #endif
        
                        if(derivedType.IsAbstract)
                        {
                            continue;
                        }


                        if(result != null)
					    {
                            return null;
					    }

                        result = derivedType;
                    }

                    return result;
                }
		    }
        }
	}
}