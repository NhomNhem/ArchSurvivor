#pragma warning disable CS0414

using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using static Sisus.Init.Internal.InitializerUtility;
using static Sisus.NullExtensions;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using Sisus.Init.EditorOnly;
#endif

namespace Sisus.Init
{
	/// <summary>
	/// A base class for a component that can be used to specify the two arguments used to asynchronously initialize an object that implements
	/// <see cref="IInitializable{TFirstArgument, TSecondArgument}"/>.
	/// <para>
	/// The arguments are injected to the <typeparamref name="TClient">client</typeparamref> whenever they become available.
	/// </para>
	/// <para>
	/// The client receives the arguments via the
	/// <see cref="IInitializable{TFirstArgument, TSecondArgument}.Init">Init</see>
	/// method where it can assign them to member fields or properties.
	/// </para>
	/// <para>
	/// After the arguments have been injected the initializer is removed from the
	/// <see cref="GameObject"/> that holds it.
	/// </para>
	/// <para>
	/// When you derive your Initializer class from <see cref="AsyncInitializerBase{,,}"/>
	/// you are responsible for implementing the argument properties and serializing their values.
	/// This means you will need to write a little bit more code, but it also grants you more options
	/// in how to handle the serialization, making it possible to support types that Unity can't serialize
	/// automatically. If you derive from <see cref="AsyncInitializer{,,}"/> instead, then these things will be handled for you.
	/// </para>
	/// </summary>
	/// <typeparam name="TClient"> Type of the initialized client component. </typeparam>
	/// <typeparam name="TFirstArgument"> Type of the first argument to pass to the client component's Init function. </typeparam>
	/// <typeparam name="TSecondArgument"> Type of the second argument to pass to the client component's Init function. </typeparam>
	public abstract class AsyncInitializerBase<TClient, TFirstArgument, TSecondArgument> : MonoBehaviour, IInitializer<TClient, TFirstArgument, TSecondArgument>, IValueProvider<TClient>
		#if UNITY_EDITOR
		, IInitializerEditorOnly
		#endif
		where TClient : MonoBehaviour, IInitializable<TFirstArgument, TSecondArgument>
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
		/// The first argument passed to the <typeparamref name="TClient">client</typeparamref>'s Init function.
		/// </summary>
		protected abstract TFirstArgument FirstArgument { get; set; }

		/// <summary>
		/// The second argument passed to the <typeparamref name="TClient">client</typeparamref>'s Init function.
		/// </summary>
		protected abstract TSecondArgument SecondArgument { get; set; }

		#if UNITY_EDITOR
		bool IInitializerEditorOnly.ShowNullArgumentGuard => false;
		NullArgumentGuard IInitializerEditorOnly.NullArgumentGuard { get => NullArgumentGuard.None; set { } }
		string IInitializerEditorOnly.NullGuardFailedMessage { get => ""; set { } }
		bool IInitializerEditorOnly.HasNullArguments => IsNull(FirstArgument) || IsNull(SecondArgument);
		bool IInitializerEditorOnly.MultipleInitializersPerTargetAllowed => false;
		#endif

		/// <inheritdoc/>
		public TClient InitTarget()
		{
			if(this == null)
			{
				return target;
			}

			if(TryInitTarget(FirstArgument, SecondArgument))
			{
				DestroySelf();
			}

			return target;
		}

		/// <summary>
		/// Resets the Init arguments to their default values.
		/// <para>
		/// <see cref="OnReset"/> is called when the user hits the Reset button in the Inspector's
		/// context menu or when adding the component to a GameObject the first time.
		/// <para>
		/// This function is only called in the editor in edit mode.
		/// </summary>
		/// <param name="firstArgument"> The first argument to reset. </param>
		/// <param name="secondArgument"> The second argument to reset. </param>
		protected virtual void OnReset(ref TFirstArgument firstArgument, ref TSecondArgument secondArgument) { }

		/// <summary>
		/// Initialize the existing <see cref="target"/> or new instance of type <see cref="TClient"/>
		/// using the provided arguments if the argument is not <see langword="null"/>.
		/// </summary>
		/// <param name="firstArgument"> The first argument to pass to the target's Init function. </param>
		/// <param name="secondArgument"> The second argument to pass to the target's Init function. </param>
		/// <returns> <see langword="true"/> if was able to initialize the target; otherwise, <see langword="false"/>. </returns>
		[NotNull]
		protected virtual bool TryInitTarget(TFirstArgument firstArgument, TSecondArgument secondArgument)
        {
			if(firstArgument == Null || secondArgument == Null)
			{
				return false;
			}

            if(target == null)
            {
                return target = gameObject.AddComponent<TClient, TFirstArgument, TSecondArgument>(firstArgument, secondArgument);
            }

			if(target.gameObject != gameObject)
			{
                return target.Instantiate(firstArgument, secondArgument);
            }
			
			if(target is MonoBehaviour<TFirstArgument, TSecondArgument> monoBehaviourT)
			{
				monoBehaviourT.InitInternal(firstArgument, secondArgument);
			}
			else
			{
				target.Init(firstArgument, secondArgument);
			}

			return target;
        }

		#if UNITY_EDITOR
        private void Reset()
		{
			var set = HandleReset(this, ref target, FirstArgument, SecondArgument, OnReset);
			if(!AreEqual(FirstArgument, set.firstArgument)) FirstArgument = set.firstArgument;
			if(!AreEqual(SecondArgument, set.secondArgument)) SecondArgument = set.secondArgument;
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
			while(!TryInitTarget(FirstArgument, SecondArgument))
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