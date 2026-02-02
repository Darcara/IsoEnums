namespace IsoEnums.Generator;

using System.Collections.Frozen;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using IsoEnums.Iso4217;
using Neco.Common.Extensions;

internal class CurrencyGenerator {
	private readonly List<CurrencyEntry> _currencies;

	public CurrencyGenerator(List<CurrencyEntry> currencies) {
		_currencies = currencies;
	}

	public void Generate4217CurrencyEnum(String? fileoutput) {
		StringBuilder sb = new();
		BaseGenerator.AppendDefaultHeader(sb);
		sb.AppendLine("namespace IsoEnums.Iso4217;");
		sb.AppendLine("#region Designer generated code");
		sb.AppendLine("/// <summary>ISO 4217 is a standard that defines alpha and numeric codes for the representation of currencies</summary>");
		sb.AppendLine("public enum Currency {");
		Int32 numCurrencies = 0;

		FrozenSet<String> codesToIgnore = [
			// US Dollar (Next day),
			"USN",
			// SDR (Special Drawing Right)
			"XDR",
			// Bond Markets Unit European Composite Unit (EURCO)
			"XBA",
			// Bond Markets Unit European Monetary Unit (E.M.U.-6)
			"XBB",
			// Bond Markets Unit European Unit of Account 9 (E.U.A.-9)
			"XBC",
			// Bond Markets Unit European Unit of Account 17 (E.U.A.-17)
			"XBD",
			// ADB Unit of Account
			"XUA",
		];

		HashSet<GenericEnumMember> generatedCurrencies = [];
		generatedCurrencies.Add(new("NotACurrency", "-1", "/// <summary>Not a currency.</summary>", -1));
		generatedCurrencies.Add(new("Uninitialized", "0", "/// <summary>Not a currency, but instead an uninitialized variable.</summary>", -1));

		foreach (IGrouping<String, CurrencyEntry> currencyEntries in _currencies.DistinctBy(cur => cur.Ccy).GroupBy(GetCurrencyName)) {
			Boolean hasMultiple = currencyEntries.Count() > 1;
			foreach (CurrencyEntry currencyEntry in currencyEntries) {
				if (currencyEntry.Ccy == null || codesToIgnore.Contains(currencyEntry.Ccy.ToUpperInvariant()))
					continue;

				numCurrencies++;

				generatedCurrencies.Add(new GenericEnumMember(currencyEntry.Ccy, currencyEntry.CcyNm.Text, currencyEntry.CcyNbr, $"{GetCurrencyName(currencyEntry)}{(hasMultiple ? $"_{currencyEntry.Ccy}" : String.Empty)}", CalculateFrom3CodeAndShort(currencyEntry.Ccy, currencyEntry.CcyNbr)));
			}
		}

		HashSet<GenericEnumMember> missingCurrencies = GetCurrentlyAvailableCurrencies();
		missingCurrencies.ExceptWith(generatedCurrencies);
		// Exception on missing obsoletion is beneficial
		missingCurrencies.ForEach(c => c.ObsoletionNotice = Obsoletion.Currency[c.EnumName]);
		generatedCurrencies.UnionWith(missingCurrencies);
		
		generatedCurrencies.OrderBy(c => c.Order).ThenBy(c => c.EnumName).ForEach(c => c.WriteTo(sb));

		sb.AppendLine("}");
		sb.AppendLine("#endregion");
		Console.WriteLine($"{numCurrencies} live and {missingCurrencies.Count} obsolete currencies created.");
		if (fileoutput != null)
			File.WriteAllText(fileoutput, sb.ToString().Trim(), new UTF8Encoding(false));
	}

	private static String GetCurrencyName(CurrencyEntry currencyEntry) {
		String name = Regex.Replace(currencyEntry.CcyNm.Text, @"[-’ \.]+", "");
		name = Regex.Replace(name, @"\([A-Z]+\)$", "", RegexOptions.Singleline);

		if (currencyEntry.Ccy == "XTS")
			name = "TestCurrency";
		if (currencyEntry.Ccy == "XXX")
			name = "NoCurrencyInvolved";
		return BaseGenerator.RemoveDiacritics(name);
	}

	private HashSet<GenericEnumMember> GetCurrentlyAvailableCurrencies() {
		XmlDocument isoEnumsDocumentation = new();
		isoEnumsDocumentation.PreserveWhitespace = true;
		XmlReaderSettings readerSettings = new() {
			IgnoreWhitespace = false,
		};
		using FileStream fileStream = File.OpenRead("IsoEnums.xml");
		using XmlReader reader = XmlReader.Create(fileStream, readerSettings);
		isoEnumsDocumentation.Load(reader);

		HashSet<GenericEnumMember> knownCurrencies = [];
		const String nodeNamePrefix = "F:IsoEnums.Iso4217.Currency.";
		foreach (XmlElement node in isoEnumsDocumentation.SelectNodes($"/doc/members/member[starts-with(@name,'{nodeNamePrefix}')]")!) {
			String documentation = String.Join(Environment.NewLine, node.InnerXml.Trim().Split(['\r','\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(line => $"/// {line}"));
			String nodeName = node.GetAttribute("name").Substring(nodeNamePrefix.Length);
			Currency currency = Enum.Parse<Currency>(nodeName);
			knownCurrencies.Add(new GenericEnumMember(nodeName, ((Int32)currency).ToString(BaseGenerator.NumberFormatForEnums), documentation));
		}

		return knownCurrencies;
	}

	// 26 letters need 5 bytes to encode
	// byte 0 is reserved and always 0
	// UInt16 need 2 bytes to encode
	private static String CalculateFrom3CodeAndShort(String code3, UInt16 num) {
		ArgumentException.ThrowIfNullOrEmpty(code3);
		Int32 idAsInteger = num;
		
		Byte[] id3Bytes = Encoding.ASCII.GetBytes(code3.ToLowerInvariant());
		idAsInteger |= ((id3Bytes[0] - (Byte)'a') & 0b11111) << 17;
		idAsInteger |= ((id3Bytes[1] - (Byte)'a') & 0b11111) << 22;
		idAsInteger |= ((id3Bytes[2] - (Byte)'a') & 0b11111) << 27;

		return idAsInteger.ToString("N0", BaseGenerator.NumberFormatForEnums);
	}
}