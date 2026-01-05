#if ODIN_INSPECTOR
namespace Sisus.Init
{
	/// <summary>
	/// Non generic interface implemented by Any{T}.
	/// <para>
	/// Because Odin inspector does not support custom property drawers
	/// targeting open generic types, this non-generic interface
	/// was needed to get Any{T} fields to be drawn using the right
	/// drawer inside OdinEditor.
	/// </para>
	/// </summary>
	public interface IAny { }
}
#endif