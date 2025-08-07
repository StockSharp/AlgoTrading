namespace StockSharp.Tests;

using StockSharp.Samples.Strategies;

[TestClass]
public class CSharpTests
{
	public static Task RunStrategy<T>(Action<T, Security> extra = null)
		where T : Strategy
		=> AsmInit.RunStrategy(TypeHelper.CreateInstance<T>(typeof(T)), extra);

	[TestMethod]
	public Task MaCrossoverStrategy()
		=> RunStrategy<MaCrossoverStrategy>();

	[TestMethod]
	public Task NdayBreakoutStrategy()
		=> RunStrategy<NdayBreakoutStrategy>();

	[TestMethod]
	public Task AdxTrendStrategy()
		=> RunStrategy<AdxTrendStrategy>();

	[TestMethod]
	public Task ParabolicSarTrendStrategy()
		=> RunStrategy<ParabolicSarTrendStrategy>();

	[TestMethod]
	public Task DonchianChannelStrategy()
		=> RunStrategy<DonchianChannelStrategy>();

	[TestMethod]
	public Task TripleMAStrategy()
		=> RunStrategy<TripleMAStrategy>();

	[TestMethod]
	public Task KeltnerChannelBreakoutStrategy()
		=> RunStrategy<KeltnerChannelBreakoutStrategy>();

	[TestMethod]
	public Task HullMaTrendStrategy()
		=> RunStrategy<HullMaTrendStrategy>();

	[TestMethod]
	public Task MacdTrendStrategy()
		=> RunStrategy<MacdTrendStrategy>();

	[TestMethod]
	public Task SupertrendStrategy()
		=> RunStrategy<SupertrendStrategy>();

	[TestMethod]
	public Task IchimokuKumoBreakoutStrategy()
		=> RunStrategy<IchimokuKumoBreakoutStrategy>();

	[TestMethod]
	public Task HeikinAshiConsecutiveStrategy()
		=> RunStrategy<HeikinAshiConsecutiveStrategy>();

	[TestMethod]
	public Task DmiPowerMoveStrategy()
		=> RunStrategy<DmiPowerMoveStrategy>();

	[TestMethod]
	public Task TradingviewSupertrendFlipStrategy()
		=> RunStrategy<TradingViewSupertrendFlipStrategy>();

	[TestMethod]
	public Task GannSwingBreakoutStrategy()
		=> RunStrategy<GannSwingBreakoutStrategy>();

	[TestMethod]
	public Task RsiDivergenceStrategy()
		=> RunStrategy<RsiDivergenceStrategy>();

	[TestMethod]
	public Task WilliamsPercentRStrategy()
		=> RunStrategy<WilliamsPercentRStrategy>();

	[TestMethod]
	public Task RocImpulseStrategy()
		=> RunStrategy<RocImpulseStrategy>();

	[TestMethod]
	public Task CciBreakoutStrategy()
		=> RunStrategy<CciBreakoutStrategy>();

	[TestMethod]
	public Task MomentumPercentageStrategy()
		=> RunStrategy<MomentumPercentageStrategy>();

	[TestMethod]
	public Task BollingerSqueezeStrategy()
		=> RunStrategy<BollingerSqueezeStrategy>();

	[TestMethod]
	public Task AdxDiStrategy()
		=> RunStrategy<AdxDiStrategy>();

	[TestMethod]
	public Task ElderImpulseStrategy()
		=> RunStrategy<ElderImpulseStrategy>();

	[TestMethod]
	public Task LaguerreRsiStrategy()
		=> RunStrategy<LaguerreRsiStrategy>();

	[TestMethod]
	public Task StochasticRsiCrossStrategy()
		=> RunStrategy<StochasticRsiCrossStrategy>();

	[TestMethod]
	public Task RsiReversionStrategy()
		=> RunStrategy<RsiReversionStrategy>();

	[TestMethod]
	public Task BollingerReversionStrategy()
		=> RunStrategy<BollingerReversionStrategy>();

	[TestMethod]
	public Task ZScoreStrategy()
		=> RunStrategy<ZScoreStrategy>();

	[TestMethod]
	public Task MADeviationStrategy()
		=> RunStrategy<MADeviationStrategy>();

	[TestMethod]
	public Task VwapReversionStrategy()
		=> RunStrategy<VwapReversionStrategy>();

	[TestMethod]
	public Task KeltnerReversionStrategy()
		=> RunStrategy<KeltnerReversionStrategy>();

	[TestMethod]
	public Task AtrReversionStrategy()
		=> RunStrategy<AtrReversionStrategy>();

	[TestMethod]
	public Task MacdZeroStrategy()
		=> RunStrategy<MacdZeroStrategy>();

	[TestMethod]
	public Task LowVolReversionStrategy()
		=> RunStrategy<LowVolReversionStrategy>();

	[TestMethod]
	public Task BollingerPercentBStrategy()
		=> RunStrategy<BollingerPercentBStrategy>();

	[TestMethod]
	public Task AtrExpansionStrategy()
		=> RunStrategy<AtrExpansionStrategy>();

	[TestMethod]
	public Task VixTriggerStrategy()
		=> RunStrategy<VixTriggerStrategy>();

	[TestMethod]
	public Task BollingerBandWidthStrategy()
		=> RunStrategy<BollingerBandWidthStrategy>();

	[TestMethod]
	public Task HvBreakoutStrategy()
		=> RunStrategy<HvBreakoutStrategy>();

	[TestMethod]
	public Task AtrTrailingStrategy()
		=> RunStrategy<AtrTrailingStrategy>();

	[TestMethod]
	public Task VolAdjustedMaStrategy()
		=> RunStrategy<VolAdjustedMAStrategy>();

	[TestMethod]
	public Task IVSpikeStrategy()
		=> RunStrategy<IVSpikeStrategy>();

	[TestMethod]
	public Task VCPStrategy()
		=> RunStrategy<VCPStrategy>();

	[TestMethod]
	public Task ATRRangeStrategy()
		=> RunStrategy<ATRRangeStrategy>();

	[TestMethod]
	public Task ChoppinessIndexBreakoutStrategy()
		=> RunStrategy<ChoppinessIndexBreakoutStrategy>();

	[TestMethod]
	public Task VolumeSpikeStrategy()
		=> RunStrategy<VolumeSpikeStrategy>();

	[TestMethod]
	public Task OBVBreakoutStrategy()
		=> RunStrategy<OBVBreakoutStrategy>();

	[TestMethod]
	public Task VWAPBreakoutStrategy()
		=> RunStrategy<VWAPBreakoutStrategy>();

	[TestMethod]
	public Task VWMAStrategy()
		=> RunStrategy<VWMAStrategy>();

	[TestMethod]
	public Task ADStrategy()
		=> RunStrategy<ADStrategy>();

	[TestMethod]
	public Task VolumeWeightedPriceBreakoutStrategy()
		=> RunStrategy<VolumeWeightedPriceBreakoutStrategy>();

	[TestMethod]
	public Task VolumeDivergenceStrategy()
		=> RunStrategy<VolumeDivergenceStrategy>();

	[TestMethod]
	public Task VolumeMAXrossStrategy()
		=> RunStrategy<VolumeMAXrossStrategy>();

	[TestMethod]
	public Task CumulativeDeltaBreakoutStrategy()
		=> RunStrategy<CumulativeDeltaBreakoutStrategy>();

	[TestMethod]
	public Task VolumeSurgeStrategy()
		=> RunStrategy<VolumeSurgeStrategy>();

	[TestMethod]
	public Task DoubleBottomStrategy()
		=> RunStrategy<DoubleBottomStrategy>();

	[TestMethod]
	public Task DoubleTopStrategy()
		=> RunStrategy<DoubleTopStrategy>();

	[TestMethod]
	public Task RsiOverboughtOversoldStrategy()
		=> RunStrategy<RsiOverboughtOversoldStrategy>();

	[TestMethod]
	public Task HammerCandleStrategy()
		=> RunStrategy<HammerCandleStrategy>();

	[TestMethod]
	public Task ShootingStarStrategy()
		=> RunStrategy<ShootingStarStrategy>();

	[TestMethod]
	public Task MacdDivergenceStrategy()
		=> RunStrategy<MacdDivergenceStrategy>();

	[TestMethod]
	public Task StochasticOverboughtOversoldStrategy()
		=> RunStrategy<StochasticOverboughtOversoldStrategy>();

	[TestMethod]
	public Task EngulfingBullishStrategy()
		=> RunStrategy<EngulfingBullishStrategy>();

	[TestMethod]
	public Task EngulfingBearishStrategy()
		=> RunStrategy<EngulfingBearishStrategy>();

	[TestMethod]
	public Task PinbarReversalStrategy()
		=> RunStrategy<PinbarReversalStrategy>();

	[TestMethod]
	public Task ThreeBarReversalUpStrategy()
		=> RunStrategy<ThreeBarReversalUpStrategy>();

	[TestMethod]
	public Task ThreeBarReversalDownStrategy()
		=> RunStrategy<ThreeBarReversalDownStrategy>();

	[TestMethod]
	public Task CciDivergenceStrategy()
		=> RunStrategy<CciDivergenceStrategy>();

	[TestMethod]
	public Task BollingerBandReversalStrategy()
		=> RunStrategy<BollingerBandReversalStrategy>();

	[TestMethod]
	public Task MorningStarStrategy()
		=> RunStrategy<MorningStarStrategy>();

	[TestMethod]
	public Task EveningStarStrategy()
		=> RunStrategy<EveningStarStrategy>();

	[TestMethod]
	public Task DojiReversalStrategy()
		=> RunStrategy<DojiReversalStrategy>();

	[TestMethod]
	public Task KeltnerChannelReversalStrategy()
		=> RunStrategy<KeltnerChannelReversalStrategy>();

	[TestMethod]
	public Task WilliamsPercentRDivergenceStrategy()
		=> RunStrategy<WilliamsPercentRDivergenceStrategy>();

	[TestMethod]
	public Task OBVDivergenceStrategy()
		=> RunStrategy<OBVDivergenceStrategy>();

	[TestMethod]
	public Task FibonacciRetracementReversalStrategy()
		=> RunStrategy<FibonacciRetracementReversalStrategy>();

	[TestMethod]
	public Task InsideBarBreakoutStrategy()
		=> RunStrategy<InsideBarBreakoutStrategy>();

	[TestMethod]
	public Task OutsideBarReversalStrategy()
		=> RunStrategy<OutsideBarReversalStrategy>();

	[TestMethod]
	public Task TrendlineBounceStrategy()
		=> RunStrategy<TrendlineBounceStrategy>();

	[TestMethod]
	public Task PivotPointReversalStrategy()
		=> RunStrategy<PivotPointReversalStrategy>();

	[TestMethod]
	public Task VwapBounceStrategy()
		=> RunStrategy<VwapBounceStrategy>();

	[TestMethod]
	public Task VolumeExhaustionStrategy()
		=> RunStrategy<VolumeExhaustionStrategy>();

	[TestMethod]
	public Task AdxWeakeningStrategy()
		=> RunStrategy<AdxWeakeningStrategy>();

	[TestMethod]
	public Task AtrExhaustionStrategy()
		=> RunStrategy<AtrExhaustionStrategy>();

	[TestMethod]
	public Task IchimokuTenkanKijunStrategy()
		=> RunStrategy<IchimokuTenkanKijunStrategy>();

	[TestMethod]
	public Task HeikinAshiReversalStrategy()
		=> RunStrategy<HeikinAshiReversalStrategy>();

	[TestMethod]
	public Task ParabolicSarReversalStrategy()
		=> RunStrategy<ParabolicSarReversalStrategy>();

	[TestMethod]
	public Task SupertrendReversalStrategy()
		=> RunStrategy<SupertrendReversalStrategy>();

	[TestMethod]
	public Task HullMaReversalStrategy()
		=> RunStrategy<HullMaReversalStrategy>();

	[TestMethod]
	public Task DonchianReversalStrategy()
		=> RunStrategy<DonchianReversalStrategy>();

	[TestMethod]
	public Task MacdHistogramReversalStrategy()
		=> RunStrategy<MacdHistogramReversalStrategy>();

	[TestMethod]
	public Task RsiHookReversalStrategy()
		=> RunStrategy<RsiHookReversalStrategy>();

	[TestMethod]
	public Task StochasticHookReversalStrategy()
		=> RunStrategy<StochasticHookReversalStrategy>();

	[TestMethod]
	public Task CciHookReversalStrategy()
		=> RunStrategy<CciHookReversalStrategy>();

	[TestMethod]
	public Task WilliamsRHookReversalStrategy()
		=> RunStrategy<WilliamsRHookReversalStrategy>();

	[TestMethod]
	public Task ThreeWhiteSoldiersStrategy()
		=> RunStrategy<ThreeWhiteSoldiersStrategy>();

	[TestMethod]
	public Task ThreeBlackCrowsStrategy()
		=> RunStrategy<ThreeBlackCrowsStrategy>();

	[TestMethod]
	public Task GapFillReversalStrategy()
		=> RunStrategy<GapFillReversalStrategy>();

	[TestMethod]
	public Task TweezerBottomStrategy()
		=> RunStrategy<TweezerBottomStrategy>();

	[TestMethod]
	public Task TweezerTopStrategy()
		=> RunStrategy<TweezerTopStrategy>();

	[TestMethod]
	public Task HaramiBullishStrategy()
		=> RunStrategy<HaramiBullishStrategy>();

	[TestMethod]
	public Task HaramiBearishStrategy()
		=> RunStrategy<HaramiBearishStrategy>();

	[TestMethod]
	public Task DarkPoolPrintsStrategy()
		=> RunStrategy<DarkPoolPrintsStrategy>();

	[TestMethod]
	public Task RejectionCandleStrategy()
		=> RunStrategy<RejectionCandleStrategy>();

	[TestMethod]
	public Task FalseBreakoutTrapStrategy()
		=> RunStrategy<FalseBreakoutTrapStrategy>();

	[TestMethod]
	public Task SpringReversalStrategy()
		=> RunStrategy<SpringReversalStrategy>();

	[TestMethod]
	public Task UpthrustReversalStrategy()
		=> RunStrategy<UpthrustReversalStrategy>();

	[TestMethod]
	public Task WyckoffAccumulationStrategy()
		=> RunStrategy<WyckoffAccumulationStrategy>();

	[TestMethod]
	public Task WyckoffDistributionStrategy()
		=> RunStrategy<WyckoffDistributionStrategy>();

	[TestMethod]
	public Task RsiFailureSwingStrategy()
		=> RunStrategy<RsiFailureSwingStrategy>();

	[TestMethod]
	public Task StochasticFailureSwingStrategy()
		=> RunStrategy<StochasticFailureSwingStrategy>();

	[TestMethod]
	public Task CciFailureSwingStrategy()
		=> RunStrategy<CciFailureSwingStrategy>();

	[TestMethod]
	public Task BullishAbandonedBabyStrategy()
		=> RunStrategy<BullishAbandonedBabyStrategy>();

	[TestMethod]
	public Task BearishAbandonedBabyStrategy()
		=> RunStrategy<BearishAbandonedBabyStrategy>();

	[TestMethod]
	public Task VolumeClimaxReversalStrategy()
		=> RunStrategy<VolumeClimaxReversalStrategy>();

	[TestMethod]
	public Task DayOfWeekStrategy()
		=> RunStrategy<DayOfWeekStrategy>();

	[TestMethod]
	public Task MonthOfYearStrategy()
		=> RunStrategy<MonthOfYearStrategy>();

	[TestMethod]
	public Task TurnaroundTuesdayStrategy()
		=> RunStrategy<TurnaroundTuesdayStrategy>();

	[TestMethod]
	public Task EndOfMonthStrengthStrategy()
		=> RunStrategy<EndOfMonthStrengthStrategy>();

	[TestMethod]
	public Task FirstDayOfMonthStrategy()
		=> RunStrategy<FirstDayOfMonthStrategy>();

	[TestMethod]
	public Task SantaClausRallyStrategy()
		=> RunStrategy<SantaClausRallyStrategy>();

	[TestMethod]
	public Task JanuaryEffectStrategy()
		=> RunStrategy<JanuaryEffectStrategy>();

	[TestMethod]
	public Task MondayWeaknessStrategy()
		=> RunStrategy<MondayWeaknessStrategy>();

	[TestMethod]
	public Task PreHolidayStrengthStrategy()
		=> RunStrategy<PreHolidayStrengthStrategy>();

	[TestMethod]
	public Task PostHolidayWeaknessStrategy()
		=> RunStrategy<PostHolidayWeaknessStrategy>();

	[TestMethod]
	public Task QuarterlyExpiryStrategy()
		=> RunStrategy<QuarterlyExpiryStrategy>();

	[TestMethod]
	public Task OpenDriveStrategy()
		=> RunStrategy<OpenDriveStrategy>();

	[TestMethod]
	public Task MiddayReversalStrategy()
		=> RunStrategy<MiddayReversalStrategy>();

	[TestMethod]
	public Task OvernightGapStrategy()
		=> RunStrategy<OvernightGapStrategy>();

	[TestMethod]
	public Task LunchBreakFadeStrategy()
		=> RunStrategy<LunchBreakFadeStrategy>();

	[TestMethod]
	public Task MacdRsiStrategy()
		=> RunStrategy<MacdRsiStrategy>();

	[TestMethod]
	public Task BollingerStochasticStrategy()
		=> RunStrategy<BollingerStochasticStrategy>();

	[TestMethod]
	public Task MaVolumeStrategy()
		=> RunStrategy<MaVolumeStrategy>();

	[TestMethod]
	public Task AdxMacdStrategy()
		=> RunStrategy<AdxMacdStrategy>();

	[TestMethod]
	public Task IchimokuRsiStrategy()
		=> RunStrategy<IchimokuRsiStrategy>();

	[TestMethod]
	public Task SupertrendVolumeStrategy()
		=> RunStrategy<SupertrendVolumeStrategy>();

	[TestMethod]
	public Task BollingerRsiStrategy()
		=> RunStrategy<BollingerRsiStrategy>();

	[TestMethod]
	public Task MaStochasticStrategy()
		=> RunStrategy<MaStochasticStrategy>();

	[TestMethod]
	public Task AtrMacdStrategy()
		=> RunStrategy<AtrMacdStrategy>();

	[TestMethod]
	public Task VwapRsiStrategy()
		=> RunStrategy<VwapRsiStrategy>();

	[TestMethod]
	public Task DonchianVolumeStrategy()
		=> RunStrategy<DonchianVolumeStrategy>();

	[TestMethod]
	public Task KeltnerStochasticStrategy()
		=> RunStrategy<KeltnerStochasticStrategy>();

	[TestMethod]
	public Task ParabolicSarRsiStrategy()
		=> RunStrategy<ParabolicSarRsiStrategy>();

	[TestMethod]
	public Task HullMaVolumeStrategy()
		=> RunStrategy<HullMaVolumeStrategy>();

	[TestMethod]
	public Task AdxStochasticStrategy()
		=> RunStrategy<AdxStochasticStrategy>();

	[TestMethod]
	public Task MacdVolumeStrategy()
		=> RunStrategy<MacdVolumeStrategy>();

	[TestMethod]
	public Task BollingerVolumeStrategy()
		=> RunStrategy<BollingerVolumeStrategy>();

	[TestMethod]
	public Task RsiStochasticStrategy()
		=> RunStrategy<RsiStochasticStrategy>();

	[TestMethod]
	public Task MaAdxStrategy()
		=> RunStrategy<MaAdxStrategy>();

	[TestMethod]
	public Task VwapStochasticStrategy()
		=> RunStrategy<VwapStochasticStrategy>();

	[TestMethod]
	public Task IchimokuVolumeStrategy()
		=> RunStrategy<IchimokuVolumeStrategy>();

	[TestMethod]
	public Task SupertrendRsiStrategy()
		=> RunStrategy<SupertrendRsiStrategy>();

	[TestMethod]
	public Task BollingerAdxStrategy()
		=> RunStrategy<BollingerAdxStrategy>();

	[TestMethod]
	public Task MaCciStrategy()
		=> RunStrategy<MaCciStrategy>();

	[TestMethod]
	public Task VwapVolumeStrategy()
		=> RunStrategy<VwapVolumeStrategy>();

	[TestMethod]
	public Task DonchianRsiStrategy()
		=> RunStrategy<DonchianRsiStrategy>();

	[TestMethod]
	public Task KeltnerVolumeStrategy()
		=> RunStrategy<KeltnerVolumeStrategy>();

	[TestMethod]
	public Task ParabolicSarStochasticStrategy()
		=> RunStrategy<ParabolicSarStochasticStrategy>();

	[TestMethod]
	public Task HullMaRsiStrategy()
		=> RunStrategy<HullMaRsiStrategy>();

	[TestMethod]
	public Task AdxVolumeStrategy()
		=> RunStrategy<AdxVolumeStrategy>();

	[TestMethod]
	public Task MacdCciStrategy()
		=> RunStrategy<MacdCciStrategy>();

	[TestMethod]
	public Task BollingerCciStrategy()
		=> RunStrategy<BollingerCciStrategy>();

	[TestMethod]
	public Task RsiWilliamsRStrategy()
		=> RunStrategy<RsiWilliamsRStrategy>();

	[TestMethod]
	public Task MaWilliamsRStrategy()
		=> RunStrategy<MaWilliamsRStrategy>();

	[TestMethod]
	public Task VwapCciStrategy()
		=> RunStrategy<VwapCciStrategy>();

	[TestMethod]
	public Task DonchianStochasticStrategy()
		=> RunStrategy<DonchianStochasticStrategy>();

	[TestMethod]
	public Task KeltnerRsiStrategy()
		=> RunStrategy<KeltnerRsiStrategy>();

	[TestMethod]
	public Task HullMaStochasticStrategy()
		=> RunStrategy<HullMaStochasticStrategy>();

	[TestMethod]
	public Task AdxCciStrategy()
		=> RunStrategy<AdxCciStrategy>();

	[TestMethod]
	public Task MacdWilliamsRStrategy()
		=> RunStrategy<MacdWilliamsRStrategy>();

	[TestMethod]
	public Task BollingerWilliamsRStrategy()
		=> RunStrategy<BollingerWilliamsRStrategy>();

	[TestMethod]
	public Task MacdVwapStrategy()
		=> RunStrategy<MacdVwapStrategy>();

	[TestMethod]
	public Task RsiSupertrendStrategy()
		=> RunStrategy<RsiSupertrendStrategy>();

	[TestMethod]
	public Task AdxBollingerStrategy()
		=> RunStrategy<AdxBollingerStrategy>();

	[TestMethod]
	public Task IchimokuStochasticStrategy()
		=> RunStrategy<IchimokuStochasticStrategy>();

	[TestMethod]
	public Task SupertrendStochasticStrategy()
		=> RunStrategy<SupertrendStochasticStrategy>();

	[TestMethod]
	public Task DonchianMacdStrategy()
		=> RunStrategy<DonchianMacdStrategy>();

	[TestMethod]
	public Task ParabolicSarVolumeStrategy()
		=> RunStrategy<ParabolicSarVolumeStrategy>();

	[TestMethod]
	public Task VwapAdxStrategy()
		=> RunStrategy<VwapAdxStrategy>();

	[TestMethod]
	public Task SupertrendAdxStrategy()
		=> RunStrategy<SupertrendAdxStrategy>();

	[TestMethod]
	public Task KeltnerMacdStrategy()
		=> RunStrategy<KeltnerMacdStrategy>();

	[TestMethod]
	public Task HullMaAdxStrategy()
		=> RunStrategy<HullMaAdxStrategy>();

	[TestMethod]
	public Task VwapMacdStrategy()
		=> RunStrategy<VwapMacdStrategy>();

	[TestMethod]
	public Task IchimokuAdxStrategy()
		=> RunStrategy<IchimokuAdxStrategy>();

	[TestMethod]
	public Task VwapWilliamsRStrategy()
		=> RunStrategy<VwapWilliamsRStrategy>();

	[TestMethod]
	public Task DonchianCciStrategy()
		=> RunStrategy<DonchianCciStrategy>();

	[TestMethod]
	public Task KeltnerWilliamsRStrategy()
		=> RunStrategy<KeltnerWilliamsRStrategy>();

	[TestMethod]
	public Task ParabolicSarCciStrategy()
		=> RunStrategy<ParabolicSarCciStrategy>();

	[TestMethod]
	public Task HullMaCciStrategy()
		=> RunStrategy<HullMaCciStrategy>();

	[TestMethod]
	public Task MacdBollingerStrategy()
		=> RunStrategy<MacdBollingerStrategy>();

	[TestMethod]
	public Task RsiHullMaStrategy()
		=> RunStrategy<RsiHullMaStrategy>();

	[TestMethod]
	public Task StochasticKeltnerStrategy()
		=> RunStrategy<StochasticKeltnerStrategy>();

	[TestMethod]
	public Task VolumeSupertrendStrategy()
		=> RunStrategy<VolumeSupertrendStrategy>();

	[TestMethod]
	public Task AdxDonchianStrategy()
		=> RunStrategy<AdxDonchianStrategy>();

	[TestMethod]
	public Task CciVwapStrategy()
		=> RunStrategy<CciVwapStrategy>();

	[TestMethod]
	public Task WilliamsIchimokuStrategy()
		=> RunStrategy<WilliamsIchimokuStrategy>();

	[TestMethod]
	public Task MaParabolicSarStrategy()
		=> RunStrategy<MaParabolicSarStrategy>();

	[TestMethod]
	public Task BollingerSupertrendStrategy()
		=> RunStrategy<BollingerSupertrendStrategy>();

	[TestMethod]
	public Task RsiDonchianStrategy()
		=> RunStrategy<RsiDonchianStrategy>();

	[TestMethod]
	public Task MeanReversionStrategy()
		=> RunStrategy<MeanReversionStrategy>();

	[TestMethod]
	public Task PairsTradingStrategy()
		=> RunStrategy<PairsTradingStrategy>((strategy, sec) =>
		{
			strategy.SecondSecurity = sec;
		});

	[TestMethod]
	public Task ZScoreReversalStrategy()
		=> RunStrategy<ZScoreReversalStrategy>();

	[TestMethod]
	public Task StatisticalArbitrageStrategy()
		=> RunStrategy<StatisticalArbitrageStrategy>((strategy, sec) =>
		{
			strategy.SecondSecurity = sec;
		});

	[TestMethod]
	public Task VolatilityBreakoutStrategy()
		=> RunStrategy<VolatilityBreakoutStrategy>();

	[TestMethod]
	public Task BollingerBandSqueezeStrategy()
		=> RunStrategy<BollingerBandSqueezeStrategy>();

	[TestMethod]
	public Task CointegrationPairsStrategy()
		=> RunStrategy<CointegrationPairsStrategy>((strategy, sec) => strategy.Asset2 = sec);

	[TestMethod]
	public Task MomentumDivergenceStrategy()
		=> RunStrategy<MomentumDivergenceStrategy>();

	[TestMethod]
	public Task AtrMeanReversionStrategy()
		=> RunStrategy<AtrMeanReversionStrategy>();

	[TestMethod]
	public Task KalmanFilterTrendStrategy()
		=> RunStrategy<KalmanFilterTrendStrategy>();

	[TestMethod]
	public Task VolatilityAdjustedMeanReversionStrategy()
		=> RunStrategy<VolatilityAdjustedMeanReversionStrategy>();

	[TestMethod]
	public Task HurstExponentTrendStrategy()
		=> RunStrategy<HurstExponentTrendStrategy>();

	[TestMethod]
	public Task HurstExponentReversionStrategy()
		=> RunStrategy<HurstExponentReversionStrategy>();

	[TestMethod]
	public Task AutocorrelationReversionStrategy()
		=> RunStrategy<AutocorrelationReversionStrategy>();

	[TestMethod]
	public Task DeltaNeutralArbitrageStrategy()
		=> RunStrategy<DeltaNeutralArbitrageStrategy>((strategy, sec) =>
		{
			strategy.Asset2Security = sec;
			strategy.Asset2Portfolio = strategy.Portfolio;
		});

	[TestMethod]
	public Task VolatilitySkewArbitrageStrategy()
		=> RunStrategy<VolatilitySkewArbitrageStrategy>();

	[TestMethod]
	public Task CorrelationBreakoutStrategy()
		=> RunStrategy<CorrelationBreakoutStrategy>();

	[TestMethod]
	public Task BetaNeutralArbitrageStrategy()
		=> RunStrategy<BetaNeutralArbitrageStrategy>();

	[TestMethod]
	public Task VwapMeanReversionStrategy()
		=> RunStrategy<VwapMeanReversionStrategy>();

	[TestMethod]
	public Task RsiMeanReversionStrategy()
		=> RunStrategy<RsiMeanReversionStrategy>();

	[TestMethod]
	public Task StochasticMeanReversionStrategy()
		=> RunStrategy<StochasticMeanReversionStrategy>();

	[TestMethod]
	public Task CciMeanReversionStrategy()
		=> RunStrategy<CciMeanReversionStrategy>();

	[TestMethod]
	public Task WilliamsRMeanReversionStrategy()
		=> RunStrategy<WilliamsRMeanReversionStrategy>();

	[TestMethod]
	public Task MacdMeanReversionStrategy()
		=> RunStrategy<MacdMeanReversionStrategy>();

	[TestMethod]
	public Task AdxMeanReversionStrategy()
		=> RunStrategy<AdxMeanReversionStrategy>();

	[TestMethod]
	public Task VolatilityMeanReversionStrategy()
		=> RunStrategy<VolatilityMeanReversionStrategy>();

	[TestMethod]
	public Task VolumeMeanReversionStrategy()
		=> RunStrategy<VolumeMeanReversionStrategy>();

	[TestMethod]
	public Task ObvMeanReversionStrategy()
		=> RunStrategy<ObvMeanReversionStrategy>();

	[TestMethod]
	public Task MomentumBreakoutStrategy()
		=> RunStrategy<MomentumBreakoutStrategy>();

	[TestMethod]
	public Task RsiBreakoutStrategy()
		=> RunStrategy<RsiBreakoutStrategy>();

	[TestMethod]
	public Task StochasticBreakoutStrategy()
		=> RunStrategy<StochasticBreakoutStrategy>();

	[TestMethod]
	public Task WilliamsRBreakoutStrategy()
		=> RunStrategy<WilliamsRBreakoutStrategy>();

	[TestMethod]
	public Task MacdBreakoutStrategy()
		=> RunStrategy<MacdBreakoutStrategy>();

	[TestMethod]
	public Task ADXBreakoutStrategy()
		=> RunStrategy<ADXBreakoutStrategy>();

	[TestMethod]
	public Task VolumeBreakoutStrategy()
		=> RunStrategy<VolumeBreakoutStrategy>();

	[TestMethod]
	public Task BollingerWidthBreakoutStrategy()
		=> RunStrategy<BollingerWidthBreakoutStrategy>();

	[TestMethod]
	public Task KeltnerWidthBreakoutStrategy()
		=> RunStrategy<KeltnerWidthBreakoutStrategy>();

	[TestMethod]
	public Task DonchianWidthBreakoutStrategy()
		=> RunStrategy<DonchianWidthBreakoutStrategy>();

	[TestMethod]
	public Task IchimokuWidthBreakoutStrategy()
		=> RunStrategy<IchimokuWidthBreakoutStrategy>();

	[TestMethod]
	public Task SupertrendDistanceBreakoutStrategy()
		=> RunStrategy<SupertrendDistanceBreakoutStrategy>();

	[TestMethod]
	public Task ParabolicSarDistanceBreakoutStrategy()
		=> RunStrategy<ParabolicSarDistanceBreakoutStrategy>();

	[TestMethod]
	public Task HullMaSlopeBreakoutStrategy()
		=> RunStrategy<HullMaSlopeBreakoutStrategy>();

	[TestMethod]
	public Task MaSlopeBreakoutStrategy()
		=> RunStrategy<MaSlopeBreakoutStrategy>();

	[TestMethod]
	public Task EmaSlopeBreakoutStrategy()
		=> RunStrategy<EmaSlopeBreakoutStrategy>();

	[TestMethod]
	public Task VolatilityAdjustedMomentumStrategy()
		=> RunStrategy<VolatilityAdjustedMomentumStrategy>();

	[TestMethod]
	public Task VwapSlopeBreakoutStrategy()
		=> RunStrategy<VwapSlopeBreakoutStrategy>();

	[TestMethod]
	public Task RsiSlopeBreakoutStrategy()
		=> RunStrategy<RsiSlopeBreakoutStrategy>();

	[TestMethod]
	public Task StochasticSlopeBreakoutStrategy()
		=> RunStrategy<StochasticSlopeBreakoutStrategy>();

	[TestMethod]
	public Task CciSlopeBreakoutStrategy()
		=> RunStrategy<CciSlopeBreakoutStrategy>();

	[TestMethod]
	public Task WilliamsRSlopeBreakoutStrategy()
		=> RunStrategy<WilliamsRSlopeBreakoutStrategy>();

	[TestMethod]
	public Task MacdSlopeBreakoutStrategy()
		=> RunStrategy<MacdSlopeBreakoutStrategy>();

	[TestMethod]
	public Task AdxSlopeBreakoutStrategy()
		=> RunStrategy<AdxSlopeBreakoutStrategy>();

	[TestMethod]
	public Task AtrSlopeBreakoutStrategy()
		=> RunStrategy<AtrSlopeBreakoutStrategy>();

	[TestMethod]
	public Task VolumeSlopeBreakoutStrategy()
		=> RunStrategy<VolumeSlopeBreakoutStrategy>();

	[TestMethod]
	public Task ObvSlopeBreakoutStrategy()
		=> RunStrategy<ObvSlopeBreakoutStrategy>();

	[TestMethod]
	public Task BollingerWidthMeanReversionStrategy()
		=> RunStrategy<BollingerWidthMeanReversionStrategy>();

	[TestMethod]
	public Task KeltnerWidthMeanReversionStrategy()
		=> RunStrategy<KeltnerWidthMeanReversionStrategy>();

	[TestMethod]
	public Task DonchianWidthMeanReversionStrategy()
		=> RunStrategy<DonchianWidthMeanReversionStrategy>();

	[TestMethod]
	public Task IchimokuCloudWidthMeanReversionStrategy()
		=> RunStrategy<IchimokuCloudWidthMeanReversionStrategy>();

	[TestMethod]
	public Task SupertrendDistanceMeanReversionStrategy()
		=> RunStrategy<SupertrendDistanceMeanReversionStrategy>();

	[TestMethod]
	public Task ParabolicSarDistanceMeanReversionStrategy()
		=> RunStrategy<ParabolicSarDistanceMeanReversionStrategy>();

	[TestMethod]
	public Task HullMaSlopeMeanReversionStrategy()
		=> RunStrategy<HullMaSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task MaSlopeMeanReversionStrategy()
		=> RunStrategy<MaSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task EmaSlopeMeanReversionStrategy()
		=> RunStrategy<EmaSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task VwapSlopeMeanReversionStrategy()
		=> RunStrategy<VwapSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task RsiSlopeMeanReversionStrategy()
		=> RunStrategy<RsiSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task StochasticSlopeMeanReversionStrategy()
		=> RunStrategy<StochasticSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task CciSlopeMeanReversionStrategy()
		=> RunStrategy<CciSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task WilliamsRSlopeMeanReversionStrategy()
		=> RunStrategy<WilliamsRSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task MacdSlopeMeanReversionStrategy()
		=> RunStrategy<MacdSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task AdxSlopeMeanReversionStrategy()
		=> RunStrategy<AdxSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task AtrSlopeMeanReversionStrategy()
		=> RunStrategy<AtrSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task VolumeSlopeMeanReversionStrategy()
		=> RunStrategy<VolumeSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task ObvSlopeMeanReversionStrategy()
		=> RunStrategy<ObvSlopeMeanReversionStrategy>();

	[TestMethod]
	public Task PairsTradingVolatilityFilterStrategy()
		=> RunStrategy<PairsTradingVolatilityFilterStrategy>((strategy, sec) =>
		{
			strategy.Security2 = sec;
		});

	[TestMethod]
	public Task ZScoreVolumeFilterStrategy()
		=> RunStrategy<ZScoreVolumeFilterStrategy>();

	[TestMethod]
	public Task CorrelationMeanReversionStrategy()
		=> RunStrategy<CorrelationMeanReversionStrategy>((strategy, sec) => strategy.Security2 = sec);

	[TestMethod]
	public Task BetaAdjustedPairsStrategy()
		=> RunStrategy<BetaAdjustedPairsStrategy>((strategy, sec) =>
		{
			strategy.Asset2 = sec;
			strategy.Asset2Portfolio = strategy.Portfolio;
		});

	[TestMethod]
	public Task HurstVolatilityFilterStrategy()
		=> RunStrategy<HurstVolatilityFilterStrategy>();

	[TestMethod]
	public Task AdaptiveEmaBreakoutStrategy()
		=> RunStrategy<AdaptiveEmaBreakoutStrategy>();

	[TestMethod]
	public Task VolatilityClusterBreakoutStrategy()
		=> RunStrategy<VolatilityClusterBreakoutStrategy>();

	[TestMethod]
	public Task SeasonalityAdjustedMomentumStrategy()
		=> RunStrategy<SeasonalityAdjustedMomentumStrategy>();

	[TestMethod]
	public Task RsiDynamicOverboughtOversoldStrategy()
		=> RunStrategy<RsiDynamicOverboughtOversoldStrategy>();

	[TestMethod]
	public Task BollingerVolatilityBreakoutStrategy()
		=> RunStrategy<BollingerVolatilityBreakoutStrategy>();

	[TestMethod]
	public Task MacdAdaptiveHistogramStrategy()
		=> RunStrategy<MacdAdaptiveHistogramStrategy>();

	[TestMethod]
	public Task IchimokuVolumeClusterStrategy()
		=> RunStrategy<IchimokuVolumeClusterStrategy>();

	[TestMethod]
	public Task SupertrendWithMomentumStrategy()
		=> RunStrategy<SupertrendWithMomentumStrategy>();

	[TestMethod]
	public Task DonchianWithVolatilityContractionStrategy()
		=> RunStrategy<DonchianWithVolatilityContractionStrategy>();

	[TestMethod]
	public Task KeltnerWithRsiDivergenceStrategy()
		=> RunStrategy<KeltnerWithRsiDivergenceStrategy>();

	[TestMethod]
	public Task HullMaWithVolumeSpikeStrategy()
		=> RunStrategy<HullMaWithVolumeSpikeStrategy>();

	[TestMethod]
	public Task VwapWithAdxTrendStrengthStrategy()
		=> RunStrategy<VwapWithAdxTrendStrengthStrategy>();

	[TestMethod]
	public Task ParabolicSarWithVolatilityExpansionStrategy()
		=> RunStrategy<ParabolicSarWithVolatilityExpansionStrategy>();

	[TestMethod]
	public Task StochasticWithDynamicZonesStrategy()
		=> RunStrategy<StochasticWithDynamicZonesStrategy>();

	[TestMethod]
	public Task AdxWithVolumeBreakoutStrategy()
		=> RunStrategy<AdxWithVolumeBreakoutStrategy>();

	[TestMethod]
	public Task CciWithVolatilityFilterStrategy()
		=> RunStrategy<CciWithVolatilityFilterStrategy>();

	[TestMethod]
	public Task WilliamsPercentRWithMomentumStrategy()
		=> RunStrategy<WilliamsPercentRWithMomentumStrategy>();

	[TestMethod]
	public Task BollingerKMeansStrategy()
		=> RunStrategy<BollingerKMeansStrategy>();

	[TestMethod]
	public Task MacdHmmStrategy()
		=> RunStrategy<MacdHmmStrategy>();

	[TestMethod]
	public Task IchimokuHurstStrategy()
		=> RunStrategy<IchimokuHurstStrategy>();

	[TestMethod]
	public Task SupertrendRsiDivergenceStrategy()
		=> RunStrategy<SupertrendRsiDivergenceStrategy>();

	[TestMethod]
	public Task DonchianSeasonalStrategy()
		=> RunStrategy<DonchianSeasonalStrategy>();

	[TestMethod]
	public Task KeltnerKalmanStrategy()
		=> RunStrategy<KeltnerKalmanStrategy>();

	[TestMethod]
	public Task HullMaVolatilityContractionStrategy()
		=> RunStrategy<HullMaVolatilityContractionStrategy>();

	[TestMethod]
	public Task VwapAdxTrendStrategy()
		=> RunStrategy<VwapAdxTrendStrategy>();

	[TestMethod]
	public Task ParabolicSarHurstStrategy()
		=> RunStrategy<ParabolicSarHurstStrategy>();

	[TestMethod]
	public Task BollingerKalmanFilterStrategy()
		=> RunStrategy<BollingerKalmanFilterStrategy>();

	[TestMethod]
	public Task MacdVolumeClusterStrategy()
		=> RunStrategy<MacdVolumeClusterStrategy>();

	[TestMethod]
	public Task IchimokuVolatilityContractionStrategy()
		=> RunStrategy<IchimokuVolatilityContractionStrategy>();

	[TestMethod]
	public Task DonchianHurstStrategy()
		=> RunStrategy<DonchianHurstStrategy>();

	[TestMethod]
	public Task KeltnerSeasonalStrategy()
		=> RunStrategy<KeltnerSeasonalStrategy>();

	[TestMethod]
	public Task HullKMeansClusterStrategy()
		=> RunStrategy<HullKMeansClusterStrategy>();

	[TestMethod]
	public Task VwapHiddenMarkovModelStrategy()
		=> RunStrategy<VwapHiddenMarkovModelStrategy>();

	[TestMethod]
	public Task ParabolicSarRsiDivergenceStrategy()
		=> RunStrategy<ParabolicSarRsiDivergenceStrategy>();

	[TestMethod]
	public Task AdaptiveRsiVolumeStrategy()
		=> RunStrategy<AdaptiveRsiVolumeStrategy>();

	[TestMethod]
	public Task AdaptiveBollingerBreakoutStrategy()
		=> RunStrategy<AdaptiveBollingerBreakoutStrategy>();

	[TestMethod]
	public Task MacdWithSentimentFilterStrategy()
		=> RunStrategy<MacdWithSentimentFilterStrategy>();

	[TestMethod]
	public Task IchimokuWithImpliedVolatilityStrategy()
		=> RunStrategy<IchimokuWithImpliedVolatilityStrategy>();

	[TestMethod]
	public Task SupertrendWithPutCallRatioStrategy()
		=> RunStrategy<SupertrendWithPutCallRatioStrategy>();

	[TestMethod]
	public Task DonchianWithSentimentSpikeStrategy()
		=> RunStrategy<DonchianWithSentimentSpikeStrategy>();

	[TestMethod]
	public Task KeltnerWithRLSignalStrategy()
		=> RunStrategy<KeltnerWithRLSignalStrategy>();

	[TestMethod]
	public Task HullMAWithImpliedVolatilityBreakoutStrategy()
		=> RunStrategy<HullMAWithImpliedVolatilityBreakoutStrategy>();

	[TestMethod]
	public Task VwapWithBehavioralBiasFilterStrategy()
		=> RunStrategy<VwapWithBehavioralBiasFilterStrategy>();

	[TestMethod]
	public Task ParabolicSarSentimentDivergenceStrategy()
		=> RunStrategy<ParabolicSarSentimentDivergenceStrategy>();

	[TestMethod]
	public Task RsiWithOptionOpenInterestStrategy()
		=> RunStrategy<RsiWithOptionOpenInterestStrategy>();

	[TestMethod]
	public Task StochasticImpliedVolatilitySkewStrategy()
		=> RunStrategy<StochasticImpliedVolatilitySkewStrategy>();

	[TestMethod]
	public Task AdxSentimentMomentumStrategy()
		=> RunStrategy<AdxSentimentMomentumStrategy>();

	[TestMethod]
	public Task CciPutCallRatioDivergenceStrategy()
		=> RunStrategy<CciPutCallRatioDivergenceStrategy>();

	[TestMethod]
	public Task AccrualAnomalyStrategy()
		=> RunStrategy<AccrualAnomalyStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task AssetClassMomentumRotationalStrategy()
		=> RunStrategy<AssetClassMomentumRotationalStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task AssetClassTrendFollowingStrategy()
		=> RunStrategy<AssetClassTrendFollowingStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task AssetGrowthEffectStrategy()
		=> RunStrategy<AssetGrowthEffectStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task BettingAgainstBetaStocksStrategy()
		=> RunStrategy<BettingAgainstBetaStocksStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task BettingAgainstBetaStrategy()
		=> RunStrategy<BettingAgainstBetaStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task BitcoinIntradaySeasonalityStrategy()
		=> RunStrategy<BitcoinIntradaySeasonalityStrategy>();

	[TestMethod]
	public Task BookToMarketValueStrategy()
		=> RunStrategy<BookToMarketValueStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task CommodityMomentumStrategy()
		=> RunStrategy<CommodityMomentumStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task ConsistentMomentumStrategy()
		=> RunStrategy<ConsistentMomentumStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task CountryValueFactorStrategy()
		=> RunStrategy<CountryValueFactorStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task CrudeOilPredictsEquityStrategy()
		=> RunStrategy<CrudeOilPredictsEquityStrategy>((stra, sec) =>
		{
			stra.Oil = sec;
			stra.CashEtf = sec;
		});

	[TestMethod]
	public Task CryptoRebalancingPremiumStrategy()
		=> RunStrategy<CryptoRebalancingPremiumStrategy>((stra, sec) =>
		{
			stra.ETH = sec;
		});

	[TestMethod]
	public Task CurrencyMomentumFactorStrategy()
		=> RunStrategy<CurrencyMomentumFactorStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task CurrencyPPPValueStrategy()
		=> RunStrategy<CurrencyPPPValueStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task DispersionTradingStrategy()
		=> RunStrategy<DispersionTradingStrategy>((stra, sec) =>
		{
			stra.Constituents = [sec];
		});

	[TestMethod]
	public Task DollarCarryTradeStrategy()
		=> RunStrategy<DollarCarryTradeStrategy>((stra, sec) =>
		{
			stra.Pairs = [sec];
		});

	[TestMethod]
	public Task EarningsAnnouncementPremiumStrategy()
		=> RunStrategy<EarningsAnnouncementPremiumStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task EarningsAnnouncementReversalStrategy()
		=> RunStrategy<EarningsAnnouncementReversalStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task EarningsAnnouncementsWithBuybacksStrategy()
		=> RunStrategy<EarningsAnnouncementsWithBuybacksStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task EarningsQualityFactorStrategy()
		=> RunStrategy<EarningsQualityFactorStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task ESGFactorMomentumStrategy()
		=> RunStrategy<ESGFactorMomentumStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task FedModelStrategy()
		=> RunStrategy<FedModelStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task FScoreReversalStrategy()
		=> RunStrategy<FScoreReversalStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task FXCarryTradeStrategy()
		=> RunStrategy<FXCarryTradeStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task JanuaryBarometerStrategy()
		=> RunStrategy<JanuaryBarometerStrategy>((stra, sec) =>
		{
			stra.EquityETF = sec;
			stra.CashETF = sec;
		});

	[TestMethod]
	public Task LexicalDensityFilingsStrategy()
		=> RunStrategy<LexicalDensityFilingsStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task LowVolatilityStocksStrategy()
		=> RunStrategy<LowVolatilityStocksStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task MomentumAssetGrowthStrategy()
		=> RunStrategy<MomentumAssetGrowthStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task MomentumFactorStocksStrategy()
		=> RunStrategy<MomentumFactorStocksStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task MomentumRevVolStrategy()
		=> RunStrategy<MomentumRevVolStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task MomentumStyleRotationStrategy()
		=> RunStrategy<MomentumStyleRotationStrategy>((stra, sec) =>
		{
			stra.FactorETFs = [sec];
		});

	[TestMethod]
	public Task Month12CycleStrategy()
		=> RunStrategy<Month12CycleStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task MutualFundMomentumStrategy()
		=> RunStrategy<MutualFundMomentumStrategy>((stra, sec) =>
		{
			stra.Funds = [sec];
		});

	[TestMethod]
	public Task OptionExpirationWeekStrategy()
		=> RunStrategy<OptionExpirationWeekStrategy>();

	[TestMethod]
	public Task OvernightSentimentAnomalyStrategy()
		=> RunStrategy<OvernightSentimentAnomalyStrategy>((stra, sec) =>
		{
			stra.SentimentSymbol = sec;
		});

	[TestMethod]
	public Task PairedSwitchingStrategy()
		=> RunStrategy<PairedSwitchingStrategy>((stra, sec) =>
		{
			stra.SecondETF = sec;
		});

	[TestMethod]
	public Task PairsTradingCountryETFsStrategy()
		=> RunStrategy<PairsTradingCountryETFsStrategy>((stra, sec) =>
		{
			stra.Universe = [stra.Security, sec];
		});

	[TestMethod]
	public Task PairsTradingStocksStrategy()
		=> RunStrategy<PairsTradingStocksStrategy>((stra, sec) =>
		{
			stra.Pairs = [(stra.Security, sec)];
		});

	[TestMethod]
	public Task PaydayAnomalyStrategy()
		=> RunStrategy<PaydayAnomalyStrategy>();

	[TestMethod]
	public Task RDExpendituresStrategy()
		=> RunStrategy<RDExpendituresStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task ResidualMomentumFactorStrategy()
		=> RunStrategy<ResidualMomentumFactorStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task ReturnAsymmetryCommodityStrategy()
		=> RunStrategy<ReturnAsymmetryCommodityStrategy>((stra, sec) =>
		{
			stra.Futures = [sec];
		});

	[TestMethod]
	public Task ROAEffectStocksStrategy()
		=> RunStrategy<ROAEffectStocksStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task SectorMomentumRotationStrategy()
		=> RunStrategy<SectorMomentumRotationStrategy>((stra, sec) =>
		{
			stra.SectorETFs = [sec];
		});

	[TestMethod]
	public Task ShortInterestEffectStrategy()
		=> RunStrategy<ShortInterestEffectStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task ShortTermReversalFuturesStrategy()
		=> RunStrategy<ShortTermReversalFuturesStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task ShortTermReversalStocksStrategy()
		=> RunStrategy<ShortTermReversalStocksStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task SkewnessCommodityStrategy()
		=> RunStrategy<SkewnessCommodityStrategy>((stra, sec) =>
		{
			stra.Futures = [sec];
		});

	[TestMethod]
	public Task SmallCapPremiumStrategy()
		=> RunStrategy<SmallCapPremiumStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task SmartFactorsMomentumMarketStrategy()
		=> RunStrategy<SmartFactorsMomentumMarketStrategy>((stra, sec) =>
		{
			stra.Factors = [sec];
		});

	[TestMethod]
	public Task SoccerClubsArbitrageStrategy()
		=> RunStrategy<SoccerClubsArbitrageStrategy>((stra, sec) =>
		{
			stra.Pair = [stra.Security, sec];
		});

	[TestMethod]
	public Task SyntheticLendingRatesStrategy()
		=> RunStrategy<SyntheticLendingRatesStrategy>();

	[TestMethod]
	public Task TermStructureCommoditiesStrategy()
		=> RunStrategy<TermStructureCommoditiesStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task TimeSeriesMomentumStrategy()
		=> RunStrategy<TimeSeriesMomentumStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task TrendFollowingStocksStrategy()
		=> RunStrategy<TrendFollowingStocksStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task TurnOfMonthStrategy()
		=> RunStrategy<TurnOfMonthStrategy>();

	[TestMethod]
	public Task ValueMomentumAcrossAssetsStrategy()
		=> RunStrategy<ValueMomentumAcrossAssetsStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task VolatilityRiskPremiumStrategy()
		=> RunStrategy<VolatilityRiskPremiumStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task Weeks52HighStrategy()
		=> RunStrategy<Weeks52HighStrategy>((stra, sec) =>
		{
			stra.Universe = [sec];
		});

	[TestMethod]
	public Task WTIBrentSpreadStrategy()
		=> RunStrategy<WTIBrentSpreadStrategy>((stra, sec) =>
		{
			stra.Brent = sec;
		});

	[TestMethod]
	public Task BollingerAroonStrategy()
		=> RunStrategy<BollingerAroonStrategy>();

	[TestMethod]
	public Task BollingerDivergenceStrategy()
		=> RunStrategy<BollingerDivergenceStrategy>();

	[TestMethod]
	public Task BollingerWinnerLiteStrategy()
		=> RunStrategy<BollingerWinnerLiteStrategy>();

	[TestMethod]
	public Task BollingerWinnerProStrategy()
		=> RunStrategy<BollingerWinnerProStrategy>();

	[TestMethod]
	public Task BollingerBreakoutStrategy()
		=> RunStrategy<BollingerBreakoutStrategy>();

	[TestMethod]
	public Task DmiWinnerStrategy()
		=> RunStrategy<DmiWinnerStrategy>();

	[TestMethod]
	public Task DoubleRsiStrategy()
		=> RunStrategy<DoubleRsiStrategy>();

	[TestMethod]
	public Task DoubleSupertrendStrategy()
		=> RunStrategy<DoubleSupertrendStrategy>();

	[TestMethod]
	public Task EmaMovingAwayStrategy()
		=> RunStrategy<EmaMovingAwayStrategy>();

	[TestMethod]
	public Task EmaSmaRsiStrategy()
		=> RunStrategy<EmaSmaRsiStrategy>();

	[TestMethod]
	public Task ExceededCandleStrategy()
		=> RunStrategy<ExceededCandleStrategy>();

	[TestMethod]
	public Task FlawlessVictoryStrategy()
		=> RunStrategy<FlawlessVictoryStrategy>();

	[TestMethod]
	public Task FullCandleStrategy()
		=> RunStrategy<FullCandleStrategy>();

	[TestMethod]
	public Task GridBotStrategy()
		=> RunStrategy<GridBotStrategy>();

	[TestMethod]
	public Task HaUniversalStrategy()
		=> RunStrategy<HaUniversalStrategy>();

	[TestMethod]
	public Task HeikinAshiV2Strategy()
		=> RunStrategy<HeikinAshiV2Strategy>();

	[TestMethod]
	public Task ImprovisandoStrategy()
		=> RunStrategy<ImprovisandoStrategy>();

	[TestMethod]
	public Task JavoV1Strategy()
		=> RunStrategy<JavoV1Strategy>();

	[TestMethod]
	public Task MacdBbRsiStrategy()
		=> RunStrategy<MacdBbRsiStrategy>();

	[TestMethod]
	public Task MacdDmiStrategy()
		=> RunStrategy<MacdDmiStrategy>();

	[TestMethod]
	public Task MacdLongStrategy()
		=> RunStrategy<MacdLongStrategy>();

	[TestMethod]
	public Task MaCrossDmiStrategy()
		=> RunStrategy<MaCrossDmiStrategy>();

	[TestMethod]
	public Task MemaBbRsiStrategy()
		=> RunStrategy<MemaBbRsiStrategy>();

	[TestMethod]
	public Task MtfBbStrategy()
		=> RunStrategy<MtfBbStrategy>();

	[TestMethod]
	public Task OmarMmrStrategy()
		=> RunStrategy<OmarMmrStrategy>();

	[TestMethod]
	public Task PinBarMagicStrategy()
		=> RunStrategy<PinBarMagicStrategy>();

	[TestMethod]
	public Task QqeSignalsStrategy()
		=> RunStrategy<QqeSignalsStrategy>();

	[TestMethod]
	public Task RsiPlus1200Strategy()
		=> RunStrategy<RsiPlus1200Strategy>();

	[TestMethod]
	public Task RsiEmaStrategy()
		=> RunStrategy<RsiEmaStrategy>();

	[TestMethod]
	public Task StochRsiCrossoverStrategy()
		=> RunStrategy<StochRsiCrossoverStrategy>();

	[TestMethod]
	public Task StochRsiSupertrendStrategy()
		=> RunStrategy<StochRsiSupertrendStrategy>();

	[TestMethod]
	public Task StrategyTesterStrategy()
		=> RunStrategy<StrategyTesterStrategy>();

	[TestMethod]
	public Task StratBaseStrategy()
		=> RunStrategy<StratBaseStrategy>();

	[TestMethod]
	public Task SupertrendEmaReboundStrategy()
		=> RunStrategy<SupertrendEmaReboundStrategy>();

	[TestMethod]
	public Task TendencyEmaRsiStrategy()
		=> RunStrategy<TendencyEmaRsiStrategy>();

	[TestMethod]
	public Task ThreeEmaCrossStrategy()
		=> RunStrategy<ThreeEmaCrossStrategy>();

	[TestMethod]
	public Task TtmSqueezeStrategy()
		=> RunStrategy<TtmSqueezeStrategy>();

	[TestMethod]
	public Task VelaSuperadaStrategy()
		=> RunStrategy<VelaSuperadaStrategy>();

	[TestMethod]
	public Task WilliamsVixFixStrategy()
		=> RunStrategy<WilliamsVixFixStrategy>();
}
