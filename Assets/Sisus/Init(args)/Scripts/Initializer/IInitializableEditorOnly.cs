#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

[assembly: InternalsVisibleTo("InitArgs")]

namespace Sisus.Init.EditorOnly
{
	internal interface IInitializableEditorOnly
	{
		[CanBeNull]
		IInitializer Initializer { get; set; }
	}
}
#endif