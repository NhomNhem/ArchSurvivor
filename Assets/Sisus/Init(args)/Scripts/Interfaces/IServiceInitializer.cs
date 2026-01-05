using JetBrains.Annotations;

namespace Sisus.Init
{
	/// <summary>
	/// Represents an initializer that specifies how a service object should be initialized.
	/// <para>
	/// Base interface for all generic <see cref="IServiceInitializer{}"/> interfaces,
	/// which should be implemented by all service initializer classes.
	/// </para>
	/// </summary>
	public interface IServiceInitializer : IInitializer { }

	/// <summary>
	/// Represents an initializer that specifies how a service of type
	/// <typeparamref name="TService"/> should be initialized.
	/// <para>
	/// Implemented by initializers of services that are initialized automatically by the framework
	/// or services that don't depend on any other services and are initialized manually via the
	/// <see cref="IInitializer{TService}.InitTarget"/> method.
	/// </para>
	/// </summary>
	/// <typeparam name="TService"> The concrete type of the service object class. </typeparam>
	public interface IServiceInitializer<TService> : IServiceInitializer, IInitializer<TService> { }

	/// <summary>
	/// Represents an initializer that specifies how a service of type
	/// <typeparamref name="TService"/> should be initialized.
	/// <para>
	/// Implemented by initializers of services that depend on one other service and
	/// are initialized manually via the <see cref="InitTarget"/> method.
	/// </para>
	/// </summary>
	/// <typeparam name="TService"> The concrete type of the service object class. </typeparam>
	/// <typeparam name="TArgument"> Type of another service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	public interface IServiceInitializer<TService, TArgument> : IServiceInitializer, IInitializer<TService, TArgument>
	{
		/// <summary>
		/// Initializes the service with another service that it depends on.
		/// </summary>
		/// <param name="argument"> Service used during initialization of the target service. </param>
		[NotNull]
		TService InitTarget(TArgument argument);
	}

	/// <summary>
	/// Represents an initializer that specifies how a service of type
	/// <typeparamref name="TService"/> should be initialized.
	/// <para>
	/// Implemented by initializers of services that depend on two other services and
	/// are initialized manually via the <see cref="InitTarget"/> method.
	/// </para>
	/// </summary>
	/// <typeparam name="TService"> The concrete type of the service object class. </typeparam>
	/// <typeparam name="TFirstArgument"> Type of the first service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TSecondArgument"> Type of the second service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
		public interface IServiceInitializer<TService, TFirstArgument, TSecondArgument> : IServiceInitializer, IInitializer<TService, TFirstArgument, TSecondArgument>
	{
		/// <summary>
		/// Initializes the service with two other services that it depends on.
		/// </summary>
		/// <param name="firstArgument"> First service used during initialization of the target service. </param>
		/// <param name="secondArgument"> Second service used during initialization of the target service. </param>
		[NotNull]
		TService InitTarget(TFirstArgument firstArgument, TSecondArgument secondArgument);
	}

	/// <summary>
	/// Represents an initializer that specifies how a service of type
	/// <typeparamref name="TService"/> should be initialized.
	/// <para>
	/// Implemented by initializers of services that depend on three other services and
	/// are initialized manually via the <see cref="InitTarget"/> method.
	/// </para>
	/// </summary>
	/// <typeparam name="TService"> The concrete type of the service object class. </typeparam>
	/// <typeparam name="TFirstArgument"> Type of the first service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TSecondArgument"> Type of the second service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TThirdArgument"> Type of the third service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	public interface IServiceInitializer<TService, TFirstArgument, TSecondArgument, TThirdArgument> : IServiceInitializer, IInitializer<TService, TFirstArgument, TSecondArgument, TThirdArgument>
	{
		/// <summary>
		/// Initializes the service with three other services that it depends on.
		/// </summary>
		/// <param name="firstArgument"> First service used during initialization of the target service. </param>
		/// <param name="secondArgument"> Second service used during initialization of the target service. </param>
		/// <param name="thirdArgument"> Third service used during initialization of the target service. </param>
		[NotNull]
		TService InitTarget(TFirstArgument firstArgument, TSecondArgument secondArgument, TThirdArgument thirdArgument);
	}

	/// <summary>
	/// Represents an initializer that specifies how a service of type
	/// <typeparamref name="TService"/> should be initialized.
	/// <para>
	/// Implemented by initializers of services that depend on four other services and
	/// are initialized manually via the <see cref="InitTarget"/> method.
	/// </para>
	/// </summary>
	/// <typeparam name="TService"> The concrete type of the service object class. </typeparam>
	/// <typeparam name="TFirstArgument"> Type of the first service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TSecondArgument"> Type of the second service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TThirdArgument"> Type of the third service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TFourthArgument"> Type of the fourth service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	public interface IServiceInitializer<TService, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument> : IServiceInitializer, IInitializer<TService, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument>
	{
		/// <summary>
		/// Initializes the service with four other services that it depends on.
		/// </summary>
		/// <param name="firstArgument"> First service used during initialization of the target service. </param>
		/// <param name="secondArgument"> Second service used during initialization of the target service. </param>
		/// <param name="thirdArgument"> Third service used during initialization of the target service. </param>
		/// <param name="fourthArgument"> Fourth service used during initialization of the target service. </param>
		[NotNull]
		TService InitTarget(TFirstArgument firstArgument, TSecondArgument secondArgument, TThirdArgument thirdArgument, TFourthArgument fourthArgument);
	}

	/// <summary>
	/// Represents an initializer that specifies how a service of type
	/// <typeparamref name="TService"/> should be initialized.
	/// <para>
	/// Implemented by initializers of services that depend on five other services and
	/// are initialized manually via the <see cref="InitTarget"/> method.
	/// </para>
	/// </summary>
	/// <typeparam name="TService"> The concrete type of the service object class. </typeparam>
	/// <typeparam name="TFirstArgument"> Type of the first service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TSecondArgument"> Type of the second service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TThirdArgument"> Type of the third service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TFourthArgument"> Type of the fourth service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TFifthArgument"> Type of the fifth service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	public interface IServiceInitializer<TService, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument, TFifthArgument> : IServiceInitializer, IInitializer<TService, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument, TFifthArgument>
	{
		/// <summary>
		/// Initializes the service with five other services that it depends on.
		/// </summary>
		/// <param name="firstArgument"> First service used during initialization of the target service. </param>
		/// <param name="secondArgument"> Second service used during initialization of the target service. </param>
		/// <param name="thirdArgument"> Third service used during initialization of the target service. </param>
		/// <param name="fourthArgument"> Fourth service used during initialization of the target service. </param>
		/// <param name="fifthArgument"> Fifth service used during initialization of the target service. </param>
		[NotNull]
		TService InitTarget(TFirstArgument firstArgument, TSecondArgument secondArgument, TThirdArgument thirdArgument, TFourthArgument fourthArgument, TFifthArgument fifthArgument);
	}

	/// <summary>
	/// Represents an initializer that specifies how a service of type
	/// <typeparamref name="TService"/> should be initialized.
	/// <para>
	/// Implemented by initializers of services that depend on six other services and
	/// are initialized manually via the <see cref="InitTarget"/> method.
	/// </para>
	/// </summary>
	/// <typeparam name="TService"> The concrete type of the service object class. </typeparam>
	/// <typeparam name="TFirstArgument"> Type of the first service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TSecondArgument"> Type of the second service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TThirdArgument"> Type of the third service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TFourthArgument"> Type of the fourth service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TFifthArgument"> Type of the fifth service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	/// <typeparam name="TSixthArgument"> Type of the sixth service which the service of type <typeparamref name="TService"/> depends on. </typeparam>
	public interface IServiceInitializer<TService, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument, TFifthArgument, TSixthArgument> : IServiceInitializer, IInitializer<TService, TFirstArgument, TSecondArgument, TThirdArgument, TFourthArgument, TFifthArgument, TSixthArgument>
	{
		/// <summary>
		/// Initializes the service with six other services that it depends on.
		/// </summary>
		/// <param name="firstArgument"> First service used during initialization of the target service. </param>
		/// <param name="secondArgument"> Second service used during initialization of the target service. </param>
		/// <param name="thirdArgument"> Third service used during initialization of the target service. </param>
		/// <param name="fourthArgument"> Fourth service used during initialization of the target service. </param>
		/// <param name="fifthArgument"> Fifth service used during initialization of the target service. </param>
		/// <param name="sixthArgument"> Sixth service used during initialization of the target service. </param>
		[NotNull]
		TService InitTarget(TFirstArgument firstArgument, TSecondArgument secondArgument, TThirdArgument thirdArgument, TFourthArgument fourthArgument, TFifthArgument fifthArgument, TSixthArgument sixthArgument);
	}
}