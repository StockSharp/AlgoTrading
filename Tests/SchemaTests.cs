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
		var problems = new List<string>();

		foreach (var folder in SchemaGallery.EnumerateFolders())
		{
			var name = Path.GetFileName(folder);
			var expected = new[]
			{
				"schema.svg",
				"README.md",
			}
				.Concat(SchemaGallery.Translations.Select(language => $"README_{language}.md"))
				.ToHashSet(StringComparer.Ordinal);
			var actual = Directory
				.EnumerateFileSystemEntries(folder)
				.Select(Path.GetFileName)
				.ToHashSet(StringComparer.Ordinal);

			// The schemas are counted rather than named: what a schema file is called says nothing about
			// the example, and a folder is free to hold more than one.
			if (!SchemaGallery.SchemaFiles(folder).Any())
				problems.Add($"{name}: missing a schema");

			foreach (var missing in expected.Except(actual))
				problems.Add($"{name}: missing {missing}");

			foreach (var extra in actual.Except(expected).Where(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
				problems.Add($"{name}: unexpected {extra}");
		}

		problems.Count.AssertEqual(0, $"Incomplete examples: {string.Join("; ", problems)}");
	}

	/// <summary>
	/// Every example replayed over less than the whole history is an example that still exists. A name
	/// left behind by a renamed or deleted example would quietly shorten nothing, and the next example to
	/// take that name would inherit a bar nobody chose for it.
	/// </summary>
	[TestMethod]
	public void ShortenedReplaysNameExamplesThatExist()
	{
		var published = SchemaGallery.EnumerateSchemas().Select(s => s.name).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var gone = _shortTapeReplay.Keys.Where(name => !published.Contains(name)).ToArray();

		gone.Length.AssertEqual(0, $"Replayed over a shortened window but not in the gallery: {string.Join(", ", gone)}");
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
	/// Examples whose decision is a print on the tape, and how much history each is replayed over. Every
	/// print of a month of two crypto futures cannot be delivered inside the harness minute -- measured,
	/// such a run reaches hours of the month, not days -- so these are held to the same bar over a shorter
	/// window rather than left out. Each is named with the reason, because reading the tape is not by
	/// itself one: an example that only prices a stop off it keeps the whole month, and adding a name here
	/// has to be a decision rather than a side effect of subscribing ticks.
	/// </summary>
	private static readonly Dictionary<string, string> _shortTapeReplay = new(System.StringComparer.OrdinalIgnoreCase)
	{
		["Tape_Reader"] = "Every print is measured against the average size of the last hundred.",
		["Tick_Spike_Fade"] = "The entry is the first print far enough from the close twenty bars back.",
		["Random_Entry_Trailing_Stop"] = "The entry is a coin toss on a candle, but the stop is repriced on every print.",
	};

	/// <summary>How much history an example named in <see cref="_shortTapeReplay"/> is replayed over.</summary>
	private static readonly TimeSpan _tapeWindow = TimeSpan.FromDays(2);

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

		var tape = _shortTapeReplay.ContainsKey(name);

		try
		{
			await AsmInit.RunStrategy(strategy, replayDuration: tape ? _tapeWindow : null);
		}
		finally
		{
			var profit = withPnL ? $", PnL {strategy.PnL:0.##}" : string.Empty;
			// A count off a shortened run must not read as a count off the whole month.
			var window = tape ? $", over the first {_tapeWindow.TotalDays:0} day(s) of the tape" : string.Empty;

			Console.WriteLine($"{name}: {orders} order(s), {trades} trade(s){profit}{window}");
		}
	}
}
