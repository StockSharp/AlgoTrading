namespace StockSharp.Tests;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Diagram;

/// <summary>
/// Writes down what the Designer registry actually offers. A schema is authored against this file, so it
/// is generated from the running registry rather than kept by hand.
/// </summary>
[TestClass]
public class CatalogTests
{
	/// <summary>Where the generated catalog is kept.</summary>
	public static string FileName => Path.Combine(SchemaGallery.Root, "_Tools", "catalog.md");

	/// <summary>Regenerates the element catalog from the registry.</summary>
	[TestMethod]
	public async Task Write()
	{
		var registry = await SchemaLoader.GetRegistryAsync(CancellationToken.None);
		var builder = new StringBuilder();

		builder.AppendLine("# Designer element catalog");
		builder.AppendLine();
		builder.AppendLine("Generated from the running composition registry by `CatalogTests.Write`. Do not edit by hand.");
		builder.AppendLine();
		builder.AppendLine("Ports are listed by socket identifier, which is what a schema file links by.");
		builder.AppendLine();

		foreach (var element in registry.DiagramElements.OrderBy(e => e.GetType().Name, StringComparer.Ordinal))
		{
			builder.AppendLine($"## {element.GetType().Name}");
			builder.AppendLine();
			builder.AppendLine($"- **Name:** {element.Name}");
			builder.AppendLine($"- **TypeId:** `{element.TypeId}`");

			var inputs = element.InputSockets.Select(s => $"`{s.Id}` ({s.Type?.Name})").ToArray();
			var outputs = element.OutputSockets.Select(s => $"`{s.Id}` ({s.Type?.Name})").ToArray();
			var parameters = element.Parameters
				.Where(p => p.Name is not ("Name" or "LogLevel" or "ShowParameters" or "ShowSockets" or "ProcessNullValues" or "ElementName"))
				.Select(p => $"`{p.Name}`: {p.Type?.Name}")
				.ToArray();

			builder.AppendLine($"- **In:** {(inputs.Length > 0 ? string.Join(", ", inputs) : "none")}");
			builder.AppendLine($"- **Out:** {(outputs.Length > 0 ? string.Join(", ", outputs) : "none")}");
			builder.AppendLine($"- **Params:** {(parameters.Length > 0 ? string.Join(", ", parameters) : "none")}");
			builder.AppendLine();
		}

		Directory.CreateDirectory(Path.GetDirectoryName(FileName));
		await File.WriteAllTextAsync(FileName, builder.ToString(), CancellationToken.None);

		registry.DiagramElements.Count.AssertGreater(30, "The registry holds suspiciously few elements.");
	}
}
