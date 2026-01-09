// Curved World <http://u3d.as/1W8h>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using System;

using UnityEditor;
using UnityEngine.Assertions;


namespace AmazingAssets.CurvedWorld.Editor.Universal.ShaderGUI
{
    class CurvedWorld_SavedParameter<T>
        where T : IEquatable<T>
    {
        internal delegate void SetParameter(string key, T value);
        internal delegate T GetParameter(string key, T defaultValue);

        readonly string m_Key;
        bool m_Loaded;
        T m_Value;

        readonly SetParameter m_Setter;
        readonly GetParameter m_Getter;

        public CurvedWorld_SavedParameter(string key, T value, GetParameter getter, SetParameter setter)
        {
            Assert.IsNotNull(setter);
            Assert.IsNotNull(getter);

            m_Key = key;
            m_Loaded = false;
            m_Value = value;
            m_Setter = setter;
            m_Getter = getter;
        }

        void Load()
        {
            if (m_Loaded)
                return;

            m_Loaded = true;
            m_Value = m_Getter(m_Key, m_Value);
        }

        public T value
        {
            get
            {
                Load();
                return m_Value;
            }
            set
            {
                Load();

                if (m_Value.Equals(value))
                    return;

                m_Value = value;
                m_Setter(m_Key, value);
            }
        }
    }

    // Pre-specialized class for easier use and compatibility with existing code
    sealed class CurvedWorld_SavedBool : CurvedWorld_SavedParameter<bool>
    {
        public CurvedWorld_SavedBool(string key, bool value)
            : base(key, value, EditorPrefs.GetBool, EditorPrefs.SetBool) { }
    }
}
