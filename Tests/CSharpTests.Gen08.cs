namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task CpStratOrb()
        => RunStrategy<CpStratOrbStrategy>();

    [TestMethod]
    public Task CronexAc()
        => RunStrategy<CronexAcStrategy>();

    [TestMethod]
    public Task CronexCci()
        => RunStrategy<CronexCciStrategy>();

    [TestMethod]
    public Task CronexDeMarkerCrossover()
        => RunStrategy<CronexDeMarkerCrossoverStrategy>();

    [TestMethod]
    public Task CronexDeMarker()
        => RunStrategy<CronexDeMarkerStrategy>();

    [TestMethod]
    public Task CronexRsi()
        => RunStrategy<CronexRsiStrategy>();

    [TestMethod]
    public Task CrossLineTrader()
        => RunStrategy<CrossLineTraderStrategy>();

    [TestMethod]
    public Task CrossMA()
        => RunStrategy<CrossMAStrategy>();

    [TestMethod]
    public Task CrossMaAtrNotification()
        => RunStrategy<CrossMaAtrNotificationStrategy>();

    [TestMethod]
    public Task Cross()
        => RunStrategy<CrossStrategy>();

    [TestMethod]
    public Task CrossingMovingAverage()
        => RunStrategy<CrossingMovingAverageStrategy>();

    [TestMethod]
    public Task CrossingOfTwoIMA()
        => RunStrategy<CrossingOfTwoIMAStrategy>();

    [TestMethod]
    public Task CrossingOfTwoIMaV2()
        => RunStrategy<CrossingOfTwoIMaV2Strategy>();

    [TestMethod]
    public Task Crossover2Ema()
        => RunStrategy<Crossover2EmaStrategy>();

    [TestMethod]
    public Task CrossoverMa()
        => RunStrategy<CrossoverMaStrategy>();

    [TestMethod]
    public Task CrunchstersNormalisedTrend()
        => RunStrategy<CrunchstersNormalisedTrendStrategy>();

    [TestMethod]
    public Task CrunchstersTurtleAndTrendSystem()
        => RunStrategy<CrunchstersTurtleAndTrendSystemStrategy>();

    [TestMethod]
    public Task CryptoAnalysis()
        => RunStrategy<CryptoAnalysisStrategy>();

    [TestMethod]
    public Task CryptoMvrvZScore()
        => RunStrategy<CryptoMvrvZScoreStrategy>();

    [TestMethod]
    public Task CryptoScalperMomentum()
        => RunStrategy<CryptoScalperMomentumStrategy>();

    [TestMethod]
    public Task CryptoScalper()
        => RunStrategy<CryptoScalperStrategy>();

    [TestMethod]
    public Task CryptoSr()
        => RunStrategy<CryptoSrStrategy>();

    [TestMethod]
    public Task CryptoSusdt10Min()
        => RunStrategy<CryptoSusdt10MinStrategy>();

    [TestMethod]
    public Task CryptoVolatilityBitcoinCorrelation()
        => RunStrategy<CryptoVolatilityBitcoinCorrelationStrategy>();

    [TestMethod]
    public Task CryptocurrencyDivergence()
        => RunStrategy<CryptocurrencyDivergenceStrategy>();

    [TestMethod]
    public Task CryptocurrencyFibonacciMas()
        => RunStrategy<CryptocurrencyFibonacciMasStrategy>();

    [TestMethod]
    public Task Cryptos()
        => RunStrategy<CryptosStrategy>();

    [TestMethod]
    public Task Cs2011()
        => RunStrategy<Cs2011Strategy>();

    [TestMethod]
    public Task Cspa143()
        => RunStrategy<Cspa143Strategy>();

    [TestMethod]
    public Task CupFinder()
        => RunStrategy<CupFinderStrategy>();

    [TestMethod]
    public Task CurrencyStrengthEa()
        => RunStrategy<CurrencyStrengthEaStrategy>();

    [TestMethod]
    public Task CurrencyStrength()
        => RunStrategy<CurrencyStrengthStrategy>();

    [TestMethod]
    public Task CurrencyStrengthV11()
        => RunStrategy<CurrencyStrengthV11Strategy>();

    [TestMethod]
    public Task CurrencyprofitsHighLowChannel()
        => RunStrategy<CurrencyprofitsHighLowChannelStrategy>();

    [TestMethod]
    public Task CustomBuyBid()
        => RunStrategy<CustomBuyBidStrategy>();

    [TestMethod]
    public Task CustomSignalOscillator()
        => RunStrategy<CustomSignalOscillatorStrategy>();

    [TestMethod]
    public Task CustomizableBtcSeasonality()
        => RunStrategy<CustomizableBtcSeasonalityStrategy>();

    [TestMethod]
    public Task CvdDivergence()
        => RunStrategy<CvdDivergenceStrategy>();

    [TestMethod]
    public Task CvdDivergenceVolumeHmaRsiMacd()
        => RunStrategy<CvdDivergenceVolumeHmaRsiMacdStrategy>();

    [TestMethod]
    public Task CyberiaTraderAdaptive()
        => RunStrategy<CyberiaTraderAdaptiveStrategy>();

    [TestMethod]
    public Task CyberiaTraderAi()
        => RunStrategy<CyberiaTraderAiStrategy>();

    [TestMethod]
    public Task CyberiaTrader()
        => RunStrategy<CyberiaTraderStrategy>();

    [TestMethod]
    public Task CycleBiologique()
        => RunStrategy<CycleBiologiqueStrategy>();

    [TestMethod]
    public Task CycleLines()
        => RunStrategy<CycleLinesStrategy>();

    [TestMethod]
    public Task CycleMarketOrder()
        => RunStrategy<CycleMarketOrderStrategy>();

    [TestMethod]
    public Task CyclopsCycleIdentifier()
        => RunStrategy<CyclopsCycleIdentifierStrategy>();

    [TestMethod]
    public Task DAlembertExposureBalancer()
        => RunStrategy<DAlembertExposureBalancerStrategy>();

    [TestMethod]
    public Task DBoTAlphaShortSmaAndRsi()
        => RunStrategy<DBoTAlphaShortSmaAndRsiStrategy>();

    [TestMethod]
    public Task DBotAlphaRsiBreakout()
        => RunStrategy<DBotAlphaRsiBreakoutStrategy>();

    [TestMethod]
    public Task DailyBollingerBand()
        => RunStrategy<DailyBollingerBandStrategy>();

    [TestMethod]
    public Task DailyBreakPoint()
        => RunStrategy<DailyBreakPointStrategy>();

    [TestMethod]
    public Task DailyBreakoutDailyShadow()
        => RunStrategy<DailyBreakoutDailyShadowStrategy>();

    [TestMethod]
    public Task DailyBreakpoint()
        => RunStrategy<DailyBreakpointStrategy>();

    [TestMethod]
    public Task DailyPerformanceAnalysis()
        => RunStrategy<DailyPerformanceAnalysisStrategy>();

    [TestMethod]
    public Task DailyPlayAceSpectrum()
        => RunStrategy<DailyPlayAceSpectrumStrategy>();

    [TestMethod]
    public Task DailyRange()
        => RunStrategy<DailyRangeStrategy>();

    [TestMethod]
    public Task DailyStpEntryFrame()
        => RunStrategy<DailyStpEntryFrameStrategy>();

    [TestMethod]
    public Task DailySupertrendEmaCrossoverRsiFilter()
        => RunStrategy<DailySupertrendEmaCrossoverRsiFilterStrategy>();

    [TestMethod]
    public Task DailyTarget()
        => RunStrategy<DailyTargetStrategy>();

    [TestMethod]
    public Task DailyTrendReversal()
        => RunStrategy<DailyTrendReversalStrategy>();

    [TestMethod]
    public Task DarkCloudPiercingCci()
        => RunStrategy<DarkCloudPiercingCciStrategy>();

    [TestMethod]
    public Task DarvasBoxesSystem()
        => RunStrategy<DarvasBoxesSystemStrategy>();

    [TestMethod]
    public Task DataChart()
        => RunStrategy<DataChartStrategy>();

    [TestMethod]
    public Task DayOpeningMacdHistogram()
        => RunStrategy<DayOpeningMacdHistogramStrategy>();

    [TestMethod]
    public Task DayTradingImpulse()
        => RunStrategy<DayTradingImpulseStrategy>();

    [TestMethod]
    public Task DayTradingIndicatorFusion()
        => RunStrategy<DayTradingIndicatorFusionStrategy>();

    [TestMethod]
    public Task DayTradingPamxa()
        => RunStrategy<DayTradingPamxaStrategy>();

    [TestMethod]
    public Task DayTrading()
        => RunStrategy<DayTradingStrategy>();

    [TestMethod]
    public Task DayTradingTrendPullback()
        => RunStrategy<DayTradingTrendPullbackStrategy>();

    [TestMethod]
    public Task DaydreamChannelBreakout()
        => RunStrategy<DaydreamChannelBreakoutStrategy>();

    [TestMethod]
    public Task Daydream()
        => RunStrategy<DaydreamStrategy>();

    [TestMethod]
    public Task DaytradingESWickLength()
        => RunStrategy<DaytradingESWickLengthStrategy>();

    [TestMethod]
    public Task Dca2()
        => RunStrategy<Dca2Strategy>();

    [TestMethod]
    public Task DcaDualTrailing()
        => RunStrategy<DcaDualTrailingStrategy>();

    [TestMethod]
    public Task DcaMeanReversionBollingerBand()
        => RunStrategy<DcaMeanReversionBollingerBandStrategy>();

    [TestMethod]
    public Task DcaSimulationForCryptoCommunity()
        => RunStrategy<DcaSimulationForCryptoCommunityStrategy>();

    [TestMethod]
    public Task Dca()
        => RunStrategy<DcaStrategy>();

    [TestMethod]
    public Task DcaSupportAndResistanceWithRsiAndTrendFilter()
        => RunStrategy<DcaSupportAndResistanceWithRsiAndTrendFilterStrategy>();

    [TestMethod]
    public Task DcaWithHedging()
        => RunStrategy<DcaWithHedgingStrategy>();

    [TestMethod]
    public Task DdCloseAll()
        => RunStrategy<DdCloseAllStrategy>();

    [TestMethod]
    public Task DeMarkLines()
        => RunStrategy<DeMarkLinesStrategy>();

    [TestMethod]
    public Task DeMarkerGainingPositionVolume2()
        => RunStrategy<DeMarkerGainingPositionVolume2Strategy>();

    [TestMethod]
    public Task DeMarkerGainingPositionVolume()
        => RunStrategy<DeMarkerGainingPositionVolumeStrategy>();

    [TestMethod]
    public Task DeMarkerPending2()
        => RunStrategy<DeMarkerPending2Strategy>();

    [TestMethod]
    public Task DeMarkerPending()
        => RunStrategy<DeMarkerPendingStrategy>();

    [TestMethod]
    public Task DeMarkerSign()
        => RunStrategy<DeMarkerSignStrategy>();

    [TestMethod]
    public Task DealersTradeMacdMql4()
        => RunStrategy<DealersTradeMacdMql4Strategy>();

    [TestMethod]
    public Task DealersTradeMacd()
        => RunStrategy<DealersTradeMacdStrategy>();

    [TestMethod]
    public Task DealersTradeV751Rivot()
        => RunStrategy<DealersTradeV751RivotStrategy>();

    [TestMethod]
    public Task DealersTradeZeroLagMacd()
        => RunStrategy<DealersTradeZeroLagMacdStrategy>();

    [TestMethod]
    public Task DebugConsole()
        => RunStrategy<DebugConsoleStrategy>();

    [TestMethod]
    public Task DecEma()
        => RunStrategy<DecEmaStrategy>();

    [TestMethod]
    public Task DeepDrawdownMa()
        => RunStrategy<DeepDrawdownMaStrategy>();

    [TestMethod]
    public Task DekidakaAshiCandlesVolume()
        => RunStrategy<DekidakaAshiCandlesVolumeStrategy>();

    [TestMethod]
    public Task DeltaMfi()
        => RunStrategy<DeltaMfiStrategy>();

    [TestMethod]
    public Task DeltaRsiOscillator()
        => RunStrategy<DeltaRsiOscillatorStrategy>();

    [TestMethod]
    public Task DeltaRsi()
        => RunStrategy<DeltaRsiStrategy>();

    [TestMethod]
    public Task DeltaSma1YearHighLow()
        => RunStrategy<DeltaSma1YearHighLowStrategy>();

    [TestMethod]
    public Task DeltaWpr()
        => RunStrategy<DeltaWprStrategy>();

    [TestMethod]
    public Task DemaRsi()
        => RunStrategy<DemaRsiStrategy>();

}
