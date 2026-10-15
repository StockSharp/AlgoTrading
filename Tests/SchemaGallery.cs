namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// The schema gallery as it lies on disk: one folder per example, holding the schema file, its picture
/// and the localized descriptions.
/// </summary>
public static class SchemaGallery
{
	/// <summary>Folders under the gallery that hold tooling rather than an example.</summary>
	private const string _toolsPrefix = "_";

	/// <summary>
	/// Examples to run, by folder name, comma separated. Replaying the whole gallery takes minutes, which
	/// is too slow to work against while an example is being written, and the test adapter cannot select
	/// one row of a data-driven test.
	/// </summary>
	private static readonly string[] _only = (Environment.GetEnvironmentVariable("SCHEMA_GALLERY_ONLY") ?? string.Empty)
		.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	/// <summary>Languages every example is described in, beside the English original.</summary>
	public static readonly string[] Translations = ["ru", "zh", "es", "de", "pt", "ja"];

	/// <summary>Full path of the gallery folder.</summary>
	public static string Root { get; } = FindRoot("Gallery", "Schemas");

	/// <summary>Full path of the lessons folder. A lesson is a schema too, and is held to the same load.</summary>
	public static string EducationRoot { get; } = FindRoot("Education");

	/// <summary>Every example folder of the gallery.</summary>
	public static IEnumerable<string> EnumerateFolders()
	{
		var folders = Folders(Root);

		return _only.Length == 0
			? folders
			: folders.Where(dir => _only.Contains(Path.GetFileName(dir), StringComparer.OrdinalIgnoreCase));
	}

	/// <summary>Every lesson folder.</summary>
	public static IEnumerable<string> EnumerateLessonFolders()
		=> Folders(EducationRoot);

	/// <summary>Every schema file of an example folder, whatever each one is called.</summary>
	public static IEnumerable<string> SchemaFiles(string folder)
		=> Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly).OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Every example, as a name and the schema file it is in. An example is named by its folder, not by
	/// the file inside it: what a schema file is called is nobody's business but the folder's, and a
	/// folder holding more than one names each by the file so the two can be told apart.
	/// </summary>
	public static IEnumerable<(string name, string fileName)> EnumerateSchemas()
	{
		foreach (var folder in EnumerateFolders())
		{
			var files = SchemaFiles(folder).ToArray();
			var folderName = Path.GetFileName(folder);

			foreach (var file in files)
			{
				yield return files.Length == 1
					? (folderName, file)
					: ($"{folderName}/{Path.GetFileNameWithoutExtension(file)}", file);
			}
		}
	}

	/// <summary>Every lesson schema. A lesson folder may hold several, so each is named by its file.</summary>
	public static IEnumerable<(string name, string fileName)> EnumerateLessons()
	{
		foreach (var folder in EnumerateLessonFolders())
		{
			foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
				yield return ($"{Path.GetFileName(folder)}/{Path.GetFileNameWithoutExtension(file)}", file);
		}
	}

	private static IEnumerable<string> Folders(string root)
		=> Directory
			.EnumerateDirectories(root)
			.Where(dir => !Path.GetFileName(dir).StartsWith(_toolsPrefix, StringComparison.Ordinal))
			.OrderBy(dir => dir, StringComparer.OrdinalIgnoreCase);

	private static string FindRoot(params string[] parts)
	{
		// The tests run from the build output, so the gallery is found by walking up to the repository
		// root rather than by counting folders from the assembly.
		var dir = new DirectoryInfo(AppContext.BaseDirectory);

		while (dir is not null)
		{
			if (File.Exists(Path.Combine(dir.FullName, "AlgoTrading.slnx")))
			{
				var root = Path.Combine([dir.FullName, "Designer", .. parts]);

				return Directory.Exists(root)
					? root
					: throw new DirectoryNotFoundException($"Nothing at '{root}'.");
			}

			dir = dir.Parent;
		}

		throw new DirectoryNotFoundException($"No repository root above '{AppContext.BaseDirectory}'.");
	}
}
