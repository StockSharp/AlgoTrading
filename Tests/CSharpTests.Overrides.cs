namespace StockSharp.Tests;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Messages;
using StockSharp.Samples.Strategies;

partial class CSharpTests
{
	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2000_HftSpreaderForForts()
		// A full month creates tens of thousands of fills. One natural day still
		// exercises hundreds of entry/exit cycles without turning CI into a load test.
		=> RunStrategy<HftSpreaderForFortsStrategy>(replayDuration: System.TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S3064_TwoPerbar()
		// This intentionally trades on nearly every bar. A natural one-day window
		// retains high trade coverage without generating ~16k fills per language.
		=> RunStrategy<TwoPerBarStrategy>(replayDuration: System.TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S4048_BurgExtrapolatorForecast()
		=> RunStrategy<BurgExtrapolatorForecastStrategy>(replayDuration: System.TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2096_BreakoutBarsTrend()
		// Compact parameters make the signal reachable in the bundled history window.
		=> RunStrategy<BreakoutBarsTrendStrategy>((s, _) =>
		{
			s.Volume = 0.001m;
			s.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			s.Negatives = 0;
		});

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2776_Ch2010Structure()
		=> RunStrategy<Ch2010StructureStrategy>((s, sec2) =>
		{
			s.UsdChfSecurity = s.Security;
			s.GbpUsdSecurity = sec2;
			s.DailyCandleType = System.TimeSpan.FromHours(1).TimeFrame();
			s.IntradayCandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			s.MinTradeVolume = 0.001m;
		});

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0365_DispersionTrading()
		=> RunStrategy<DispersionTradingStrategy>((s, sec2) => s.Constituents = new[] { sec2 });

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0222_CointegrationPairs()
		=> RunStrategy<CointegrationPairsStrategy>((s, sec2) => s.Asset2 = sec2);

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0230_DeltaNeutralArbitrage()
		=> RunStrategy<DeltaNeutralArbitrageStrategy>((s, sec2) => { s.Asset2Security = sec2; s.Asset2Portfolio = s.Portfolio; });

	[TestMethod]
	[TestCategory("Shard07")]
	public Task S2679_MulticurrencyOverlayHedge()
		=> RunStrategy<MulticurrencyOverlayHedgeStrategy>((s, sec2) =>
		{
			s.Universe = new[] { s.Security, sec2 };
			s.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			s.CorrelationThreshold = 0.01m;
			s.CorrelationLookback = 50;
			s.RangeLength = 20;
			s.AtrLookback = 20;
			s.MaxSpread = 100000m;
			s.OverlayThreshold = 0.001m;
			s.RecalculationHour = 0;
		});

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S2798_ImproveMaRsiHedge()
		=> RunStrategy<ImproveMaRsiHedgeStrategy>((s, sec2) => s.HedgeSecurity = sec2);

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0333_KeltnerSeasonalFilter()
		// Compact periods make the signal reachable in the bundled history window.
		=> RunStrategy<KeltnerSeasonalStrategy>((s, _) =>
		{
			s.EmaPeriod = 2;
			s.AtrPeriod = 2;
			s.AtrMultiplier = 0.01m;
			s.SeasonalThreshold = 0m;
			s.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
		});

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S1153_Pairs()
		=> RunStrategy<PairsStrategy>((s, sec2) => s.ReferenceSecurity = sec2);

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S0217_PairsTrading()
		=> RunStrategy<PairsTradingStrategy>((s, sec2) => s.SecondSecurity = sec2);

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0526_SpotFuturesArbitrage()
		=> RunStrategy<SpotFuturesArbitrageStrategy>((s, sec2) => { s.Spot = s.Security; s.Future = sec2; });

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S2705_Spreader2()
		=> RunStrategy<Spreader2Strategy>((s, sec2) => { s.SecondSecurity = sec2; s.DayBars = 10; s.ShiftLength = 3; s.TargetProfit = 1m; });

	[TestMethod]
	[TestCategory("Shard03")]
	public Task S0219_StatisticalArbitrage()
		=> RunStrategy<StatisticalArbitrageStrategy>((s, sec2) => s.SecondSecurity = sec2);
}
