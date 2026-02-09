namespace IsoEnums.Generator;

using System.Text;
using System.Text.RegularExpressions;
using IsoEnums.Iso639;
using Neco.Common.Extensions;

internal partial class LanguageGenerator(List<Iso639Entry> Iso639Entries, List<Iso639NameEntry> Iso639NameEntries, List<Iso6392Entry> Iso6392Entries) {
	public void Generate639LanguageEnum(String? fileoutput) {
		HashSet<GenericEnumMember> generatedLanguages = [];
		generatedLanguages.Add(new("Uninitialized", "0", "///<summary>Not a language, but instead an uninitialized variable</summary>", -1));

		IOrderedEnumerable<(Iso639Entry entry, String)> nonExtinctLanguages = Iso639Entries.Where(entry => !entry.Language_Type.Equals("E", StringComparison.OrdinalIgnoreCase)).Select(entry => (entry, GetEnumName(entry))).OrderBy(tpl => tpl.Item2);
		Int32 numLanguages = 0;
		StringBuilder docBuilder = new();
		HashSet<String> knownLanguageNames = new(StringComparer.OrdinalIgnoreCase);
		HashSet<String> knownLanguageCodes = new(StringComparer.OrdinalIgnoreCase);
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
				docBuilder.Clear();
				docBuilder.AppendLine("/// <summary>");
				docBuilder.AppendLine($"/// <para><a href=\"https://en.wikipedia.org/wiki/ISO_639:{iso639Entry.Id}\">{iso639Entry.Ref_Name}</a></para>");
				docBuilder.AppendLine($"/// {scope}{type}Language");
				docBuilder.AppendLine("/// </summary>");
				docBuilder.Append($"/// <value>id={iso639Entry.Id}");
				knownLanguageNames.Add(iso639Entry.Ref_Name);
				knownLanguageCodes.Add(iso639Entry.Id);
				if (!String.IsNullOrWhiteSpace(iso639Entry.Part1)) {
					docBuilder.Append($"; 2code={iso639Entry.Part1}");
					knownLanguageCodes.Add(iso639Entry.Part1);
				}

				knownLanguageCodes.Add(iso639Entry.Part2b);
				knownLanguageCodes.Add(iso639Entry.Part2t);
				String otherIds = String.Join(", ", Enumerable.Distinct([iso639Entry.Part2b, iso639Entry.Part2t]).Where(id => id != String.Empty && id != iso639Entry.Id));
				if (!String.IsNullOrEmpty(otherIds))
					docBuilder.Append($"; other={otherIds}");
				docBuilder.AppendLine("</value>");

				List<String> otherNames = Iso639NameEntries.Where(nameEntry => nameEntry.Id == iso639Entry.Id && nameEntry.Print_Name != iso639Entry.Ref_Name).Select(nameEntry => nameEntry.Print_Name).ToList();
				otherNames.ForEach(n => knownLanguageNames.Add(n));
				String alsoKnownAs = String.Join(", ", otherNames);
				if (!String.IsNullOrEmpty(alsoKnownAs))
					docBuilder.AppendLine($"/// <remarks>Also known as: {alsoKnownAs}</remarks>");

				generatedLanguages.Add(new(name, value, docBuilder.ToString().Trim()));
				knownLanguageNames.Add(name);
			}
		}

		foreach (Iso6392Entry iso6392Entry in Iso6392Entries) {
			List<String> languageNames = iso6392Entry.EnglishName.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
			IEnumerable<String> codes = new List<String>([iso6392Entry.Alpha3, iso6392Entry.Alpha2, iso6392Entry.Alpha3Alt])
				.Where(s => !String.IsNullOrWhiteSpace(s));
			if (knownLanguageNames.ContainsAny(languageNames) || knownLanguageCodes.ContainsAny(codes)) {
				Console.WriteLine($"Skipping ISO639-2 entry for {iso6392Entry.EnglishName}");
			} else {
				String mainName = languageNames.First();

				docBuilder.Clear();
				docBuilder.AppendLine("/// <summary>");
				docBuilder.AppendLine($"/// <para><a href=\"https://en.wikipedia.org/wiki/ISO_639:{iso6392Entry.Alpha3}\">{mainName}</a></para>");
				docBuilder.AppendLine("/// </summary>");
				docBuilder.Append($"/// <value>id={iso6392Entry.Alpha3}");
				if (!String.IsNullOrWhiteSpace(iso6392Entry.Alpha2)) {
					docBuilder.Append($"; 2code={iso6392Entry.Alpha2}");
				}

				if (!String.IsNullOrEmpty(iso6392Entry.Alpha3Alt) && !String.Equals(iso6392Entry.Alpha3Alt, iso6392Entry.Alpha3, StringComparison.OrdinalIgnoreCase))
					docBuilder.Append($"; other={iso6392Entry.Alpha3Alt}");
				docBuilder.AppendLine("</value>");

				List<String> otherNames = Iso639NameEntries.Where(nameEntry => nameEntry.Id == iso6392Entry.Alpha3 && nameEntry.Print_Name != mainName).Select(nameEntry => nameEntry.Print_Name).ToList();
				String alsoKnownAs = String.Join(", ", otherNames);
				if (!String.IsNullOrEmpty(alsoKnownAs))
					docBuilder.AppendLine($"/// <remarks>Also known as: {alsoKnownAs}</remarks>");

				generatedLanguages.Add(new(NormalizeName(iso6392Entry.EnglishName), BaseGenerator.CalculateFrom3And2Code(iso6392Entry.Alpha3, iso6392Entry.Alpha2), docBuilder.ToString().Trim()));
				++numLanguages;
			}
		}

		HashSet<GenericEnumMember> missingLanguages = BaseGenerator.GetCurrentlyAvailableEnumMembers<Language>();
		missingLanguages.ExceptWith(generatedLanguages);
		// Exception on missing obsoletion is beneficial
		missingLanguages.ForEach(c => c.ObsoletionNotice = Obsoletion.Language[c.EnumName]);
		generatedLanguages.UnionWith(missingLanguages);

		StringBuilder sb = new();
		BaseGenerator.AppendDefaultHeader(sb);
		sb.AppendLine("namespace IsoEnums.Iso639;");
		sb.AppendLine("#region Designer generated code");
		sb.AppendLine("public enum Language {");

		generatedLanguages.OrderBy(e => e.Order).ThenBy(e => e.EnumName).ForEach(e => e.WriteTo(sb));

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
	private static String CalculateEnumValue(Iso639Entry entry) => BaseGenerator.CalculateFrom3And2Code(entry.Id.ToLowerInvariant(), entry.Part1.ToLowerInvariant());

	private String GetEnumName(Iso639Entry entry) {
		Iso639NameEntry? nameEntry = Iso639NameEntries.FirstOrDefault(nameEntry => nameEntry.Id == entry.Id && nameEntry.Print_Name == entry.Ref_Name);
		String rawName = nameEntry?.Inverted_Name ?? entry.Ref_Name;
		return NormalizeName(rawName);
	}

	private static String NormalizeName(String rawName) {
		//normalize
		String name = BaseGenerator.RemoveDiacritics(rawName);

		name = EmptyReplacementRegex().Replace(name, String.Empty);
		name = UnderscoreReplacementRegex().Replace(name, "_");
		return name.Trim('_', ' ');
	}

	[GeneratedRegex("[^a-zA-Z]+")]
	private static partial Regex UnderscoreReplacementRegex();

	[GeneratedRegex("[']+")]
	private static partial Regex EmptyReplacementRegex();
}