namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task RsiCciFusion()
        => RunStrategy<RsiCciFusionStrategy>();

    [TestMethod]
    public Task RsiCciWilliamsR()
        => RunStrategy<RsiCciWilliamsRStrategy>();

    [TestMethod]
    public Task RsiCrossoverEa()
        => RunStrategy<RsiCrossoverEaStrategy>();

    [TestMethod]
    public Task RsiCrossoverWithCompoundingMonthly()
        => RunStrategy<RsiCrossoverWithCompoundingMonthlyStrategy>();

    [TestMethod]
    public Task RsiCyclicSmoothed()
        => RunStrategy<RsiCyclicSmoothedStrategy>();

    [TestMethod]
    public Task RsiDivergence2()
        => RunStrategy<RsiDivergence2Strategy>();

    [TestMethod]
    public Task RsiDivergenceAliferCrypto()
        => RunStrategy<RsiDivergenceAliferCryptoStrategy>();

    [TestMethod]
    public Task RsiDualCloud()
        => RunStrategy<RsiDualCloudStrategy>();

    [TestMethod]
    public Task RsiEa()
        => RunStrategy<RsiEaStrategy>();

    [TestMethod]
    public Task RsiEaV2()
        => RunStrategy<RsiEaV2Strategy>();

    [TestMethod]
    public Task RsiEraser()
        => RunStrategy<RsiEraserStrategy>();

    [TestMethod]
    public Task RsiExpertBreakout()
        => RunStrategy<RsiExpertBreakoutStrategy>();

    [TestMethod]
    public Task RsiExpert()
        => RunStrategy<RsiExpertStrategy>();

    [TestMethod]
    public Task RsiExpertTrendFilter()
        => RunStrategy<RsiExpertTrendFilterStrategy>();

    [TestMethod]
    public Task RsiHistogram()
        => RunStrategy<RsiHistogramStrategy>();

    [TestMethod]
    public Task RsiLevels()
        => RunStrategy<RsiLevelsStrategy>();

    [TestMethod]
    public Task RsiLongOnlyWithConfirmedCrossbacks()
        => RunStrategy<RsiLongOnlyWithConfirmedCrossbacksStrategy>();

    [TestMethod]
    public Task RsiLongPositionDax2HoursDowJones1Hour()
        => RunStrategy<RsiLongPositionDax2HoursDowJones1HourStrategy>();

    [TestMethod]
    public Task RsiLongTerm15min()
        => RunStrategy<RsiLongTerm15minStrategy>();

    [TestMethod]
    public Task RsiMaOnRsiDual()
        => RunStrategy<RsiMaOnRsiDualStrategy>();

    [TestMethod]
    public Task RsiMaOnRsiFillingStep()
        => RunStrategy<RsiMaOnRsiFillingStepStrategy>();

    [TestMethod]
    public Task RsiMa()
        => RunStrategy<RsiMaStrategy>();

    [TestMethod]
    public Task RsiMaTrend()
        => RunStrategy<RsiMaTrendStrategy>();

    [TestMethod]
    public Task RsiMacdLongOnly()
        => RunStrategy<RsiMacdLongOnlyStrategy>();

    [TestMethod]
    public Task RsiProPlusBearMarket()
        => RunStrategy<RsiProPlusBearMarketStrategy>();

    [TestMethod]
    public Task RsiRftl()
        => RunStrategy<RsiRftlStrategy>();

    [TestMethod]
    public Task RsiSign()
        => RunStrategy<RsiSignStrategy>();

    [TestMethod]
    public Task RsiSlowdown()
        => RunStrategy<RsiSlowdownStrategy>();

    [TestMethod]
    public Task RsiStochasticMa()
        => RunStrategy<RsiStochasticMaStrategy>();

    [TestMethod]
    public Task RsiStochasticWma()
        => RunStrategy<RsiStochasticWmaStrategy>();

    [TestMethod]
    public Task Rsi()
        => RunStrategy<RsiStrategy>();

    [TestMethod]
    public Task RsiSwingRadar()
        => RunStrategy<RsiSwingRadarStrategy>();

    [TestMethod]
    public Task RsiTest()
        => RunStrategy<RsiTestStrategy>();

    [TestMethod]
    public Task RsiThreshold()
        => RunStrategy<RsiThresholdStrategy>();

    [TestMethod]
    public Task RsiTraderAlignedAverages()
        => RunStrategy<RsiTraderAlignedAveragesStrategy>();

    [TestMethod]
    public Task RsiTrader()
        => RunStrategy<RsiTraderStrategy>();

    [TestMethod]
    public Task RsiTraderV1()
        => RunStrategy<RsiTraderV1Strategy>();

    [TestMethod]
    public Task RsiTrendFollowing()
        => RunStrategy<RsiTrendFollowingStrategy>();

    [TestMethod]
    public Task RsiTrend()
        => RunStrategy<RsiTrendStrategy>();

    [TestMethod]
    public Task RsiValue()
        => RunStrategy<RsiValueStrategy>();

    [TestMethod]
    public Task RsiVolumeMacdEmaCombo()
        => RunStrategy<RsiVolumeMacdEmaComboStrategy>();

    [TestMethod]
    public Task RsiWithAdjustableRsiAndStopLoss()
        => RunStrategy<RsiWithAdjustableRsiAndStopLossStrategy>();

    [TestMethod]
    public Task RsiWithManualTpAndSl()
        => RunStrategy<RsiWithManualTpAndSlStrategy>();

    [TestMethod]
    public Task RsiWithTpSlLowerTf()
        => RunStrategy<RsiWithTpSlLowerTfStrategy>();

    [TestMethod]
    public Task RtbMomentumBreakout()
        => RunStrategy<RtbMomentumBreakoutStrategy>();

    [TestMethod]
    public Task RubberBandsGrid()
        => RunStrategy<RubberBandsGridStrategy>();

    [TestMethod]
    public Task Rubberbands3()
        => RunStrategy<Rubberbands3Strategy>();

    [TestMethod]
    public Task RubberbandsSafetyNet()
        => RunStrategy<RubberbandsSafetyNetStrategy>();

    [TestMethod]
    public Task Russian20MomentumMa()
        => RunStrategy<Russian20MomentumMaStrategy>();

    [TestMethod]
    public Task Russian20TimeFilterMomentum()
        => RunStrategy<Russian20TimeFilterMomentumStrategy>();

    [TestMethod]
    public Task RviCrossover()
        => RunStrategy<RviCrossoverStrategy>();

    [TestMethod]
    public Task RviDiffReversal()
        => RunStrategy<RviDiffReversalStrategy>();

    [TestMethod]
    public Task RviHistogramReversal()
        => RunStrategy<RviHistogramReversalStrategy>();

    [TestMethod]
    public Task S4IBSMeanRev3candleExit()
        => RunStrategy<S4IBSMeanRev3candleExitStrategy>();

    [TestMethod]
    public Task S7UpBot()
        => RunStrategy<S7UpBotStrategy>();

    [TestMethod]
    public Task SMADirectionalMatrixLuxAlgo()
        => RunStrategy<SMADirectionalMatrixLuxAlgoStrategy>();

    [TestMethod]
    public Task SP100OptionExpirationWeek()
        => RunStrategy<SP100OptionExpirationWeekStrategy>();

    [TestMethod]
    public Task SafaBotAlert()
        => RunStrategy<SafaBotAlertStrategy>();

    [TestMethod]
    public Task SailSystemEa()
        => RunStrategy<SailSystemEaStrategy>();

    [TestMethod]
    public Task SampleCheckPendingOrder()
        => RunStrategy<SampleCheckPendingOrderStrategy>();

    [TestMethod]
    public Task SampleDetectEconomicCalendar()
        => RunStrategy<SampleDetectEconomicCalendarStrategy>();

    [TestMethod]
    public Task SampleTrailingStop()
        => RunStrategy<SampleTrailingStopStrategy>();

    [TestMethod]
    public Task SampleTrailingstopMt5()
        => RunStrategy<SampleTrailingstopMt5Strategy>();

    [TestMethod]
    public Task SarAutomated()
        => RunStrategy<SarAutomatedStrategy>();

    [TestMethod]
    public Task SarRsiMts()
        => RunStrategy<SarRsiMtsStrategy>();

    [TestMethod]
    public Task SarTradingV20()
        => RunStrategy<SarTradingV20Strategy>();

    [TestMethod]
    public Task SarTrailingSystem()
        => RunStrategy<SarTrailingSystemStrategy>();

    [TestMethod]
    public Task SawSystem1()
        => RunStrategy<SawSystem1Strategy>();

    [TestMethod]
    public Task ScaleInScaleOut()
        => RunStrategy<ScaleInScaleOutStrategy>();

    [TestMethod]
    public Task ScalpRsi()
        => RunStrategy<ScalpRsiStrategy>();

    [TestMethod]
    public Task ScalpWiz9001()
        => RunStrategy<ScalpWiz9001Strategy>();

    [TestMethod]
    public Task ScalpWizBollinger()
        => RunStrategy<ScalpWizBollingerStrategy>();

    [TestMethod]
    public Task ScalpelEa()
        => RunStrategy<ScalpelEaStrategy>();

    [TestMethod]
    public Task Scalpel()
        => RunStrategy<ScalpelStrategy>();

    [TestMethod]
    public Task Scalper555()
        => RunStrategy<Scalper555Strategy>();

    [TestMethod]
    public Task ScalperEmaSimple()
        => RunStrategy<ScalperEmaSimpleStrategy>();

    [TestMethod]
    public Task Scalping15minEmaMacdRsiAtr()
        => RunStrategy<Scalping15minEmaMacdRsiAtrStrategy>();

    [TestMethod]
    public Task ScalpingAssistant()
        => RunStrategy<ScalpingAssistantStrategy>();

    [TestMethod]
    public Task ScalpingByTradingConToto()
        => RunStrategy<ScalpingByTradingConTotoStrategy>();

    [TestMethod]
    public Task ScalpingEA()
        => RunStrategy<ScalpingEAStrategy>();

    [TestMethod]
    public Task ScalpingEmaRsiMacd()
        => RunStrategy<ScalpingEmaRsiMacdStrategy>();

    [TestMethod]
    public Task ScalpingWithWilliamsRMacdAndSma()
        => RunStrategy<ScalpingWithWilliamsRMacdAndSmaStrategy>();

    [TestMethod]
    public Task ScheduledTimeTrader()
        => RunStrategy<ScheduledTimeTraderStrategy>();

    [TestMethod]
    public Task ScreenerMeanReversionChannel()
        => RunStrategy<ScreenerMeanReversionChannelStrategy>();

    [TestMethod]
    public Task SeaDragon2()
        => RunStrategy<SeaDragon2Strategy>();

    [TestMethod]
    public Task SecondEasiest()
        => RunStrategy<SecondEasiestStrategy>();

    [TestMethod]
    public Task SecurityFreeMtfExample()
        => RunStrategy<SecurityFreeMtfExampleStrategy>();

    [TestMethod]
    public Task SecurityRevisited()
        => RunStrategy<SecurityRevisitedStrategy>();

    [TestMethod]
    public Task SecwentaMultiBarSignals()
        => RunStrategy<SecwentaMultiBarSignalsStrategy>();

    [TestMethod]
    public Task SelfLearningExperts()
        => RunStrategy<SelfLearningExpertsStrategy>();

    [TestMethod]
    public Task SelfOptimizingRsiOrMfiTraderV3()
        => RunStrategy<SelfOptimizingRsiOrMfiTraderV3Strategy>();

    [TestMethod]
    public Task SemaSdiWebhook()
        => RunStrategy<SemaSdiWebhookStrategy>();

    [TestMethod]
    public Task SemilongWwwForexInstrumentsInfo()
        => RunStrategy<SemilongWwwForexInstrumentsInfoStrategy>();

    [TestMethod]
    public Task SendCloseOrder()
        => RunStrategy<SendCloseOrderStrategy>();

    [TestMethod]
    public Task SendClose()
        => RunStrategy<SendCloseStrategy>();

    [TestMethod]
    public Task SensitiveMacdTrailing()
        => RunStrategy<SensitiveMacdTrailingStrategy>();

    [TestMethod]
    public Task SeparateTrade()
        => RunStrategy<SeparateTradeStrategy>();

    [TestMethod]
    public Task SeparatedMovingAverage()
        => RunStrategy<SeparatedMovingAverageStrategy>();

    [TestMethod]
    public Task SerialMASwing()
        => RunStrategy<SerialMASwingStrategy>();

    [TestMethod]
    public Task SessionBreakoutScalperTradingBot()
        => RunStrategy<SessionBreakoutScalperTradingBotStrategy>();

}
