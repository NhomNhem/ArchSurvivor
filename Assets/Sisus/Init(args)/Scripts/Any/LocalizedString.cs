using System;
using UnityEngine;

namespace Sisus.Init.Internal
{
	#if UNITY_LOCALIZATION
	internal sealed class LocalizedString : ScriptableObject, IValueProvider<string>, IConvertible
	{
		[SerializeField]
		internal UnityEngine.Localization.LocalizedString value = new UnityEngine.Localization.LocalizedString();

		public bool HasValue => !value.IsEmpty;
		public string Value => value.IsEmpty ? null : value.GetLocalizedString();
		object IValueProvider.Value => value.GetLocalizedString();

		TypeCode IConvertible.GetTypeCode() => TypeCode.String;
		string IConvertible.ToString(IFormatProvider provider) => Value;
		bool IConvertible.ToBoolean(IFormatProvider provider) => ((IConvertible)Value).ToBoolean(provider);
		byte IConvertible.ToByte(IFormatProvider provider) => ((IConvertible)Value).ToByte(provider);
		char IConvertible.ToChar(IFormatProvider provider) => ((IConvertible)Value).ToChar(provider);
		DateTime IConvertible.ToDateTime(IFormatProvider provider) => ((IConvertible)Value).ToDateTime(provider);
		decimal IConvertible.ToDecimal(IFormatProvider provider) => ((IConvertible)Value).ToDecimal(provider);
		double IConvertible.ToDouble(IFormatProvider provider) => ((IConvertible)Value).ToDouble(provider);
		short IConvertible.ToInt16(IFormatProvider provider) => ((IConvertible)Value).ToInt16(provider);
		int IConvertible.ToInt32(IFormatProvider provider) => ((IConvertible)Value).ToInt32(provider);
		long IConvertible.ToInt64(IFormatProvider provider) => ((IConvertible)Value).ToInt64(provider);
		sbyte IConvertible.ToSByte(IFormatProvider provider) => ((IConvertible)Value).ToSByte(provider);
		float IConvertible.ToSingle(IFormatProvider provider) => ((IConvertible)Value).ToSingle(provider);
		object IConvertible.ToType(Type conversionType, IFormatProvider provider) => ((IConvertible)Value).ToType(conversionType, provider);
		ushort IConvertible.ToUInt16(IFormatProvider provider) => ((IConvertible)Value).ToUInt16(provider);
		uint IConvertible.ToUInt32(IFormatProvider provider) => ((IConvertible)Value).ToUInt32(provider);
		ulong IConvertible.ToUInt64(IFormatProvider provider) => ((IConvertible)Value).ToUInt64(provider);

		public static implicit operator string(LocalizedString localizedString) => localizedString.Value;

		private void OnValidate() => value.RefreshString();
	}
	#endif
}