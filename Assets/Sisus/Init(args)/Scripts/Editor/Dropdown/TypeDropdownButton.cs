using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sisus.Init.EditorOnly
{
    internal sealed class TypeDropdownButton : DropdownButton
    {
        public TypeDropdownButton(GUIContent prefixLabel, GUIContent buttonLabel, IEnumerable<Type> types, IEnumerable<Type> selectedItems, Action<Type> onSelectedItemChanged, bool highlightMissingValue, bool useGroups = false, string menuTitle = "Types")
         : base(prefixLabel, buttonLabel, (Rect belowRect) => TypeDropdownWindow.Show(belowRect, types, useGroups, selectedItems, onSelectedItemChanged, menuTitle), highlightMissingValue) { }
    }
}