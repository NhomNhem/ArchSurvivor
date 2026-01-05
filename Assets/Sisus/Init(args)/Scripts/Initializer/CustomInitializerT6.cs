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
	/// A base class for a component that can specify the six arguments used to initialize an object of type <typeparamref name="TClient"/>.
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
	/// <typeparam name="TThirdArgument"> Type of the third argument to pass to the client's Init function. </typeparam>
	/// <typeparam name="TFourthArgument"> Type of the fourth argument to pass to the client's Init function. </typeparam>
	/// <typeparam name="TFifthArgument"> Type of the fifth argument to pass to the client's Init function. </typeparam>
	/// <typeparam name="TSixthArgument"> Type of the sixth argument to pass to the client's Init function. </typeparam>
	public abstract class CustomInitializer<TClient, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument, TFifthArgument, TSixthArgument>
		: Initializer<TClient>, IInitializer<TClient, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument, TFifthArgument, TSixthArgument>
	{
		[SerializeField] private Any<TFirstArgument> firstArgument = default;
		[SerializeField] private Any<TSecondArgument> secondArgument = default;
		[SerializeField] private Any<TThirdArgument> thirdArgument = default;
		[SerializeField] private Any<TFourthArgument> fourthArgument = default;
		[SerializeField] private Any<TFifthArgument> fifthArgument = default;
		[SerializeField] private Any<TSixthArgument> sixthArgument = default;

		protected override bool HasNullArguments => IsNull(this, firstArgument) || IsNull(this, secondArgument) || IsNull(this, thirdArgument)
												 || IsNull(this, fourthArgument) || IsNull(this, fifthArgument) || IsNull(this, sixthArgument);

		protected sealed override void InitTarget(TClient target) => InitTarget(target, firstArgument, secondArgument, thirdArgument, fourthArgument, fifthArgument, sixthArgument);

		/// <summary>
		/// Initializes the <paramref name="target"/> of type <see cref="TClient"/> using the provided arguments.
		/// </summary>
		/// <param name="target"> The target to initialize. </param>
		/// <param name="firstArgument"> The first argument used to initialize the target. </param>
		/// <param name="secondArgument"> The second argument used to initialize the target. </param>
		/// <param name="thirdArgument"> The third argument used to initialize the target. </param>
		/// <param name="fourthArgument"> The fourth argument used to initialize the target. </param>
		/// <param name="fifthArgument"> The fifth argument used to initialize the target. </param>
		/// <param name="sixthArgument"> The sixth argument used to initialize the target. </param>
		[NotNull]
		protected abstract void InitTarget(TClient target, TFirstArgument firstArgument, TSecondArgument secondArgument, TThirdArgument thirdArgument, TFourthArgument fourthArgument, TFifthArgument fifthArgument, TSixthArgument sixthArgument);

		#if UNITY_EDITOR
		private protected sealed override void Reset()
		{
			AutoInitUtility.PrepareArgumentsForAutoInit<TClient>(this, 6);
			ResetArgument(ref firstArgument, 0, true, this);
			ResetArgument(ref secondArgument, 1, true, this);
			ResetArgument(ref thirdArgument, 2, true, this);
			ResetArgument(ref fourthArgument, 3, true, this);
			ResetArgument(ref fifthArgument, 4, true, this);
			ResetArgument(ref sixthArgument, 5, true, this);
			base.Reset();
		}

		private void OnValidate() => OnMainThread(Validate);
		#endif
    }
}