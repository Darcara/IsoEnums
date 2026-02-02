namespace IsoEnums.Iso639;

/// <summary>
/// Extensions for the <see cref="Language"/> enum
/// </summary>
public static class LanguageExtensions {
	/// <summary>
	/// Returns TRUE if this language has a 2-code (like 'en' for <see cref="Language.English"/> that can be retrieved with <see cref="Get2Code"/>
	/// </summary>
	/// <remarks>Not all Language have 2-codes, prefer to use <see cref="Get3Code"/> instead</remarks>
	public static Boolean Has2Code(this Language language) {
		if (!Enum.IsDefined(language) || language == Language.Uninitialized) return false;
		UInt32 value = ((UInt32)language) & 0xFFFF0000;
		if (value == 0x0000) return false;
		return true;
	}

	public static String Get2Code(this Language language) {
		if (!Enum.IsDefined(language) || language == Language.Uninitialized) return LanguageHelper.Unavailable2;
		UInt32 value = ((UInt32)language) & 0xFFFF0000;
		if (value == 0x0000) return LanguageHelper.Unavailable2;
		--value;
		Span<Char> chars = stackalloc Char[2];
		chars[0] = (Char)((Byte)'a' + ((value >> 17) & 0b11111));
		chars[1] = (Char)((Byte)'a' + ((value >> 22) & 0b11111));
		return new String(chars);
	}

	public static void Get2CodeBytes(this Language language, Span<Byte> bytes) {
		if (!Enum.IsDefined(language) || language == Language.Uninitialized) {
			LanguageHelper.Unavailable2Bytes.CopyTo(bytes);
			return;
		}

		UInt32 value = ((UInt32)language) & 0xFFFF0000;
		if (value == 0x0000) {
			LanguageHelper.Unavailable2Bytes.CopyTo(bytes);
			return;
		}

		--value;
		bytes[0] = (Byte)((Byte)'a' + ((value >> 17) & 0b11111));
		bytes[1] = (Byte)((Byte)'a' + ((value >> 22) & 0b11111));
	}

	public static String Get3Code(this Language language) {
		if (!Enum.IsDefined(language) || language == Language.Uninitialized) return LanguageHelper.Unavailable3;
		Int32 value = (Int32)language - 1;
		Span<Char> chars = stackalloc Char[3];
		chars[0] = (Char)((Byte)'a' + ((value >> 1) & 0b11111));
		chars[1] = (Char)((Byte)'a' + ((value >> 6) & 0b11111));
		chars[2] = (Char)((Byte)'a' + ((value >> 11) & 0b11111));
		return new String(chars);
	}

	public static void Get3CodeBytes(this Language language, Span<Byte> bytes) {
		if (!Enum.IsDefined(language) || language == Language.Uninitialized) {
			LanguageHelper.Unavailable3Bytes.CopyTo(bytes);
			return;
		}

		Int32 value = (Int32)language - 1;
		bytes[0] = (Byte)((Byte)'a' + ((value >> 1) & 0b11111));
		bytes[1] = (Byte)((Byte)'a' + ((value >> 6) & 0b11111));
		bytes[2] = (Byte)((Byte)'a' + ((value >> 11) & 0b11111));
	}
}