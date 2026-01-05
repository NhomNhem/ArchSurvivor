#pragma warning disable CS0414

using System;
using Object = UnityEngine.Object;

namespace Sisus.Init
{
	/// <summary>
	/// A base class for an initializer that is responsible for initializing a service of type
	/// <typeparamref name="TService"/>, which itself depends on another service of type
	/// <typeparamref name="TArgument"/>.
	/// <para>
	/// The <see cref="ServiceAttribute"/> must be added to all classes that derive from this
	/// base class; otherwise the framework will not discover the initializer and the
	/// service will not get registered.
	/// </para>
	/// <para>
	/// The <see cref="ServiceAttribute"/> can also be used to specify additional details
	/// about the service, such as its <see cref="ServiceAttribute.definingType">defining type</see>.
	/// </para>
	/// <para>
	/// Adding the <see cref="ServiceAttribute"/> to a service initializer instead of the service
	/// class itself makes it possible to decouple the service class from the ServiceAttribute.
	/// If you want to keep your service classes as decoupled from Init(args) as possible,
	/// this is one tool at your disposable that can help with that.
	/// </para>
	/// </summary>
	/// <typeparam name="TService"> The concrete type of the service object class. </typeparam>
	/// <typeparam name="TArgument"> The defining type of the other service which the initialized service depends on. </typeparam>
	/// <seealso cref="ServiceInitializer{TService}"/>
	public abstract class ServiceInitializer<TService, TArgument> : IServiceInitializer<TService, TArgument> where TService : class
	{
		/// <inheritdoc/>
		Object IInitializer.Target { get => null; set => throw new NotSupportedException("ServiceInitializer.Target will always have a null value and can not be set."); }

		/// <inheritdoc/>
		bool IInitializer.TargetIsAssignableOrConvertibleToType(Type type) => type.IsAssignableFrom(typeof(TService));

		/// <inheritdoc/>
		object IInitializer.InitTarget() => ((IInitializer<TService>)this).InitTarget();

		/// <inheritdoc/>
		TService IInitializer<TService>.InitTarget() => throw new NotSupportedException($"Use the {nameof(InitTarget)} overload with an argument instead.");

		/// <summary>
		/// Initializes a new instance of the service of type <see cref="TService"/>.
		/// </summary>
		/// <param name="argument"> A service of type <see cref="TArgument"/> which the initialized service depends on. </param>
		/// <returns>
		/// An instance of type <see cref="TService"/>, or <see langword="null"/> if the framework should handle creating the instance instead.
		/// </returns>
		public abstract TService InitTarget(TArgument argument);
	}
}