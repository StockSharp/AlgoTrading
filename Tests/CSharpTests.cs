namespace StockSharp.Tests;

using StockSharp.Samples.Strategies;

[TestClass]
public class CSharpTests
{
	public static Task RunStrategy<T>(Action<T, Security> extra = null)
		where T : Strategy
		=> AsmInit.RunStrategy(TypeHelper.CreateInstance<T>(typeof(T)), extra);

	[TestMethod]
	public Task MaCrossoverStrategyTest()
		=> RunStrategy<MaCrossoverStrategy>();

	[TestMethod]
	public Task NdayBreakoutStrategyTest()
		=> RunStrategy<NdayBreakoutStrategy>();

	[TestMethod]
	public Task AdxTrendStrategyTest()
		=> RunStrategy<AdxTrendStrategy>();

	[TestMethod]
	public Task ParabolicSarTrendStrategyTest()
		=> RunStrategy<ParabolicSarTrendStrategy>();

	[TestMethod]
	public Task DonchianChannelStrategyTest()
		=> RunStrategy<DonchianChannelStrategy>();

	[TestMethod]
	public Task TripleMAStrategyTest()
		=> RunStrategy<TripleMAStrategy>();

	[TestMethod]
	public Task KeltnerChannelBreakoutStrategyTest()
		=> RunStrategy<KeltnerChannelBreakoutStrategy>();

	[TestMethod]
	public Task HullMaTrendStrategyTest()
		=> RunStrategy<HullMaTrendStrategy>();

	[TestMethod]
	public Task MacdTrendStrategyTest()
		=> RunStrategy<MacdTrendStrategy>();

	[TestMethod]
	public Task SupertrendStrategyTest()
		=> RunStrategy<SupertrendStrategy>();

	[TestMethod]
	public Task IchimokuKumoBreakoutStrategyTest()
		=> RunStrategy<IchimokuKumoBreakoutStrategy>();

	[TestMethod]
	public Task HeikinAshiConsecutiveStrategyTest()
		=> RunStrategy<HeikinAshiConsecutiveStrategy>();

	[TestMethod]
	public Task DmiPowerMoveStrategyTest()
		=> RunStrategy<DmiPowerMoveStrategy>();

	[TestMethod]
	public Task TradingviewSupertrendFlipStrategyTest()
		=> RunStrategy<TradingViewSupertrendFlipStrategy>();

	[TestMethod]
	public Task GannSwingBreakoutStrategyTest()
		=> RunStrategy<GannSwingBreakoutStrategy>();

	[TestMethod]
	public Task RsiDivergenceStrategyTest()
		=> RunStrategy<RsiDivergenceStrategy>();

	[TestMethod]
	public Task WilliamsPercentRStrategyTest()
		=> RunStrategy<WilliamsPercentRStrategy>();

	[TestMethod]
	public Task RocImpulseStrategyTest()
		=> RunStrategy<RocImpulseStrategy>();

	[TestMethod]
	public Task CciBreakoutStrategyTest()
		=> RunStrategy<CciBreakoutStrategy>();

	[TestMethod]
	public Task MomentumPercentageStrategyTest()
		=> RunStrategy<MomentumPercentageStrategy>();

	[TestMethod]
	public Task BollingerSqueezeStrategyTest()
		=> RunStrategy<BollingerSqueezeStrategy>();

	[TestMethod]
	public Task AdxDiStrategyTest()
		=> RunStrategy<AdxDiStrategy>();

	[TestMethod]
	public Task ElderImpulseStrategyTest()
		=> RunStrategy<ElderImpulseStrategy>();

	[TestMethod]
	public Task LaguerreRsiStrategyTest()
		=> RunStrategy<LaguerreRsiStrategy>();

	[TestMethod]
	public Task StochasticRsiCrossStrategyTest()
		=> RunStrategy<StochasticRsiCrossStrategy>();

	[TestMethod]
	public Task RsiReversionStrategyTest()
		=> RunStrategy<RsiReversionStrategy>();

	[TestMethod]
	public Task BollingerReversionStrategyTest()
		=> RunStrategy<BollingerReversionStrategy>();

	[TestMethod]
	public Task ZScoreStrategyTest()
		=> RunStrategy<ZScoreStrategy>();

	[TestMethod]
	public Task MADeviationStrategyTest()
		=> RunStrategy<MADeviationStrategy>();

	[TestMethod]
	public Task VwapReversionStrategyTest()
		=> RunStrategy<VwapReversionStrategy>();

	[TestMethod]
	public Task KeltnerReversionStrategyTest()
		=> RunStrategy<KeltnerReversionStrategy>();

	[TestMethod]
	public Task AtrReversionStrategyTest()
		=> RunStrategy<AtrReversionStrategy>();

	[TestMethod]
	public Task MacdZeroStrategyTest()
		=> RunStrategy<MacdZeroStrategy>();

	[TestMethod]
	public Task LowVolReversionStrategyTest()
		=> RunStrategy<LowVolReversionStrategy>();

	[TestMethod]
	public Task BollingerPercentBStrategyTest()
		=> RunStrategy<BollingerPercentBStrategy>();

	[TestMethod]
	public Task AtrExpansionStrategyTest()
		=> RunStrategy<AtrExpansionStrategy>();

	[TestMethod]
	public Task VixTriggerStrategyTest()
		=> RunStrategy<VixTriggerStrategy>();

	[TestMethod]
	public Task BollingerBandWidthStrategyTest()
		=> RunStrategy<BollingerBandWidthStrategy>();

	[TestMethod]
	public Task HvBreakoutStrategyTest()
		=> RunStrategy<HvBreakoutStrategy>();

	[TestMethod]
	public Task AtrTrailingStrategyTest()
		=> RunStrategy<AtrTrailingStrategy>();

	[TestMethod]
	public Task VolAdjustedMaStrategyTest()
		=> RunStrategy<VolAdjustedMAStrategy>();

	[TestMethod]
	public Task IVSpikeStrategyTest()
		=> RunStrategy<IVSpikeStrategy>();

	[TestMethod]
	public Task VCPStrategyTest()
		=> RunStrategy<VCPStrategy>();

	[TestMethod]
	public Task ATRRangeStrategyTest()
		=> RunStrategy<ATRRangeStrategy>();

	[TestMethod]
	public Task ChoppinessIndexBreakoutStrategyTest()
		=> RunStrategy<ChoppinessIndexBreakoutStrategy>();

	[TestMethod]
	public Task VolumeSpikeStrategyTest()
		=> RunStrategy<VolumeSpikeStrategy>();

	[TestMethod]
	public Task OBVBreakoutStrategyTest()
		=> RunStrategy<OBVBreakoutStrategy>();

	[TestMethod]
	public Task VWAPBreakoutStrategyTest()
		=> RunStrategy<VWAPBreakoutStrategy>();

	[TestMethod]
	public Task VWMAStrategyTest()
		=> RunStrategy<VWMAStrategy>();

	[TestMethod]
	public Task ADStrategyTest()
		=> RunStrategy<ADStrategy>();

	[TestMethod]
	public Task VolumeWeightedPriceBreakoutStrategyTest()
		=> RunStrategy<VolumeWeightedPriceBreakoutStrategy>();

	[TestMethod]
	public Task VolumeDivergenceStrategyTest()
		=> RunStrategy<VolumeDivergenceStrategy>();

	[TestMethod]
	public Task VolumeMAXrossStrategyTest()
		=> RunStrategy<VolumeMAXrossStrategy>();

	[TestMethod]
	public Task CumulativeDeltaBreakoutStrategyTest()
		=> RunStrategy<CumulativeDeltaBreakoutStrategy>();

	[TestMethod]
	public Task VolumeSurgeStrategyTest()
		=> RunStrategy<VolumeSurgeStrategy>();

	[TestMethod]
	public Task DoubleBottomStrategyTest()
		=> RunStrategy<DoubleBottomStrategy>();

	[TestMethod]
	public Task DoubleTopStrategyTest()
		=> RunStrategy<DoubleTopStrategy>();

	[TestMethod]
	public Task RsiOverboughtOversoldStrategyTest()
		=> RunStrategy<RsiOverboughtOversoldStrategy>();

	[TestMethod]
	public Task HammerCandleStrategyTest()
		=> RunStrategy<HammerCandleStrategy>();

	[TestMethod]
	public Task ShootingStarStrategyTest()
		=> RunStrategy<ShootingStarStrategy>();

	[TestMethod]
	public Task MacdDivergenceStrategyTest()
		=> RunStrategy<MacdDivergenceStrategy>();

	[TestMethod]
	public Task StochasticOverboughtOversoldStrategyTest()
		=> RunStrategy<StochasticOverboughtOversoldStrategy>();

	[TestMethod]
	public Task EngulfingBullishStrategyTest()
		=> RunStrategy<EngulfingBullishStrategy>();

	[TestMethod]
	public Task EngulfingBearishStrategyTest()
		=> RunStrategy<EngulfingBearishStrategy>();

	[TestMethod]
	public Task PinbarReversalStrategyTest()
		=> RunStrategy<PinbarReversalStrategy>();

	[TestMethod]
	public Task ThreeBarReversalUpStrategyTest()
		=> RunStrategy<ThreeBarReversalUpStrategy>();

	[TestMethod]
	public Task ThreeBarReversalDownStrategyTest()
		=> RunStrategy<ThreeBarReversalDownStrategy>();

	[TestMethod]
	public Task CciDivergenceStrategyTest()
		=> RunStrategy<CciDivergenceStrategy>();

	[TestMethod]
	public Task BollingerBandReversalStrategyTest()
		=> RunStrategy<BollingerBandReversalStrategy>();

	[TestMethod]
	public Task MorningStarStrategyTest()
		=> RunStrategy<MorningStarStrategy>();

	[TestMethod]
	public Task EveningStarStrategyTest()
		=> RunStrategy<EveningStarStrategy>();

	[TestMethod]
	public Task DojiReversalStrategyTest()
		=> RunStrategy<DojiReversalStrategy>();

	[TestMethod]
	public Task KeltnerChannelReversalStrategyTest()
		=> RunStrategy<KeltnerChannelReversalStrategy>();

	[TestMethod]
	public Task WilliamsPercentRDivergenceStrategyTest()
		=> RunStrategy<WilliamsPercentRDivergenceStrategy>();

	[TestMethod]
	public Task OBVDivergenceStrategyTest()
		=> RunStrategy<OBVDivergenceStrategy>();

	[TestMethod]
	public Task FibonacciRetracementReversalStrategyTest()
		=> RunStrategy<FibonacciRetracementReversalStrategy>();

	[TestMethod]
	public Task InsideBarBreakoutStrategyTest()
		=> RunStrategy<InsideBarBreakoutStrategy>();

	[TestMethod]
	public Task OutsideBarReversalStrategyTest()
		=> RunStrategy<OutsideBarReversalStrategy>();

	[TestMethod]
	public Task TrendlineBounceStrategyTest()
		=> RunStrategy<TrendlineBounceStrategy>();

	[TestMethod]
	public Task PivotPointReversalStrategyTest()
		=> RunStrategy<PivotPointReversalStrategy>();

	[TestMethod]
	public Task VwapBounceStrategyTest()
		=> RunStrategy<VwapBounceStrategy>();

	[TestMethod]
	public Task VolumeExhaustionStrategyTest()
		=> RunStrategy<VolumeExhaustionStrategy>();

	[TestMethod]
	public Task AdxWeakeningStrategyTest()
		=> RunStrategy<AdxWeakeningStrategy>();

	[TestMethod]
	public Task AtrExhaustionStrategyTest()
		=> RunStrategy<AtrExhaustionStrategy>();

	[TestMethod]
	public Task IchimokuTenkanKijunStrategyTest()
		=> RunStrategy<IchimokuTenkanKijunStrategy>();

	[TestMethod]
	public Task HeikinAshiReversalStrategyTest()
		=> RunStrategy<HeikinAshiReversalStrategy>();

	[TestMethod]
	public Task ParabolicSarReversalStrategyTest()
		=> RunStrategy<ParabolicSarReversalStrategy>();

	[TestMethod]
	public Task SupertrendReversalStrategyTest()
		=> RunStrategy<SupertrendReversalStrategy>();

	[TestMethod]
	public Task HullMaReversalStrategyTest()
		=> RunStrategy<HullMaReversalStrategy>();

	[TestMethod]
	public Task DonchianReversalStrategyTest()
		=> RunStrategy<DonchianReversalStrategy>();

	[TestMethod]
	public Task MacdHistogramReversalStrategyTest()
		=> RunStrategy<MacdHistogramReversalStrategy>();

	[TestMethod]
	public Task RsiHookReversalStrategyTest()
		=> RunStrategy<RsiHookReversalStrategy>();

	[TestMethod]
	public Task StochasticHookReversalStrategyTest()
		=> RunStrategy<StochasticHookReversalStrategy>();

	[TestMethod]
	public Task CciHookReversalStrategyTest()
		=> RunStrategy<CciHookReversalStrategy>();

	[TestMethod]
	public Task WilliamsRHookReversalStrategyTest()
		=> RunStrategy<WilliamsRHookReversalStrategy>();

	[TestMethod]
	public Task ThreeWhiteSoldiersStrategyTest()
		=> RunStrategy<ThreeWhiteSoldiersStrategy>();

	[TestMethod]
	public Task ThreeBlackCrowsStrategyTest()
		=> RunStrategy<ThreeBlackCrowsStrategy>();

	[TestMethod]
	public Task GapFillReversalStrategyTest()
		=> RunStrategy<GapFillReversalStrategy>();

	[TestMethod]
	public Task TweezerBottomStrategyTest()
		=> RunStrategy<TweezerBottomStrategy>();

	[TestMethod]
	public Task TweezerTopStrategyTest()
		=> RunStrategy<TweezerTopStrategy>();

	[TestMethod]
	public Task HaramiBullishStrategyTest()
		=> RunStrategy<HaramiBullishStrategy>();

	[TestMethod]
	public Task HaramiBearishStrategyTest()
		=> RunStrategy<HaramiBearishStrategy>();

	[TestMethod]
	public Task DarkPoolPrintsStrategyTest()
		=> RunStrategy<DarkPoolPrintsStrategy>();

	[TestMethod]
	public Task RejectionCandleStrategyTest()
		=> RunStrategy<RejectionCandleStrategy>();

	[TestMethod]
	public Task FalseBreakoutTrapStrategyTest()
		=> RunStrategy<FalseBreakoutTrapStrategy>();

	[TestMethod]
	public Task SpringReversalStrategyTest()
		=> RunStrategy<SpringReversalStrategy>();

	[TestMethod]
	public Task UpthrustReversalStrategyTest()
		=> RunStrategy<UpthrustReversalStrategy>();

	[TestMethod]
	public Task WyckoffAccumulationStrategyTest()
		=> RunStrategy<WyckoffAccumulationStrategy>();

	[TestMethod]
	public Task WyckoffDistributionStrategyTest()
		=> RunStrategy<WyckoffDistributionStrategy>();

	[TestMethod]
	public Task RsiFailureSwingStrategyTest()
		=> RunStrategy<RsiFailureSwingStrategy>();

	[TestMethod]
	public Task StochasticFailureSwingStrategyTest()
		=> RunStrategy<StochasticFailureSwingStrategy>();

	[TestMethod]
	public Task CciFailureSwingStrategyTest()
		=> RunStrategy<CciFailureSwingStrategy>();

	[TestMethod]
	public Task BullishAbandonedBabyStrategyTest()
		=> RunStrategy<BullishAbandonedBabyStrategy>();

	[TestMethod]
	public Task BearishAbandonedBabyStrategyTest()
		=> RunStrategy<BearishAbandonedBabyStrategy>();

	[TestMethod]
	public Task VolumeClimaxReversalStrategyTest()
		=> RunStrategy<VolumeClimaxReversalStrategy>();

	[TestMethod]
	public Task DayOfWeekStrategyTest()
		=> RunStrategy<DayOfWeekStrategy>();

	[TestMethod]
	public Task MonthOfYearStrategyTest()
		=> RunStrategy<MonthOfYearStrategy>();

	[TestMethod]
	public Task TurnaroundTuesdayStrategyTest()
		=> RunStrategy<TurnaroundTuesdayStrategy>();

	[TestMethod]
	public Task EndOfMonthStrengthStrategyTest()
		=> RunStrategy<EndOfMonthStrengthStrategy>();

	[TestMethod]
	public Task FirstDayOfMonthStrategyTest()
		=> RunStrategy<FirstDayOfMonthStrategy>();

	[TestMethod]
	public Task SantaClausRallyStrategyTest()
		=> RunStrategy<SantaClausRallyStrategy>();

	[TestMethod]
	public Task JanuaryEffectStrategyTest()
		=> RunStrategy<JanuaryEffectStrategy>();

	[TestMethod]
	public Task MondayWeaknessStrategyTest()
		=> RunStrategy<MondayWeaknessStrategy>();

	[TestMethod]
	public Task PreHolidayStrengthStrategyTest()
		=> RunStrategy<PreHolidayStrengthStrategy>();

	[TestMethod]
	public Task PostHolidayWeaknessStrategyTest()
		=> RunStrategy<PostHolidayWeaknessStrategy>();

	[TestMethod]
	public Task QuarterlyExpiryStrategyTest()
		=> RunStrategy<QuarterlyExpiryStrategy>();

	[TestMethod]
	public Task OpenDriveStrategyTest()
		=> RunStrategy<OpenDriveStrategy>();

	[TestMethod]
	public Task MiddayReversalStrategyTest()
		=> RunStrategy<MiddayReversalStrategy>();

	[TestMethod]
	public Task OvernightGapStrategyTest()
		=> RunStrategy<OvernightGapStrategy>();

	[TestMethod]
	public Task LunchBreakFadeStrategyTest()
		=> RunStrategy<LunchBreakFadeStrategy>();

	[TestMethod]
	public Task MacdRsiStrategyTest()
		=> RunStrategy<MacdRsiStrategy>();

	[TestMethod]
	public Task BollingerStochasticStrategyTest()
		=> RunStrategy<BollingerStochasticStrategy>();

	[TestMethod]
	public Task MaVolumeStrategyTest()
		=> RunStrategy<MaVolumeStrategy>();

	[TestMethod]
	public Task AdxMacdStrategyTest()
		=> RunStrategy<AdxMacdStrategy>();

	[TestMethod]
	public Task IchimokuRsiStrategyTest()
		=> RunStrategy<IchimokuRsiStrategy>();

	[TestMethod]
	public Task SupertrendVolumeStrategyTest()
		=> RunStrategy<SupertrendVolumeStrategy>();

	[TestMethod]
	public Task BollingerRsiStrategyTest()
		=> RunStrategy<BollingerRsiStrategy>();

	[TestMethod]
	public Task MaStochasticStrategyTest()
		=> RunStrategy<MaStochasticStrategy>();

	[TestMethod]
	public Task AtrMacdStrategyTest()
		=> RunStrategy<AtrMacdStrategy>();

	[TestMethod]
	public Task VwapRsiStrategyTest()
		=> RunStrategy<VwapRsiStrategy>();

	[TestMethod]
	public Task DonchianVolumeStrategyTest()
		=> RunStrategy<DonchianVolumeStrategy>();

	[TestMethod]
	public Task KeltnerStochasticStrategyTest()
		=> RunStrategy<KeltnerStochasticStrategy>();

	[TestMethod]
	public Task ParabolicSarRsiStrategyTest()
		=> RunStrategy<ParabolicSarRsiStrategy>();

	[TestMethod]
	public Task HullMaVolumeStrategyTest()
		=> RunStrategy<HullMaVolumeStrategy>();

	[TestMethod]
	public Task AdxStochasticStrategyTest()
		=> RunStrategy<AdxStochasticStrategy>();

	[TestMethod]
	public Task MacdVolumeStrategyTest()
		=> RunStrategy<MacdVolumeStrategy>();

	[TestMethod]
	public Task BollingerVolumeStrategyTest()
		=> RunStrategy<BollingerVolumeStrategy>();

	[TestMethod]
	public Task RsiStochasticStrategyTest()
		=> RunStrategy<RsiStochasticStrategy>();

	[TestMethod]
	public Task MaAdxStrategyTest()
		=> RunStrategy<MaAdxStrategy>();

	[TestMethod]
	public Task VwapStochasticStrategyTest()
		=> RunStrategy<VwapStochasticStrategy>();

	[TestMethod]
	public Task IchimokuVolumeStrategyTest()
		=> RunStrategy<IchimokuVolumeStrategy>();

	[TestMethod]
	public Task SupertrendRsiStrategyTest()
		=> RunStrategy<SupertrendRsiStrategy>();

	[TestMethod]
	public Task BollingerAdxStrategyTest()
		=> RunStrategy<BollingerAdxStrategy>();

	[TestMethod]
	public Task MaCciStrategyTest()
		=> RunStrategy<MaCciStrategy>();

	[TestMethod]
	public Task VwapVolumeStrategyTest()
		=> RunStrategy<VwapVolumeStrategy>();

	[TestMethod]
	public Task DonchianRsiStrategyTest()
		=> RunStrategy<DonchianRsiStrategy>();

	[TestMethod]
	public Task KeltnerVolumeStrategyTest()
		=> RunStrategy<KeltnerVolumeStrategy>();

	[TestMethod]
	public Task ParabolicSarStochasticStrategyTest()
		=> RunStrategy<ParabolicSarStochasticStrategy>();

	[TestMethod]
	public Task HullMaRsiStrategyTest()
		=> RunStrategy<HullMaRsiStrategy>();

	[TestMethod]
	public Task AdxVolumeStrategyTest()
		=> RunStrategy<AdxVolumeStrategy>();

	[TestMethod]
	public Task MacdCciStrategyTest()
		=> RunStrategy<MacdCciStrategy>();

	[TestMethod]
	public Task BollingerCciStrategyTest()
		=> RunStrategy<BollingerCciStrategy>();

	[TestMethod]
	public Task RsiWilliamsRStrategyTest()
		=> RunStrategy<RsiWilliamsRStrategy>();

	[TestMethod]
	public Task MaWilliamsRStrategyTest()
		=> RunStrategy<MaWilliamsRStrategy>();

	[TestMethod]
	public Task VwapCciStrategyTest()
		=> RunStrategy<VwapCciStrategy>();

	[TestMethod]
	public Task DonchianStochasticStrategyTest()
		=> RunStrategy<DonchianStochasticStrategy>();

	[TestMethod]
	public Task KeltnerRsiStrategyTest()
		=> RunStrategy<KeltnerRsiStrategy>();

	[TestMethod]
	public Task HullMaStochasticStrategyTest()
		=> RunStrategy<HullMaStochasticStrategy>();

	[TestMethod]
	public Task AdxCciStrategyTest()
		=> RunStrategy<AdxCciStrategy>();

	[TestMethod]
	public Task MacdWilliamsRStrategyTest()
		=> RunStrategy<MacdWilliamsRStrategy>();

	[TestMethod]
	public Task BollingerWilliamsRStrategyTest()
		=> RunStrategy<BollingerWilliamsRStrategy>();

	[TestMethod]
	public Task MacdVwapStrategyTest()
		=> RunStrategy<MacdVwapStrategy>();

	[TestMethod]
	public Task RsiSupertrendStrategyTest()
		=> RunStrategy<RsiSupertrendStrategy>();

	[TestMethod]
	public Task AdxBollingerStrategyTest()
		=> RunStrategy<AdxBollingerStrategy>();

	[TestMethod]
	public Task IchimokuStochasticStrategyTest()
		=> RunStrategy<IchimokuStochasticStrategy>();

	[TestMethod]
	public Task SupertrendStochasticStrategyTest()
		=> RunStrategy<SupertrendStochasticStrategy>();

	[TestMethod]
	public Task DonchianMacdStrategyTest()
		=> RunStrategy<DonchianMacdStrategy>();

	[TestMethod]
	public Task ParabolicSarVolumeStrategyTest()
		=> RunStrategy<ParabolicSarVolumeStrategy>();

	[TestMethod]
	public Task VwapAdxStrategyTest()
		=> RunStrategy<VwapAdxStrategy>();

	[TestMethod]
	public Task SupertrendAdxStrategyTest()
		=> RunStrategy<SupertrendAdxStrategy>();

	[TestMethod]
	public Task KeltnerMacdStrategyTest()
		=> RunStrategy<KeltnerMacdStrategy>();

	[TestMethod]
	public Task HullMaAdxStrategyTest()
		=> RunStrategy<HullMaAdxStrategy>();

	[TestMethod]
	public Task VwapMacdStrategyTest()
		=> RunStrategy<VwapMacdStrategy>();

	[TestMethod]
	public Task IchimokuAdxStrategyTest()
		=> RunStrategy<IchimokuAdxStrategy>();

	[TestMethod]
	public Task VwapWilliamsRStrategyTest()
		=> RunStrategy<VwapWilliamsRStrategy>();

	[TestMethod]
	public Task DonchianCciStrategyTest()
		=> RunStrategy<DonchianCciStrategy>();

	[TestMethod]
	public Task KeltnerWilliamsRStrategyTest()
		=> RunStrategy<KeltnerWilliamsRStrategy>();

	[TestMethod]
	public Task ParabolicSarCciStrategyTest()
		=> RunStrategy<ParabolicSarCciStrategy>();

	[TestMethod]
	public Task HullMaCciStrategyTest()
		=> RunStrategy<HullMaCciStrategy>();

	[TestMethod]
	public Task MacdBollingerStrategyTest()
		=> RunStrategy<MacdBollingerStrategy>();

	[TestMethod]
	public Task RsiHullMaStrategyTest()
		=> RunStrategy<RsiHullMaStrategy>();

	[TestMethod]
	public Task StochasticKeltnerStrategyTest()
		=> RunStrategy<StochasticKeltnerStrategy>();

	[TestMethod]
	public Task VolumeSupertrendStrategyTest()
		=> RunStrategy<VolumeSupertrendStrategy>();

	[TestMethod]
	public Task AdxDonchianStrategyTest()
		=> RunStrategy<AdxDonchianStrategy>();

	[TestMethod]
	public Task CciVwapStrategyTest()
		=> RunStrategy<CciVwapStrategy>();

	[TestMethod]
	public Task WilliamsIchimokuStrategyTest()
		=> RunStrategy<WilliamsIchimokuStrategy>();

	[TestMethod]
	public Task MaParabolicSarStrategyTest()
		=> RunStrategy<MaParabolicSarStrategy>();

	[TestMethod]
	public Task BollingerSupertrendStrategyTest()
		=> RunStrategy<BollingerSupertrendStrategy>();

	[TestMethod]
	public Task RsiDonchianStrategyTest()
		=> RunStrategy<RsiDonchianStrategy>();

	[TestMethod]
	public Task MeanReversionStrategyTest()
		=> RunStrategy<MeanReversionStrategy>();

	[TestMethod]
	public Task PairsTradingStrategyTest()
		=> RunStrategy<PairsTradingStrategy>((strategy, sec) =>
		{
			strategy.SecondSecurity = sec;
		});

	[TestMethod]
	public Task ZScoreReversalStrategyTest()
		=> RunStrategy<ZScoreReversalStrategy>();

	[TestMethod]
	public Task StatisticalArbitrageStrategyTest()
		=> RunStrategy<StatisticalArbitrageStrategy>((strategy, sec) =>
		{
			strategy.SecondSecurity = sec;
		});

	[TestMethod]
	public Task VolatilityBreakoutStrategyTest()
		=> RunStrategy<VolatilityBreakoutStrategy>();

	[TestMethod]
	public Task BollingerBandSqueezeStrategyTest()
		=> RunStrategy<BollingerBandSqueezeStrategy>();

	[TestMethod]
	public Task CointegrationPairsStrategyTest()
		=> RunStrategy<CointegrationPairsStrategy>((strategy, sec) => strategy.Asset2 = sec);

	[TestMethod]
	public Task MomentumDivergenceStrategyTest()
		=> RunStrategy<MomentumDivergenceStrategy>();

	[TestMethod]
	public Task AtrMeanReversionStrategyTest()
		=> RunStrategy<AtrMeanReversionStrategy>();

	[TestMethod]
	public Task KalmanFilterTrendStrategyTest()
		=> RunStrategy<KalmanFilterTrendStrategy>();

	[TestMethod]
	public Task VolatilityAdjustedMeanReversionStrategyTest()
		=> RunStrategy<VolatilityAdjustedMeanReversionStrategy>();

	[TestMethod]
	public Task HurstExponentTrendStrategyTest()
		=> RunStrategy<HurstExponentTrendStrategy>();

	[TestMethod]
	public Task HurstExponentReversionStrategyTest()
		=> RunStrategy<HurstExponentReversionStrategy>();

	[TestMethod]
	public Task AutocorrelationReversionStrategyTest()
		=> RunStrategy<AutocorrelationReversionStrategy>();

	[TestMethod]
	public Task DeltaNeutralArbitrageStrategyTest()
		=> RunStrategy<DeltaNeutralArbitrageStrategy>((strategy, sec) =>
		{
			strategy.Asset2Security = sec;
			strategy.Asset2Portfolio = strategy.Portfolio;
		});

	[TestMethod]
	public Task VolatilitySkewArbitrageStrategyTest()
		=> RunStrategy<VolatilitySkewArbitrageStrategy>();

	[TestMethod]
	public Task CorrelationBreakoutStrategyTest()
		=> RunStrategy<CorrelationBreakoutStrategy>();

	[TestMethod]
	public Task BetaNeutralArbitrageStrategyTest()
		=> RunStrategy<BetaNeutralArbitrageStrategy>();

	[TestMethod]
	public Task VwapMeanReversionStrategyTest()
		=> RunStrategy<VwapMeanReversionStrategy>();

	[TestMethod]
	public Task RsiMeanReversionStrategyTest()
		=> RunStrategy<RsiMeanReversionStrategy>();

	[TestMethod]
	public Task StochasticMeanReversionStrategyTest()
		=> RunStrategy<StochasticMeanReversionStrategy>();

	[TestMethod]
	public Task CciMeanReversionStrategyTest()
		=> RunStrategy<CciMeanReversionStrategy>();

	[TestMethod]
	public Task WilliamsRMeanReversionStrategyTest()
		=> RunStrategy<WilliamsRMeanReversionStrategy>();

	[TestMethod]
	public Task MacdMeanReversionStrategyTest()
		=> RunStrategy<MacdMeanReversionStrategy>();

	[TestMethod]
	public Task AdxMeanReversionStrategyTest()
		=> RunStrategy<AdxMeanReversionStrategy>();

	[TestMethod]
	public Task VolatilityMeanReversionStrategyTest()
		=> RunStrategy<VolatilityMeanReversionStrategy>();

	[TestMethod]
	public Task VolumeMeanReversionStrategyTest()
		=> RunStrategy<VolumeMeanReversionStrategy>();

	[TestMethod]
	public Task ObvMeanReversionStrategyTest()
		=> RunStrategy<ObvMeanReversionStrategy>();

	[TestMethod]
	public Task MomentumBreakoutStrategyTest()
		=> RunStrategy<MomentumBreakoutStrategy>();

	[TestMethod]
	public Task RsiBreakoutStrategyTest()
		=> RunStrategy<RsiBreakoutStrategy>();

	[TestMethod]
	public Task StochasticBreakoutStrategyTest()
		=> RunStrategy<StochasticBreakoutStrategy>();

	[TestMethod]
	public Task WilliamsRBreakoutStrategyTest()
		=> RunStrategy<WilliamsRBreakoutStrategy>();

	[TestMethod]
	public Task MacdBreakoutStrategyTest()
		=> RunStrategy<MacdBreakoutStrategy>();

	[TestMethod]
	public Task ADXBreakoutStrategyTest()
		=> RunStrategy<ADXBreakoutStrategy>();

	[TestMethod]
	public Task VolumeBreakoutStrategyTest()
		=> RunStrategy<VolumeBreakoutStrategy>();

	[TestMethod]
	public Task BollingerWidthBreakoutStrategyTest()
		=> RunStrategy<BollingerWidthBreakoutStrategy>();

	[TestMethod]
	public Task KeltnerWidthBreakoutStrategyTest()
		=> RunStrategy<KeltnerWidthBreakoutStrategy>();

	[TestMethod]
	public Task DonchianWidthBreakoutStrategyTest()
		=> RunStrategy<DonchianWidthBreakoutStrategy>();

	[TestMethod]
	public Task IchimokuWidthBreakoutStrategyTest()
		=> RunStrategy<IchimokuWidthBreakoutStrategy>();

	[TestMethod]
	public Task SupertrendDistanceBreakoutStrategyTest()
		=> RunStrategy<SupertrendDistanceBreakoutStrategy>();

	[TestMethod]
	public Task ParabolicSarDistanceBreakoutStrategyTest()
		=> RunStrategy<ParabolicSarDistanceBreakoutStrategy>();

	[TestMethod]
	public Task HullMaSlopeBreakoutStrategyTest()
		=> RunStrategy<HullMaSlopeBreakoutStrategy>();

	[TestMethod]
	public Task MaSlopeBreakoutStrategyTest()
		=> RunStrategy<MaSlopeBreakoutStrategy>();

	[TestMethod]
	public Task EmaSlopeBreakoutStrategyTest()
		=> RunStrategy<EmaSlopeBreakoutStrategy>();

	[TestMethod]
	public Task VolatilityAdjustedMomentumStrategyTest()
		=> RunStrategy<VolatilityAdjustedMomentumStrategy>();

	[TestMethod]
	public Task VwapSlopeBreakoutStrategyTest()
		=> RunStrategy<VwapSlopeBreakoutStrategy>();

	[TestMethod]
	public Task RsiSlopeBreakoutStrategyTest()
		=> RunStrategy<RsiSlopeBreakoutStrategy>();

	[TestMethod]
	public Task StochasticSlopeBreakoutStrategyTest()
		=> RunStrategy<StochasticSlopeBreakoutStrategy>();

	[TestMethod]
	public Task CciSlopeBreakoutStrategyTest()
		=> RunStrategy<CciSlopeBreakoutStrategy>();

	[TestMethod]
	public Task WilliamsRSlopeBreakoutStrategyTest()
		=> RunStrategy<WilliamsRSlopeBreakoutStrategy>();

	[TestMethod]
	public Task MacdSlopeBreakoutStrategyTest()
		=> RunStrategy<MacdSlopeBreakoutStrategy>();

	[TestMethod]
	public Task AdxSlopeBreakoutStrategyTest()
		=> RunStrategy<AdxSlopeBreakoutStrategy>();

	[TestMethod]
	public Task AtrSlopeBreakoutStrategyTest()
		=> RunStrategy<AtrSlopeBreakoutStrategy>();

	[TestMethod]
	public Task VolumeSlopeBreakoutStrategyTest()
		=> RunStrategy<VolumeSlopeBreakoutStrategy>();

	[TestMethod]
	public Task ObvSlopeBreakoutStrategyTest()
		=> RunStrategy<ObvSlopeBreakoutStrategy>();

	[TestMethod]
	public Task BollingerWidthMeanReversionStrategyTest()
		=> RunStrategy<BollingerWidthMeanReversionStrategy>();

	[TestMethod]
	public Task KeltnerWidthMeanReversionStrategyTest()
		=> RunStrategy<KeltnerWidthMeanReversionStrategy>();

	[TestMethod]
	public Task DonchianWidthMeanReversionStrategyTest()
		=> RunStrategy<DonchianWidthMeanReversionStrategy>();

	[TestMethod]
	public Task IchimokuCloudWidthMeanReversionStrategyTest()
		=> RunStrategy<IchimokuCloudWidthMeanReversionStrategy>();

	[TestMethod]
	public Task SupertrendDistanceMeanReversionStrategyTest()
		=> RunStrategy<SupertrendDistanceMeanReversionStrategy>();

	[TestMethod]
	public Task ParabolicSarDistanceMeanReversionStrategyTest()
		=> RunStrategy<ParabolicSarDistanceMeanReversionStrategy>();

	[TestMethod]
	public Task HullMaSlopeMeanReversionStrategyTest()
		=> RunStrategy<HullMaSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task MaSlopeMeanReversionStrategyTest()
		=> RunStrategy<MaSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task EmaSlopeMeanReversionStrategyTest()
		=> RunStrategy<EmaSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task VwapSlopeMeanReversionStrategyTest()
		=> RunStrategy<VwapSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task RsiSlopeMeanReversionStrategyTest()
		=> RunStrategy<RsiSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task StochasticSlopeMeanReversionStrategyTest()
		=> RunStrategy<StochasticSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task CciSlopeMeanReversionStrategyTest()
		=> RunStrategy<CciSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task WilliamsRSlopeMeanReversionStrategyTest()
		=> RunStrategy<WilliamsRSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task MacdSlopeMeanReversionStrategyTest()
		=> RunStrategy<MacdSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task AdxSlopeMeanReversionStrategyTest()
		=> RunStrategy<AdxSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task AtrSlopeMeanReversionStrategyTest()
		=> RunStrategy<AtrSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task VolumeSlopeMeanReversionStrategyTest()
		=> RunStrategy<VolumeSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task ObvSlopeMeanReversionStrategyTest()
		=> RunStrategy<ObvSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task PairsTradingVolatilityFilterStrategyTest()
		=> RunStrategy<PairsTradingVolatilityFilterStrategy>((strategy, sec) =>
		{
			strategy.Security2 = sec;
		});

	[TestMethod]
	public Task ZScoreVolumeFilterStrategyTest()
		=> RunStrategy<ZScoreVolumeFilterStrategy>();

	[TestMethod]
	public Task CorrelationMeanReversionStrategyTest()
		=> RunStrategy<CorrelationMeanReversionStrategy>((strategy, sec) => strategy.Security2 = sec);

	[TestMethod]
	public Task BetaAdjustedPairsStrategyTest()
		=> RunStrategy<BetaAdjustedPairsStrategy>((strategy, sec) =>
		{
			strategy.Asset2 = sec;
			strategy.Asset2Portfolio = strategy.Portfolio;
		});

	[TestMethod]
	public Task HurstVolatilityFilterStrategyTest()
		=> RunStrategy<HurstVolatilityFilterStrategy>();

	[TestMethod]
	public Task AdaptiveEmaBreakoutStrategyTest()
		=> RunStrategy<AdaptiveEmaBreakoutStrategy>();

	[TestMethod]
	public Task VolatilityClusterBreakoutStrategyTest()
		=> RunStrategy<VolatilityClusterBreakoutStrategy>();

	[TestMethod]
	public Task SeasonalityAdjustedMomentumStrategyTest()
		=> RunStrategy<SeasonalityAdjustedMomentumStrategy>();

	[TestMethod]
	public Task RsiDynamicOverboughtOversoldStrategyTest()
		=> RunStrategy<RsiDynamicOverboughtOversoldStrategy>();

	[TestMethod]
	public Task BollingerVolatilityBreakoutStrategyTest()
		=> RunStrategy<BollingerVolatilityBreakoutStrategy>();

	[TestMethod]
	public Task MacdAdaptiveHistogramStrategyTest()
		=> RunStrategy<MacdAdaptiveHistogramStrategy>();

	[TestMethod]
	public Task IchimokuVolumeClusterStrategyTest()
		=> RunStrategy<IchimokuVolumeClusterStrategy>();

	[TestMethod]
	public Task SupertrendWithMomentumStrategyTest()
		=> RunStrategy<SupertrendWithMomentumStrategy>();

	[TestMethod]
	public Task DonchianWithVolatilityContractionStrategyTest()
		=> RunStrategy<DonchianWithVolatilityContractionStrategy>();

	[TestMethod]
	public Task KeltnerWithRsiDivergenceStrategyTest()
		=> RunStrategy<KeltnerWithRsiDivergenceStrategy>();

	[TestMethod]
	public Task HullMaWithVolumeSpikeStrategyTest()
		=> RunStrategy<HullMaWithVolumeSpikeStrategy>();

	[TestMethod]
	public Task VwapWithAdxTrendStrengthStrategyTest()
		=> RunStrategy<VwapWithAdxTrendStrengthStrategy>();

	[TestMethod]
	public Task ParabolicSarWithVolatilityExpansionStrategyTest()
		=> RunStrategy<ParabolicSarWithVolatilityExpansionStrategy>();

	[TestMethod]
	public Task StochasticWithDynamicZonesStrategyTest()
		=> RunStrategy<StochasticWithDynamicZonesStrategy>();

	[TestMethod]
	public Task AdxWithVolumeBreakoutStrategyTest()
		=> RunStrategy<AdxWithVolumeBreakoutStrategy>();

	[TestMethod]
	public Task CciWithVolatilityFilterStrategyTest()
		=> RunStrategy<CciWithVolatilityFilterStrategy>();

	[TestMethod]
	public Task WilliamsPercentRWithMomentumStrategyTest()
		=> RunStrategy<WilliamsPercentRWithMomentumStrategy>();

	[TestMethod]
	public Task BollingerKMeansStrategyTest()
		=> RunStrategy<BollingerKMeansStrategy>();

	[TestMethod]
	public Task MacdHmmStrategyTest()
		=> RunStrategy<MacdHmmStrategy>();

	[TestMethod]
	public Task IchimokuHurstStrategyTest()
		=> RunStrategy<IchimokuHurstStrategy>();

	[TestMethod]
	public Task SupertrendRsiDivergenceStrategyTest()
		=> RunStrategy<SupertrendRsiDivergenceStrategy>();

	[TestMethod]
	public Task DonchianSeasonalStrategyTest()
		=> RunStrategy<DonchianSeasonalStrategy>();

	[TestMethod]
	public Task KeltnerKalmanStrategyTest()
		=> RunStrategy<KeltnerKalmanStrategy>();

	[TestMethod]
	public Task HullMaVolatilityContractionStrategyTest()
		=> RunStrategy<HullMaVolatilityContractionStrategy>();

	[TestMethod]
	public Task VwapAdxTrendStrategyTest()
		=> RunStrategy<VwapAdxTrendStrategy>();

	[TestMethod]
	public Task ParabolicSarHurstStrategyTest()
		=> RunStrategy<ParabolicSarHurstStrategy>();

	[TestMethod]
	public Task BollingerKalmanFilterStrategyTest()
		=> RunStrategy<BollingerKalmanFilterStrategy>();

	[TestMethod]
	public Task MacdVolumeClusterStrategyTest()
		=> RunStrategy<MacdVolumeClusterStrategy>();

	[TestMethod]
	public Task IchimokuVolatilityContractionStrategyTest()
		=> RunStrategy<IchimokuVolatilityContractionStrategy>();

	[TestMethod]
	public Task DonchianHurstStrategyTest()
		=> RunStrategy<DonchianHurstStrategy>();

	[TestMethod]
	public Task KeltnerSeasonalStrategyTest()
		=> RunStrategy<KeltnerSeasonalStrategy>();

	[TestMethod]
	public Task HullKMeansClusterStrategyTest()
		=> RunStrategy<HullKMeansClusterStrategy>();

	[TestMethod]
	public Task VwapHiddenMarkovModelStrategyTest()
		=> RunStrategy<VwapHiddenMarkovModelStrategy>();

	[TestMethod]
	public Task ParabolicSarRsiDivergenceStrategyTest()
		=> RunStrategy<ParabolicSarRsiDivergenceStrategy>();

	[TestMethod]
	public Task AdaptiveRsiVolumeStrategyTest()
		=> RunStrategy<AdaptiveRsiVolumeStrategy>();

	[TestMethod]
	public Task AdaptiveBollingerBreakoutStrategyTest()
		=> RunStrategy<AdaptiveBollingerBreakoutStrategy>();

	[TestMethod]
	public Task MacdWithSentimentFilterStrategyTest()
		=> RunStrategy<MacdWithSentimentFilterStrategy>();

	[TestMethod]
	public Task IchimokuWithImpliedVolatilityStrategyTest()
		=> RunStrategy<IchimokuWithImpliedVolatilityStrategy>();

	[TestMethod]
	public Task SupertrendWithPutCallRatioStrategyTest()
		=> RunStrategy<SupertrendWithPutCallRatioStrategy>();

	[TestMethod]
	public Task DonchianWithSentimentSpikeStrategyTest()
		=> RunStrategy<DonchianWithSentimentSpikeStrategy>();

	[TestMethod]
	public Task KeltnerWithRLSignalStrategyTest()
		=> RunStrategy<KeltnerWithRLSignalStrategy>();

	[TestMethod]
	public Task HullMAWithImpliedVolatilityBreakoutStrategyTest()
		=> RunStrategy<HullMAWithImpliedVolatilityBreakoutStrategy>();

	[TestMethod]
	public Task VwapWithBehavioralBiasFilterStrategyTest()
		=> RunStrategy<VwapWithBehavioralBiasFilterStrategy>();

	[TestMethod]
	public Task ParabolicSarSentimentDivergenceStrategyTest()
		=> RunStrategy<ParabolicSarSentimentDivergenceStrategy>();

	[TestMethod]
	public Task RsiWithOptionOpenInterestStrategyTest()
		=> RunStrategy<RsiWithOptionOpenInterestStrategy>();

	[TestMethod]
	public Task StochasticImpliedVolatilitySkewStrategyTest()
		=> RunStrategy<StochasticImpliedVolatilitySkewStrategy>();

	[TestMethod]
	public Task AdxSentimentMomentumStrategyTest()
		=> RunStrategy<AdxSentimentMomentumStrategy>();

	[TestMethod]
	public Task CciPutCallRatioDivergenceStrategyTest()
		=> RunStrategy<CciPutCallRatioDivergenceStrategy>();
}
