using System;
using System.Linq;
using UnityEditor;

namespace Sisus.Init.EditorOnly.Internal
{
    [CanEditMultipleObjects]
    internal sealed class WrapperInitializerEditor : InitializerEditor
    {
        protected override Type[] GetInitArgumentTypes(Type[] genericArguments) => genericArguments.Skip(2).ToArray();
    }
}