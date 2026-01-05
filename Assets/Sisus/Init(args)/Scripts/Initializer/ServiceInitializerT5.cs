#pragma warning disable CS0414

using System;
using Object = UnityEngine.Object;

namespace Sisus.Init
{
	/// <summary>
	/// A base class for an initializer that is responsible for initializing a service of type
	/// <typeparamref name="TService"/>, which itself depends on two other services.
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
	/// <typeparam name="TFirstArgument"> The defining type of the first service which the initialized service depends on. </typeparam>
	/// <typeparam name="TSecondArgument"> The defining type of the second service which the initialized service depends on. </typeparam>
	/// <typeparam name="TThirdArgument"> The defining type of the third service which the initialized service depends on. </typeparam>
	/// <typeparam name="TFourthArgument"> The defining type of the fourth service which the initialized service depends on. </typeparam>
	/// <typeparam name="TFifthArgument"> The defining type of the fifth service which the initialized service depends on. </typeparam>
	public abstract class ServiceInitializer<TService, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument, TFifthArgument> : IServiceInitializer<TService, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument, TFifthArgument> where TService : class
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
		/// <para>
		/// This allows for more control when creating the service instance and also makes it possible to create the
		/// instance without using reflection, potentially helping to improve initial loading times slightly.
		/// </para>
		/// </summary>
		/// <param name="firstArgument"> First service used during initialization of the target service. </param>
		/// <param name="secondArgument"> Second service used during initialization of the target service. </param>
		/// <param name="thirdArgument"> Third service used during initialization of the target service. </param>
		/// <param name="fourthArgument"> Fourth service used during initialization of the target service. </param>
		/// <param name="fifthArgument"> Fifth service used during initialization of the target service. </param>
		/// <returns>
		/// An instance of type <see cref="TService"/>.
		/// </returns>
		public abstract TService InitTarget(TFirstArgument firstArgument, TSecondArgument secondArgument, TThirdArgument thirdArgument, TFourthArgument fourthArgument, TFifthArgument fifthArgument);
	}
}