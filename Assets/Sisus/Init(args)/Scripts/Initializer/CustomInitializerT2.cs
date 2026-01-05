#pragma warning disable CS0414

using JetBrains.Annotations;
using UnityEngine;
using static Sisus.Init.Internal.InitializerUtility;
#if UNITY_EDITOR
using Sisus.Init.EditorOnly;
#endif

namespace Sisus.Init
{
	/// <summary>
	/// A base class for a component that can specify the two arguments used to initialize an object of type <typeparamref name="TClient"/>.
	/// <para>
	/// The arguments can be assigned using the inspector and are serialized as part of the client's scene or prefab asset.
	/// </para>
	/// <para>
	/// The <typeparamref name="TClient">client</typeparamref> component does not need to implement any <see cref="IInitializable{}"/> interface.
	/// The initialization arguments can instead be injected, for example, directly into properties with public setters.
	/// </para>
	/// <para>
	/// After the arguments have been injected the initializer is removed from the <see cref="GameObject"/> that holds it.
	/// </para>
	/// </summary>
	/// <typeparam name="TClient"> Type of the initialized client component. </typeparam>
	/// <typeparam name="TFirstArgument"> Type of the first argument to pass to the client's Init function. </typeparam>
	/// <typeparam name="TSecondArgument"> Type of the second argument to pass to the client's Init function. </typeparam>
	public abstract class CustomInitializer<TClient, TFirstArgument, TSecondArgument> : Initializer<TClient>, IInitializer<TClient, TFirstArgument, TSecondArgument>
	{
		[SerializeField] private Any<TFirstArgument> firstArgument = default;
		[SerializeField] private Any<TSecondArgument> secondArgument = default;

		protected override bool HasNullArguments => IsNull(this, firstArgument) || IsNull(this, secondArgument);

		protected sealed override void InitTarget(TClient target) => InitTarget(target, firstArgument, secondArgument);

		/// <summary>
		/// Initializes the <paramref name="target"/> of type <see cref="TClient"/> using the provided arguments.
		/// </summary>
		/// <param name="target"> The target to initialize. </param>
		/// <param name="firstArgument"> The first argument used to initialize the target. </param>
		/// <param name="secondArgument"> The second argument used to initialize the target. </param>
		[NotNull]
		protected abstract void InitTarget(TClient target, TFirstArgument firstArgument, TSecondArgument secondArgument);

		#if UNITY_EDITOR
		private protected sealed override void Reset()
		{
			AutoInitUtility.PrepareArgumentsForAutoInit<TClient>(this, 2);
			ResetArgument(ref firstArgument, 0, true, this);
			ResetArgument(ref secondArgument, 1, true, this);
			base.Reset();
		}

		private void OnValidate() => OnMainThread(Validate);
		#endif
    }
}