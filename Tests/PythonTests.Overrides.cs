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
	[TestCategory("Shard00")]
	public Task BreakoutBarsTrend()
		// Compact parameters make the signal reachable in the bundled history window.
		=> RunStrategy("2001-2100/2096_Breakout_Bars_Trend/PY/breakout_bars_trend_strategy.py", (s, _) =>
		{
			s.Volume = 0.001m;
			SetParam(s, "CandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(s, "Negatives", 0);
		});

	[TestMethod]
	[TestCategory("Shard00")]
	public Task Ch2010Structure()
		=> RunStrategy("2701-2800/2776_CH2010_Structure/PY/ch2010_structure_strategy.py", (s, _) =>
		{
			SetParam(s, "DailyCandleType", System.TimeSpan.FromHours(1).TimeFrame());
			SetParam(s, "IntradayCandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
		});

	[TestMethod]
	[TestCategory("Shard05")]
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
	[TestCategory("Shard06")]
	public Task CointegrationPairs()
		=> RunStrategy("0201-0300/0222_Cointegration_Pairs/PY/cointegration_pairs_strategy.py", (s, sec2) => { SetParam(s, "Asset2", sec2); SetParam(s, "Beta", 10000.0); });

	[TestMethod]
	[TestCategory("Shard06")]
	public Task DeltaNeutralArbitrage()
		=> RunStrategy("0201-0300/0230_Delta_Neutral_Arbitrage/PY/delta_neutral_arbitrage_strategy.py", (s, sec2) => { SetParam(s, "Asset2Security", sec2); SetParam(s, "Asset2Portfolio", s.Portfolio); });

	[TestMethod]
	[TestCategory("Shard06")]
	public Task ImproveMaRsiHedge()
		=> RunStrategy("2701-2800/2798_Improve_MA_RSI_Hedge/PY/improve_ma_rsi_hedge_strategy.py");

	[TestMethod]
	[TestCategory("Shard05")]
	public Task KeltnerSeasonalFilter()
		// Compact periods make the signal reachable in the bundled history window.
		=> RunStrategy("0301-0400/0333_Keltner_Seasonal_Filter/PY/keltner_seasonal_strategy.py", (s, _) =>
		{
			SetParam(s, "EmaPeriod", 2);
			SetParam(s, "AtrPeriod", 2);
			SetParam(s, "AtrMultiplier", 0.01);
			SetParam(s, "SeasonalThreshold", 0.0);
			SetParam(s, "CandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
		});

	[TestMethod]
	[TestCategory("Shard01")]
	public Task Pairs()
		=> RunStrategy("1101-1200/1153_Pairs/PY/pairs_strategy.py", (s, sec2) => SetParam(s, "ReferenceSecurity", sec2));

	[TestMethod]
	[TestCategory("Shard01")]
	public Task PairsTrading()
		=> RunStrategy("0201-0300/0217_Pairs_Trading/PY/pairs_trading_strategy.py", (s, sec2) => SetParam(s, "SecondSecurity", sec2));

	[TestMethod]
	[TestCategory("Shard06")]
	public Task SpotFuturesArbitrage()
		=> RunStrategy("0501-0600/0526_Spot_Futures_Arbitrage/PY/spot_futures_arbitrage_strategy.py", (s, sec2) => { SetParam(s, "Spot", s.Security); SetParam(s, "Future", sec2); });

	[TestMethod]
	[TestCategory("Shard03")]
	public Task StatisticalArbitrage()
		=> RunStrategy("0201-0300/0219_Statistical_Arbitrage/PY/statistical_arbitrage_strategy.py", (s, sec2) => SetParam(s, "SecondSecurity", sec2));
}
