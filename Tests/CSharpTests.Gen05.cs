namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task BtcChopReversal()
        => RunStrategy<BtcChopReversalStrategy>();

    [TestMethod]
    public Task BtcDcaAhr999()
        => RunStrategy<BtcDcaAhr999Strategy>();

    [TestMethod]
    public Task BtcDifficultyAdjustments()
        => RunStrategy<BtcDifficultyAdjustmentsStrategy>();

    [TestMethod]
    public Task BtcFutureGammaWeightedMomentumModel()
        => RunStrategy<BtcFutureGammaWeightedMomentumModelStrategy>();

    [TestMethod]
    public Task BtcOutperform()
        => RunStrategy<BtcOutperformStrategy>();

    [TestMethod]
    public Task BtcTradingRobot()
        => RunStrategy<BtcTradingRobotStrategy>();

    [TestMethod]
    public Task BtcusdAdjustableSltp()
        => RunStrategy<BtcusdAdjustableSltpStrategy>();

    [TestMethod]
    public Task BtcusdMomentumAfterAbnormalDays()
        => RunStrategy<BtcusdMomentumAfterAbnormalDaysStrategy>();

    [TestMethod]
    public Task Btfd()
        => RunStrategy<BtfdStrategy>();

    [TestMethod]
    public Task BuildYourGrid()
        => RunStrategy<BuildYourGridStrategy>();

    [TestMethod]
    public Task BuilderWithIndicators()
        => RunStrategy<BuilderWithIndicatorsStrategy>();

    [TestMethod]
    public Task BuiltInKellyRatio()
        => RunStrategy<BuiltInKellyRatioStrategy>();

    [TestMethod]
    public Task BullBearCandleMartingale()
        => RunStrategy<BullBearCandleMartingaleStrategy>();

    [TestMethod]
    public Task BullBearVolumePercentileTp()
        => RunStrategy<BullBearVolumePercentileTpStrategy>();

    [TestMethod]
    public Task BullRowBreakout()
        => RunStrategy<BullRowBreakoutStrategy>();

    [TestMethod]
    public Task BullVsMedved()
        => RunStrategy<BullVsMedvedStrategy>();

    [TestMethod]
    public Task BullVsMedvedWindow()
        => RunStrategy<BullVsMedvedWindowStrategy>();

    [TestMethod]
    public Task BullishBearishEngulfing()
        => RunStrategy<BullishBearishEngulfingStrategy>();

    [TestMethod]
    public Task BullishBearishHaramiStochastic()
        => RunStrategy<BullishBearishHaramiStochasticStrategy>();

    [TestMethod]
    public Task BullishBsRsiDivergence()
        => RunStrategy<BullishBsRsiDivergenceStrategy>();

    [TestMethod]
    public Task BullishDivergenceShortTermLongTradeFinder()
        => RunStrategy<BullishDivergenceShortTermLongTradeFinderStrategy>();

    [TestMethod]
    public Task BullishReversalBar()
        => RunStrategy<BullishReversalBarStrategy>();

    [TestMethod]
    public Task BullishReversal()
        => RunStrategy<BullishReversalStrategy>();

    [TestMethod]
    public Task BullsBearsEyesEa()
        => RunStrategy<BullsBearsEyesEaStrategy>();

    [TestMethod]
    public Task BullsBearsEyes()
        => RunStrategy<BullsBearsEyesStrategy>();

    [TestMethod]
    public Task BullsBearsPowerAverage()
        => RunStrategy<BullsBearsPowerAverageStrategy>();

    [TestMethod]
    public Task BullsBearsPowerCross()
        => RunStrategy<BullsBearsPowerCrossStrategy>();

    [TestMethod]
    public Task BullsVsBearsCrossover()
        => RunStrategy<BullsVsBearsCrossoverStrategy>();

    [TestMethod]
    public Task BurgExtrapolatorForecast()
        => RunStrategy<BurgExtrapolatorForecastStrategy>();

    [TestMethod]
    public Task BurgExtrapolator()
        => RunStrategy<BurgExtrapolatorStrategy>();

    [TestMethod]
    public Task ButterflyPattern()
        => RunStrategy<ButterflyPatternStrategy>();

    [TestMethod]
    public Task ButtonCloseBuySell()
        => RunStrategy<ButtonCloseBuySellStrategy>();

    [TestMethod]
    public Task BuyAndHold()
        => RunStrategy<BuyAndHoldStrategy>();

    [TestMethod]
    public Task BuyDipMultiplePositions()
        => RunStrategy<BuyDipMultiplePositionsStrategy>();

    [TestMethod]
    public Task BuyOn5DayLow()
        => RunStrategy<BuyOn5DayLowStrategy>();

    [TestMethod]
    public Task BuyOnlyEmaBb()
        => RunStrategy<BuyOnlyEmaBbStrategy>();

    [TestMethod]
    public Task BuySellBullishEngulfing()
        => RunStrategy<BuySellBullishEngulfingStrategy>();

    [TestMethod]
    public Task BuySellGrid()
        => RunStrategy<BuySellGridStrategy>();

    [TestMethod]
    public Task BuySellOnYourPrice()
        => RunStrategy<BuySellOnYourPriceStrategy>();

    [TestMethod]
    public Task BuySellRenkoBased()
        => RunStrategy<BuySellRenkoBasedStrategy>();

    [TestMethod]
    public Task BuySellStopButtons()
        => RunStrategy<BuySellStopButtonsStrategy>();

    [TestMethod]
    public Task BuySell()
        => RunStrategy<BuySellStrategy>();

    [TestMethod]
    public Task BuyTheDipsTrend()
        => RunStrategy<BuyTheDipsTrendStrategy>();

    [TestMethod]
    public Task BuyingSellingVolume()
        => RunStrategy<BuyingSellingVolumeStrategy>();

    [TestMethod]
    public Task BwWiseman1()
        => RunStrategy<BwWiseman1Strategy>();

    [TestMethod]
    public Task BykovTrendColorX2MaMmRec()
        => RunStrategy<BykovTrendColorX2MaMmRecStrategy>();

    [TestMethod]
    public Task BykovTrendColorX2Ma()
        => RunStrategy<BykovTrendColorX2MaStrategy>();

    [TestMethod]
    public Task BykovTrendReOpen()
        => RunStrategy<BykovTrendReOpenStrategy>();

    [TestMethod]
    public Task BykovTrend()
        => RunStrategy<BykovTrendStrategy>();

    [TestMethod]
    public Task CCIAndMartin()
        => RunStrategy<CCIAndMartinStrategy>();

    [TestMethod]
    public Task CCTrend2DowntrendShort()
        => RunStrategy<CCTrend2DowntrendShortStrategy>();

    [TestMethod]
    public Task CE_XAU_USDT()
        => RunStrategy<CE_XAU_USDTStrategy>();

    [TestMethod]
    public Task CFactorHlh4BuyOnly()
        => RunStrategy<CFactorHlh4BuyOnlyStrategy>();

    [TestMethod]
    public Task CGOscillatorX2()
        => RunStrategy<CGOscillatorX2Strategy>();

    [TestMethod]
    public Task CaiChannelSystemDigit()
        => RunStrategy<CaiChannelSystemDigitStrategy>();

    [TestMethod]
    public Task CaiStandardDeviation()
        => RunStrategy<CaiStandardDeviationStrategy>();

    [TestMethod]
    public Task CalcProfitLossOnLinePrice()
        => RunStrategy<CalcProfitLossOnLinePriceStrategy>();

    [TestMethod]
    public Task CalculationPositionSizeBasedOnRisk()
        => RunStrategy<CalculationPositionSizeBasedOnRiskStrategy>();

    [TestMethod]
    public Task CandelsHighOpen()
        => RunStrategy<CandelsHighOpenStrategy>();

    [TestMethod]
    public Task Candle245Breakout()
        => RunStrategy<Candle245BreakoutStrategy>();

    [TestMethod]
    public Task CandleBodyShapes()
        => RunStrategy<CandleBodyShapesStrategy>();

    [TestMethod]
    public Task CandlePatternsTest()
        => RunStrategy<CandlePatternsTestStrategy>();

    [TestMethod]
    public Task CandleShadowPercent()
        => RunStrategy<CandleShadowPercentStrategy>();

    [TestMethod]
    public Task CandleShadowsV1()
        => RunStrategy<CandleShadowsV1Strategy>();

    [TestMethod]
    public Task CandleStopSystemTmPlus()
        => RunStrategy<CandleStopSystemTmPlusStrategy>();

    [TestMethod]
    public Task CandleStopTrailing()
        => RunStrategy<CandleStopTrailingStrategy>();

    [TestMethod]
    public Task Candle()
        => RunStrategy<CandleStrategy>();

    [TestMethod]
    public Task CandleTrader()
        => RunStrategy<CandleTraderStrategy>();

    [TestMethod]
    public Task CandleTrailingStop()
        => RunStrategy<CandleTrailingStopStrategy>();

    [TestMethod]
    public Task CandleTrend()
        => RunStrategy<CandleTrendStrategy>();

    [TestMethod]
    public Task CandlesSmoothed()
        => RunStrategy<CandlesSmoothedStrategy>();

    [TestMethod]
    public Task CandlestickStochastic()
        => RunStrategy<CandlestickStochasticStrategy>();

    [TestMethod]
    public Task CandlesticksBw()
        => RunStrategy<CandlesticksBwStrategy>();

    [TestMethod]
    public Task CanxMaCrossover()
        => RunStrategy<CanxMaCrossoverStrategy>();

    [TestMethod]
    public Task CaptainBacktestModel()
        => RunStrategy<CaptainBacktestModelStrategy>();

    [TestMethod]
    public Task CarbophosGrid()
        => RunStrategy<CarbophosGridStrategy>();

    [TestMethod]
    public Task CashMachine5minLegacy()
        => RunStrategy<CashMachine5minLegacyStrategy>();

    [TestMethod]
    public Task CashMachine5min()
        => RunStrategy<CashMachine5minStrategy>();

    [TestMethod]
    public Task Casino111()
        => RunStrategy<Casino111Strategy>();

    [TestMethod]
    public Task CaudateXPeriodCandleTmPlus()
        => RunStrategy<CaudateXPeriodCandleTmPlusStrategy>();

    [TestMethod]
    public Task CbcWithTrendConfirmationAndSeparateStopLoss()
        => RunStrategy<CbcWithTrendConfirmationAndSeparateStopLossStrategy>();

    [TestMethod]
    public Task CbcWsRsi()
        => RunStrategy<CbcWsRsiStrategy>();

    [TestMethod]
    public Task CcfpCurrencyStrength()
        => RunStrategy<CcfpCurrencyStrengthStrategy>();

    [TestMethod]
    public Task CciAutomated()
        => RunStrategy<CciAutomatedStrategy>();

    [TestMethod]
    public Task CciComa()
        => RunStrategy<CciComaStrategy>();

    [TestMethod]
    public Task CciEmaAtrTpSl()
        => RunStrategy<CciEmaAtrTpSlStrategy>();

    [TestMethod]
    public Task CciExpert()
        => RunStrategy<CciExpertStrategy>();

    [TestMethod]
    public Task CciHistogram()
        => RunStrategy<CciHistogramStrategy>();

    [TestMethod]
    public Task CciMaV15()
        => RunStrategy<CciMaV15Strategy>();

    [TestMethod]
    public Task CciMacdScalper()
        => RunStrategy<CciMacdScalperStrategy>();

    [TestMethod]
    public Task CciMacd()
        => RunStrategy<CciMacdStrategy>();

    [TestMethod]
    public Task CciNormalizedReversal()
        => RunStrategy<CciNormalizedReversalStrategy>();

    [TestMethod]
    public Task CciSupportResistance()
        => RunStrategy<CciSupportResistanceStrategy>();

    [TestMethod]
    public Task CciThreshold()
        => RunStrategy<CciThresholdStrategy>();

    [TestMethod]
    public Task CciWoodies()
        => RunStrategy<CciWoodiesStrategy>();

    [TestMethod]
    public Task Ccit3ZeroCross()
        => RunStrategy<Ccit3ZeroCrossStrategy>();

    [TestMethod]
    public Task CdcPlMfi()
        => RunStrategy<CdcPlMfiStrategy>();

    [TestMethod]
    public Task CdcPlRsi()
        => RunStrategy<CdcPlRsiStrategy>();

    [TestMethod]
    public Task CeZlsma5MinCandlechart()
        => RunStrategy<CeZlsma5MinCandlechartStrategy>();

    [TestMethod]
    public Task CenterOfGravityCandle()
        => RunStrategy<CenterOfGravityCandleStrategy>();

}
