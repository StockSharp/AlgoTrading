namespace StockSharp.Tests;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

partial class PythonTests
{
	private static void SetParam(Strategy s, string name, object value)
	{
		if (!s.Parameters.TryGetValue(name, out var param))
			throw new System.InvalidOperationException($"Parameter '{name}' not found. Available: {string.Join(", ", System.Linq.Enumerable.Select(s.Parameters.CachedKeys, k => k.ToString()))}");
		param.Value = value;
	}

	[TestMethod]
	[TestCategory("Shard08")]
	public Task Ch2010Structure()
		=> RunStrategy("2701-2800/2776_CH2010_Structure/PY/ch2010_structure_strategy.py", (s, _) =>
		{
			SetParam(s, "DailyCandleType", System.TimeSpan.FromHours(1).TimeFrame());
			SetParam(s, "IntradayCandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
		});

	[TestMethod]
	[TestCategory("Shard13")]
	public Task DispersionTrading()
		=> RunStrategy("0301-0400/0365_Dispersion_Trading/PY/dispersion_trading_strategy.py", (s, sec2) => SetParam(s, "Constituents", new[] { sec2 }));

	[TestMethod]
	[TestCategory("Shard07")]
	public Task MulticurrencyOverlayHedge()
		=> RunStrategy("2601-2700/2679_Multicurrency_Overlay_Hedge/PY/multicurrency_overlay_hedge_strategy.py", (s, sec2) =>
		{
			SetParam(s, "Universe", new[] { s.Security, sec2 });
			SetParam(s, "CandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(s, "CorrelationThreshold", 0.01);
			SetParam(s, "CorrelationLookback", 50);
			SetParam(s, "RangeLength", 20);
			SetParam(s, "AtrLookback", 20);
			SetParam(s, "MaxSpread", 100000.0);
			SetParam(s, "OverlayThreshold", 0.001);
			SetParam(s, "RecalculationHour", 0);
		});

	[TestMethod]
	[TestCategory("Shard01")]
	public Task Spreader2()
		=> RunStrategy("2701-2800/2705_Spreader_2/PY/spreader2_strategy.py", (s, sec2) =>
		{
			SetParam(s, "SecondSecurity", sec2);
			SetParam(s, "DayBars", 10);
			SetParam(s, "ShiftLength", 3);
			SetParam(s, "TargetProfit", 1.0);
		});

	[TestMethod]
	[TestCategory("Shard14")]
	public Task CointegrationPairs()
		=> RunStrategy("0201-0300/0222_Cointegration_Pairs/PY/cointegration_pairs_strategy.py", (s, sec2) => { SetParam(s, "Asset2", sec2); SetParam(s, "Beta", 10000.0); });

	[TestMethod]
	[TestCategory("Shard06")]
	public Task DeltaNeutralArbitrage()
		=> RunStrategy("0201-0300/0230_Delta_Neutral_Arbitrage/PY/delta_neutral_arbitrage_strategy.py", (s, sec2) => { SetParam(s, "Asset2Security", sec2); SetParam(s, "Asset2Portfolio", s.Portfolio); });

	[TestMethod]
	[TestCategory("Shard14")]
	public Task ImproveMaRsiHedge()
		=> RunStrategy("2701-2800/2798_Improve_MA_RSI_Hedge/PY/improve_ma_rsi_hedge_strategy.py");

	[TestMethod]
	[TestCategory("Shard01")]
	public Task Pairs()
		=> RunStrategy("1101-1200/1153_Pairs/PY/pairs_strategy.py", (s, sec2) => SetParam(s, "ReferenceSecurity", sec2));

	[TestMethod]
	[TestCategory("Shard09")]
	public Task PairsTrading()
		=> RunStrategy("0201-0300/0217_Pairs_Trading/PY/pairs_trading_strategy.py", (s, sec2) => SetParam(s, "SecondSecurity", sec2));

	[TestMethod]
	[TestCategory("Shard14")]
	public Task SpotFuturesArbitrage()
		=> RunStrategy("0501-0600/0526_Spot_Futures_Arbitrage/PY/spot_futures_arbitrage_strategy.py", (s, sec2) => { SetParam(s, "Spot", s.Security); SetParam(s, "Future", sec2); });

	[TestMethod]
	[TestCategory("Shard11")]
	public Task StatisticalArbitrage()
		=> RunStrategy("0201-0300/0219_Statistical_Arbitrage/PY/statistical_arbitrage_strategy.py", (s, sec2) => SetParam(s, "SecondSecurity", sec2));
}
