namespace StockSharp.Tests;

using StockSharp.Samples.Strategies;

[TestClass]
public class CSharpTests
{
	public static Task RunStrategy<T>(Action<T, Security> extra = null)
	where T : Strategy
		=> AsmInit.RunStrategy(TypeHelper.CreateInstance<T>(typeof(T)), extra);
	
	[TestMethod]
	public Task MaCrossover()
		=> RunStrategy<MaCrossoverStrategy>();
	
	[TestMethod]
	public Task NdayBreakout()
		=> RunStrategy<NdayBreakoutStrategy>();
	
	[TestMethod]
	public Task AdxTrend()
		=> RunStrategy<AdxTrendStrategy>();
	
	[TestMethod]
	public Task ParabolicSarTrend()
		=> RunStrategy<ParabolicSarTrendStrategy>();
	
	[TestMethod]
	public Task DonchianChannel()
		=> RunStrategy<DonchianChannelStrategy>();
	
	[TestMethod]
	public Task TripleMA()
		=> RunStrategy<TripleMAStrategy>();
	
	[TestMethod]
	public Task KeltnerChannelBreakout()
		=> RunStrategy<KeltnerChannelBreakoutStrategy>();
	
	[TestMethod]
	public Task HullMaTrend()
		=> RunStrategy<HullMaTrendStrategy>();
	
	[TestMethod]
	public Task MacdTrend()
		=> RunStrategy<MacdTrendStrategy>();
	
	[TestMethod]
	public Task Supertrend()
		=> RunStrategy<SupertrendStrategy>();
	
	[TestMethod]
	public Task IchimokuKumoBreakout()
		=> RunStrategy<IchimokuKumoBreakoutStrategy>();
	
	[TestMethod]
	public Task HeikinAshiConsecutive()
		=> RunStrategy<HeikinAshiConsecutiveStrategy>();
	
	[TestMethod]
	public Task DmiPowerMove()
		=> RunStrategy<DmiPowerMoveStrategy>();
	
	[TestMethod]
	public Task TradingviewSupertrendFlip()
		=> RunStrategy<TradingViewSupertrendFlipStrategy>();
	
	[TestMethod]
	public Task GannSwingBreakout()
		=> RunStrategy<GannSwingBreakoutStrategy>();
	
	[TestMethod]
	public Task RsiDivergence()
		=> RunStrategy<RsiDivergenceStrategy>();
	
	[TestMethod]
	public Task WilliamsPercentR()
		=> RunStrategy<WilliamsPercentRStrategy>();
	
	[TestMethod]
	public Task RocImpulse()
		=> RunStrategy<RocImpulseStrategy>();
	
	[TestMethod]
	public Task CciBreakout()
		=> RunStrategy<CciBreakoutStrategy>();
	
	[TestMethod]
	public Task MomentumPercentage()
		=> RunStrategy<MomentumPercentageStrategy>();
	
	[TestMethod]
	public Task BollingerSqueeze()
		=> RunStrategy<BollingerSqueezeStrategy>();
	
	[TestMethod]
	public Task AdxDi()
		=> RunStrategy<AdxDiStrategy>();
	
	[TestMethod]
	public Task ElderImpulse()
		=> RunStrategy<ElderImpulseStrategy>();
	
	[TestMethod]
	public Task LaguerreRsi()
		=> RunStrategy<LaguerreRsiStrategy>();
	
	[TestMethod]
	public Task StochasticRsiCross()
		=> RunStrategy<StochasticRsiCrossStrategy>();
	
	[TestMethod]
	public Task RsiReversion()
		=> RunStrategy<RsiReversionStrategy>();
	
	[TestMethod]
	public Task BollingerReversion()
		=> RunStrategy<BollingerReversionStrategy>();
	
	[TestMethod]
	public Task ZScore()
		=> RunStrategy<ZScoreStrategy>();
	
	[TestMethod]
	public Task MADeviation()
		=> RunStrategy<MADeviationStrategy>();
	
	[TestMethod]
	public Task VwapReversion()
		=> RunStrategy<VwapReversionStrategy>();
	
	[TestMethod]
	public Task KeltnerReversion()
		=> RunStrategy<KeltnerReversionStrategy>();
	
	[TestMethod]
	public Task AtrReversion()
		=> RunStrategy<AtrReversionStrategy>();
	
	[TestMethod]
	public Task MacdZero()
		=> RunStrategy<MacdZeroStrategy>();
	
	[TestMethod]
	public Task LowVolReversion()
		=> RunStrategy<LowVolReversionStrategy>();
	
	[TestMethod]
	public Task BollingerPercentB()
		=> RunStrategy<BollingerPercentBStrategy>();
	
	[TestMethod]
	public Task AtrExpansion()
		=> RunStrategy<AtrExpansionStrategy>();
	
	[TestMethod]
	public Task VixTrigger()
		=> RunStrategy<VixTriggerStrategy>();
	
	[TestMethod]
	public Task BollingerBandWidth()
		=> RunStrategy<BollingerBandWidthStrategy>();
	
	[TestMethod]
	public Task HvBreakout()
		=> RunStrategy<HvBreakoutStrategy>();
	
	[TestMethod]
	public Task AtrTrailing()
		=> RunStrategy<AtrTrailingStrategy>();
	
	[TestMethod]
	public Task VolAdjustedMa()
		=> RunStrategy<VolAdjustedMAStrategy>();
	
	[TestMethod]
	public Task IVSpike()
		=> RunStrategy<IVSpikeStrategy>();
	
	[TestMethod]
	public Task VCP()
		=> RunStrategy<VCPStrategy>();
	
	[TestMethod]
	public Task ATRRange()
		=> RunStrategy<ATRRangeStrategy>();
	
	[TestMethod]
	public Task ChoppinessIndexBreakout()
		=> RunStrategy<ChoppinessIndexBreakoutStrategy>();
	
	[TestMethod]
	public Task VolumeSpike()
		=> RunStrategy<VolumeSpikeStrategy>();
	
	[TestMethod]
	public Task OBVBreakout()
		=> RunStrategy<OBVBreakoutStrategy>();
	
	[TestMethod]
	public Task VWAPBreakout()
		=> RunStrategy<VWAPBreakoutStrategy>();
	
	[TestMethod]
	public Task VWMA()
		=> RunStrategy<VWMAStrategy>();
	
	[TestMethod]
	public Task AD()
		=> RunStrategy<ADStrategy>();
	
	[TestMethod]
	public Task VolumeWeightedPriceBreakout()
		=> RunStrategy<VolumeWeightedPriceBreakoutStrategy>();
	
	[TestMethod]
	public Task VolumeDivergence()
		=> RunStrategy<VolumeDivergenceStrategy>();
	
	[TestMethod]
	public Task VolumeMAXross()
		=> RunStrategy<VolumeMAXrossStrategy>();
	
	[TestMethod]
	public Task CumulativeDeltaBreakout()
		=> RunStrategy<CumulativeDeltaBreakoutStrategy>();
	
	[TestMethod]
	public Task VolumeSurge()
		=> RunStrategy<VolumeSurgeStrategy>();
	
	[TestMethod]
	public Task DoubleBottom()
		=> RunStrategy<DoubleBottomStrategy>();
	
	[TestMethod]
	public Task DoubleTop()
		=> RunStrategy<DoubleTopStrategy>();
	
	[TestMethod]
	public Task RsiOverboughtOversold()
		=> RunStrategy<RsiOverboughtOversoldStrategy>();
	
	[TestMethod]
	public Task HammerCandle()
		=> RunStrategy<HammerCandleStrategy>();
	
	[TestMethod]
	public Task ShootingStar()
		=> RunStrategy<ShootingStarStrategy>();
	
	[TestMethod]
	public Task MacdDivergence()
		=> RunStrategy<MacdDivergenceStrategy>();
	
	[TestMethod]
	public Task StochasticOverboughtOversold()
		=> RunStrategy<StochasticOverboughtOversoldStrategy>();
	
	[TestMethod]
	public Task EngulfingBullish()
		=> RunStrategy<EngulfingBullishStrategy>();
	
	[TestMethod]
	public Task EngulfingBearish()
		=> RunStrategy<EngulfingBearishStrategy>();
	
	[TestMethod]
	public Task PinbarReversal()
		=> RunStrategy<PinbarReversalStrategy>();
	
	[TestMethod]
	public Task ThreeBarReversalUp()
		=> RunStrategy<ThreeBarReversalUpStrategy>();
	
	[TestMethod]
	public Task ThreeBarReversalDown()
		=> RunStrategy<ThreeBarReversalDownStrategy>();
	
	[TestMethod]
	public Task CciDivergence()
		=> RunStrategy<CciDivergenceStrategy>();
	
	[TestMethod]
	public Task BollingerBandReversal()
		=> RunStrategy<BollingerBandReversalStrategy>();
	
	[TestMethod]
	public Task MorningStar()
		=> RunStrategy<MorningStarStrategy>();
	
	[TestMethod]
	public Task EveningStar()
		=> RunStrategy<EveningStarStrategy>();
	
	[TestMethod]
	public Task DojiReversal()
		=> RunStrategy<DojiReversalStrategy>();
	
	[TestMethod]
	public Task KeltnerChannelReversal()
		=> RunStrategy<KeltnerChannelReversalStrategy>();
	
	[TestMethod]
	public Task WilliamsPercentRDivergence()
		=> RunStrategy<WilliamsPercentRDivergenceStrategy>();
	
	[TestMethod]
	public Task OBVDivergence()
		=> RunStrategy<OBVDivergenceStrategy>();
	
	[TestMethod]
	public Task FibonacciRetracementReversal()
		=> RunStrategy<FibonacciRetracementReversalStrategy>();
	
	[TestMethod]
	public Task InsideBarBreakout()
		=> RunStrategy<InsideBarBreakoutStrategy>();
	
	[TestMethod]
	public Task OutsideBarReversal()
		=> RunStrategy<OutsideBarReversalStrategy>();
	
	[TestMethod]
	public Task TrendlineBounce()
		=> RunStrategy<TrendlineBounceStrategy>();
	
	[TestMethod]
	public Task PivotPointReversal()
		=> RunStrategy<PivotPointReversalStrategy>();
	
	[TestMethod]
	public Task VwapBounce()
		=> RunStrategy<VwapBounceStrategy>();
	
	[TestMethod]
	public Task VolumeExhaustion()
		=> RunStrategy<VolumeExhaustionStrategy>();
	
	[TestMethod]
	public Task AdxWeakening()
		=> RunStrategy<AdxWeakeningStrategy>();
	
	[TestMethod]
	public Task AtrExhaustion()
		=> RunStrategy<AtrExhaustionStrategy>();
	
	[TestMethod]
	public Task IchimokuTenkanKijun()
		=> RunStrategy<IchimokuTenkanKijunStrategy>();
	
	[TestMethod]
	public Task HeikinAshiReversal()
		=> RunStrategy<HeikinAshiReversalStrategy>();
	
	[TestMethod]
	public Task ParabolicSarReversal()
		=> RunStrategy<ParabolicSarReversalStrategy>();
	
	[TestMethod]
	public Task SupertrendReversal()
		=> RunStrategy<SupertrendReversalStrategy>();
	
	[TestMethod]
	public Task HullMaReversal()
		=> RunStrategy<HullMaReversalStrategy>();
	
	[TestMethod]
	public Task DonchianReversal()
		=> RunStrategy<DonchianReversalStrategy>();
	
	[TestMethod]
	public Task MacdHistogramReversal()
		=> RunStrategy<MacdHistogramReversalStrategy>();
	
	[TestMethod]
	public Task RsiHookReversal()
		=> RunStrategy<RsiHookReversalStrategy>();
	
	[TestMethod]
	public Task StochasticHookReversal()
		=> RunStrategy<StochasticHookReversalStrategy>();
	
	[TestMethod]
	public Task CciHookReversal()
		=> RunStrategy<CciHookReversalStrategy>();
	
	[TestMethod]
	public Task WilliamsRHookReversal()
		=> RunStrategy<WilliamsRHookReversalStrategy>();
	
	[TestMethod]
	public Task ThreeWhiteSoldiers()
		=> RunStrategy<ThreeWhiteSoldiersStrategy>();
	
	[TestMethod]
	public Task ThreeBlackCrows()
		=> RunStrategy<ThreeBlackCrowsStrategy>();
	
	[TestMethod]
	public Task GapFillReversal()
		=> RunStrategy<GapFillReversalStrategy>();
	
	[TestMethod]
	public Task TweezerBottom()
		=> RunStrategy<TweezerBottomStrategy>();
	
	[TestMethod]
	public Task TweezerTop()
		=> RunStrategy<TweezerTopStrategy>();
	
	[TestMethod]
	public Task HaramiBullish()
		=> RunStrategy<HaramiBullishStrategy>();
	
	[TestMethod]
	public Task HaramiBearish()
		=> RunStrategy<HaramiBearishStrategy>();
	
	[TestMethod]
	public Task DarkPoolPrints()
		=> RunStrategy<DarkPoolPrintsStrategy>();
	
	[TestMethod]
	public Task RejectionCandle()
		=> RunStrategy<RejectionCandleStrategy>();
	
	[TestMethod]
	public Task FalseBreakoutTrap()
		=> RunStrategy<FalseBreakoutTrapStrategy>();
	
	[TestMethod]
	public Task SpringReversal()
		=> RunStrategy<SpringReversalStrategy>();
	
	[TestMethod]
	public Task UpthrustReversal()
		=> RunStrategy<UpthrustReversalStrategy>();
	
	[TestMethod]
	public Task WyckoffAccumulation()
		=> RunStrategy<WyckoffAccumulationStrategy>();
	
	[TestMethod]
	public Task WyckoffDistribution()
		=> RunStrategy<WyckoffDistributionStrategy>();
	
	[TestMethod]
	public Task RsiFailureSwing()
		=> RunStrategy<RsiFailureSwingStrategy>();
	
	[TestMethod]
	public Task StochasticFailureSwing()
		=> RunStrategy<StochasticFailureSwingStrategy>();
	
	[TestMethod]
	public Task CciFailureSwing()
		=> RunStrategy<CciFailureSwingStrategy>();
	
	[TestMethod]
	public Task BullishAbandonedBaby()
		=> RunStrategy<BullishAbandonedBabyStrategy>();
	
	[TestMethod]
	public Task BearishAbandonedBaby()
		=> RunStrategy<BearishAbandonedBabyStrategy>();
	
	[TestMethod]
	public Task VolumeClimaxReversal()
		=> RunStrategy<VolumeClimaxReversalStrategy>();
	
	[TestMethod]
	public Task DayOfWeek()
		=> RunStrategy<DayOfWeekStrategy>();
	
	[TestMethod]
	public Task MonthOfYear()
		=> RunStrategy<MonthOfYearStrategy>();
	
	[TestMethod]
	public Task TurnaroundTuesday()
		=> RunStrategy<TurnaroundTuesdayStrategy>();
	
	[TestMethod]
	public Task EndOfMonthStrength()
		=> RunStrategy<EndOfMonthStrengthStrategy>();
	
	[TestMethod]
	public Task FirstDayOfMonth()
		=> RunStrategy<FirstDayOfMonthStrategy>();
	
	[TestMethod]
	public Task SantaClausRally()
		=> RunStrategy<SantaClausRallyStrategy>();
	
	[TestMethod]
	public Task JanuaryEffect()
		=> RunStrategy<JanuaryEffectStrategy>();
	
	[TestMethod]
	public Task MondayWeakness()
		=> RunStrategy<MondayWeaknessStrategy>();
	
	[TestMethod]
	public Task PreHolidayStrength()
		=> RunStrategy<PreHolidayStrengthStrategy>();
	
	[TestMethod]
	public Task PostHolidayWeakness()
		=> RunStrategy<PostHolidayWeaknessStrategy>();
	
	[TestMethod]
	public Task QuarterlyExpiry()
		=> RunStrategy<QuarterlyExpiryStrategy>();
	
	[TestMethod]
	public Task OpenDrive()
		=> RunStrategy<OpenDriveStrategy>();
	
	[TestMethod]
	public Task MiddayReversal()
		=> RunStrategy<MiddayReversalStrategy>();
	
	[TestMethod]
	public Task OvernightGap()
		=> RunStrategy<OvernightGapStrategy>();
	
	[TestMethod]
	public Task LunchBreakFade()
		=> RunStrategy<LunchBreakFadeStrategy>();
	
	[TestMethod]
	public Task MacdRsi()
		=> RunStrategy<MacdRsiStrategy>();
	
	[TestMethod]
	public Task BollingerStochastic()
		=> RunStrategy<BollingerStochasticStrategy>();
	
	[TestMethod]
	public Task MaVolume()
		=> RunStrategy<MaVolumeStrategy>();
	
	[TestMethod]
	public Task AdxMacd()
		=> RunStrategy<AdxMacdStrategy>();
	
	[TestMethod]
	public Task IchimokuRsi()
		=> RunStrategy<IchimokuRsiStrategy>();
	
	[TestMethod]
	public Task SupertrendVolume()
		=> RunStrategy<SupertrendVolumeStrategy>();
	
	[TestMethod]
	public Task BollingerRsi()
		=> RunStrategy<BollingerRsiStrategy>();
	
	[TestMethod]
	public Task MaStochastic()
		=> RunStrategy<MaStochasticStrategy>();
	
	[TestMethod]
	public Task AtrMacd()
		=> RunStrategy<AtrMacdStrategy>();
	
	[TestMethod]
	public Task VwapRsi()
		=> RunStrategy<VwapRsiStrategy>();
	
	[TestMethod]
	public Task DonchianVolume()
		=> RunStrategy<DonchianVolumeStrategy>();
	
	[TestMethod]
	public Task KeltnerStochastic()
		=> RunStrategy<KeltnerStochasticStrategy>();
	
	[TestMethod]
	public Task ParabolicSarRsi()
		=> RunStrategy<ParabolicSarRsiStrategy>();
	
	[TestMethod]
	public Task HullMaVolume()
		=> RunStrategy<HullMaVolumeStrategy>();
	
	[TestMethod]
	public Task AdxStochastic()
		=> RunStrategy<AdxStochasticStrategy>();
	
	[TestMethod]
	public Task MacdVolume()
		=> RunStrategy<MacdVolumeStrategy>();
	
	[TestMethod]
	public Task BollingerVolume()
		=> RunStrategy<BollingerVolumeStrategy>();
	
	[TestMethod]
	public Task RsiStochastic()
		=> RunStrategy<RsiStochasticStrategy>();
	
	[TestMethod]
	public Task MaAdx()
		=> RunStrategy<MaAdxStrategy>();
	
	[TestMethod]
	public Task VwapStochastic()
		=> RunStrategy<VwapStochasticStrategy>();
	
	[TestMethod]
	public Task IchimokuVolume()
		=> RunStrategy<IchimokuVolumeStrategy>();
	
	[TestMethod]
	public Task SupertrendRsi()
		=> RunStrategy<SupertrendRsiStrategy>();
	
	[TestMethod]
	public Task BollingerAdx()
		=> RunStrategy<BollingerAdxStrategy>();
	
	[TestMethod]
	public Task MaCci()
		=> RunStrategy<MaCciStrategy>();
	
	[TestMethod]
	public Task VwapVolume()
		=> RunStrategy<VwapVolumeStrategy>();
	
	[TestMethod]
	public Task DonchianRsi()
		=> RunStrategy<DonchianRsiStrategy>();
	
	[TestMethod]
	public Task KeltnerVolume()
		=> RunStrategy<KeltnerVolumeStrategy>();
	
	[TestMethod]
	public Task ParabolicSarStochastic()
		=> RunStrategy<ParabolicSarStochasticStrategy>();
	
	[TestMethod]
	public Task HullMaRsi()
		=> RunStrategy<HullMaRsiStrategy>();
	
	[TestMethod]
	public Task AdxVolume()
		=> RunStrategy<AdxVolumeStrategy>();
	
	[TestMethod]
	public Task MacdCci()
		=> RunStrategy<MacdCciStrategy>();
	
	[TestMethod]
	public Task BollingerCci()
		=> RunStrategy<BollingerCciStrategy>();
	
	[TestMethod]
	public Task RsiWilliamsR()
		=> RunStrategy<RsiWilliamsRStrategy>();
	
	[TestMethod]
	public Task MaWilliamsR()
		=> RunStrategy<MaWilliamsRStrategy>();
	
	[TestMethod]
	public Task VwapCci()
		=> RunStrategy<VwapCciStrategy>();
	
	[TestMethod]
	public Task DonchianStochastic()
		=> RunStrategy<DonchianStochasticStrategy>();
	
	[TestMethod]
	public Task KeltnerRsi()
		=> RunStrategy<KeltnerRsiStrategy>();
	
	[TestMethod]
	public Task HullMaStochastic()
		=> RunStrategy<HullMaStochasticStrategy>();
	
	[TestMethod]
	public Task AdxCci()
		=> RunStrategy<AdxCciStrategy>();
	
	[TestMethod]
	public Task MacdWilliamsR()
		=> RunStrategy<MacdWilliamsRStrategy>();
	
	[TestMethod]
	public Task BollingerWilliamsR()
		=> RunStrategy<BollingerWilliamsRStrategy>();
	
	[TestMethod]
	public Task MacdVwap()
		=> RunStrategy<MacdVwapStrategy>();
	
	[TestMethod]
	public Task RsiSupertrend()
		=> RunStrategy<RsiSupertrendStrategy>();
	
	[TestMethod]
	public Task AdxBollinger()
		=> RunStrategy<AdxBollingerStrategy>();
	
	[TestMethod]
	public Task IchimokuStochastic()
		=> RunStrategy<IchimokuStochasticStrategy>();
	
	[TestMethod]
	public Task SupertrendStochastic()
		=> RunStrategy<SupertrendStochasticStrategy>();
	
	[TestMethod]
	public Task DonchianMacd()
		=> RunStrategy<DonchianMacdStrategy>();
	
	[TestMethod]
	public Task ParabolicSarVolume()
		=> RunStrategy<ParabolicSarVolumeStrategy>();
	
	[TestMethod]
	public Task VwapAdx()
		=> RunStrategy<VwapAdxStrategy>();
	
	[TestMethod]
	public Task SupertrendAdx()
		=> RunStrategy<SupertrendAdxStrategy>();
	
	[TestMethod]
	public Task KeltnerMacd()
		=> RunStrategy<KeltnerMacdStrategy>();
	
	[TestMethod]
	public Task HullMaAdx()
		=> RunStrategy<HullMaAdxStrategy>();
	
	[TestMethod]
	public Task VwapMacd()
		=> RunStrategy<VwapMacdStrategy>();
	
	[TestMethod]
	public Task IchimokuAdx()
		=> RunStrategy<IchimokuAdxStrategy>();
	
	[TestMethod]
	public Task VwapWilliamsR()
		=> RunStrategy<VwapWilliamsRStrategy>();
	
	[TestMethod]
	public Task DonchianCci()
		=> RunStrategy<DonchianCciStrategy>();
	
	[TestMethod]
	public Task KeltnerWilliamsR()
		=> RunStrategy<KeltnerWilliamsRStrategy>();
	
	[TestMethod]
	public Task ParabolicSarCci()
		=> RunStrategy<ParabolicSarCciStrategy>();
	
	[TestMethod]
	public Task HullMaCci()
		=> RunStrategy<HullMaCciStrategy>();
	
	[TestMethod]
	public Task MacdBollinger()
		=> RunStrategy<MacdBollingerStrategy>();
	
	[TestMethod]
	public Task RsiHullMa()
		=> RunStrategy<RsiHullMaStrategy>();
	
	[TestMethod]
	public Task StochasticKeltner()
		=> RunStrategy<StochasticKeltnerStrategy>();
	
	[TestMethod]
	public Task VolumeSupertrend()
		=> RunStrategy<VolumeSupertrendStrategy>();
	
	[TestMethod]
	public Task AdxDonchian()
		=> RunStrategy<AdxDonchianStrategy>();
	
	[TestMethod]
	public Task CciVwap()
		=> RunStrategy<CciVwapStrategy>();
	
	[TestMethod]
	public Task WilliamsIchimoku()
		=> RunStrategy<WilliamsIchimokuStrategy>();
	
	[TestMethod]
	public Task MaParabolicSar()
		=> RunStrategy<MaParabolicSarStrategy>();
	
	[TestMethod]
	public Task BollingerSupertrend()
		=> RunStrategy<BollingerSupertrendStrategy>();
	
	[TestMethod]
	public Task RsiDonchian()
		=> RunStrategy<RsiDonchianStrategy>();
	
	[TestMethod]
	public Task MeanReversion()
		=> RunStrategy<MeanReversionStrategy>();
	
	[TestMethod]
	public Task PairsTrading()
		=> RunStrategy<PairsTradingStrategy>((strategy, sec) =>
	{
		strategy.SecondSecurity = sec;
	});
	
	[TestMethod]
	public Task ZScoreReversal()
		=> RunStrategy<ZScoreReversalStrategy>();
	
	[TestMethod]
	public Task StatisticalArbitrage()
		=> RunStrategy<StatisticalArbitrageStrategy>((strategy, sec) =>
	{
		strategy.SecondSecurity = sec;
	});
	
	[TestMethod]
	public Task VolatilityBreakout()
		=> RunStrategy<VolatilityBreakoutStrategy>();
	
	[TestMethod]
	public Task BollingerBandSqueeze()
		=> RunStrategy<BollingerBandSqueezeStrategy>();
	
	[TestMethod]
	public Task CointegrationPairs()
		=> RunStrategy<CointegrationPairsStrategy>((strategy, sec) => strategy.Asset2 = sec);
	
	[TestMethod]
	public Task MomentumDivergence()
		=> RunStrategy<MomentumDivergenceStrategy>();
	
	[TestMethod]
	public Task AtrMeanReversion()
		=> RunStrategy<AtrMeanReversionStrategy>();
	
	[TestMethod]
	public Task KalmanFilterTrend()
		=> RunStrategy<KalmanFilterTrendStrategy>();
	
	[TestMethod]
	public Task VolatilityAdjustedMeanReversion()
		=> RunStrategy<VolatilityAdjustedMeanReversionStrategy>();
	
	[TestMethod]
	public Task HurstExponentTrend()
		=> RunStrategy<HurstExponentTrendStrategy>();
	
	[TestMethod]
	public Task HurstExponentReversion()
		=> RunStrategy<HurstExponentReversionStrategy>();
	
	[TestMethod]
	public Task AutocorrelationReversion()
		=> RunStrategy<AutocorrelationReversionStrategy>();
	
	[TestMethod]
	public Task DeltaNeutralArbitrage()
		=> RunStrategy<DeltaNeutralArbitrageStrategy>((strategy, sec) =>
	{
		strategy.Asset2Security = sec;
		strategy.Asset2Portfolio = strategy.Portfolio;
	});
	
	[TestMethod]
	public Task VolatilitySkewArbitrage()
		=> RunStrategy<VolatilitySkewArbitrageStrategy>();
	
	[TestMethod]
	public Task CorrelationBreakout()
		=> RunStrategy<CorrelationBreakoutStrategy>();
	
	[TestMethod]
	public Task BetaNeutralArbitrage()
		=> RunStrategy<BetaNeutralArbitrageStrategy>();
	
	[TestMethod]
	public Task VwapMeanReversion()
		=> RunStrategy<VwapMeanReversionStrategy>();
	
	[TestMethod]
	public Task RsiMeanReversion()
		=> RunStrategy<RsiMeanReversionStrategy>();
	
	[TestMethod]
	public Task StochasticMeanReversion()
		=> RunStrategy<StochasticMeanReversionStrategy>();
	
	[TestMethod]
	public Task CciMeanReversion()
		=> RunStrategy<CciMeanReversionStrategy>();
	
	[TestMethod]
	public Task WilliamsRMeanReversion()
		=> RunStrategy<WilliamsRMeanReversionStrategy>();
	
	[TestMethod]
	public Task MacdMeanReversion()
		=> RunStrategy<MacdMeanReversionStrategy>();
	
	[TestMethod]
	public Task AdxMeanReversion()
		=> RunStrategy<AdxMeanReversionStrategy>();
	
	[TestMethod]
	public Task VolatilityMeanReversion()
		=> RunStrategy<VolatilityMeanReversionStrategy>();
	
	[TestMethod]
	public Task VolumeMeanReversion()
		=> RunStrategy<VolumeMeanReversionStrategy>();
	
	[TestMethod]
	public Task ObvMeanReversion()
		=> RunStrategy<ObvMeanReversionStrategy>();
	
	[TestMethod]
	public Task MomentumBreakout()
		=> RunStrategy<MomentumBreakoutStrategy>();
	
	[TestMethod]
	public Task RsiBreakout()
		=> RunStrategy<RsiBreakoutStrategy>();
	
	[TestMethod]
	public Task StochasticBreakout()
		=> RunStrategy<StochasticBreakoutStrategy>();
	
	[TestMethod]
	public Task WilliamsRBreakout()
		=> RunStrategy<WilliamsRBreakoutStrategy>();
	
	[TestMethod]
	public Task MacdBreakout()
		=> RunStrategy<MacdBreakoutStrategy>();
	
	[TestMethod]
	public Task ADXBreakout()
		=> RunStrategy<ADXBreakoutStrategy>();
	
	[TestMethod]
	public Task VolumeBreakout()
		=> RunStrategy<VolumeBreakoutStrategy>();
	
	[TestMethod]
	public Task BollingerWidthBreakout()
		=> RunStrategy<BollingerWidthBreakoutStrategy>();
	
	[TestMethod]
	public Task KeltnerWidthBreakout()
		=> RunStrategy<KeltnerWidthBreakoutStrategy>();
	
	[TestMethod]
	public Task DonchianWidthBreakout()
		=> RunStrategy<DonchianWidthBreakoutStrategy>();
	
	[TestMethod]
	public Task IchimokuWidthBreakout()
		=> RunStrategy<IchimokuWidthBreakoutStrategy>();
	
	[TestMethod]
	public Task SupertrendDistanceBreakout()
		=> RunStrategy<SupertrendDistanceBreakoutStrategy>();
	
	[TestMethod]
	public Task ParabolicSarDistanceBreakout()
		=> RunStrategy<ParabolicSarDistanceBreakoutStrategy>();
	
	[TestMethod]
	public Task HullMaSlopeBreakout()
		=> RunStrategy<HullMaSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task MaSlopeBreakout()
		=> RunStrategy<MaSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task EmaSlopeBreakout()
		=> RunStrategy<EmaSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task VolatilityAdjustedMomentum()
		=> RunStrategy<VolatilityAdjustedMomentumStrategy>();
	
	[TestMethod]
	public Task VwapSlopeBreakout()
		=> RunStrategy<VwapSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task RsiSlopeBreakout()
		=> RunStrategy<RsiSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task StochasticSlopeBreakout()
		=> RunStrategy<StochasticSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task CciSlopeBreakout()
		=> RunStrategy<CciSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task WilliamsRSlopeBreakout()
		=> RunStrategy<WilliamsRSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task MacdSlopeBreakout()
		=> RunStrategy<MacdSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task AdxSlopeBreakout()
		=> RunStrategy<AdxSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task AtrSlopeBreakout()
		=> RunStrategy<AtrSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task VolumeSlopeBreakout()
		=> RunStrategy<VolumeSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task ObvSlopeBreakout()
		=> RunStrategy<ObvSlopeBreakoutStrategy>();
	
	[TestMethod]
	public Task BollingerWidthMeanReversion()
		=> RunStrategy<BollingerWidthMeanReversionStrategy>();
	
	[TestMethod]
	public Task KeltnerWidthMeanReversion()
		=> RunStrategy<KeltnerWidthMeanReversionStrategy>();
	
	[TestMethod]
	public Task DonchianWidthMeanReversion()
		=> RunStrategy<DonchianWidthMeanReversionStrategy>();
	
	[TestMethod]
	public Task IchimokuCloudWidthMeanReversion()
		=> RunStrategy<IchimokuCloudWidthMeanReversionStrategy>();
	
	[TestMethod]
	public Task SupertrendDistanceMeanReversion()
		=> RunStrategy<SupertrendDistanceMeanReversionStrategy>();
	
	[TestMethod]
	public Task ParabolicSarDistanceMeanReversion()
		=> RunStrategy<ParabolicSarDistanceMeanReversionStrategy>();
	
	[TestMethod]
	public Task HullMaSlopeMeanReversion()
		=> RunStrategy<HullMaSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task MaSlopeMeanReversion()
		=> RunStrategy<MaSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task EmaSlopeMeanReversion()
		=> RunStrategy<EmaSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task VwapSlopeMeanReversion()
		=> RunStrategy<VwapSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task RsiSlopeMeanReversion()
		=> RunStrategy<RsiSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task StochasticSlopeMeanReversion()
		=> RunStrategy<StochasticSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task CciSlopeMeanReversion()
		=> RunStrategy<CciSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task WilliamsRSlopeMeanReversion()
		=> RunStrategy<WilliamsRSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task MacdSlopeMeanReversion()
		=> RunStrategy<MacdSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task AdxSlopeMeanReversion()
		=> RunStrategy<AdxSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task AtrSlopeMeanReversion()
		=> RunStrategy<AtrSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task VolumeSlopeMeanReversion()
		=> RunStrategy<VolumeSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task ObvSlopeMeanReversion()
		=> RunStrategy<ObvSlopeMeanReversionStrategy>();
	
	[TestMethod]
	public Task PairsTradingVolatilityFilter()
		=> RunStrategy<PairsTradingVolatilityFilterStrategy>((strategy, sec) =>
	{
		strategy.Security2 = sec;
	});
	
	[TestMethod]
	public Task ZScoreVolumeFilter()
		=> RunStrategy<ZScoreVolumeFilterStrategy>();
	
	[TestMethod]
	public Task CorrelationMeanReversion()
		=> RunStrategy<CorrelationMeanReversionStrategy>((strategy, sec) => strategy.Security2 = sec);
	
	[TestMethod]
	public Task BetaAdjustedPairs()
		=> RunStrategy<BetaAdjustedPairsStrategy>((strategy, sec) =>
	{
		strategy.Asset2 = sec;
		strategy.Asset2Portfolio = strategy.Portfolio;
	});
	
	[TestMethod]
	public Task HurstVolatilityFilter()
		=> RunStrategy<HurstVolatilityFilterStrategy>();
	
	[TestMethod]
	public Task AdaptiveEmaBreakout()
		=> RunStrategy<AdaptiveEmaBreakoutStrategy>();
	
	[TestMethod]
	public Task VolatilityClusterBreakout()
		=> RunStrategy<VolatilityClusterBreakoutStrategy>();
	
	[TestMethod]
	public Task SeasonalityAdjustedMomentum()
		=> RunStrategy<SeasonalityAdjustedMomentumStrategy>();
	
	[TestMethod]
	public Task RsiDynamicOverboughtOversold()
		=> RunStrategy<RsiDynamicOverboughtOversoldStrategy>();
	
	[TestMethod]
	public Task BollingerVolatilityBreakout()
		=> RunStrategy<BollingerVolatilityBreakoutStrategy>();
	
	[TestMethod]
	public Task MacdAdaptiveHistogram()
		=> RunStrategy<MacdAdaptiveHistogramStrategy>();
	
	[TestMethod]
	public Task IchimokuVolumeCluster()
		=> RunStrategy<IchimokuVolumeClusterStrategy>();
	
	[TestMethod]
	public Task SupertrendWithMomentum()
		=> RunStrategy<SupertrendWithMomentumStrategy>();
	
	[TestMethod]
	public Task DonchianWithVolatilityContraction()
		=> RunStrategy<DonchianWithVolatilityContractionStrategy>();
	
	[TestMethod]
	public Task KeltnerWithRsiDivergence()
		=> RunStrategy<KeltnerWithRsiDivergenceStrategy>();
	
	[TestMethod]
	public Task HullMaWithVolumeSpike()
		=> RunStrategy<HullMaWithVolumeSpikeStrategy>();
	
	[TestMethod]
	public Task VwapWithAdxTrendStrength()
		=> RunStrategy<VwapWithAdxTrendStrengthStrategy>();
	
	[TestMethod]
	public Task ParabolicSarWithVolatilityExpansion()
		=> RunStrategy<ParabolicSarWithVolatilityExpansionStrategy>();
	
	[TestMethod]
	public Task StochasticWithDynamicZones()
		=> RunStrategy<StochasticWithDynamicZonesStrategy>();
	
	[TestMethod]
	public Task AdxWithVolumeBreakout()
		=> RunStrategy<AdxWithVolumeBreakoutStrategy>();
	
	[TestMethod]
	public Task CciWithVolatilityFilter()
		=> RunStrategy<CciWithVolatilityFilterStrategy>();
	
	[TestMethod]
	public Task WilliamsPercentRWithMomentum()
		=> RunStrategy<WilliamsPercentRWithMomentumStrategy>();
	
	[TestMethod]
	public Task BollingerKMeans()
		=> RunStrategy<BollingerKMeansStrategy>();
	
	[TestMethod]
	public Task MacdHmm()
		=> RunStrategy<MacdHmmStrategy>();
	
	[TestMethod]
	public Task IchimokuHurst()
		=> RunStrategy<IchimokuHurstStrategy>();
	
	[TestMethod]
	public Task SupertrendRsiDivergence()
		=> RunStrategy<SupertrendRsiDivergenceStrategy>();
	
	[TestMethod]
	public Task DonchianSeasonal()
		=> RunStrategy<DonchianSeasonalStrategy>();
	
	[TestMethod]
	public Task KeltnerKalman()
		=> RunStrategy<KeltnerKalmanStrategy>();
	
	[TestMethod]
	public Task HullMaVolatilityContraction()
		=> RunStrategy<HullMaVolatilityContractionStrategy>();
	
	[TestMethod]
	public Task VwapAdxTrend()
		=> RunStrategy<VwapAdxTrendStrategy>();
	
	[TestMethod]
	public Task ParabolicSarHurst()
		=> RunStrategy<ParabolicSarHurstStrategy>();
	
	[TestMethod]
	public Task BollingerKalmanFilter()
		=> RunStrategy<BollingerKalmanFilterStrategy>();
	
	[TestMethod]
	public Task MacdVolumeCluster()
		=> RunStrategy<MacdVolumeClusterStrategy>();
	
	[TestMethod]
	public Task IchimokuVolatilityContraction()
		=> RunStrategy<IchimokuVolatilityContractionStrategy>();
	
	[TestMethod]
	public Task DonchianHurst()
		=> RunStrategy<DonchianHurstStrategy>();
	
	[TestMethod]
	public Task KeltnerSeasonal()
		=> RunStrategy<KeltnerSeasonalStrategy>();
	
	[TestMethod]
	public Task HullKMeansCluster()
		=> RunStrategy<HullKMeansClusterStrategy>();
	
	[TestMethod]
	public Task VwapHiddenMarkovModel()
		=> RunStrategy<VwapHiddenMarkovModelStrategy>();
	
	[TestMethod]
	public Task ParabolicSarRsiDivergence()
		=> RunStrategy<ParabolicSarRsiDivergenceStrategy>();
	
	[TestMethod]
	public Task AdaptiveRsiVolume()
		=> RunStrategy<AdaptiveRsiVolumeStrategy>();
	
	[TestMethod]
	public Task AdaptiveBollingerBreakout()
		=> RunStrategy<AdaptiveBollingerBreakoutStrategy>();
	
	[TestMethod]
	public Task MacdWithSentimentFilter()
		=> RunStrategy<MacdWithSentimentFilterStrategy>();
	
	[TestMethod]
	public Task IchimokuWithImpliedVolatility()
		=> RunStrategy<IchimokuWithImpliedVolatilityStrategy>();
	
	[TestMethod]
	public Task SupertrendWithPutCallRatio()
		=> RunStrategy<SupertrendWithPutCallRatioStrategy>();
	
	[TestMethod]
	public Task DonchianWithSentimentSpike()
		=> RunStrategy<DonchianWithSentimentSpikeStrategy>();
	
	[TestMethod]
	public Task KeltnerWithRLSignal()
		=> RunStrategy<KeltnerWithRLSignalStrategy>();
	
	[TestMethod]
	public Task HullMAWithImpliedVolatilityBreakout()
		=> RunStrategy<HullMAWithImpliedVolatilityBreakoutStrategy>();
	
	[TestMethod]
	public Task VwapWithBehavioralBiasFilter()
		=> RunStrategy<VwapWithBehavioralBiasFilterStrategy>();
	
	[TestMethod]
	public Task ParabolicSarSentimentDivergence()
		=> RunStrategy<ParabolicSarSentimentDivergenceStrategy>();
	
	[TestMethod]
	public Task RsiWithOptionOpenInterest()
		=> RunStrategy<RsiWithOptionOpenInterestStrategy>();
	
	[TestMethod]
	public Task StochasticImpliedVolatilitySkew()
		=> RunStrategy<StochasticImpliedVolatilitySkewStrategy>();
	
	[TestMethod]
	public Task AdxSentimentMomentum()
		=> RunStrategy<AdxSentimentMomentumStrategy>();
	
	[TestMethod]
	public Task CciPutCallRatioDivergence()
		=> RunStrategy<CciPutCallRatioDivergenceStrategy>();
	
	[TestMethod]
	public Task AccrualAnomaly()
		=> RunStrategy<AccrualAnomalyStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task AssetClassMomentumRotational()
		=> RunStrategy<AssetClassMomentumRotationalStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task AssetClassTrendFollowing()
		=> RunStrategy<AssetClassTrendFollowingStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task AssetGrowthEffect()
		=> RunStrategy<AssetGrowthEffectStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task BettingAgainstBetaStocks()
		=> RunStrategy<BettingAgainstBetaStocksStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task BettingAgainstBeta()
		=> RunStrategy<BettingAgainstBetaStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task BitcoinIntradaySeasonality()
		=> RunStrategy<BitcoinIntradaySeasonalityStrategy>();
	
	[TestMethod]
	public Task BookToMarketValue()
		=> RunStrategy<BookToMarketValueStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task CommodityMomentum()
		=> RunStrategy<CommodityMomentumStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task ConsistentMomentum()
		=> RunStrategy<ConsistentMomentumStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task CountryValueFactor()
		=> RunStrategy<CountryValueFactorStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task CrudeOilPredictsEquity()
		=> RunStrategy<CrudeOilPredictsEquityStrategy>((stra, sec) =>
	{
		stra.Oil = sec;
		stra.CashEtf = sec;
	});
	
	[TestMethod]
	public Task CryptoRebalancingPremium()
		=> RunStrategy<CryptoRebalancingPremiumStrategy>((stra, sec) =>
	{
		stra.ETH = sec;
	});
	
	[TestMethod]
	public Task CurrencyMomentumFactor()
		=> RunStrategy<CurrencyMomentumFactorStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task CurrencyPPPValue()
		=> RunStrategy<CurrencyPPPValueStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task DispersionTrading()
		=> RunStrategy<DispersionTradingStrategy>((stra, sec) =>
	{
		stra.Constituents = [sec];
	});
	
	[TestMethod]
	public Task DollarCarryTrade()
		=> RunStrategy<DollarCarryTradeStrategy>((stra, sec) =>
	{
		stra.Pairs = [sec];
	});
	
	[TestMethod]
	public Task EarningsAnnouncementPremium()
		=> RunStrategy<EarningsAnnouncementPremiumStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task EarningsAnnouncementReversal()
		=> RunStrategy<EarningsAnnouncementReversalStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task EarningsAnnouncementsWithBuybacks()
		=> RunStrategy<EarningsAnnouncementsWithBuybacksStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task EarningsQualityFactor()
		=> RunStrategy<EarningsQualityFactorStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task ESGFactorMomentum()
		=> RunStrategy<ESGFactorMomentumStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task FedModel()
		=> RunStrategy<FedModelStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task FScoreReversal()
		=> RunStrategy<FScoreReversalStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task FXCarryTrade()
		=> RunStrategy<FXCarryTradeStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task JanuaryBarometer()
		=> RunStrategy<JanuaryBarometerStrategy>((stra, sec) =>
	{
		stra.EquityETF = sec;
		stra.CashETF = sec;
	});
	
	[TestMethod]
	public Task LexicalDensityFilings()
		=> RunStrategy<LexicalDensityFilingsStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task LowVolatilityStocks()
		=> RunStrategy<LowVolatilityStocksStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task MomentumAssetGrowth()
		=> RunStrategy<MomentumAssetGrowthStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task MomentumFactorStocks()
		=> RunStrategy<MomentumFactorStocksStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task MomentumRevVol()
		=> RunStrategy<MomentumRevVolStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task MomentumStyleRotation()
		=> RunStrategy<MomentumStyleRotationStrategy>((stra, sec) =>
	{
		stra.FactorETFs = [sec];
	});
	
	[TestMethod]
	public Task Month12Cycle()
		=> RunStrategy<Month12CycleStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task MutualFundMomentum()
		=> RunStrategy<MutualFundMomentumStrategy>((stra, sec) =>
	{
		stra.Funds = [sec];
	});
	
	[TestMethod]
	public Task OptionExpirationWeek()
		=> RunStrategy<OptionExpirationWeekStrategy>();
	
	[TestMethod]
	public Task OvernightSentimentAnomaly()
		=> RunStrategy<OvernightSentimentAnomalyStrategy>((stra, sec) =>
	{
		stra.SentimentSymbol = sec;
	});
	
	[TestMethod]
	public Task PairedSwitching()
		=> RunStrategy<PairedSwitchingStrategy>((stra, sec) =>
	{
		stra.SecondETF = sec;
	});
	
	[TestMethod]
	public Task PairsTradingCountryETFs()
		=> RunStrategy<PairsTradingCountryETFsStrategy>((stra, sec) =>
	{
		stra.Universe = [stra.Security, sec];
	});
	
	[TestMethod]
	public Task PairsTradingStocks()
		=> RunStrategy<PairsTradingStocksStrategy>((stra, sec) =>
	{
		stra.Pairs = [(stra.Security, sec)];
	});
	
	[TestMethod]
	public Task PaydayAnomaly()
		=> RunStrategy<PaydayAnomalyStrategy>();
	
	[TestMethod]
	public Task RDExpenditures()
		=> RunStrategy<RDExpendituresStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task ResidualMomentumFactor()
		=> RunStrategy<ResidualMomentumFactorStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task ReturnAsymmetryCommodity()
		=> RunStrategy<ReturnAsymmetryCommodityStrategy>((stra, sec) =>
	{
		stra.Futures = [sec];
	});
	
	[TestMethod]
	public Task ROAEffectStocks()
		=> RunStrategy<ROAEffectStocksStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task SectorMomentumRotation()
		=> RunStrategy<SectorMomentumRotationStrategy>((stra, sec) =>
	{
		stra.SectorETFs = [sec];
	});
	
	[TestMethod]
	public Task ShortInterestEffect()
		=> RunStrategy<ShortInterestEffectStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task ShortTermReversalFutures()
		=> RunStrategy<ShortTermReversalFuturesStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task ShortTermReversalStocks()
		=> RunStrategy<ShortTermReversalStocksStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task SkewnessCommodity()
		=> RunStrategy<SkewnessCommodityStrategy>((stra, sec) =>
	{
		stra.Futures = [sec];
	});
	
	[TestMethod]
	public Task SmallCapPremium()
		=> RunStrategy<SmallCapPremiumStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task SmartFactorsMomentumMarket()
		=> RunStrategy<SmartFactorsMomentumMarketStrategy>((stra, sec) =>
	{
		stra.Factors = [sec];
	});
	
	[TestMethod]
	public Task SoccerClubsArbitrage()
		=> RunStrategy<SoccerClubsArbitrageStrategy>((stra, sec) =>
	{
		stra.Pair = [stra.Security, sec];
	});
	
	[TestMethod]
	public Task SyntheticLendingRates()
		=> RunStrategy<SyntheticLendingRatesStrategy>();
	
	[TestMethod]
	public Task TermStructureCommodities()
		=> RunStrategy<TermStructureCommoditiesStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task TimeSeriesMomentum()
		=> RunStrategy<TimeSeriesMomentumStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task TrendFollowingStocks()
		=> RunStrategy<TrendFollowingStocksStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task TurnOfMonth()
		=> RunStrategy<TurnOfMonthStrategy>();
	
	[TestMethod]
	public Task ValueMomentumAcrossAssets()
		=> RunStrategy<ValueMomentumAcrossAssetsStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task VolatilityRiskPremium()
		=> RunStrategy<VolatilityRiskPremiumStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task Weeks52High()
		=> RunStrategy<Weeks52HighStrategy>((stra, sec) =>
	{
		stra.Universe = [sec];
	});
	
	[TestMethod]
	public Task WTIBrentSpread()
		=> RunStrategy<WTIBrentSpreadStrategy>((stra, sec) =>
	{
		stra.Brent = sec;
	});
	
	[TestMethod]
	public Task BollingerAroon()
		=> RunStrategy<BollingerAroonStrategy>();
	
	[TestMethod]
	public Task BollingerDivergence()
		=> RunStrategy<BollingerDivergenceStrategy>();
	
	[TestMethod]
	public Task BollingerWinnerLite()
		=> RunStrategy<BollingerWinnerLiteStrategy>();
	
	[TestMethod]
	public Task BollingerWinnerPro()
		=> RunStrategy<BollingerWinnerProStrategy>();
	
	[TestMethod]
	public Task BollingerBreakout()
		=> RunStrategy<BollingerBreakoutStrategy>();
	
	[TestMethod]
	public Task DmiWinner()
		=> RunStrategy<DmiWinnerStrategy>();
	
	[TestMethod]
	public Task DoubleRsi()
		=> RunStrategy<DoubleRsiStrategy>();
	
	[TestMethod]
	public Task DoubleSupertrend()
		=> RunStrategy<DoubleSupertrendStrategy>();
	
	[TestMethod]
	public Task EmaMovingAway()
		=> RunStrategy<EmaMovingAwayStrategy>();
	
	[TestMethod]
	public Task EmaSmaRsi()
		=> RunStrategy<EmaSmaRsiStrategy>();
	
	[TestMethod]
	public Task ExceededCandle()
		=> RunStrategy<ExceededCandleStrategy>();
	
	[TestMethod]
	public Task FlawlessVictory()
		=> RunStrategy<FlawlessVictoryStrategy>();
	
	[TestMethod]
	public Task FullCandle()
		=> RunStrategy<FullCandleStrategy>();
	
	[TestMethod]
	public Task GridBot()
		=> RunStrategy<GridBotStrategy>();
	
	[TestMethod]
	public Task HaUniversal()
		=> RunStrategy<HaUniversalStrategy>();
	
	[TestMethod]
	public Task HeikinAshiV2()
		=> RunStrategy<HeikinAshiV2Strategy>();
	
	[TestMethod]
	public Task Improvisando()
		=> RunStrategy<ImprovisandoStrategy>();
	
	[TestMethod]
	public Task JavoV1()
		=> RunStrategy<JavoV1Strategy>();
	
	[TestMethod]
	public Task MacdBbRsi()
		=> RunStrategy<MacdBbRsiStrategy>();
	
	[TestMethod]
	public Task MacdDmi()
		=> RunStrategy<MacdDmiStrategy>();
	
	[TestMethod]
	public Task MacdLong()
		=> RunStrategy<MacdLongStrategy>();
	
	[TestMethod]
	public Task MaCrossDmi()
		=> RunStrategy<MaCrossDmiStrategy>();
	
	[TestMethod]
	public Task MemaBbRsi()
		=> RunStrategy<MemaBbRsiStrategy>();
	
	[TestMethod]
	public Task MtfBb()
		=> RunStrategy<MtfBbStrategy>();
	
	[TestMethod]
	public Task OmarMmr()
		=> RunStrategy<OmarMmrStrategy>();
	
	[TestMethod]
	public Task PinBarMagic()
		=> RunStrategy<PinBarMagicStrategy>();
	
	[TestMethod]
	public Task QqeSignals()
		=> RunStrategy<QqeSignalsStrategy>();
	
	[TestMethod]
	public Task RsiPlus1200()
		=> RunStrategy<RsiPlus1200Strategy>();
	
	[TestMethod]
	public Task RsiEma()
		=> RunStrategy<RsiEmaStrategy>();
	
	[TestMethod]
	public Task StochRsiCrossover()
		=> RunStrategy<StochRsiCrossoverStrategy>();
	
	[TestMethod]
	public Task StochRsiSupertrend()
		=> RunStrategy<StochRsiSupertrendStrategy>();
	
	[TestMethod]
	public Task StrategyTester()
		=> RunStrategy<StrategyTesterStrategy>();
	
	[TestMethod]
	public Task StratBase()
		=> RunStrategy<StratBaseStrategy>();
	
	[TestMethod]
	public Task SupertrendEmaRebound()
		=> RunStrategy<SupertrendEmaReboundStrategy>();
	
	[TestMethod]
	public Task TendencyEmaRsi()
		=> RunStrategy<TendencyEmaRsiStrategy>();
	
	[TestMethod]
	public Task ThreeEmaCross()
		=> RunStrategy<ThreeEmaCrossStrategy>();
	
	[TestMethod]
	public Task TtmSqueeze()
		=> RunStrategy<TtmSqueezeStrategy>();
	
	[TestMethod]
	public Task VelaSuperada()
		=> RunStrategy<VelaSuperadaStrategy>();
	
	[TestMethod]
        public Task WilliamsVixFix()
                => RunStrategy<WilliamsVixFixStrategy>();

        [TestMethod]
        public Task AoDivergence()
                => RunStrategy<AoDivergenceStrategy>();
}

