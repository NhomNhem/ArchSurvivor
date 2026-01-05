#pragma warning disable CS0414

using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using static Sisus.Init.Internal.InitializerUtility;
using Object = UnityEngine.Object;
using static Sisus.NullExtensions;
#if UNITY_EDITOR
using Sisus.Init.EditorOnly;
#endif

namespace Sisus.Init
{
	/// <summary>
	/// A base class for a component that can be used to specify the argument used to asynchronously
	/// initialize an object that implements <see cref="IInitializable{TArgument}"/>.
	/// <para>
	/// The argument is injected to the <typeparamref name="TClient">client</typeparamref>
	/// whenever it becomes available.
	/// </para>
	/// <para>
	/// The client receives the argument via the <see cref="IInitializable{TArgument}.Init">Init</see>
	/// method where it can assign it to a member field or property.
	/// </para>
	/// <para>
	/// After the argument has been injected the <see cref="Initializer{,}"/> is removed from the
	/// <see cref="GameObject"/> that holds it.
	/// </para>
	/// <para>
	/// When you derive your Initializer class from <see cref="InitializerBase{,}"/>
	/// you are responsible for implementing the Argument property and serializing its value.
	/// This means you will need to write a little bit more code, but it also grants you more options
	/// in how to handle the serialization, making it possible to support types that Unity can't serialize
	/// automatically. If you derive from <see cref="Initializer{,}"/> instead, then these things will be handled for you.
	/// </para>
	/// </summary>
	/// <typeparam name="TClient"> Type of the initialized client component. </typeparam>
	/// <typeparam name="TArgument"> Type of the argument to pass to the client component's Init function. </typeparam>
	public abstract class AsyncInitializerBase<TClient, TArgument> : MonoBehaviour, IInitializer<TClient, TArgument>, IValueProvider<TClient>
		#if UNITY_EDITOR
		, IInitializerEditorOnly
		#endif
		where TClient : MonoBehaviour, IInitializable<TArgument>
	{
		[SerializeField, HideInInspector, Tooltip(TargetTooltip)]
		protected TClient target = null;

		/// <inheritdoc/>
		TClient IValueProvider<TClient>.Value => target;

		/// <inheritdoc/>
		object IValueProvider.Value => target;

		/// <inheritdoc/>
		Object IInitializer.Target { get => target; set => target = (TClient)value; }

		/// <inheritdoc/>
		bool IInitializer.TargetIsAssignableOrConvertibleToType(Type type) => type.IsAssignableFrom(typeof(TClient));

		/// <inheritdoc/>
		object IInitializer.InitTarget() => InitTarget();

		/// <summary>
		/// The argument passed to the <typeparamref name="TClient">client</typeparamref>'s Init function.
		/// </summary>
		protected abstract TArgument Argument { get; set; }

		#if UNITY_EDITOR
		bool IInitializerEditorOnly.ShowNullArgumentGuard => false;
		NullArgumentGuard IInitializerEditorOnly.NullArgumentGuard { get => NullArgumentGuard.None; set { } }
		string IInitializerEditorOnly.NullGuardFailedMessage { get => ""; set { } }
		bool IInitializerEditorOnly.HasNullArguments => IsNull(Argument);
		bool IInitializerEditorOnly.MultipleInitializersPerTargetAllowed => false;
		#endif

		/// <inheritdoc/>
		public TClient InitTarget()
		{
			if(this == null)
			{
				return target;
			}

			if(TryInitTarget(Argument))
			{
				DestroySelf();
			}

			return target;
		}

		/// <summary>
		/// Resets the Init <paramref name="argument"/> to its default value.
		/// <para>
		/// <see cref="OnReset"/> is called when the user hits the Reset button in the Inspector's
		/// context menu or when adding the component to a GameObject the first time.
		/// <para>
		/// This function is only called in the editor in edit mode.
		/// </summary>
		/// <param name="argument"> The argument to reset. </param>
		protected virtual void OnReset(ref TArgument argument) { }

		/// <summary>
		/// Initialize the existing <see cref="target"/> or new instance of type <see cref="TClient"/>
		/// using the provided argument if the argument is not <see langword="null"/>.
		/// </summary>
		/// <param name="argument"> The argument to pass to the target's Init function. </param>
		/// <returns> <see langword="true"/> if was able to initialize the target; otherwise, <see langword="false"/>. </returns>
		[NotNull]
		protected virtual bool TryInitTarget(TArgument argument)
        {
			if(argument == Null)
			{
				return false;
			}

            if(target == null)
            {
                return target = gameObject.AddComponent<TClient, TArgument>(argument);
            }

			if(target.gameObject != gameObject)
			{
                return target.Instantiate(argument);
            }
			
			if(target is MonoBehaviour<TArgument> monoBehaviourT)
			{
				monoBehaviourT.InitInternal(argument);
			}
			else
			{
				target.Init(argument);
			}

			return target;
        }

		#if UNITY_EDITOR
        private void Reset()
		{
			var set = HandleReset(this, ref target, Argument, OnReset);
			if(!AreEqual(Argument, set)) Argument = set;
		}

		private void OnValidate() => OnMainThread(Validate);
		#endif

		protected virtual void Validate()
		{
			#if UNITY_EDITOR
			ValidateOnMainThread(this);
			#endif
		}

		protected virtual void OnEnable() => StartCoroutine(TryInitTargetLoop());
		protected virtual void OnDisable() => StopAllCoroutines();

		private IEnumerator TryInitTargetLoop()
		{
			while(!TryInitTarget(Argument))
			{
				yield return null;
			}

			DestroySelf();
		}

		private void DestroySelf()
		{
			if(this != null)
			{
				Destroy(this);
			}
		}
	}
}