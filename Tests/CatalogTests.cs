namespace StockSharp.Tests;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Diagram;

/// <summary>
/// Writes down what the Designer registry actually offers. A schema is authored against this file, so it
/// is generated from the running registry rather than kept by hand.
/// </summary>
[TestClass]
public class CatalogTests : BaseTestClass
{
	/// <summary>Where the generated catalog is kept.</summary>
	public static string FileName => Path.Combine(SchemaGallery.Root, "_Tools", "catalog.md");

	/// <summary>The same catalog for the tools that draw the pictures.</summary>
	public static string DataFileName => Path.Combine(SchemaGallery.Root, "_Tools", "catalog.json");

	/// <summary>Regenerates the element catalog from the registry.</summary>
	[TestMethod]
	public async Task Write()
	{
		var registry = await SchemaLoader.GetRegistryAsync(CancellationToken);
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
		await File.WriteAllTextAsync(FileName, builder.ToString(), CancellationToken);

		// The same catalog for the renderer: what a block is called, what it looks like, which ports it
		// has and in which colour.
		var socketKeys = typeof(DiagramSocketType)
			.GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(f => f.FieldType == typeof(DiagramSocketType))
			.ToDictionary(f => (DiagramSocketType)f.GetValue(null), f => f.Name);

		string KeyOf(DiagramSocketType type)
			=> type is not null && socketKeys.TryGetValue(type, out var key) ? key : type?.Name ?? "Any";

		var socketTypes = socketKeys
			.OrderBy(pair => pair.Value, StringComparer.Ordinal)
			.Select(pair => new
			{
				name = pair.Value,
				displayName = pair.Key.Name,
				color = $"#{pair.Key.Color.R:X2}{pair.Key.Color.G:X2}{pair.Key.Color.B:X2}",
			})
			.ToArray();

		object Port(DiagramSocket socket)
			=> new { key = socket.Id, name = socket.Name, type = KeyOf(socket.Type), maxLinks = socket.LinkableMaximum == int.MaxValue ? 0 : socket.LinkableMaximum };

		var elements = registry.DiagramElements
			.OrderBy(e => e.GetType().Name, StringComparer.Ordinal)
			.Select(e => new
			{
				typeId = e.TypeId.ToString().ToUpperInvariant(),
				name = e.GetType().GetDisplayName(),
				description = e.Description ?? string.Empty,
				groupName = e.GetCategory(),
				icon = e.IconName,
				inPorts = e.InputSockets.Select(Port).ToArray(),
				outPorts = e.OutputSockets.Select(Port).ToArray(),
			})
			.ToArray();

		await File.WriteAllTextAsync(DataFileName, JsonSerializer.Serialize(new { socketTypes, elements },
			new JsonSerializerOptions { WriteIndented = true }), CancellationToken);

		registry.DiagramElements.Count.AssertGreater(30, "The registry holds suspiciously few elements.");
	}
}
