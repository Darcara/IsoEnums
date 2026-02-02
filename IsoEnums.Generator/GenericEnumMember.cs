namespace IsoEnums.Generator;

using System.Text;

internal sealed class GenericEnumMember : IEquatable<GenericEnumMember> {
	public Int32 Order { get; }
	public String EnumName { get; }

	public String EnumValue { get; }

	public String Documentation { get; }
	public String? ObsoletionNotice { get; set; }

	public GenericEnumMember(String enumName, String enumValue, String documentation, Int32 order = 0) {
		EnumName = enumName;
		EnumValue = enumValue;
		Documentation = documentation;
		Order = order;
	}

	public GenericEnumMember(String id, String summary, UInt16 numeric, String enumName, String enumValue) {
		EnumName = enumName;
		EnumValue = enumValue;
		Documentation = $"/// <summary>{Environment.NewLine}/// <para>{summary}</para>{Environment.NewLine}/// </summary>{Environment.NewLine}/// <value>id={id}, numeric={numeric}</value>";
	}

	#region Equality members

	/// <inheritdoc />
	public Boolean Equals(GenericEnumMember? other) {
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;
		return String.Equals(EnumName, other.EnumName, StringComparison.Ordinal);
	}

	/// <inheritdoc />
	public override Boolean Equals(Object? obj) => ReferenceEquals(this, obj) || obj is GenericEnumMember other && Equals(other);

	/// <inheritdoc />
	public override Int32 GetHashCode() => HashCode.Combine(EnumName);

	public static Boolean operator ==(GenericEnumMember? left, GenericEnumMember? right) => Equals(left, right);

	public static Boolean operator !=(GenericEnumMember? left, GenericEnumMember? right) => !Equals(left, right);

	#endregion

	public void WriteTo(StringBuilder sb) {
		sb.AppendLine(Documentation);
		
		if(!String.IsNullOrEmpty(ObsoletionNotice))
			sb.AppendLine($"[Obsolete(\"{ObsoletionNotice}\")]");
		
		sb.Append(EnumName);
		sb.Append('=');
		sb.Append(EnumValue);
		sb.AppendLine(",");
	}
}