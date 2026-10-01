namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

/// <summary>
/// The strategy examples as they lie on disk. They are not compiled into this assembly, so the
/// inventory is the file tree: every test row is a path, and the tests compile it at run time.
/// </summary>
static class StrategyInventory
{
	/// <summary>Number of shards the examples are split across; mirrored by the CI job matrix.</summary>
	public const int ShardCount = 8;

	private const string _root = "../../../../API/";
	private static readonly Lazy<Example[]> _snapshot = new(() => Directory
		.EnumerateFiles(_root, "*", SearchOption.AllDirectories)
		.Where(path => Path.GetExtension(path) is ".cs" or ".py")
		.Select(ToExample)
		.Where(example => example != null)
		.Select(example => example.Value)
		.OrderBy(example => example.Id)
		.ThenBy(example => example.Key, StringComparer.Ordinal)
		.ToArray());

	/// <summary>
	/// Folder keys whose tests are hand-written in the Overrides files, because they need a second
	/// security or reach the strategy's own parameters. A test that names the strategy's own type
	/// needs its source listed in Tests.csproj; one that runs the example by path needs no entry.
	/// </summary>
	public static readonly IReadOnlyCollection<string> Overrides = new HashSet<string>(StringComparer.Ordinal)
	{
		"0001_MA_CrossOver",
		// The unchanged 5% ROC10 threshold is reachable on packaged TON, not packaged BTC.
		"0020_Momentum_Percentage",
		// Published 5% SMA20 deviation is reachable on packaged TON, not packaged BTC.
		"0029_MA_Deviation",
		// An explicit second stream is required; packaged TON tests mechanics, not actual VIX history.
		"0037_VIX_Trigger",
		// An explicit IV stream is required; packaged TON tests the mechanics at a reachable spike threshold, not actual IV history.
		"0042_IV_Spike",
		"0201_VWAP_Williams_R",
		"0217_Pairs_Trading",
		"0219_Statistical_Arbitrage",
		"0222_Cointegration_Pairs",
		"0230_Delta_Neutral_Arbitrage",
		"0320_MACD_Hidden_Markov_Model",
		"0333_Keltner_Seasonal_Filter",
		"0343_Keltner_Reinforcement_Learning_Signal",
		"0362_Crypto_Rebalancing_Premium",
		"0365_Dispersion_Trading",
		"0401_Soccer_Clubs_Arbitrage",
		// Rate series and venue legs are explicit inputs; packaged BTC and TON stand in for them as a mechanics fixture.
		"0402_Synthetic_Lending_Rates",
		// An option contract is required; packaged TON prices stand in for its premiums to test mechanics, not option market history.
		"0408_Volatility_Risk_Premium",
		// Mechanics fixture: packaged TON stands in for the Brent leg against BTC, not crude oil history.
		"0410_WTIBrent_Spread",
		"0498_Advanced_Adaptive_Grid",
		"0503_Advanced_Position_Management",
		"0526_Spot_Futures_Arbitrage",
		"1005_MA_With_Logistic",
		"1153_Pairs",
		"1507_Ultimate_Template",
		"1908_Random_Trailing_Stop",
		"2000_HFT_Spreader_for_FORTS",
		"2096_Breakout_Bars_Trend",
		"2101_Linear_Regression_Slope_V1",
		"2403_ReOpen_Positions",
		"2502_21Hour_Session_Breakout",
		"2606_Statistics_Repeating_Behavior",
		"2679_Multicurrency_Overlay_Hedge",
		"2703_Self_Optimizing_RSI_or_MFI_Trader_v3",
		"2705_Spreader_2",
		"2776_CH2010_Structure",
		"2798_Improve_MA_RSI_Hedge",
		// The replay holds no account basket; a fixture opens one with orders of its own.
		"2808_Multi_Pair_Closer",
		"2907_CCFp_Currency_Strength",
		// Manual UI examples require explicit arming or a supplied calendar, not permissive defaults.
		"3008_OCO_Pending_Orders",
		"3064_Two_PerBar",
		"3507_Sample_Detect_Economic_Calendar",
		"3623_Matrix_Machine_Learning",
		"3710_RRSRandomness",
		"4006_TenPips_Opposite_Last_N_Hour_Trend",
		"4048_Burg_Extrapolator_Forecast",
		"4207_Rich_Kohonen_Map",
	};

	/// <summary>One example implementation: the folder it belongs to and the file to run.</summary>
	public readonly record struct Example(string Key, int Id, string TestName, int Shard, string RelativePath);

	/// <summary>
	/// Every implementation of the given extension found under API, ordered by id so a shard is a
	/// stable set from run to run. All sixteen C#/Python shard providers share one file-tree snapshot
	/// per test process; discovery must not repeat the full recursive traversal for every shard.
	/// </summary>
	public static IEnumerable<Example> Enumerate(string extension)
		=> _snapshot.Value.Where(example => Path.GetExtension(example.RelativePath) == extension);

	/// <summary>Rows for one shard, excluding the examples that carry a hand-written test.</summary>
	public static IEnumerable<object[]> Shard(string extension, int shard)
		=> Enumerate(extension)
			.Where(e => e.Shard == shard && !Overrides.Contains(e.Key))
			.Select(e => new object[] { e.RelativePath, e.TestName });

	/// <summary>
	/// The source file of one example in one language, by its folder key such as 0068_CCI_Divergence.
	/// </summary>
	/// <param name="key">Folder key of the example.</param>
	/// <param name="extension">Extension of the language: ".cs" or ".py".</param>
	/// <returns>Path of the source file under API.</returns>
	public static string GetFile(string key, string extension)
		=> Enumerate(extension).FirstOrDefault(e => e.Key == key).RelativePath
			?? throw new ArgumentException($"No {extension} implementation of {key} under API.", nameof(key));

	/// <summary>
	/// Names a row after the strategy, so a failure reads as S0002_NdayBreakout and
	/// --filter "Name~S0002_NdayBreakout" selects exactly it.
	/// </summary>
	public static string RowName(MethodInfo method, object[] data)
		=> (string)data[1];

	private static Example? ToExample(string path)
	{
		var normalized = path.Replace('\\', '/');
		var idx = normalized.IndexOf("/API/", StringComparison.Ordinal);

		var afterApi = idx < 0
			? normalized.Substring(normalized.IndexOf("API/", StringComparison.Ordinal) + "API/".Length)
			: normalized.Substring(idx + "/API/".Length);

		foreach (var segment in afterApi.Split('/'))
		{
			var underscore = segment.IndexOf('_');

			if (underscore <= 0 || !int.TryParse(segment.Substring(0, underscore), out var id))
				continue;

			return new(segment, id, $"S{id:D4}_{ToPascal(segment.Substring(underscore + 1))}", id % ShardCount, afterApi);
		}

		return null;
	}

	private static string ToPascal(string snake)
	{
		var sb = new StringBuilder();
		var upper = true;

		foreach (var c in snake)
		{
			if (c is '_' or '-')
			{
				upper = true;
				continue;
			}

			sb.Append(upper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
			upper = false;
		}

		return sb.ToString();
	}
}
