namespace StockSharp.Tests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Samples.Strategies;

[TestClass]
public class StrategyTests
{
	private static readonly MarketDataStorageCache _cache = new();

	private static async Task RunStrategy(Type strategyType)
	{
		var strategy = (Strategy)Activator.CreateInstance(strategyType);

		//var logManager = new LogManager();
		//logManager.Listeners.Add(new ConsoleLogListener());

		var token = CancellationToken.None;

		var secId = Paths.HistoryDefaultSecurity;
		var security = new Security { Id = secId };

		var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.HistoryDataPath) };

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		var pf = Portfolio.CreateSimulator();
		pf.CurrentValue = 1000000m;

		var connector = new HistoryEmulationConnector([security], [pf], storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
				//AdapterCache = _cache,
			}
		};

		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				strategy.Stop();
		};

		strategy.Portfolio = pf;
		strategy.Security = security;
		strategy.Connector = connector;
		strategy.Volume = 1;

		Exception error = null;
		strategy.Error += (s, e) =>
		{
			error = e;
			s.Stop();
		};

		//logManager.Sources.Add(connector);
		//logManager.Sources.Add(strategy);

		await connector.ConnectAsync(token);

		var task = strategy.ExecAsync(null, token);
		connector.Start();
		await task.AsTask();

		if (error is not null)
			throw error;

		Assert.IsTrue(strategy.Orders.Count() > 10, $"{strategy.GetType().Name} placed {strategy.Orders.Count()} orders");
		Assert.IsTrue(strategy.MyTrades.Count() > 5, $"{strategy.GetType().Name} executed {strategy.MyTrades.Count()} trades");
	}

	[TestMethod]
	public Task MaCrossoverStrategyTest()
		=> RunStrategy(typeof(MaCrossoverStrategy));

	[TestMethod]
	public Task NdayBreakoutStrategyTest()
		=> RunStrategy(typeof(NdayBreakoutStrategy));

	[TestMethod]
	public Task AdxTrendStrategyTest()
		=> RunStrategy(typeof(AdxTrendStrategy));

	[TestMethod]
	public Task ParabolicSarTrendStrategyTest()
		=> RunStrategy(typeof(ParabolicSarTrendStrategy));

	[TestMethod]
	public Task DonchianChannelStrategyTest()
		=> RunStrategy(typeof(DonchianChannelStrategy));

	[TestMethod]
	public Task TripleMAStrategyTest()
		=> RunStrategy(typeof(TripleMAStrategy));

	[TestMethod]
	public Task KeltnerChannelBreakoutStrategyTest()
		=> RunStrategy(typeof(KeltnerChannelBreakoutStrategy));

	[TestMethod]
	public Task HullMaTrendStrategyTest()
		=> RunStrategy(typeof(HullMaTrendStrategy));

	[TestMethod]
	public Task MacdTrendStrategyTest()
		=> RunStrategy(typeof(MacdTrendStrategy));

	[TestMethod]
	public Task SupertrendStrategyTest()
		=> RunStrategy(typeof(SupertrendStrategy));

	[TestMethod]
	public Task IchimokuKumoBreakoutStrategyTest()
		=> RunStrategy(typeof(IchimokuKumoBreakoutStrategy));

	[TestMethod]
	public Task HeikinAshiConsecutiveStrategyTest()
		=> RunStrategy(typeof(HeikinAshiConsecutiveStrategy));

	[TestMethod]
	public Task DmiPowerMoveStrategyTest()
		=> RunStrategy(typeof(DmiPowerMoveStrategy));

	[TestMethod]
	public Task TradingviewSupertrendFlipStrategyTest()
		=> RunStrategy(typeof(TradingViewSupertrendFlipStrategy));

	[TestMethod]
	public Task GannSwingBreakoutStrategyTest()
		=> RunStrategy(typeof(GannSwingBreakoutStrategy));

	[TestMethod]
	public Task RsiDivergenceStrategyTest()
		=> RunStrategy(typeof(RsiDivergenceStrategy));

	[TestMethod]
	public Task WilliamsPercentRStrategyTest()
		=> RunStrategy(typeof(WilliamsPercentRStrategy));

	[TestMethod]
	public Task RocImpulseStrategyTest()
		=> RunStrategy(typeof(RocImpulseStrategy));

	[TestMethod]
	public Task CciBreakoutStrategyTest()
		=> RunStrategy(typeof(CciBreakoutStrategy));

	[TestMethod]
	public Task MomentumPercentageStrategyTest()
		=> RunStrategy(typeof(MomentumPercentageStrategy));

	[TestMethod]
	public Task BollingerSqueezeStrategyTest()
		=> RunStrategy(typeof(BollingerSqueezeStrategy));

	[TestMethod]
	public Task AdxDiStrategyTest()
		=> RunStrategy(typeof(AdxDiStrategy));

	[TestMethod]
	public Task ElderImpulseStrategyTest()
		=> RunStrategy(typeof(ElderImpulseStrategy));

	[TestMethod]
	public Task LaguerreRsiStrategyTest()
		=> RunStrategy(typeof(LaguerreRsiStrategy));

	[TestMethod]
	public Task StochasticRsiCrossStrategyTest()
		=> RunStrategy(typeof(StochasticRsiCrossStrategy));

	[TestMethod]
	public Task RsiReversionStrategyTest()
		=> RunStrategy(typeof(RsiReversionStrategy));

	[TestMethod]
	public Task BollingerReversionStrategyTest()
		=> RunStrategy(typeof(BollingerReversionStrategy));

	[TestMethod]
	public Task ZScoreStrategyTest()
		=> RunStrategy(typeof(ZScoreStrategy));

	[TestMethod]
	public Task MADeviationStrategyTest()
		=> RunStrategy(typeof(MADeviationStrategy));

	[TestMethod]
	public Task VwapReversionStrategyTest()
		=> RunStrategy(typeof(VwapReversionStrategy));

	[TestMethod]
	public Task KeltnerReversionStrategyTest()
		=> RunStrategy(typeof(KeltnerReversionStrategy));

	[TestMethod]
	public Task AtrReversionStrategyTest()
		=> RunStrategy(typeof(AtrReversionStrategy));

	[TestMethod]
	public Task MacdZeroStrategyTest()
		=> RunStrategy(typeof(MacdZeroStrategy));

	[TestMethod]
	public Task LowVolReversionStrategyTest()
		=> RunStrategy(typeof(LowVolReversionStrategy));

	[TestMethod]
	public Task BollingerPercentBStrategyTest()
		=> RunStrategy(typeof(BollingerPercentBStrategy));

	[TestMethod]
	public Task AtrExpansionStrategyTest()
		=> RunStrategy(typeof(AtrExpansionStrategy));

	[TestMethod]
	public Task VixTriggerStrategyTest()
		=> RunStrategy(typeof(VixTriggerStrategy));

	[TestMethod]
	public Task BollingerBandWidthStrategyTest()
		=> RunStrategy(typeof(BollingerBandWidthStrategy));

	[TestMethod]
	public Task HvBreakoutStrategyTest()
		=> RunStrategy(typeof(HvBreakoutStrategy));

	[TestMethod]
	public Task AtrTrailingStrategyTest()
		=> RunStrategy(typeof(AtrTrailingStrategy));

	[TestMethod]
	public Task VolAdjustedMaStrategyTest()
		=> RunStrategy(typeof(VolAdjustedMAStrategy));

	[TestMethod]
	public Task IVSpikeStrategyTest()
		=> RunStrategy(typeof(IVSpikeStrategy));

	[TestMethod]
	public Task VCPStrategyTest()
		=> RunStrategy(typeof(VCPStrategy));

	[TestMethod]
	public Task ATRRangeStrategyTest()
		=> RunStrategy(typeof(ATRRangeStrategy));

	[TestMethod]
	public Task ChoppinessIndexBreakoutStrategyTest()
		=> RunStrategy(typeof(ChoppinessIndexBreakoutStrategy));

	[TestMethod]
	public Task VolumeSpikeStrategyTest()
		=> RunStrategy(typeof(VolumeSpikeStrategy));

	[TestMethod]
	public Task OBVBreakoutStrategyTest()
		=> RunStrategy(typeof(OBVBreakoutStrategy));

	[TestMethod]
	public Task VWAPBreakoutStrategyTest()
		=> RunStrategy(typeof(VWAPBreakoutStrategy));

	[TestMethod]
	public Task VWMAStrategyTest()
		=> RunStrategy(typeof(VWMAStrategy));

	[TestMethod]
	public Task ADStrategyTest()
		=> RunStrategy(typeof(ADStrategy));

	[TestMethod]
	public Task VolumeWeightedPriceBreakoutStrategyTest()
		=> RunStrategy(typeof(VolumeWeightedPriceBreakoutStrategy));

	[TestMethod]
	public Task VolumeDivergenceStrategyTest()
		=> RunStrategy(typeof(VolumeDivergenceStrategy));

	[TestMethod]
	public Task VolumeMAXrossStrategyTest()
		=> RunStrategy(typeof(VolumeMAXrossStrategy));

	[TestMethod]
	public Task CumulativeDeltaBreakoutStrategyTest()
		=> RunStrategy(typeof(CumulativeDeltaBreakoutStrategy));

	[TestMethod]
	public Task VolumeSurgeStrategyTest()
		=> RunStrategy(typeof(VolumeSurgeStrategy));

	[TestMethod]
	public Task DoubleBottomStrategyTest()
		=> RunStrategy(typeof(DoubleBottomStrategy));

	[TestMethod]
	public Task DoubleTopStrategyTest()
		=> RunStrategy(typeof(DoubleTopStrategy));

	[TestMethod]
	public Task RsiOverboughtOversoldStrategyTest()
		=> RunStrategy(typeof(RsiOverboughtOversoldStrategy));

	[TestMethod]
	public Task HammerCandleStrategyTest()
		=> RunStrategy(typeof(HammerCandleStrategy));

	[TestMethod]
	public Task ShootingStarStrategyTest()
		=> RunStrategy(typeof(ShootingStarStrategy));

	[TestMethod]
	public Task MacdDivergenceStrategyTest()
		=> RunStrategy(typeof(MacdDivergenceStrategy));

	[TestMethod]
	public Task StochasticOverboughtOversoldStrategyTest()
		=> RunStrategy(typeof(StochasticOverboughtOversoldStrategy));

	[TestMethod]
	public Task EngulfingBullishStrategyTest()
		=> RunStrategy(typeof(EngulfingBullishStrategy));

	[TestMethod]
	public Task EngulfingBearishStrategyTest()
		=> RunStrategy(typeof(EngulfingBearishStrategy));

	[TestMethod]
	public Task PinbarReversalStrategyTest()
		=> RunStrategy(typeof(PinbarReversalStrategy));

	[TestMethod]
	public Task ThreeBarReversalUpStrategyTest()
		=> RunStrategy(typeof(ThreeBarReversalUpStrategy));

	[TestMethod]
	public Task ThreeBarReversalDownStrategyTest()
		=> RunStrategy(typeof(ThreeBarReversalDownStrategy));

	[TestMethod]
	public Task CciDivergenceStrategyTest()
		=> RunStrategy(typeof(CciDivergenceStrategy));

	[TestMethod]
	public Task BollingerBandReversalStrategyTest()
		=> RunStrategy(typeof(BollingerBandReversalStrategy));

	[TestMethod]
	public Task MorningStarStrategyTest()
		=> RunStrategy(typeof(MorningStarStrategy));

	[TestMethod]
	public Task EveningStarStrategyTest()
		=> RunStrategy(typeof(EveningStarStrategy));

	[TestMethod]
	public Task DojiReversalStrategyTest()
		=> RunStrategy(typeof(DojiReversalStrategy));

	[TestMethod]
	public Task KeltnerChannelReversalStrategyTest()
		=> RunStrategy(typeof(KeltnerChannelReversalStrategy));

	[TestMethod]
	public Task WilliamsPercentRDivergenceStrategyTest()
		=> RunStrategy(typeof(WilliamsPercentRDivergenceStrategy));

	[TestMethod]
	public Task OBVDivergenceStrategyTest()
		=> RunStrategy(typeof(OBVDivergenceStrategy));

	[TestMethod]
	public Task FibonacciRetracementReversalStrategyTest()
		=> RunStrategy(typeof(FibonacciRetracementReversalStrategy));

	[TestMethod]
	public Task InsideBarBreakoutStrategyTest()
		=> RunStrategy(typeof(InsideBarBreakoutStrategy));

	[TestMethod]
	public Task OutsideBarReversalStrategyTest()
		=> RunStrategy(typeof(OutsideBarReversalStrategy));

	[TestMethod]
	public Task TrendlineBounceStrategyTest()
		=> RunStrategy(typeof(TrendlineBounceStrategy));

	[TestMethod]
	public Task PivotPointReversalStrategyTest()
		=> RunStrategy(typeof(PivotPointReversalStrategy));

	[TestMethod]
	public Task VwapBounceStrategyTest()
		=> RunStrategy(typeof(VwapBounceStrategy));

	[TestMethod]
	public Task VolumeExhaustionStrategyTest()
		=> RunStrategy(typeof(VolumeExhaustionStrategy));

	[TestMethod]
	public Task AdxWeakeningStrategyTest()
		=> RunStrategy(typeof(AdxWeakeningStrategy));

	[TestMethod]
	public Task AtrExhaustionStrategyTest()
		=> RunStrategy(typeof(AtrExhaustionStrategy));

	[TestMethod]
	public Task IchimokuTenkanKijunStrategyTest()
		=> RunStrategy(typeof(IchimokuTenkanKijunStrategy));

	[TestMethod]
	public Task HeikinAshiReversalStrategyTest()
		=> RunStrategy(typeof(HeikinAshiReversalStrategy));

	[TestMethod]
	public Task ParabolicSarReversalStrategyTest()
		=> RunStrategy(typeof(ParabolicSarReversalStrategy));

	[TestMethod]
	public Task SupertrendReversalStrategyTest()
		=> RunStrategy(typeof(SupertrendReversalStrategy));

	[TestMethod]
	public Task HullMaReversalStrategyTest()
		=> RunStrategy(typeof(HullMaReversalStrategy));

	[TestMethod]
	public Task DonchianReversalStrategyTest()
		=> RunStrategy(typeof(DonchianReversalStrategy));

	[TestMethod]
	public Task MacdHistogramReversalStrategyTest()
		=> RunStrategy(typeof(MacdHistogramReversalStrategy));

	[TestMethod]
	public Task RsiHookReversalStrategyTest()
		=> RunStrategy(typeof(RsiHookReversalStrategy));

	[TestMethod]
	public Task StochasticHookReversalStrategyTest()
		=> RunStrategy(typeof(StochasticHookReversalStrategy));

	[TestMethod]
	public Task CciHookReversalStrategyTest()
		=> RunStrategy(typeof(CciHookReversalStrategy));

	[TestMethod]
	public Task WilliamsRHookReversalStrategyTest()
		=> RunStrategy(typeof(WilliamsRHookReversalStrategy));

	[TestMethod]
	public Task ThreeWhiteSoldiersStrategyTest()
		=> RunStrategy(typeof(ThreeWhiteSoldiersStrategy));

	[TestMethod]
	public Task ThreeBlackCrowsStrategyTest()
		=> RunStrategy(typeof(ThreeBlackCrowsStrategy));

	[TestMethod]
	public Task GapFillReversalStrategyTest()
		=> RunStrategy(typeof(GapFillReversalStrategy));

	[TestMethod]
	public Task TweezerBottomStrategyTest()
		=> RunStrategy(typeof(TweezerBottomStrategy));

	[TestMethod]
	public Task TweezerTopStrategyTest()
		=> RunStrategy(typeof(TweezerTopStrategy));

	[TestMethod]
	public Task HaramiBullishStrategyTest()
		=> RunStrategy(typeof(HaramiBullishStrategy));

	[TestMethod]
	public Task HaramiBearishStrategyTest()
		=> RunStrategy(typeof(HaramiBearishStrategy));

	[TestMethod]
	public Task DarkPoolPrintsStrategyTest()
		=> RunStrategy(typeof(DarkPoolPrintsStrategy));

	[TestMethod]
	public Task RejectionCandleStrategyTest()
		=> RunStrategy(typeof(RejectionCandleStrategy));

	[TestMethod]
	public Task FalseBreakoutTrapStrategyTest()
		=> RunStrategy(typeof(FalseBreakoutTrapStrategy));

	[TestMethod]
	public Task SpringReversalStrategyTest()
		=> RunStrategy(typeof(SpringReversalStrategy));

	[TestMethod]
	public Task UpthrustReversalStrategyTest()
		=> RunStrategy(typeof(UpthrustReversalStrategy));

	[TestMethod]
	public Task WyckoffAccumulationStrategyTest()
		=> RunStrategy(typeof(WyckoffAccumulationStrategy));

	[TestMethod]
	public Task WyckoffDistributionStrategyTest()
		=> RunStrategy(typeof(WyckoffDistributionStrategy));

	[TestMethod]
	public Task RsiFailureSwingStrategyTest()
		=> RunStrategy(typeof(RsiFailureSwingStrategy));

	[TestMethod]
	public Task StochasticFailureSwingStrategyTest()
		=> RunStrategy(typeof(StochasticFailureSwingStrategy));

	[TestMethod]
	public Task CciFailureSwingStrategyTest()
		=> RunStrategy(typeof(CciFailureSwingStrategy));

	[TestMethod]
	public Task BullishAbandonedBabyStrategyTest()
		=> RunStrategy(typeof(BullishAbandonedBabyStrategy));

	[TestMethod]
	public Task BearishAbandonedBabyStrategyTest()
		=> RunStrategy(typeof(BearishAbandonedBabyStrategy));

	[TestMethod]
	public Task VolumeClimaxReversalStrategyTest()
		=> RunStrategy(typeof(VolumeClimaxReversalStrategy));

	[TestMethod]
	public Task DayOfWeekStrategyTest()
		=> RunStrategy(typeof(DayOfWeekStrategy));

	[TestMethod]
	public Task MonthOfYearStrategyTest()
		=> RunStrategy(typeof(MonthOfYearStrategy));

	[TestMethod]
	public Task TurnaroundTuesdayStrategyTest()
		=> RunStrategy(typeof(TurnaroundTuesdayStrategy));

	[TestMethod]
	public Task EndOfMonthStrengthStrategyTest()
		=> RunStrategy(typeof(EndOfMonthStrengthStrategy));

	[TestMethod]
	public Task FirstDayOfMonthStrategyTest()
		=> RunStrategy(typeof(FirstDayOfMonthStrategy));

	[TestMethod]
	public Task SantaClausRallyStrategyTest()
		=> RunStrategy(typeof(SantaClausRallyStrategy));

	[TestMethod]
	public Task JanuaryEffectStrategyTest()
		=> RunStrategy(typeof(JanuaryEffectStrategy));

	[TestMethod]
	public Task MondayWeaknessStrategyTest()
		=> RunStrategy(typeof(MondayWeaknessStrategy));

	[TestMethod]
	public Task PreHolidayStrengthStrategyTest()
		=> RunStrategy(typeof(PreHolidayStrengthStrategy));

	[TestMethod]
	public Task PostHolidayWeaknessStrategyTest()
		=> RunStrategy(typeof(PostHolidayWeaknessStrategy));

	[TestMethod]
	public Task QuarterlyExpiryStrategyTest()
		=> RunStrategy(typeof(QuarterlyExpiryStrategy));

	[TestMethod]
	public Task OpenDriveStrategyTest()
		=> RunStrategy(typeof(OpenDriveStrategy));

	[TestMethod]
	public Task MiddayReversalStrategyTest()
		=> RunStrategy(typeof(MiddayReversalStrategy));

	[TestMethod]
	public Task OvernightGapStrategyTest()
		=> RunStrategy(typeof(OvernightGapStrategy));

	[TestMethod]
	public Task LunchBreakFadeStrategyTest()
		=> RunStrategy(typeof(LunchBreakFadeStrategy));

	[TestMethod]
	public Task MacdRsiStrategyTest()
		=> RunStrategy(typeof(MacdRsiStrategy));

	[TestMethod]
	public Task BollingerStochasticStrategyTest()
		=> RunStrategy(typeof(BollingerStochasticStrategy));

	[TestMethod]
	public Task MaVolumeStrategyTest()
		=> RunStrategy(typeof(MaVolumeStrategy));

	[TestMethod]
	public Task AdxMacdStrategyTest()
		=> RunStrategy(typeof(AdxMacdStrategy));

	[TestMethod]
	public Task IchimokuRsiStrategyTest()
		=> RunStrategy(typeof(IchimokuRsiStrategy));

	[TestMethod]
	public Task SupertrendVolumeStrategyTest()
		=> RunStrategy(typeof(SupertrendVolumeStrategy));

	[TestMethod]
	public Task BollingerRsiStrategyTest()
		=> RunStrategy(typeof(BollingerRsiStrategy));

	[TestMethod]
	public Task MaStochasticStrategyTest()
		=> RunStrategy(typeof(MaStochasticStrategy));

	[TestMethod]
	public Task AtrMacdStrategyTest()
		=> RunStrategy(typeof(AtrMacdStrategy));

	[TestMethod]
	public Task VwapRsiStrategyTest()
		=> RunStrategy(typeof(VwapRsiStrategy));

	[TestMethod]
	public Task DonchianVolumeStrategyTest()
		=> RunStrategy(typeof(DonchianVolumeStrategy));

	[TestMethod]
	public Task KeltnerStochasticStrategyTest()
		=> RunStrategy(typeof(KeltnerStochasticStrategy));

	[TestMethod]
	public Task ParabolicSarRsiStrategyTest()
		=> RunStrategy(typeof(ParabolicSarRsiStrategy));

	[TestMethod]
	public Task HullMaVolumeStrategyTest()
		=> RunStrategy(typeof(HullMaVolumeStrategy));

	[TestMethod]
	public Task AdxStochasticStrategyTest()
		=> RunStrategy(typeof(AdxStochasticStrategy));

	[TestMethod]
	public Task MacdVolumeStrategyTest()
		=> RunStrategy(typeof(MacdVolumeStrategy));

	[TestMethod]
	public Task BollingerVolumeStrategyTest()
		=> RunStrategy(typeof(BollingerVolumeStrategy));

	[TestMethod]
	public Task RsiStochasticStrategyTest()
		=> RunStrategy(typeof(RsiStochasticStrategy));

	[TestMethod]
	public Task MaAdxStrategyTest()
		=> RunStrategy(typeof(MaAdxStrategy));

	[TestMethod]
	public Task VwapStochasticStrategyTest()
		=> RunStrategy(typeof(VwapStochasticStrategy));

	[TestMethod]
	public Task IchimokuVolumeStrategyTest()
		=> RunStrategy(typeof(IchimokuVolumeStrategy));

	[TestMethod]
	public Task SupertrendRsiStrategyTest()
		=> RunStrategy(typeof(SupertrendRsiStrategy));

	[TestMethod]
	public Task BollingerAdxStrategyTest()
		=> RunStrategy(typeof(BollingerAdxStrategy));

	[TestMethod]
	public Task MaCciStrategyTest()
		=> RunStrategy(typeof(MaCciStrategy));

	[TestMethod]
	public Task VwapVolumeStrategyTest()
		=> RunStrategy(typeof(VwapVolumeStrategy));

	[TestMethod]
	public Task DonchianRsiStrategyTest()
		=> RunStrategy(typeof(DonchianRsiStrategy));

	[TestMethod]
	public Task KeltnerVolumeStrategyTest()
		=> RunStrategy(typeof(KeltnerVolumeStrategy));

	[TestMethod]
	public Task ParabolicSarStochasticStrategyTest()
		=> RunStrategy(typeof(ParabolicSarStochasticStrategy));

	[TestMethod]
	public Task HullMaRsiStrategyTest()
		=> RunStrategy(typeof(HullMaRsiStrategy));

	[TestMethod]
	public Task AdxVolumeStrategyTest()
		=> RunStrategy(typeof(AdxVolumeStrategy));

	[TestMethod]
	public Task MacdCciStrategyTest()
		=> RunStrategy(typeof(MacdCciStrategy));

	[TestMethod]
	public Task BollingerCciStrategyTest()
		=> RunStrategy(typeof(BollingerCciStrategy));

	[TestMethod]
	public Task RsiWilliamsRStrategyTest()
		=> RunStrategy(typeof(RsiWilliamsRStrategy));

	[TestMethod]
	public Task MaWilliamsRStrategyTest()
		=> RunStrategy(typeof(MaWilliamsRStrategy));

	[TestMethod]
	public Task VwapCciStrategyTest()
		=> RunStrategy(typeof(VwapCciStrategy));

	[TestMethod]
	public Task DonchianStochasticStrategyTest()
		=> RunStrategy(typeof(DonchianStochasticStrategy));

	[TestMethod]
	public Task KeltnerRsiStrategyTest()
		=> RunStrategy(typeof(KeltnerRsiStrategy));

	[TestMethod]
	public Task HullMaStochasticStrategyTest()
		=> RunStrategy(typeof(HullMaStochasticStrategy));

	[TestMethod]
	public Task AdxCciStrategyTest()
		=> RunStrategy(typeof(AdxCciStrategy));

	[TestMethod]
	public Task MacdWilliamsRStrategyTest()
		=> RunStrategy(typeof(MacdWilliamsRStrategy));

	[TestMethod]
	public Task BollingerWilliamsRStrategyTest()
		=> RunStrategy(typeof(BollingerWilliamsRStrategy));

	[TestMethod]
	public Task MacdVwapStrategyTest()
		=> RunStrategy(typeof(MacdVwapStrategy));

	[TestMethod]
	public Task RsiSupertrendStrategyTest()
		=> RunStrategy(typeof(RsiSupertrendStrategy));

	[TestMethod]
	public Task AdxBollingerStrategyTest()
		=> RunStrategy(typeof(AdxBollingerStrategy));

	[TestMethod]
	public Task IchimokuStochasticStrategyTest()
		=> RunStrategy(typeof(IchimokuStochasticStrategy));

	[TestMethod]
	public Task SupertrendStochasticStrategyTest()
		=> RunStrategy(typeof(SupertrendStochasticStrategy));

	[TestMethod]
	public Task DonchianMacdStrategyTest()
		=> RunStrategy(typeof(DonchianMacdStrategy));

	[TestMethod]
	public Task ParabolicSarVolumeStrategyTest()
		=> RunStrategy(typeof(ParabolicSarVolumeStrategy));

	[TestMethod]
	public Task VwapAdxStrategyTest()
		=> RunStrategy(typeof(VwapAdxStrategy));

	[TestMethod]
	public Task SupertrendAdxStrategyTest()
		=> RunStrategy(typeof(SupertrendAdxStrategy));

	[TestMethod]
	public Task KeltnerMacdStrategyTest()
		=> RunStrategy(typeof(KeltnerMacdStrategy));

	[TestMethod]
	public Task HullMaAdxStrategyTest()
		=> RunStrategy(typeof(HullMaAdxStrategy));

	[TestMethod]
	public Task VwapMacdStrategyTest()
		=> RunStrategy(typeof(VwapMacdStrategy));

	[TestMethod]
	public Task IchimokuAdxStrategyTest()
		=> RunStrategy(typeof(IchimokuAdxStrategy));

	[TestMethod]
	public Task VwapWilliamsRStrategyTest()
		=> RunStrategy(typeof(VwapWilliamsRStrategy));

	[TestMethod]
	public Task DonchianCciStrategyTest()
		=> RunStrategy(typeof(DonchianCciStrategy));

	[TestMethod]
	public Task KeltnerWilliamsRStrategyTest()
		=> RunStrategy(typeof(KeltnerWilliamsRStrategy));

	[TestMethod]
	public Task ParabolicSarCciStrategyTest()
		=> RunStrategy(typeof(ParabolicSarCciStrategy));

	[TestMethod]
	public Task HullMaCciStrategyTest()
		=> RunStrategy(typeof(HullMaCciStrategy));

	[TestMethod]
	public Task MacdBollingerStrategyTest()
		=> RunStrategy(typeof(MacdBollingerStrategy));

	[TestMethod]
	public Task RsiHullMaStrategyTest()
		=> RunStrategy(typeof(RsiHullMaStrategy));

	[TestMethod]
	public Task StochasticKeltnerStrategyTest()
		=> RunStrategy(typeof(StochasticKeltnerStrategy));

	[TestMethod]
	public Task VolumeSupertrendStrategyTest()
		=> RunStrategy(typeof(VolumeSupertrendStrategy));

	[TestMethod]
	public Task AdxDonchianStrategyTest()
		=> RunStrategy(typeof(AdxDonchianStrategy));

	[TestMethod]
	public Task CciVwapStrategyTest()
		=> RunStrategy(typeof(CciVwapStrategy));

	[TestMethod]
	public Task WilliamsIchimokuStrategyTest()
		=> RunStrategy(typeof(WilliamsIchimokuStrategy));

	[TestMethod]
	public Task MaParabolicSarStrategyTest()
		=> RunStrategy(typeof(MaParabolicSarStrategy));

	[TestMethod]
	public Task BollingerSupertrendStrategyTest()
		=> RunStrategy(typeof(BollingerSupertrendStrategy));

	[TestMethod]
	public Task RsiDonchianStrategyTest()
		=> RunStrategy(typeof(RsiDonchianStrategy));

	[TestMethod]
	public Task MeanReversionStrategyTest()
		=> RunStrategy(typeof(MeanReversionStrategy));

	[TestMethod]
	public Task PairsTradingStrategyTest()
		=> RunStrategy(typeof(PairsTradingStrategy));

	[TestMethod]
	public Task ZScoreReversalStrategyTest()
		=> RunStrategy(typeof(ZScoreReversalStrategy));

	[TestMethod]
	public Task StatisticalArbitrageStrategyTest()
		=> RunStrategy(typeof(StatisticalArbitrageStrategy));

	[TestMethod]
	public Task VolatilityBreakoutStrategyTest()
		=> RunStrategy(typeof(VolatilityBreakoutStrategy));

	[TestMethod]
	public Task BollingerBandSqueezeStrategyTest()
		=> RunStrategy(typeof(BollingerBandSqueezeStrategy));

	[TestMethod]
	public Task CointegrationPairsStrategyTest()
		=> RunStrategy(typeof(CointegrationPairsStrategy));

	[TestMethod]
	public Task MomentumDivergenceStrategyTest()
		=> RunStrategy(typeof(MomentumDivergenceStrategy));

	[TestMethod]
	public Task AtrMeanReversionStrategyTest()
		=> RunStrategy(typeof(AtrMeanReversionStrategy));

	[TestMethod]
	public Task KalmanFilterTrendStrategyTest()
		=> RunStrategy(typeof(KalmanFilterTrendStrategy));

	[TestMethod]
	public Task VolatilityAdjustedMeanReversionStrategyTest()
		=> RunStrategy(typeof(VolatilityAdjustedMeanReversionStrategy));

	[TestMethod]
	public Task HurstExponentTrendStrategyTest()
		=> RunStrategy(typeof(HurstExponentTrendStrategy));

	[TestMethod]
	public Task HurstExponentReversionStrategyTest()
		=> RunStrategy(typeof(HurstExponentReversionStrategy));

	[TestMethod]
	public Task AutocorrelationReversionStrategyTest()
		=> RunStrategy(typeof(AutocorrelationReversionStrategy));

	[TestMethod]
	public Task DeltaNeutralArbitrageStrategyTest()
		=> RunStrategy(typeof(DeltaNeutralArbitrageStrategy));

	[TestMethod]
	public Task VolatilitySkewArbitrageStrategyTest()
		=> RunStrategy(typeof(VolatilitySkewArbitrageStrategy));

	[TestMethod]
	public Task CorrelationBreakoutStrategyTest()
		=> RunStrategy(typeof(CorrelationBreakoutStrategy));

	[TestMethod]
	public Task BetaNeutralArbitrageStrategyTest()
		=> RunStrategy(typeof(BetaNeutralArbitrageStrategy));

	[TestMethod]
	public Task VwapMeanReversionStrategyTest()
		=> RunStrategy(typeof(VwapMeanReversionStrategy));

	[TestMethod]
	public Task RsiMeanReversionStrategyTest()
		=> RunStrategy(typeof(RsiMeanReversionStrategy));

	[TestMethod]
	public Task StochasticMeanReversionStrategyTest()
		=> RunStrategy(typeof(StochasticMeanReversionStrategy));

	[TestMethod]
	public Task CciMeanReversionStrategyTest()
		=> RunStrategy(typeof(CciMeanReversionStrategy));

	[TestMethod]
	public Task WilliamsRMeanReversionStrategyTest()
		=> RunStrategy(typeof(WilliamsRMeanReversionStrategy));

	[TestMethod]
	public Task MacdMeanReversionStrategyTest()
		=> RunStrategy(typeof(MacdMeanReversionStrategy));

	[TestMethod]
	public Task AdxMeanReversionStrategyTest()
		=> RunStrategy(typeof(AdxMeanReversionStrategy));

	[TestMethod]
	public Task VolatilityMeanReversionStrategyTest()
		=> RunStrategy(typeof(VolatilityMeanReversionStrategy));

	[TestMethod]
	public Task VolumeMeanReversionStrategyTest()
		=> RunStrategy(typeof(VolumeMeanReversionStrategy));

	[TestMethod]
	public Task ObvMeanReversionStrategyTest()
		=> RunStrategy(typeof(ObvMeanReversionStrategy));

	[TestMethod]
	public Task MomentumBreakoutStrategyTest()
		=> RunStrategy(typeof(MomentumBreakoutStrategy));

	[TestMethod]
	public Task VwapBreakoutStrategyTest()
		=> RunStrategy(typeof(VwapBreakoutStrategy));

	[TestMethod]
	public Task RsiBreakoutStrategyTest()
		=> RunStrategy(typeof(RsiBreakoutStrategy));

	[TestMethod]
	public Task StochasticBreakoutStrategyTest()
		=> RunStrategy(typeof(StochasticBreakoutStrategy));

	[TestMethod]
	public Task WilliamsRBreakoutStrategyTest()
		=> RunStrategy(typeof(WilliamsRBreakoutStrategy));

	[TestMethod]
	public Task MacdBreakoutStrategyTest()
		=> RunStrategy(typeof(MacdBreakoutStrategy));

	[TestMethod]
	public Task ADXBreakoutStrategyTest()
		=> RunStrategy(typeof(ADXBreakoutStrategy));

	[TestMethod]
	public Task VolumeBreakoutStrategyTest()
		=> RunStrategy(typeof(VolumeBreakoutStrategy));

	[TestMethod]
	public Task BollingerWidthBreakoutStrategyTest()
		=> RunStrategy(typeof(BollingerWidthBreakoutStrategy));

	[TestMethod]
	public Task KeltnerWidthBreakoutStrategyTest()
		=> RunStrategy(typeof(KeltnerWidthBreakoutStrategy));

	[TestMethod]
	public Task DonchianWidthBreakoutStrategyTest()
		=> RunStrategy(typeof(DonchianWidthBreakoutStrategy));

	[TestMethod]
	public Task IchimokuWidthBreakoutStrategyTest()
		=> RunStrategy(typeof(IchimokuWidthBreakoutStrategy));

	[TestMethod]
	public Task SupertrendDistanceBreakoutStrategyTest()
		=> RunStrategy(typeof(SupertrendDistanceBreakoutStrategy));

	[TestMethod]
	public Task ParabolicSarDistanceBreakoutStrategyTest()
		=> RunStrategy(typeof(ParabolicSarDistanceBreakoutStrategy));

	[TestMethod]
	public Task HullMaSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(HullMaSlopeBreakoutStrategy));

	[TestMethod]
	public Task MaSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(MaSlopeBreakoutStrategy));

	[TestMethod]
	public Task EmaSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(EmaSlopeBreakoutStrategy));

	[TestMethod]
	public Task VolatilityAdjustedMomentumStrategyTest()
		=> RunStrategy(typeof(VolatilityAdjustedMomentumStrategy));

	[TestMethod]
	public Task VwapSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(VwapSlopeBreakoutStrategy));

	[TestMethod]
	public Task RsiSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(RsiSlopeBreakoutStrategy));

	[TestMethod]
	public Task StochasticSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(StochasticSlopeBreakoutStrategy));

	[TestMethod]
	public Task CciSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(CciSlopeBreakoutStrategy));

	[TestMethod]
	public Task WilliamsRSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(WilliamsRSlopeBreakoutStrategy));

	[TestMethod]
	public Task MacdSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(MacdSlopeBreakoutStrategy));

	[TestMethod]
	public Task AdxSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(AdxSlopeBreakoutStrategy));

	[TestMethod]
	public Task AtrSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(AtrSlopeBreakoutStrategy));

	[TestMethod]
	public Task VolumeSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(VolumeSlopeBreakoutStrategy));

	[TestMethod]
	public Task ObvSlopeBreakoutStrategyTest()
		=> RunStrategy(typeof(ObvSlopeBreakoutStrategy));

	[TestMethod]
	public Task BollingerWidthMeanReversionStrategyTest()
		=> RunStrategy(typeof(BollingerWidthMeanReversionStrategy));

	[TestMethod]
	public Task KeltnerWidthMeanReversionStrategyTest()
		=> RunStrategy(typeof(KeltnerWidthMeanReversionStrategy));

	[TestMethod]
	public Task DonchianWidthMeanReversionStrategyTest()
		=> RunStrategy(typeof(DonchianWidthMeanReversionStrategy));

	[TestMethod]
	public Task IchimokuCloudWidthMeanReversionStrategyTest()
		=> RunStrategy(typeof(IchimokuCloudWidthMeanReversionStrategy));

	[TestMethod]
	public Task SupertrendDistanceMeanReversionStrategyTest()
		=> RunStrategy(typeof(SupertrendDistanceMeanReversionStrategy));

	[TestMethod]
	public Task ParabolicSarDistanceMeanReversionStrategyTest()
		=> RunStrategy(typeof(ParabolicSarDistanceMeanReversionStrategy));

	[TestMethod]
	public Task HullMaSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(HullMaSlopeMeanReversionStrategy));

	[TestMethod]
	public Task MaSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(MaSlopeMeanReversionStrategy));

	[TestMethod]
	public Task EmaSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(EmaSlopeMeanReversionStrategy));

	[TestMethod]
	public Task VwapSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(VwapSlopeMeanReversionStrategy));

	[TestMethod]
	public Task RsiSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(RsiSlopeMeanReversionStrategy));

	[TestMethod]
	public Task StochasticSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(StochasticSlopeMeanReversionStrategy));

	[TestMethod]
	public Task CciSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(CciSlopeMeanReversionStrategy));

	[TestMethod]
	public Task WilliamsRSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(WilliamsRSlopeMeanReversionStrategy));

	[TestMethod]
	public Task MacdSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(MacdSlopeMeanReversionStrategy));

	[TestMethod]
	public Task AdxSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(AdxSlopeMeanReversionStrategy));

	[TestMethod]
	public Task AtrSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(AtrSlopeMeanReversionStrategy));

	[TestMethod]
	public Task VolumeSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(VolumeSlopeMeanReversionStrategy));

	[TestMethod]
	public Task ObvSlopeMeanReversionStrategyTest()
		=> RunStrategy(typeof(ObvSlopeMeanReversionStrategy));

	[TestMethod]
	public Task PairsTradingVolatilityFilterStrategyTest()
		=> RunStrategy(typeof(PairsTradingVolatilityFilterStrategy));

	[TestMethod]
	public Task ZScoreVolumeFilterStrategyTest()
		=> RunStrategy(typeof(ZScoreVolumeFilterStrategy));

	[TestMethod]
	public Task CorrelationMeanReversionStrategyTest()
		=> RunStrategy(typeof(CorrelationMeanReversionStrategy));

	[TestMethod]
	public Task BetaAdjustedPairsStrategyTest()
		=> RunStrategy(typeof(BetaAdjustedPairsStrategy));

	[TestMethod]
	public Task HurstVolatilityFilterStrategyTest()
		=> RunStrategy(typeof(HurstVolatilityFilterStrategy));

	[TestMethod]
	public Task AdaptiveEmaBreakoutStrategyTest()
		=> RunStrategy(typeof(AdaptiveEmaBreakoutStrategy));

	[TestMethod]
	public Task VolatilityClusterBreakoutStrategyTest()
		=> RunStrategy(typeof(VolatilityClusterBreakoutStrategy));

	[TestMethod]
	public Task SeasonalityAdjustedMomentumStrategyTest()
		=> RunStrategy(typeof(SeasonalityAdjustedMomentumStrategy));

	[TestMethod]
	public Task RsiDynamicOverboughtOversoldStrategyTest()
		=> RunStrategy(typeof(RsiDynamicOverboughtOversoldStrategy));

	[TestMethod]
	public Task BollingerVolatilityBreakoutStrategyTest()
		=> RunStrategy(typeof(BollingerVolatilityBreakoutStrategy));

	[TestMethod]
	public Task MacdAdaptiveHistogramStrategyTest()
		=> RunStrategy(typeof(MacdAdaptiveHistogramStrategy));

	[TestMethod]
	public Task IchimokuVolumeClusterStrategyTest()
		=> RunStrategy(typeof(IchimokuVolumeClusterStrategy));

	[TestMethod]
	public Task SupertrendWithMomentumStrategyTest()
		=> RunStrategy(typeof(SupertrendWithMomentumStrategy));

	[TestMethod]
	public Task DonchianWithVolatilityContractionStrategyTest()
		=> RunStrategy(typeof(DonchianWithVolatilityContractionStrategy));

	[TestMethod]
	public Task KeltnerWithRsiDivergenceStrategyTest()
		=> RunStrategy(typeof(KeltnerWithRsiDivergenceStrategy));

	[TestMethod]
	public Task HullMaWithVolumeSpikeStrategyTest()
		=> RunStrategy(typeof(HullMaWithVolumeSpikeStrategy));

	[TestMethod]
	public Task VwapWithAdxTrendStrengthStrategyTest()
		=> RunStrategy(typeof(VwapWithAdxTrendStrengthStrategy));

	[TestMethod]
	public Task ParabolicSarWithVolatilityExpansionStrategyTest()
		=> RunStrategy(typeof(ParabolicSarWithVolatilityExpansionStrategy));

	[TestMethod]
	public Task StochasticWithDynamicZonesStrategyTest()
		=> RunStrategy(typeof(StochasticWithDynamicZonesStrategy));

	[TestMethod]
	public Task AdxWithVolumeBreakoutStrategyTest()
		=> RunStrategy(typeof(AdxWithVolumeBreakoutStrategy));

	[TestMethod]
	public Task CciWithVolatilityFilterStrategyTest()
		=> RunStrategy(typeof(CciWithVolatilityFilterStrategy));

	[TestMethod]
	public Task WilliamsPercentRWithMomentumStrategyTest()
		=> RunStrategy(typeof(WilliamsPercentRWithMomentumStrategy));

	[TestMethod]
	public Task BollingerKMeansStrategyTest()
		=> RunStrategy(typeof(BollingerKMeansStrategy));

	[TestMethod]
	public Task MacdHmmStrategyTest()
		=> RunStrategy(typeof(MacdHmmStrategy));

	[TestMethod]
	public Task IchimokuHurstStrategyTest()
		=> RunStrategy(typeof(IchimokuHurstStrategy));

	[TestMethod]
	public Task SupertrendRsiDivergenceStrategyTest()
		=> RunStrategy(typeof(SupertrendRsiDivergenceStrategy));

	[TestMethod]
	public Task DonchianSeasonalStrategyTest()
		=> RunStrategy(typeof(DonchianSeasonalStrategy));

	[TestMethod]
	public Task KeltnerKalmanStrategyTest()
		=> RunStrategy(typeof(KeltnerKalmanStrategy));

	[TestMethod]
	public Task HullMaVolatilityContractionStrategyTest()
		=> RunStrategy(typeof(HullMaVolatilityContractionStrategy));

	[TestMethod]
	public Task VwapAdxTrendStrategyTest()
		=> RunStrategy(typeof(VwapAdxTrendStrategy));

	[TestMethod]
	public Task ParabolicSarHurstStrategyTest()
		=> RunStrategy(typeof(ParabolicSarHurstStrategy));

	[TestMethod]
	public Task BollingerKalmanFilterStrategyTest()
		=> RunStrategy(typeof(BollingerKalmanFilterStrategy));

	[TestMethod]
	public Task MacdVolumeClusterStrategyTest()
		=> RunStrategy(typeof(MacdVolumeClusterStrategy));

	[TestMethod]
	public Task IchimokuVolatilityContractionStrategyTest()
		=> RunStrategy(typeof(IchimokuVolatilityContractionStrategy));

	[TestMethod]
	public Task DonchianHurstStrategyTest()
		=> RunStrategy(typeof(DonchianHurstStrategy));

	[TestMethod]
	public Task KeltnerSeasonalStrategyTest()
		=> RunStrategy(typeof(KeltnerSeasonalStrategy));

	[TestMethod]
	public Task HullKMeansClusterStrategyTest()
		=> RunStrategy(typeof(HullKMeansClusterStrategy));

	[TestMethod]
	public Task VwapHiddenMarkovModelStrategyTest()
		=> RunStrategy(typeof(VwapHiddenMarkovModelStrategy));

	[TestMethod]
	public Task ParabolicSarRsiDivergenceStrategyTest()
		=> RunStrategy(typeof(ParabolicSarRsiDivergenceStrategy));

	[TestMethod]
	public Task AdaptiveRsiVolumeStrategyTest()
		=> RunStrategy(typeof(AdaptiveRsiVolumeStrategy));

	[TestMethod]
	public Task AdaptiveBollingerBreakoutStrategyTest()
		=> RunStrategy(typeof(AdaptiveBollingerBreakoutStrategy));

	[TestMethod]
	public Task MacdWithSentimentFilterStrategyTest()
		=> RunStrategy(typeof(MacdWithSentimentFilterStrategy));

	[TestMethod]
	public Task IchimokuWithImpliedVolatilityStrategyTest()
		=> RunStrategy(typeof(IchimokuWithImpliedVolatilityStrategy));

	[TestMethod]
	public Task SupertrendWithPutCallRatioStrategyTest()
		=> RunStrategy(typeof(SupertrendWithPutCallRatioStrategy));

	[TestMethod]
	public Task DonchianWithSentimentSpikeStrategyTest()
		=> RunStrategy(typeof(DonchianWithSentimentSpikeStrategy));

	[TestMethod]
	public Task KeltnerWithRLSignalStrategyTest()
		=> RunStrategy(typeof(KeltnerWithRLSignalStrategy));

	[TestMethod]
	public Task HullMAWithImpliedVolatilityBreakoutStrategyTest()
		=> RunStrategy(typeof(HullMAWithImpliedVolatilityBreakoutStrategy));

	[TestMethod]
	public Task VwapWithBehavioralBiasFilterStrategyTest()
		=> RunStrategy(typeof(VwapWithBehavioralBiasFilterStrategy));

	[TestMethod]
	public Task ParabolicSarSentimentDivergenceStrategyTest()
		=> RunStrategy(typeof(ParabolicSarSentimentDivergenceStrategy));

	[TestMethod]
	public Task RsiWithOptionOpenInterestStrategyTest()
		=> RunStrategy(typeof(RsiWithOptionOpenInterestStrategy));

	[TestMethod]
	public Task StochasticImpliedVolatilitySkewStrategyTest()
		=> RunStrategy(typeof(StochasticImpliedVolatilitySkewStrategy));

	[TestMethod]
	public Task AdxSentimentMomentumStrategyTest()
		=> RunStrategy(typeof(AdxSentimentMomentumStrategy));

	[TestMethod]
	public Task CciPutCallRatioDivergenceStrategyTest()
		=> RunStrategy(typeof(CciPutCallRatioDivergenceStrategy));
}
