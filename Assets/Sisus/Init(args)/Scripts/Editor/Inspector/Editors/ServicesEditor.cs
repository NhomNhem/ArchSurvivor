using System.Linq;
using Sisus.Init.Internal;
using UnityEditor;
using UnityEngine;

namespace Sisus.Init.EditorOnly.Internal
{
    [CustomEditor(typeof(Services), true, isFallback = true), CanEditMultipleObjects]
    internal sealed class ServicesEditor : Editor
    {
        private SerializedProperty providesServices;
        private SerializedProperty toClients;

        private static readonly GUIContent clientsLabel = new GUIContent("For Clients");

        private void OnEnable() => Setup();
        
        private void Setup()
        {
            providesServices = serializedObject.FindProperty(nameof(providesServices));
            toClients = serializedObject.FindProperty(nameof(toClients));
        }

        public override void OnInspectorGUI()
        {
            if(providesServices is null)
            {
                Setup();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(providesServices);
            EditorGUILayout.PropertyField(toClients, clientsLabel);
            if(EditorGUI.EndChangeCheck())
			{
                providesServices.serializedObject.ApplyModifiedProperties();
                
                var allInstances = 
                #if UNITY_2023_1_OR_NEWER
                FindObjectsByType<Services>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                #else
                FindObjectsOfType<Services>();
                #endif

                foreach(var servicesComponent in allInstances)
                {
                    foreach(var service in servicesComponent.providesServices)
					{
                        ServiceUtility.RemoveFromClients(service.service, service.definingType, servicesComponent.toClients, servicesComponent);
					}
                }

                foreach(var servicesComponent in allInstances)
				{
                    foreach(var service in servicesComponent.providesServices)
					{
                        ServiceUtility.AddFor(service.service, service.definingType, servicesComponent.toClients, servicesComponent);
					}
				}
            }
        }
    }
}