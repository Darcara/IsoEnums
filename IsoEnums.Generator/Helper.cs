namespace IsoEnums.Generator;

using System.Net.Http;
using System.Threading.Tasks;

internal static class Helper {
	internal static Task DownloadFile(HttpClient client, String uri, String destination, Predicate<FileInfo>? destinationPredicate = null) => DownloadFile(client, new Uri(uri), destination, destinationPredicate);

	internal static async Task DownloadFile(HttpClient client, Uri uri, String destination, Predicate<FileInfo>? destinationPredicate = null) {
		FileInfo fi = new(destination);
		if (fi.Exists && destinationPredicate != null && !destinationPredicate(fi)) return;

		Console.WriteLine($"Downloading {destination} from {uri}");
		String targetFileAbs = Path.GetFullPath(destination);
		Directory.CreateDirectory(Path.GetDirectoryName(targetFileAbs) ?? ".");
		String tempFile = targetFileAbs + ".tmp";
		await using (Stream netStream = await client.GetStreamAsync(uri).ConfigureAwait(false)) {
			await using FileStream fileStream = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
			await netStream.CopyToAsync(fileStream).ConfigureAwait(false);
		}

		File.Move(tempFile, targetFileAbs, true);
	}

	public static Predicate<FileInfo> MaxAge(TimeSpan maxAge, TimeProvider? time = null) {
		time ??= TimeProvider.System;
		return fileInfo => {
			DateTimeOffset lastWriteTime = fileInfo.LastWriteTimeUtc > fileInfo.CreationTimeUtc ? fileInfo.LastWriteTimeUtc : fileInfo.CreationTimeUtc;
			DateTimeOffset now = time.GetUtcNow();
			if (lastWriteTime > now) lastWriteTime = DateTimeOffset.MinValue;

			return now - lastWriteTime > maxAge;
		};
	}
}