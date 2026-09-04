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

	/// <summary>Languages every example is described in, beside the English original.</summary>
	public static readonly string[] Translations = ["ru", "zh", "es", "de", "pt", "ja"];

	/// <summary>Full path of the gallery folder.</summary>
	public static string Root { get; } = FindRoot("Gallery", "Schemas");

	/// <summary>Full path of the lessons folder. A lesson is a schema too, and is held to the same load.</summary>
	public static string EducationRoot { get; } = FindRoot("Education");

	/// <summary>Every example folder of the gallery.</summary>
	public static IEnumerable<string> EnumerateFolders()
		=> Folders(Root);

	/// <summary>Every lesson folder.</summary>
	public static IEnumerable<string> EnumerateLessonFolders()
		=> Folders(EducationRoot);

	/// <summary>The schema file of an example folder, or null when the folder holds none.</summary>
	public static string FindSchemaFile(string folder)
		=> Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault();

	/// <summary>Every example, as the folder name and the schema file inside it.</summary>
	public static IEnumerable<(string name, string fileName)> EnumerateSchemas()
	{
		foreach (var folder in EnumerateFolders())
		{
			var file = FindSchemaFile(folder);

			if (file is not null)
				yield return (Path.GetFileName(folder), file);
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
