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

	/// <summary>
	/// Folder keys whose tests are hand-written in the Overrides files, because they need a second
	/// security or reach the strategy's own parameters. These are the only examples compiled into
	/// the test assembly; Tests.csproj lists the same keys.
	/// </summary>
	public static readonly IReadOnlyCollection<string> Overrides = new HashSet<string>(StringComparer.Ordinal)
	{
		"0001_MA_CrossOver",
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
		"0402_Synthetic_Lending_Rates",
		"0410_WTIBrent_Spread",
		"0425_Grid_Bot",
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
		"2907_CCFp_Currency_Strength",
		"3064_Two_PerBar",
		"3104_MA_MACD_Position_Averaging",
		"3206_Risk_Reward_Ratio",
		"3301_Crypto_Analysis",
		"3623_Matrix_Machine_Learning",
		"3710_RRSRandomness",
		"3908_Five_MA_Multi_Timeframe",
		"4006_TenPips_Opposite_Last_N_Hour_Trend",
		"4048_Burg_Extrapolator_Forecast",
	};

	/// <summary>One example implementation: the folder it belongs to and the file to run.</summary>
	public readonly record struct Example(string Key, int Id, string TestName, int Shard, string RelativePath);

	/// <summary>
	/// Every implementation of the given extension found under API, ordered by id so a shard is a
	/// stable set from run to run.
	/// </summary>
	public static IEnumerable<Example> Enumerate(string extension)
		=> Directory
			.EnumerateFiles(_root, "*" + extension, SearchOption.AllDirectories)
			.Select(ToExample)
			.Where(e => e is not null)
			.Select(e => e.Value)
			.OrderBy(e => e.Id)
			.ThenBy(e => e.Key, StringComparer.Ordinal);

	/// <summary>Rows for one shard, excluding the examples that carry a hand-written test.</summary>
	public static IEnumerable<object[]> Shard(string extension, int shard)
		=> Enumerate(extension)
			.Where(e => e.Shard == shard && !Overrides.Contains(e.Key))
			.Select(e => new object[] { e.RelativePath, e.TestName });

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
