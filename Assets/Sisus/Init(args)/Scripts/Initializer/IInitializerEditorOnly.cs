#if UNITY_EDITOR
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("InitArgs.Editor")]
[assembly: InternalsVisibleTo("Tests.EditMode")]

namespace Sisus.Init.EditorOnly
{
	internal interface IInitializerEditorOnly : IInitializer
	{
		bool ShowNullArgumentGuard { get; }
		NullArgumentGuard NullArgumentGuard { get; set; }
		string NullGuardFailedMessage { get; set; }
		bool HasNullArguments { get; }
		bool MultipleInitializersPerTargetAllowed { get; }
	}
}
#endif