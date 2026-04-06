namespace StockSharp.Tests;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Messages;
using StockSharp.Samples.Strategies;

partial class CSharpTests
{
	[TestMethod]
	public Task Ch2010Structure()
		=> RunStrategy<Ch2010StructureStrategy>((s, sec2) =>
		{
			s.UsdChfSecurity = s.Security;
			s.GbpUsdSecurity = sec2;
			s.DailyCandleType = System.TimeSpan.FromHours(1).TimeFrame();
			s.IntradayCandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			s.MinTradeVolume = 0.001m;
		});

	[TestMethod]
	public Task DispersionTrading()
		=> RunStrategy<DispersionTradingStrategy>((s, sec2) => s.Constituents = new[] { sec2 });

	[TestMethod]
	public Task CointegrationPairs()
		=> RunStrategy<CointegrationPairsStrategy>((s, sec2) => s.Asset2 = sec2);

	[TestMethod]
	public Task DeltaNeutralArbitrage()
		=> RunStrategy<DeltaNeutralArbitrageStrategy>((s, sec2) => { s.Asset2Security = sec2; s.Asset2Portfolio = s.Portfolio; });

	[TestMethod]
	public Task MulticurrencyOverlayHedge()
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
	public Task ImproveMaRsiHedge()
		=> RunStrategy<ImproveMaRsiHedgeStrategy>((s, sec2) => s.HedgeSecurity = sec2);

	[TestMethod]
	public Task Pairs()
		=> RunStrategy<PairsStrategy>((s, sec2) => s.ReferenceSecurity = sec2);

	[TestMethod]
	public Task PairsTrading()
		=> RunStrategy<PairsTradingStrategy>((s, sec2) => s.SecondSecurity = sec2);

	[TestMethod]
	public Task SpotFuturesArbitrage()
		=> RunStrategy<SpotFuturesArbitrageStrategy>((s, sec2) => { s.Spot = s.Security; s.Future = sec2; });

	[TestMethod]
	public Task Spreader2()
		=> RunStrategy<Spreader2Strategy>((s, sec2) => { s.SecondSecurity = sec2; s.DayBars = 10; s.ShiftLength = 3; s.TargetProfit = 1m; });

	[TestMethod]
	public Task StatisticalArbitrage()
		=> RunStrategy<StatisticalArbitrageStrategy>((s, sec2) => s.SecondSecurity = sec2);
}
