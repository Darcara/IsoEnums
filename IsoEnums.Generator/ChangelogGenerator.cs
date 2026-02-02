namespace IsoEnums.Generator;

public class ChangelogGenerator {
	public List<ChangelogEntry> Entries { get; } = [];
}

public class ChangelogEntry {
	public ChangelogType Type {get;}
	public String MarkdownDescription { get; }

	public ChangelogEntry(ChangelogType type, String markdownDescription) {
		Type = type;
		MarkdownDescription = markdownDescription;
	}
}

public enum ChangelogType {
	Add,
	Change,
	Remove,
}