namespace IsoEnums.Generator;

using System.Collections.Frozen;

public static class Obsoletion {
	public static readonly FrozenDictionary<String, String> Currency = new Dictionary<String, String>(){
		{"BulgarianLev", "Replaced by Euro starting 2026-01-01"},
	}.ToFrozenDictionary(StringComparer.Ordinal);
	
	public static readonly FrozenDictionary<String, String> Country = new Dictionary<String, String>(){
	}.ToFrozenDictionary(StringComparer.Ordinal);
	
	public static readonly FrozenDictionary<String, String> Language = new Dictionary<String, String>(){
	}.ToFrozenDictionary(StringComparer.Ordinal);
}