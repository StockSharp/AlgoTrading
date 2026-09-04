namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ecng.Common;

using StockSharp.Configuration;
using StockSharp.Diagram;

/// <summary>
/// Holds every gallery example to the same bar: it is complete on disk, it opens in the Designer, and
/// unless it is a viewing example it trades on the packaged history.
/// </summary>
[TestClass]
public class SchemaTests : BaseTestClass
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

	/// <summary>Every lesson of the Education section.</summary>
	public static IEnumerable<object[]> AllLessons
		=> SchemaGallery.EnumerateLessons().Select(s => new object[] { s.name, s.fileName });

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

	/// <summary>A lesson opens into a composition too; it teaches from a schema that has to work.</summary>
	/// <param name="name">Lesson name.</param>
	/// <param name="fileName">Schema file.</param>
	[TestMethod]
	[DynamicData(nameof(AllLessons))]
	public async Task LessonLoads(string name, string fileName)
	{
		var composition = await SchemaLoader.LoadAsync(fileName, CancellationToken);

		composition.AssertNotNull($"'{name}' produced no composition.");

		var model = (CompositionModel<InMemoryCompositionModelNode, InMemoryCompositionModelLink>)composition.Model;
		model.Nodes.Count().AssertGreater(0, $"'{name}' has no blocks.");

		// A lesson on building your own cube uses the cube the student builds in it, and that one lives
		// in the reader's own library rather than in the file. Such a node may go unbuilt here; anything
		// else that fails to build is a lesson that would not open for a reader either.
		if (!composition.HasErrors)
			return;

		var composites = await SchemaLoader.ReadCompositeTypesAsync(fileName, CancellationToken);

		composites.Count.AssertGreater(0, $"'{name}' holds elements that did not load and asks for no composite of its own.");
	}

	/// <summary>A lesson runs on the packaged history under the harness the strategies use.</summary>
	/// <param name="name">Lesson name.</param>
	/// <param name="fileName">Schema file.</param>
	[TestMethod]
	[DynamicData(nameof(AllLessons))]
	public async Task LessonRuns(string name, string fileName)
	{
		if ((await SchemaLoader.ReadCompositeTypesAsync(fileName, CancellationToken)).Count > 0)
			Fail($"'{name}' teaches how to build a cube of your own, which lives in the reader's library rather than in the file.");

		var composition = await SchemaLoader.LoadAsync(fileName, CancellationToken);

		if (!SchemaLoader.IsRunnable(composition))
			Fail($"'{name}' is a fragment: it has no market data feeding something that trades, so there is nothing to replay.");

		// A lesson may teach on instruments of its own -- a pair of Russian shares, say -- and the packaged
		// history holds two crypto futures. Replaying it would prove nothing about the lesson.
		var unknown = SchemaLoader
			.SecurityIds(composition)
			.Where(id => !id.EqualsIgnoreCase(Paths.HistoryDefaultSecurity) && !id.EqualsIgnoreCase(Paths.HistoryDefaultSecurity2))
			.Distinct()
			.ToArray();

		if (unknown.Length > 0)
			Fail($"'{name}' trades {string.Join(", ", unknown)}, which the packaged history does not hold.");

		await ReplayAsync(name, composition, withPnL: false);
	}

	/// <summary>The file opens into a composition the Designer can build.</summary>
	/// <param name="name">Example name.</param>
	/// <param name="fileName">Schema file.</param>
	[TestMethod]
	[DynamicData(nameof(AllSchemas))]
	public async Task Loads(string name, string fileName)
	{
		var composition = await SchemaLoader.LoadAsync(fileName, CancellationToken);

		composition.AssertNotNull($"'{name}' produced no composition.");
		composition.HasErrors.AssertFalse($"'{name}' holds elements that did not load.");

		// A composition that lost its nodes still loads without error and then does nothing at all.
		var model = (CompositionModel<InMemoryCompositionModelNode, InMemoryCompositionModelLink>)composition.Model;
		model.Nodes.Count().AssertGreater(0, $"'{name}' has no blocks.");

		// A name written in a shape nothing reads back is lost without a word, and the example opens in the
		// Designer as the nameless block the registry hands out.
		var registry = await SchemaLoader.GetRegistryAsync(CancellationToken);
		using var unnamed = registry.CreateComposition();

		composition.Name.IsEmpty().AssertFalse($"'{name}' opens without a name.");
		(composition.Name == unnamed.Name).AssertFalse($"'{name}' opens as '{unnamed.Name}' rather than under a name of its own.");
	}

	/// <summary>Assigning the composition to a strategy binds every element and its parameters.</summary>
	/// <param name="name">Example name.</param>
	/// <param name="fileName">Schema file.</param>
	[TestMethod]
	[DynamicData(nameof(AllSchemas))]
	public async Task Materializes(string name, string fileName)
	{
		var composition = await SchemaLoader.LoadAsync(fileName, CancellationToken);

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
		var composition = await SchemaLoader.LoadAsync(fileName, CancellationToken);

		await ReplayAsync(name, composition, withPnL: true);
	}

	/// <summary>
	/// Replays a composition under the harness the API strategies use. The harness only asserts that a
	/// schema traded; how much it traded says whether the example is worth showing, so the counts are
	/// written out for every run.
	/// </summary>
	/// <param name="name">Example name.</param>
	/// <param name="composition">The composition to replay.</param>
	/// <param name="withPnL">Whether the result line carries the profit as well as the counts.</param>
	private static async Task ReplayAsync(string name, CompositionDiagramElement composition, bool withPnL)
	{
		using var strategy = new DiagramStrategy { Composition = composition };

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
			var profit = withPnL ? $", PnL {strategy.PnL:0.##}" : string.Empty;

			Console.WriteLine($"{name}: {orders} order(s), {trades} trade(s){profit}");
		}
	}
}
