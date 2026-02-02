namespace IsoEnums.Test;

[TestFixture]
public class GeoDataInfoTests {
	[Test]
	public void InfoIsAvailable() {
		Assert.That(GeoDataInfo.LastUpdated, Is.GreaterThan(DateOnly.MinValue));
	}
}