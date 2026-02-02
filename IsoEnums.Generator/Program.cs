namespace IsoEnums.Generator;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CsvHelper;
using CsvHelper.Configuration;
using IsoEnums.Iso4217;
using IsoEnums.Iso639;
using Neco.Common;

public static partial class Program {
	public static async Task Main(String[] args) {
		await DownloadData();
		ParseLanguageData();
		ParseCountryData();
		ParseCurrencyData();

		Generate639LanguageEnum("../../../../IsoEnums/Iso639/Language.cs");
		Generate3166CountryEnum("../../../../IsoEnums/Iso3166/Country.cs");
		CurrencyGenerator cg = new(Currencies);
		cg.Generate4217CurrencyEnum("../../../../IsoEnums/Iso4217/Currency.cs");

		UpdateStaticInformation("../../../../IsoEnums/GeoDataInfo.cs");
	}

	private static List<CurrencyEntry> Currencies { get; set; } = null!;
	private static List<Iso639Entry> Iso639Entries { get; set; } = null!;
	private static List<Iso639NameEntry> Iso639NameEntries { get; set; } = null!;

	private static List<DatasetsCountryCodesEntry> DatasetsCountryCodesEntries { get; set; } = null!;
	private static List<GeonamesCountryInfoEntry> GeonamesCountryInfoEntries { get; set; } = null!;

	private static async Task DownloadData() {
		using HttpClient client = new();
		Predicate<FileInfo> maxAge = Helper.MaxAge(TimeSpan.FromDays(7));
		// Predicate<FileInfo> maxAge = Helper.MaxAge(TimeSpan.MaxValue);

		// await Helper.DownloadFile(client, "https://download.geonames.org/export/dump/allCountries.zip", "data/geo/allCountries.zip");
		// await Helper.DownloadFile(client, "https://download.geonames.org/export/dump/alternateNamesV2.zip", "data/geo/alternateNamesV2.zip");
		await Helper.DownloadFile(client, "https://download.geonames.org/export/dump/countryInfo.txt", "data/geo/countryInfo.txt", maxAge);
		await Helper.DownloadFile(client, "https://download.geonames.org/export/dump/hierarchy.zip", "data/geo/hierarchy.zip", maxAge);
		await Helper.DownloadFile(client, "https://download.geonames.org/export/dump/adminCode5.zip", "data/geo/adminCode5.zip", maxAge);
		await Helper.DownloadFile(client, "https://download.geonames.org/export/dump/shapes_all_low.zip", "data/geo/countryShapes.zip", maxAge);
		await Helper.DownloadFile(client, "https://download.geonames.org/export/dump/shapes_simplified_low.json.zip", "data/geo/countryShapesSimplified.zip", maxAge);
		await Helper.DownloadFile(client, "https://download.geonames.org/export/dump/iso-languagecodes.txt", "data/geo/isoLanguageCodes.zip", maxAge);

		// https://www.naturalearthdata.com/downloads/110m-cultural-vectors/110m-admin-0-countries/
		await Helper.DownloadFile(client, "https://naciscdn.org/naturalearth/110m/cultural/ne_110m_admin_0_countries.zip", "data/geo/countryBoundaries.zip", maxAge);
		await Helper.DownloadFile(client, "https://raw.githubusercontent.com/datasets/country-codes/refs/heads/main/data/country-codes.csv", "data/geo/countryCodes.csv", maxAge);
		await Helper.DownloadFile(client, "https://raw.githubusercontent.com/unicode-org/cldr/refs/heads/main/common/supplemental/supplementalData.xml", "data/geo/unicodeSupplementalData.xml", maxAge);

		await Helper.DownloadFile(client, "https://www.loc.gov/standards/iso639-2/ISO-639-2_utf-8.txt", "data/geo/ISO-639-2_utf-8.txt", maxAge);
		// See here for new Link https://iso639-3.sil.org/code_tables/download_tables#Complete%20Code%20Tables
		await Helper.DownloadFile(client, "https://iso639-3.sil.org/sites/iso639-3/files/downloads/iso-639-3_Code_Tables_20241010.zip", "data/geo/ISO-639-3.zip", maxAge);
		await Helper.DownloadFile(client, "https://www.six-group.com/dam/download/financial-information/data-center/iso-currrency/lists/list-one.xml", "data/geo/ISO-4217-currency.xml", maxAge);
	}

	private static void ParseCountryData() {
		CsvConfiguration countryCodesConfig = new(CultureInfo.InvariantCulture) {
			MemberTypes = MemberTypes.Properties | MemberTypes.Fields,
			HasHeaderRecord = true,
			PrepareHeaderForMatch = args => Regex.Replace(args.Header, @"[ \-\(\)]+", "_").TrimEnd('_'),
		};
		using (CsvReader csvReader = new(File.OpenText("data/geo/countryCodes.csv"), countryCodesConfig, leaveOpen: false)) {
			DatasetsCountryCodesEntries = csvReader.GetRecords<DatasetsCountryCodesEntry>().ToList();
		}

		CsvConfiguration countryInfoConfig = new(CultureInfo.InvariantCulture) {
			MemberTypes = MemberTypes.Properties | MemberTypes.Fields,
			HasHeaderRecord = true,
			Comment = '#',
			AllowComments = true,
			Delimiter = "\t",
			PrepareHeaderForMatch = args => Regex.Replace(args.Header, @"[ \-\(\)]+", "_").TrimEnd('_'),
		};

		String countryInfoData = File.ReadAllText("data/geo/countryInfo.txt").Replace("\n#ISO\tISO3", "\nISO\tISO3");
		using (CsvReader csvReader = new(new StringReader(countryInfoData), countryInfoConfig, leaveOpen: false)) {
			GeonamesCountryInfoEntries = csvReader.GetRecords<GeonamesCountryInfoEntry>().ToList();
		}
	}

	private static void ParseCurrencyData() {
		using StreamReader streamReader = File.OpenText("data/geo/ISO-4217-currency.xml");
		XmlSerializer serializer = new(typeof(ISO4217));
		ISO4217? currencies = (ISO4217?)serializer.Deserialize(streamReader);
		if (currencies == null) return;

		Currencies = currencies.CcyTbl.CcyNtry;
	}

	private static void Generate3166CountryEnum(String? fileoutput) {
		StringBuilder sb = new();
		BaseGenerator.AppendDefaultHeader(sb);
		sb.AppendLine("namespace IsoEnums.Iso3166;");
		sb.AppendLine("#region Designer generated code");
		sb.AppendLine("public enum Country {");
		sb.AppendLine("///<summary>Not a country.</summary>");
		sb.AppendLine("NotACountry=-1,");
		sb.AppendLine("///<summary>Not a country, but instead an uninitialized variable.</summary>");
		sb.AppendLine("Uninitialized=0,");

		Int32 numCountries = 0;
		foreach (GeonamesCountryInfoEntry geonamesEntry in GeonamesCountryInfoEntries.OrderBy(info => info.Country)) {
			++numCountries;
			DatasetsCountryCodesEntry? datasetEntry = DatasetsCountryCodesEntries.FirstOrDefault(c => c.ISO3166_1_Alpha_3 == geonamesEntry.ISO3);
			String value = CalculateEnumValue(geonamesEntry);
			sb.AppendLine("/// <summary>");
			sb.AppendLine($"/// <para><a href=\"https://en.wikipedia.org/wiki/ISO_3166-2:{geonamesEntry.ISO}\">{geonamesEntry.Country}</a></para>");
			sb.AppendLine($"/// <para>");
			if (TryNotNullOrEmpty(out String? location, datasetEntry?.Intermediate_Region_Name, datasetEntry?.Sub_region_Name, datasetEntry?.Region_Name))
				sb.AppendLine($"/// Located in: {location}<br/>");
			if (TryNotNullOrEmpty(out String? capital, geonamesEntry.Capital))
				sb.AppendLine($"/// Capital: {capital}<br/>");
			if (TryNotNullOrEmpty(out String? languages, geonamesEntry.Languages))
				sb.AppendLine($"/// Languages: {String.Join(", ", languages.Split(',').Select(str => LanguageHelper.GetLanguageByCode(str) == Language.Undetermined ? str : $"<see cref=\"Iso639.Language.{LanguageHelper.GetLanguageByCode(str)}\">{LanguageHelper.GetLanguageByCode(str)}</see>"))}<br/>");
			if (TryNotNullOrEmpty(out String? currency, geonamesEntry.CurrencyName)) {
				String currencyInfo = $"{currency} ({geonamesEntry.CurrencyCode})";
				if (CurrencyHelper.GetCurrencyBy3Code(geonamesEntry.CurrencyCode) != Currency.NotACurrency)
					currencyInfo = $"<see cref=\"Iso4217.Currency.{CurrencyHelper.GetCurrencyBy3Code(geonamesEntry.CurrencyCode)}\" >{CurrencyHelper.GetCurrencyBy3Code(geonamesEntry.CurrencyCode)} ({geonamesEntry.CurrencyCode})</see>";
				sb.AppendLine($"/// Currency: {currencyInfo}<br/>");
			}

			sb.AppendLine($"/// TopLevelDomain: {geonamesEntry.tld}");
			sb.AppendLine($"/// </para>");
			sb.AppendLine("/// </summary>");
			sb.Append($"/// <value>id={geonamesEntry.ISO3}, 2code={geonamesEntry.ISO}, numeric={geonamesEntry.ISO_Numeric}");
			sb.AppendLine("</value>");
			String name = Regex.Replace(geonamesEntry.Country, @"[- \.,]+", "");
			sb.AppendLine($"{name}={value},");

			sb.AppendLine();
		}

		sb.AppendLine("}");
		sb.AppendLine("#endregion");
		Console.WriteLine($"{numCountries} countries created.");
		if (fileoutput != null)
			File.WriteAllText(fileoutput, sb.ToString().Trim(), new UTF8Encoding(false));
	}

	private static Boolean TryNotNullOrEmpty([NotNullWhen(true)] out String? val, params String?[] data) {
		foreach (String? s in data) {
			if (!String.IsNullOrEmpty(s)) {
				val = s;
				return true;
			}
		}

		val = null;
		return false;
	}

	private static String CalculateEnumValue(GeonamesCountryInfoEntry geonamesEntry) => CalculateFrom3And2Code(geonamesEntry.ISO3.ToLowerInvariant(), geonamesEntry.ISO.ToLowerInvariant());

	private static void ParseLanguageData() {
		// ISO-639-3.zip
		using ZipArchive archive = new(File.OpenRead("data/geo/ISO-639-3.zip"), ZipArchiveMode.Read, false);
		CsvConfiguration config = new(CultureInfo.InvariantCulture) {
			Delimiter = "\t",
		};
		using (CsvReader csvReader = new(new StreamReader(archive.Entries.First(entry => entry.Name.EndsWith("iso-639-3.tab", StringComparison.OrdinalIgnoreCase)).Open(), Encoding.UTF8, false, leaveOpen: false), config, leaveOpen: false)) {
			Iso639Entries = csvReader.GetRecords<Iso639Entry>().ToList();
		}

		using (CsvReader csvReader = new(new StreamReader(archive.Entries.First(entry => entry.Name.EndsWith("iso-639-3_Name_Index.tab", StringComparison.OrdinalIgnoreCase)).Open(), Encoding.UTF8, false, leaveOpen: false), config, leaveOpen: false)) {
			Iso639NameEntries = csvReader.GetRecords<Iso639NameEntry>().ToList();
		}
	}

	private static void Generate639LanguageEnum(String? fileoutput) {
		StringBuilder sb = new();
		BaseGenerator.AppendDefaultHeader(sb);
		sb.AppendLine("namespace IsoEnums.Iso639;");
		sb.AppendLine("#region Designer generated code");
		sb.AppendLine("public enum Language {");
		sb.AppendLine("///<summary>Not a language, but instead an uninitialized variable</summary>");
		sb.AppendLine("Uninitialized=0,");

		IOrderedEnumerable<(Iso639Entry entry, String)> nonExtinctLanguages = Iso639Entries.Where(entry => !entry.Language_Type.Equals("E", StringComparison.OrdinalIgnoreCase)).Select(entry => (entry, GetEnumName(entry))).OrderBy(tpl => tpl.Item2);
		Int32 numLanguages = 0;
		foreach (IGrouping<String, (Iso639Entry entry, String)> groups in nonExtinctLanguages.GroupBy(tpl => tpl.Item2)) {
			Boolean hasMultiple = groups.Count() > 1;
			foreach ((Iso639Entry iso639Entry, String maybeDuplicateName) in groups) {
				++numLanguages;
				String name = hasMultiple ? $"{maybeDuplicateName}_{iso639Entry.Id}" : maybeDuplicateName;
				String scope = iso639Entry.Scope switch {
					"I" => "Individual ",
					"M" => "Meta ",
					_ => String.Empty,
				};

				String type = iso639Entry.Language_Type switch {
					"A" => "Ancient ",
					"E" => "Extinct ",
					"C" => "Constructed ",
					"L" => String.Empty,
					_ => String.Empty,
				};

				String value = CalculateEnumValue(iso639Entry);
				sb.AppendLine("/// <summary>");
				sb.AppendLine($"/// <para><a href=\"https://en.wikipedia.org/wiki/ISO_639:{iso639Entry.Id}\">{iso639Entry.Ref_Name}</a></para>");
				sb.AppendLine($"/// {scope}{type}Language");
				sb.AppendLine("/// </summary>");
				sb.Append($"/// <value>id={iso639Entry.Id}");
				if (!String.IsNullOrWhiteSpace(iso639Entry.Part1))
					sb.Append($"; 2code={iso639Entry.Part1}");
				String otherIds = String.Join(", ", Enumerable.Distinct([iso639Entry.Part2b, iso639Entry.Part2t]).Where(id => id != String.Empty && id != iso639Entry.Id));
				if (!String.IsNullOrEmpty(otherIds))
					sb.Append($"; other={otherIds}");
				sb.AppendLine("</value>");

				String alsoKnownAs = String.Join(", ", Iso639NameEntries.Where(nameEntry => nameEntry.Id == iso639Entry.Id && nameEntry.Print_Name != iso639Entry.Ref_Name).Select(nameEntry => nameEntry.Print_Name));
				if (!String.IsNullOrEmpty(alsoKnownAs))
					sb.AppendLine($"/// <remarks>Also known as: {alsoKnownAs}</remarks>");
				sb.AppendLine($"{name}={value},");

				sb.AppendLine();
			}
		}


		sb.AppendLine("}");
		sb.AppendLine("#endregion");
		Console.WriteLine($"{numLanguages} languages created.");
		if (fileoutput != null)
			File.WriteAllText(fileoutput, sb.ToString().Trim(), new UTF8Encoding(false));
	}

	// 26 letters need 5 bytes to encode
	// byte 0 is reserved and always 1
	// 3-letter Id of ISO639-3 needs 15 bytes
	// byte 16 is reserved and only 1 when a 2-letter Part1 is available
	// 2-letter Part1 of ISO639-1 needs 10 Bytes, but we will start at byte 17
	private static String CalculateEnumValue(Iso639Entry entry) => CalculateFrom3And2Code(entry.Id.ToLowerInvariant(), entry.Part1.ToLowerInvariant());

	private static String CalculateFrom3And2Code(String code3, String? code2) {
		ArgumentException.ThrowIfNullOrEmpty(code3);
		Int32 idAsInteger = 1;
		Byte[] id3Bytes = Encoding.ASCII.GetBytes(code3.ToLowerInvariant());
		idAsInteger |= ((id3Bytes[0] - (Byte)'a') & 0b11111) << 1;
		idAsInteger |= ((id3Bytes[1] - (Byte)'a') & 0b11111) << 6;
		idAsInteger |= ((id3Bytes[2] - (Byte)'a') & 0b11111) << 11;

		if (!String.IsNullOrWhiteSpace(code2)) {
			Byte[] id2Bytes = Encoding.ASCII.GetBytes(code2.ToLowerInvariant());

			idAsInteger |= 1 << 16;
			idAsInteger |= ((id2Bytes[0] - (Byte)'a') & 0b11111) << 17;
			idAsInteger |= ((id2Bytes[1] - (Byte)'a') & 0b11111) << 22;
		}


		return idAsInteger.ToString("N0", new NumberFormatInfo() { NumberGroupSeparator = "_" });
	}

	private static String GetEnumName(Iso639Entry entry) {
		Iso639NameEntry? nameEntry = Iso639NameEntries.FirstOrDefault(nameEntry => nameEntry.Id == entry.Id && nameEntry.Print_Name == entry.Ref_Name);
		String rawName = nameEntry?.Inverted_Name ?? entry.Ref_Name;

		//normalize
		String name = BaseGenerator.RemoveDiacritics(rawName);

		name = EmptyReplacementRegex().Replace(name, String.Empty);
		name = UnderscoreReplacementRegex().Replace(name, "_");
		return name.Trim('_', ' ');
	}

	private static void UpdateStaticInformation(String file) {
		ArgumentNullException.ThrowIfNull(file);
		if (!File.Exists(file)) throw new FileNotFoundException("Unable to update static information", file);

		String staticData = File.ReadAllText(file, MagicNumbers.Utf8NoBom);

		DateOnly now = DateOnly.FromDateTime(DateTime.UtcNow);
		staticData = Regex.Replace(staticData, @"LastUpdated\s*=\s*new\s*\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*", $"LastUpdated = new({now.Year},{now.Month},{now.Day}");
		
		File.WriteAllText(file, staticData, MagicNumbers.Utf8NoBom);
	}

	[GeneratedRegex("[^a-zA-Z]+")]
	private static partial Regex UnderscoreReplacementRegex();

	[GeneratedRegex("[']+")]
	private static partial Regex EmptyReplacementRegex();
}