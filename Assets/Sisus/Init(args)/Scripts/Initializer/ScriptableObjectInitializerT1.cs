using UnityEngine;
using static Sisus.Init.Internal.InitializerUtility;

namespace Sisus.Init
{
	/// <summary>
	/// A base class for an initializer that can be used to specify the argument used to
	/// initialize a scriptable object that implements <see cref="IInitializable{TArgument}"/>.
	/// <para>
	/// The argument can be assigned using the inspector and is serialized as a sub-asset of the client scriptable object.
	/// </para>
	/// <para>
	/// The argument gets injected to the <typeparamref name="TClient">client</typeparamref>
	/// during the client's <see cref="Awake"/> event, or when services become ready (whichever occurs later).
	/// </para>
	/// <para>
	/// The client receives the argument via the <see cref="IInitializable{TArgument}.Init">Init</see>
	/// method where it can assign it to a member field or property.
	/// </para>
	/// </summary>
	/// <typeparam name="TClient"> Type of the initialized scriptable object. </typeparam>
	/// <typeparam name="TArgument"> Type of the argument to pass to the client's Init function. </typeparam>
	public abstract class ScriptableObjectInitializer<TClient, TArgument> : ScriptableObjectInitializerBase<TClient, TArgument> where TClient : ScriptableObject, IInitializable<TArgument>
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