namespace IsoEnums.Iso4217;

/// <summary>
/// Helpers to deconstruct the <see cref="Currency"/> values into their parts
/// </summary>
public static class CurrencyExtensions {
	public static String Get3Code(this Currency currency) {
		if (!Enum.IsDefined(currency) || currency == Currency.Uninitialized || currency == Currency.NotACurrency) return CurrencyHelper.Unavailable3;
		Int32 value = (Int32)currency;
		Span<Char> chars = stackalloc Char[3];
		chars[0] = (Char)((Byte)'a' + ((value >> 17) & 0b11111));
		chars[1] = (Char)((Byte)'a' + ((value >> 22) & 0b11111));
		chars[2] = (Char)((Byte)'a' + ((value >> 27) & 0b11111));
		return new String(chars);
	}

	public static void Get3CodeBytes(this Currency currency, Span<Byte> bytes) {
		if (!Enum.IsDefined(currency) || currency == Currency.Uninitialized || currency == Currency.NotACurrency) {
			CurrencyHelper.Unavailable3Bytes.CopyTo(bytes);
			return;
		}

		Int32 value = (Int32)currency;
		bytes[0] = (Byte)((Byte)'a' + ((value >> 17) & 0b11111));
		bytes[1] = (Byte)((Byte)'a' + ((value >> 22) & 0b11111));
		bytes[2] = (Byte)((Byte)'a' + ((value >> 27) & 0b11111));
	}

	/// <summary>
	/// Returns the three-digit numeric code for each currency. This numeric code is usually the same as the numeric code assigned to the corresponding <see cref="Iso3166.Country"/> by ISO 3166-1
	/// </summary>
	/// <remarks>Technically the numeric code is always written as three digits, so values less than 100 should be prefixed with zeros</remarks>
	public static Int32 GetNumericCode(this Currency currency) {
		if (!Enum.IsDefined(currency) || currency == Currency.Uninitialized || currency == Currency.NotACurrency) return 0;
		return ((Int32)currency) & 0xFFFF;
	}
}