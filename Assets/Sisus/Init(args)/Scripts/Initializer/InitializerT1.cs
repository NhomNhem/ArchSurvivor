using UnityEngine;
using static Sisus.Init.Internal.InitializerUtility;

namespace Sisus.Init
{
	/// <summary>
	/// A base class for a component that can be used to specify the argument used to
	/// initialize a component that implements <see cref="IInitializable{TArgument}"/>.
	/// <para>
	/// The argument can be assigned using the inspector and is serialized as part of the client's scene or prefab asset.
	/// </para>
	/// <para>
	/// The argument gets injected to the <typeparamref name="TClient">client</typeparamref>
	/// during the <see cref="Awake"/> event.
	/// </para>
	/// <para>
	/// The client receives the argument via the <see cref="IInitializable{TArgument}.Init">Init</see>
	/// method where it can be assigned to a member field or property.
	/// </para>
	/// <para>
	/// After the argument has been injected the <see cref="Initializer{,}"/> is removed from the
	/// <see cref="GameObject"/> that holds it.
	/// </para>
	/// </summary>
	/// <typeparam name="TClient"> Type of the initialized client component. </typeparam>
	/// <typeparam name="TArgument"> Type of the argument to pass to the client component's Init function. </typeparam>
	public abstract class Initializer<TClient, TArgument> : InitializerBase<TClient, TArgument> where TClient : MonoBehaviour, IInitializable<TArgument>
	{
		[SerializeField]
		private Any<TArgument> argument = default;

		/// <inheritdoc/>
		protected override TArgument Argument { get => argument.GetValueOrDefault(this, Context.MainThread); set => argument = value; }

		#if UNITY_EDITOR
		protected override bool HasNullArguments => IsNull(this, argument);
		#endif
	}
}