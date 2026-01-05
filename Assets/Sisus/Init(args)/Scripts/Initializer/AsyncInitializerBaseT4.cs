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
	/// A base class for a component that can be used to specify the four arguments used to asynchronously initialize an object that implements
	/// <see cref="IInitializable{TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument}"/>.
	/// <para>
	/// The arguments are injected to the <typeparamref name="TClient">client</typeparamref> whenever they become available.
	/// </para>
	/// <para>
	/// The client receives the arguments via the
	/// <see cref="IInitializable{TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument}.Init">Init</see>
	/// method where it can assign them to member fields or properties.
	/// </para>
	/// <para>
	/// After the arguments have been injected the initializer is removed from the
	/// <see cref="GameObject"/> that holds it.
	/// </para>
	/// <para>
	/// When you derive your Initializer class from <see cref="AsyncInitializerBase{,,,,}"/>
	/// you are responsible for implementing the argument properties and serializing their values.
	/// This means you will need to write a little bit more code, but it also grants you more options
	/// in how to handle the serialization, making it possible to support types that Unity can't serialize
	/// automatically. If you derive from <see cref="AsyncInitializer{,,,,}"/> instead, then these things will be handled for you.
	/// </para>
	/// </summary>
	/// <typeparam name="TClient"> Type of the initialized client component. </typeparam>
	/// <typeparam name="TFirstArgument"> Type of the first argument to pass to the client component's Init function. </typeparam>
	/// <typeparam name="TSecondArgument"> Type of the second argument to pass to the client component's Init function. </typeparam>
	/// <typeparam name="TThirdArgument"> Type of the third argument to pass to the client component's Init function. </typeparam>
	/// <typeparam name="TFourthArgument"> Type of the fourth argument to pass to the client component's Init function. </typeparam>
	public abstract class AsyncInitializerBase<TClient, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument> : MonoBehaviour, IInitializer<TClient, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument>, IValueProvider<TClient>
		#if UNITY_EDITOR
		, IInitializerEditorOnly
		#endif
		where TClient : MonoBehaviour, IInitializable<TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument>
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

		/// <summary>
		/// The third argument passed to the <typeparamref name="TClient">client</typeparamref>'s Init function.
		/// </summary>
		protected abstract TThirdArgument ThirdArgument { get; set; }

		/// <summary>
		/// The fourth argument passed to the <typeparamref name="TClient">client</typeparamref>'s Init function.
		/// </summary>
		protected abstract TFourthArgument FourthArgument { get; set; }

		#if UNITY_EDITOR
		bool IInitializerEditorOnly.ShowNullArgumentGuard => false;
		NullArgumentGuard IInitializerEditorOnly.NullArgumentGuard { get => NullArgumentGuard.None; set { } }
		string IInitializerEditorOnly.NullGuardFailedMessage { get => ""; set { } }
		bool IInitializerEditorOnly.HasNullArguments => IsNull(FirstArgument) || IsNull(SecondArgument) || IsNull(ThirdArgument) || IsNull(FourthArgument);
		bool IInitializerEditorOnly.MultipleInitializersPerTargetAllowed => false;
		#endif

		/// <inheritdoc/>
		public TClient InitTarget()
		{
			if(this == null)
			{
				return target;
			}

			if(TryInitTarget(FirstArgument, SecondArgument, ThirdArgument, FourthArgument))
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
		/// <param name="thirdArgument"> The third argument to reset. </param>
		/// <param name="fourthArgument"> The fourth argument to reset. </param>
		protected virtual void OnReset(ref TFirstArgument firstArgument, ref TSecondArgument secondArgument, ref TThirdArgument thirdArgument, ref TFourthArgument fourthArgument) { }

		/// <summary>
		/// Initialize the existing <see cref="target"/> or new instance of type <see cref="TClient"/>
		/// using the provided arguments if the argument is not <see langword="null"/>.
		/// </summary>
		/// <param name="firstArgument"> The first argument to pass to the target's Init function. </param>
		/// <param name="secondArgument"> The second argument to pass to the target's Init function. </param>
		/// <param name="thirdArgument"> The third argument to pass to the target's Init function. </param>
		/// <param name="fourthArgument"> The fourth argument to pass to the target's Init function. </param>
		/// <returns> <see langword="true"/> if was able to initialize the target; otherwise, <see langword="false"/>. </returns>
		[NotNull]
		protected virtual bool TryInitTarget(TFirstArgument firstArgument, TSecondArgument secondArgument, TThirdArgument thirdArgument, TFourthArgument fourthArgument)
        {
			if(firstArgument == Null || secondArgument == Null || thirdArgument == Null || fourthArgument == Null)
			{
				return false;
			}

            if(target == null)
            {
                return target = gameObject.AddComponent<TClient, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument>(firstArgument, secondArgument, thirdArgument, fourthArgument);
            }

			if(target.gameObject != gameObject)
			{
                return target.Instantiate(firstArgument, secondArgument, thirdArgument, fourthArgument);
            }
			
			if(target is MonoBehaviour<TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument> monoBehaviourT)
			{
				monoBehaviourT.InitInternal(firstArgument, secondArgument, thirdArgument, fourthArgument);
			}
			else
			{
				target.Init(firstArgument, secondArgument, thirdArgument, fourthArgument);
			}

			return target;
        }

		#if UNITY_EDITOR
        private void Reset()
		{
			var set = HandleReset(this, ref target, FirstArgument, SecondArgument, ThirdArgument, FourthArgument, OnReset);
			if(!AreEqual(FirstArgument, set.firstArgument)) FirstArgument = set.firstArgument;
			if(!AreEqual(SecondArgument, set.secondArgument)) SecondArgument = set.secondArgument;
			if(!AreEqual(ThirdArgument, set.thirdArgument)) ThirdArgument = set.thirdArgument;
			if(!AreEqual(FourthArgument, set.fourthArgument)) FourthArgument = set.fourthArgument;
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
			while(!TryInitTarget(FirstArgument, SecondArgument, ThirdArgument, FourthArgument))
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