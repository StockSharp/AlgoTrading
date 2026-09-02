namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Diagram;

/// <summary>
/// Holds every gallery example to the same bar: it is complete on disk, it opens in the Designer, and
/// unless it is a viewing example it trades on the packaged history.
/// </summary>
[TestClass]
public class SchemaTests
{
	/// <summary>
	/// Examples that show data rather than trade it. Each is named with the reason it sends no order, so
	/// that a strategy which stops trading cannot be quietly moved in here.
	/// </summary>
	private static readonly Dictionary<string, string> _viewingOnly = new(System.StringComparer.OrdinalIgnoreCase)
	{
		["Candles"] = "Draws a candle series and nothing else.",
		["Index_Candles"] = "Builds an index series out of two securities.",
		["PreudoIndex"] = "Builds a synthetic index series.",
		["MarketDepth"] = "Shows the order book.",
	};

	/// <summary>Every example of the gallery.</summary>
	public static IEnumerable<object[]> AllSchemas
		=> SchemaGallery.EnumerateSchemas().Select(s => new object[] { s.name, s.fileName });

	/// <summary>Examples expected to send orders on the packaged history.</summary>
	public static IEnumerable<object[]> TradingSchemas
		=> SchemaGallery.EnumerateSchemas().Where(s => !_viewingOnly.ContainsKey(s.name)).Select(s => new object[] { s.name, s.fileName });

	/// <summary>Every example folder carries the schema, its picture and all seven descriptions.</summary>
	[TestMethod]
	public void Complete()
	{
		var missing = new List<string>();

		foreach (var folder in SchemaGallery.EnumerateFolders())
		{
			var name = Path.GetFileName(folder);

			if (SchemaGallery.FindSchemaFile(folder) is null)
				missing.Add($"{name}: no schema file");

			// A generated example carries the picture the diagram control draws for it; the older ones
			// carry the screenshot they were shipped with. Either is a picture of the schema.
			if (!File.Exists(Path.Combine(folder, "schema.svg")) && !File.Exists(Path.Combine(folder, "schema.png")))
				missing.Add($"{name}: no schema.svg or schema.png");

			if (!File.Exists(Path.Combine(folder, "README.md")))
				missing.Add($"{name}: no README.md");

			foreach (var language in SchemaGallery.Translations)
			{
				if (!File.Exists(Path.Combine(folder, $"README_{language}.md")))
					missing.Add($"{name}: no README_{language}.md");
			}
		}

		missing.Count.AssertEqual(0, $"Incomplete examples: {string.Join("; ", missing)}");
	}

	/// <summary>The file opens into a composition the Designer can build.</summary>
	/// <param name="name">Example name.</param>
	/// <param name="fileName">Schema file.</param>
	[TestMethod]
	[DynamicData(nameof(AllSchemas))]
	public async Task Loads(string name, string fileName)
	{
		var composition = await SchemaLoader.LoadAsync(fileName, CancellationToken.None);

		composition.AssertNotNull($"'{name}' produced no composition.");
		composition.HasErrors.AssertFalse($"'{name}' holds elements that did not load.");

		// A composition that lost its nodes still loads without error and then does nothing at all.
		var model = (CompositionModel<InMemoryCompositionModelNode, InMemoryCompositionModelLink>)composition.Model;
		model.Nodes.Count().AssertGreater(0, $"'{name}' has no blocks.");
	}

	/// <summary>Assigning the composition to a strategy binds every element and its parameters.</summary>
	/// <param name="name">Example name.</param>
	/// <param name="fileName">Schema file.</param>
	[TestMethod]
	[DynamicData(nameof(AllSchemas))]
	public async Task Materializes(string name, string fileName)
	{
		var composition = await SchemaLoader.LoadAsync(fileName, CancellationToken.None);

		using var strategy = new DiagramStrategy();
		var errors = new List<string>();

		// Parameter binding reports its failures to the log instead of throwing, so the log is the only
		// place they can be seen.
		void onLog(Ecng.Logging.LogMessage message)
		{
			if (message.Level == Ecng.Logging.LogLevels.Error)
				errors.Add(message.Message);
		}

		strategy.Log += onLog;

		try
		{
			strategy.Composition = composition;
		}
		finally
		{
			strategy.Log -= onLog;
		}

		errors.Count.AssertEqual(0, $"'{name}' failed to initialize: {string.Join("; ", errors)}");
	}

	/// <summary>The example trades on the packaged history, the same bar the API strategies are held to.</summary>
	/// <param name="name">Example name.</param>
	/// <param name="fileName">Schema file.</param>
	[TestMethod]
	[DynamicData(nameof(TradingSchemas))]
	public async Task Trades(string name, string fileName)
	{
		using var strategy = new DiagramStrategy { Composition = await SchemaLoader.LoadAsync(fileName, CancellationToken.None) };

		// The harness only asserts that a schema traded. How much it traded says whether the example is
		// worth showing, so the counts are written out for every run.
		var orders = 0;
		var trades = 0;

		strategy.OrderReceived += (_, _) => Interlocked.Increment(ref orders);
		strategy.OwnTradeReceived += (_, _) => Interlocked.Increment(ref trades);

		try
		{
			await AsmInit.RunStrategy(strategy);
		}
		finally
		{
			Console.WriteLine($"{name}: {orders} order(s), {trades} trade(s), PnL {strategy.PnL:0.##}");
		}
	}
}
