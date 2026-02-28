namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task ObvAtr()
        => RunStrategy<ObvAtrStrategy>();

    [TestMethod]
    public Task ObvTrafficLights()
        => RunStrategy<ObvTrafficLightsStrategy>();

    [TestMethod]
    public Task ObviousMa()
        => RunStrategy<ObviousMaStrategy>();

    [TestMethod]
    public Task OcoOrder()
        => RunStrategy<OcoOrderStrategy>();

    [TestMethod]
    public Task OcoPendingOrders()
        => RunStrategy<OcoPendingOrdersStrategy>();

    [TestMethod]
    public Task OctopusNest()
        => RunStrategy<OctopusNestStrategy>();

    [TestMethod]
    public Task OhlcCheck()
        => RunStrategy<OhlcCheckStrategy>();

    [TestMethod]
    public Task OhlcStochastic()
        => RunStrategy<OhlcStochasticStrategy>();

    [TestMethod]
    public Task OkxMaCrossover()
        => RunStrategy<OkxMaCrossoverStrategy>();

    [TestMethod]
    public Task OmegaGalsky()
        => RunStrategy<OmegaGalskyStrategy>();

    [TestMethod]
    public Task OmniTrend()
        => RunStrategy<OmniTrendStrategy>();

    [TestMethod]
    public Task OmzdwwiPendingManager()
        => RunStrategy<OmzdwwiPendingManagerStrategy>();

    [TestMethod]
    public Task OnTickMarketWatch()
        => RunStrategy<OnTickMarketWatchStrategy>();

    [TestMethod]
    public Task OneClickCloseAll()
        => RunStrategy<OneClickCloseAllStrategy>();

    [TestMethod]
    public Task OneHBollingerBands()
        => RunStrategy<OneHBollingerBandsStrategy>();

    [TestMethod]
    public Task OneHourEurUsd()
        => RunStrategy<OneHourEurUsdStrategy>();

    [TestMethod]
    public Task OneHrStocTrader()
        => RunStrategy<OneHrStocTraderStrategy>();

    [TestMethod]
    public Task OneMaChannelBreakout()
        => RunStrategy<OneMaChannelBreakoutStrategy>();

    [TestMethod]
    public Task OneMinuteScalper()
        => RunStrategy<OneMinuteScalperStrategy>();

    [TestMethod]
    public Task OnePriceSlTp()
        => RunStrategy<OnePriceSlTpStrategy>();

    [TestMethod]
    public Task OneTwoThreePattern()
        => RunStrategy<OneTwoThreePatternStrategy>();

    [TestMethod]
    public Task OneTwoThreeReversal()
        => RunStrategy<OneTwoThreeReversalStrategy>();

    [TestMethod]
    public Task OneTwoThree()
        => RunStrategy<OneTwoThreeStrategy>();

    [TestMethod]
    public Task OpenClose23090()
        => RunStrategy<OpenClose23090Strategy>();

    [TestMethod]
    public Task OpenClose2AmpnStochastic()
        => RunStrategy<OpenClose2AmpnStochasticStrategy>();

    [TestMethod]
    public Task OpenClose()
        => RunStrategy<OpenCloseStrategy>();

    [TestMethod]
    public Task OpenOscillatorCloudMmrec()
        => RunStrategy<OpenOscillatorCloudMmrecStrategy>();

    [TestMethod]
    public Task OpenPendingorderAfterPositionGetStopLoss()
        => RunStrategy<OpenPendingorderAfterPositionGetStopLossStrategy>();

    [TestMethod]
    public Task OpenTiks()
        => RunStrategy<OpenTiksStrategy>();

    [TestMethod]
    public Task OpenTimeDailyWindow()
        => RunStrategy<OpenTimeDailyWindowStrategy>();

    [TestMethod]
    public Task OpenTime()
        => RunStrategy<OpenTimeStrategy>();

    [TestMethod]
    public Task OpenTimeTwo()
        => RunStrategy<OpenTimeTwoStrategy>();

    [TestMethod]
    public Task OpenTwoPendingOrders()
        => RunStrategy<OpenTwoPendingOrdersStrategy>();

    [TestMethod]
    public Task OpeningAndClosingOnTimeV2()
        => RunStrategy<OpeningAndClosingOnTimeV2Strategy>();

    [TestMethod]
    public Task OpeningClosingOnTime()
        => RunStrategy<OpeningClosingOnTimeStrategy>();

    [TestMethod]
    public Task OpeningRangeBreakout2()
        => RunStrategy<OpeningRangeBreakout2Strategy>();

    [TestMethod]
    public Task OpeningRangeBreakout()
        => RunStrategy<OpeningRangeBreakoutStrategy>();

    [TestMethod]
    public Task OptimizedAutoDetect()
        => RunStrategy<OptimizedAutoDetectStrategy>();

    [TestMethod]
    public Task OptimizedGridWithKnn()
        => RunStrategy<OptimizedGridWithKnnStrategy>();

    [TestMethod]
    public Task OptimizedHeikinAshiBuySell()
        => RunStrategy<OptimizedHeikinAshiBuySellStrategy>();

    [TestMethod]
    public Task OptionsV13()
        => RunStrategy<OptionsV13Strategy>();

    [TestMethod]
    public Task OptionsV20()
        => RunStrategy<OptionsV20Strategy>();

    [TestMethod]
    public Task Orb15mFirst15minBreakout()
        => RunStrategy<Orb15mFirst15minBreakoutStrategy>();

    [TestMethod]
    public Task OrbHeikinAshiSpyCorrelation()
        => RunStrategy<OrbHeikinAshiSpyCorrelationStrategy>();

    [TestMethod]
    public Task OrbVwapBraidFilter()
        => RunStrategy<OrbVwapBraidFilterStrategy>();

    [TestMethod]
    public Task OrderBlockFinder()
        => RunStrategy<OrderBlockFinderStrategy>();

    [TestMethod]
    public Task OrderEscort()
        => RunStrategy<OrderEscortStrategy>();

    [TestMethod]
    public Task OrderExample()
        => RunStrategy<OrderExampleStrategy>();

    [TestMethod]
    public Task OrderExpert()
        => RunStrategy<OrderExpertStrategy>();

    [TestMethod]
    public Task OrderNotify()
        => RunStrategy<OrderNotifyStrategy>();

    [TestMethod]
    public Task OrderStabilization()
        => RunStrategy<OrderStabilizationStrategy>();

    [TestMethod]
    public Task OsHma()
        => RunStrategy<OsHmaStrategy>();

    [TestMethod]
    public Task OsMaFourColorsArrow()
        => RunStrategy<OsMaFourColorsArrowStrategy>();

    [TestMethod]
    public Task OsMaMaster()
        => RunStrategy<OsMaMasterStrategy>();

    [TestMethod]
    public Task OsMaSterV0()
        => RunStrategy<OsMaSterV0Strategy>();

    [TestMethod]
    public Task OscillatorEvaluator()
        => RunStrategy<OscillatorEvaluatorStrategy>();

    [TestMethod]
    public Task OsfCountertrend()
        => RunStrategy<OsfCountertrendStrategy>();

    [TestMethod]
    public Task OtkatSys()
        => RunStrategy<OtkatSysStrategy>();

    [TestMethod]
    public Task OutOfTheNoiseIntradayWithVwap()
        => RunStrategy<OutOfTheNoiseIntradayWithVwapStrategy>();

    [TestMethod]
    public Task OutlierDetectorWithNSigmaConfidenceIntervals()
        => RunStrategy<OutlierDetectorWithNSigmaConfidenceIntervalsStrategy>();

    [TestMethod]
    public Task OutsideBar()
        => RunStrategy<OutsideBarStrategy>();

    [TestMethod]
    public Task OverHedgeV2Grid()
        => RunStrategy<OverHedgeV2GridStrategy>();

    [TestMethod]
    public Task OverHedgeV2()
        => RunStrategy<OverHedgeV2Strategy>();

    [TestMethod]
    public Task OvernightEffectHighVolatilityCrypto()
        => RunStrategy<OvernightEffectHighVolatilityCryptoStrategy>();

    [TestMethod]
    public Task OvernightPositioningEma()
        => RunStrategy<OvernightPositioningEmaStrategy>();

    [TestMethod]
    public Task OzFxAcceleratorStochastic()
        => RunStrategy<OzFxAcceleratorStochasticStrategy>();

    [TestMethod]
    public Task OzFxSimple()
        => RunStrategy<OzFxSimpleStrategy>();

    [TestMethod]
    public Task Ozymandias()
        => RunStrategy<OzymandiasStrategy>();

    [TestMethod]
    public Task PChannelSystem()
        => RunStrategy<PChannelSystemStrategy>();

    [TestMethod]
    public Task PROphet()
        => RunStrategy<PROphetStrategy>();

    [TestMethod]
    public Task PSJanuaryBarometerBacktester()
        => RunStrategy<PSJanuaryBarometerBacktesterStrategy>();

    [TestMethod]
    public Task PSquareNthPercentile()
        => RunStrategy<PSquareNthPercentileStrategy>();

    [TestMethod]
    public Task PaOscillator()
        => RunStrategy<PaOscillatorStrategy>();

    [TestMethod]
    public Task Painel()
        => RunStrategy<PainelStrategy>();

    [TestMethod]
    public Task Pairs()
        => RunStrategy<PairsStrategy>();

    [TestMethod]
    public Task PanelJoke()
        => RunStrategy<PanelJokeStrategy>();

    [TestMethod]
    public Task ParaRetrace()
        => RunStrategy<ParaRetraceStrategy>();

    [TestMethod]
    public Task ParabolicRsi()
        => RunStrategy<ParabolicRsiStrategy>();

    [TestMethod]
    public Task ParabolicSarAlert()
        => RunStrategy<ParabolicSarAlertStrategy>();

    [TestMethod]
    public Task ParabolicSarBug2()
        => RunStrategy<ParabolicSarBug2Strategy>();

    [TestMethod]
    public Task ParabolicSarBug3()
        => RunStrategy<ParabolicSarBug3Strategy>();

    [TestMethod]
    public Task ParabolicSarBug5()
        => RunStrategy<ParabolicSarBug5Strategy>();

    [TestMethod]
    public Task ParabolicSarBug()
        => RunStrategy<ParabolicSarBugStrategy>();

    [TestMethod]
    public Task ParabolicSarCross()
        => RunStrategy<ParabolicSarCrossStrategy>();

    [TestMethod]
    public Task ParabolicSarCrossoverAlert()
        => RunStrategy<ParabolicSarCrossoverAlertStrategy>();

    [TestMethod]
    public Task ParabolicSarEa()
        => RunStrategy<ParabolicSarEaStrategy>();

    [TestMethod]
    public Task ParabolicSarEarlyBuyMaBasedExit()
        => RunStrategy<ParabolicSarEarlyBuyMaBasedExitStrategy>();

    [TestMethod]
    public Task ParabolicSarEarlyBuyMaExit()
        => RunStrategy<ParabolicSarEarlyBuyMaExitStrategy>();

    [TestMethod]
    public Task ParabolicSarFiboLimits()
        => RunStrategy<ParabolicSarFiboLimitsStrategy>();

    [TestMethod]
    public Task ParabolicSarFirstDot()
        => RunStrategy<ParabolicSarFirstDotStrategy>();

    [TestMethod]
    public Task ParabolicSarFlipAlert()
        => RunStrategy<ParabolicSarFlipAlertStrategy>();

    [TestMethod]
    public Task ParabolicSarLimit()
        => RunStrategy<ParabolicSarLimitStrategy>();

    [TestMethod]
    public Task ParabolicSarMacdTrendZone()
        => RunStrategy<ParabolicSarMacdTrendZoneStrategy>();

    [TestMethod]
    public Task ParabolicSarMultiTimeframe()
        => RunStrategy<ParabolicSarMultiTimeframeStrategy>();

    [TestMethod]
    public Task ParabolicTrailingStop()
        => RunStrategy<ParabolicTrailingStopStrategy>();

    [TestMethod]
    public Task ParallaxSell()
        => RunStrategy<ParallaxSellStrategy>();

    [TestMethod]
    public Task ParallelStrategies()
        => RunStrategy<ParallelStrategiesStrategy>();

    [TestMethod]
    public Task ParentSessionSweepsAlert()
        => RunStrategy<ParentSessionSweepsAlertStrategy>();

    [TestMethod]
    public Task PatternTemplate()
        => RunStrategy<PatternTemplateStrategy>();

    [TestMethod]
    public Task PatternsEa()
        => RunStrategy<PatternsEaStrategy>();

}
