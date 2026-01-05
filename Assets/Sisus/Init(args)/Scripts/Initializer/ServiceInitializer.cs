#pragma warning disable CS0414

using System;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus.Init
{
	/// <summary>
	/// A base class for an initializer that is responsible for specifying how a service of type
	/// <typeparamref name="TService"/> should be initialized and registered with the framework.
	/// <para>
	/// Optionally, the <see cref="InitTarget"/> function can be overriden to take
	/// direct control of the initialization of the service.
	/// </para>
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
	/// <seealso cref="ServiceInitializer{TService, TArgument}"/>
	public abstract class ServiceInitializer<TService> : IServiceInitializer<TService> where TService : class
	{
		/// <inheritdoc/>
		Object IInitializer.Target
		{
			get => null;
			set => throw new NotSupportedException("ServiceInitializer.Target will always have a null value and can not be set.");
		}

		/// <inheritdoc/>
		bool IInitializer.TargetIsAssignableOrConvertibleToType(Type type) => type.IsAssignableFrom(typeof(TService));

		/// <inheritdoc/>
		object IInitializer.InitTarget() => InitTarget();

		/// <summary>
		/// Returns a new instance of the <see cref="TService"/> class, or <see langword="null"/>.
		/// <para>
		/// By default this method always returns <see langword="null"/>.
		/// This tells the framework that it should handle creating the instance internally.
		/// </para>
		/// <para>
		/// If more control is needed, this method can be override to take over control of the
		/// creation of the service object from the framework.
		/// </para>
		/// </summary>
		/// <returns>
		/// A new instance of the <see cref="TService"/> class, or <see langword="null"/>
		/// if the framework should create the instance automatically.
		/// </returns>
		[CanBeNull]
		public virtual TService InitTarget() => null;
	}
}