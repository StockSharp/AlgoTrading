namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task TrailingActivateCloseAll()
        => RunStrategy<TrailingActivateCloseAllStrategy>();

    [TestMethod]
    public Task TrailingActivate()
        => RunStrategy<TrailingActivateStrategy>();

    [TestMethod]
    public Task TrailingCloseManager()
        => RunStrategy<TrailingCloseManagerStrategy>();

    [TestMethod]
    public Task TrailingMonster()
        => RunStrategy<TrailingMonsterStrategy>();

    [TestMethod]
    public Task TrailingStopActivation()
        => RunStrategy<TrailingStopActivationStrategy>();

    [TestMethod]
    public Task TrailingStopAndTake()
        => RunStrategy<TrailingStopAndTakeStrategy>();

    [TestMethod]
    public Task TrailingStopEA()
        => RunStrategy<TrailingStopEAStrategy>();

    [TestMethod]
    public Task TrailingStopFrCnSar()
        => RunStrategy<TrailingStopFrCnSarStrategy>();

    [TestMethod]
    public Task TrailingStopFrCn()
        => RunStrategy<TrailingStopFrCnStrategy>();

    [TestMethod]
    public Task TrailingStopLossAllOrders()
        => RunStrategy<TrailingStopLossAllOrdersStrategy>();

    [TestMethod]
    public Task TrailingStopManager()
        => RunStrategy<TrailingStopManagerStrategy>();

    [TestMethod]
    public Task TrailingStopStepManager()
        => RunStrategy<TrailingStopStepManagerStrategy>();

    [TestMethod]
    public Task TrailingStop()
        => RunStrategy<TrailingStopStrategy>();

    [TestMethod]
    public Task TrailingStopTriggerManager()
        => RunStrategy<TrailingStopTriggerManagerStrategy>();

    [TestMethod]
    public Task TrailingStopWhenSlUsed()
        => RunStrategy<TrailingStopWhenSlUsedStrategy>();

    [TestMethod]
    public Task TrailingStopWithRsiMomentumBased()
        => RunStrategy<TrailingStopWithRsiMomentumBasedStrategy>();

    [TestMethod]
    public Task TrailingTakeProfitCloseBased()
        => RunStrategy<TrailingTakeProfitCloseBasedStrategy>();

    [TestMethod]
    public Task TrailingTakeProfitExample()
        => RunStrategy<TrailingTakeProfitExampleStrategy>();

    [TestMethod]
    public Task TrailingTakeProfit()
        => RunStrategy<TrailingTakeProfitStrategy>();

    [TestMethod]
    public Task TrailingTpBot()
        => RunStrategy<TrailingTpBotStrategy>();

    [TestMethod]
    public Task TrainYourself()
        => RunStrategy<TrainYourselfStrategy>();

    [TestMethod]
    public Task TraxDetrendedPrice()
        => RunStrategy<TraxDetrendedPriceStrategy>();

    [TestMethod]
    public Task Trayler()
        => RunStrategy<TraylerStrategy>();

    [TestMethod]
    public Task TrendAlexcud()
        => RunStrategy<TrendAlexcudStrategy>();

    [TestMethod]
    public Task TrendArrows()
        => RunStrategy<TrendArrowsStrategy>();

    [TestMethod]
    public Task TrendCaptureLegacy()
        => RunStrategy<TrendCaptureLegacyStrategy>();

    [TestMethod]
    public Task TrendCapture()
        => RunStrategy<TrendCaptureStrategy>();

    [TestMethod]
    public Task TrendCatcherBreakout()
        => RunStrategy<TrendCatcherBreakoutStrategy>();

    [TestMethod]
    public Task TrendCatcher()
        => RunStrategy<TrendCatcherStrategy>();

    [TestMethod]
    public Task TrendCollector()
        => RunStrategy<TrendCollectorStrategy>();

    [TestMethod]
    public Task TrendConfirmation()
        => RunStrategy<TrendConfirmationStrategy>();

    [TestMethod]
    public Task TrendContinuation()
        => RunStrategy<TrendContinuationStrategy>();

    [TestMethod]
    public Task TrendDeviationBtc()
        => RunStrategy<TrendDeviationBtcStrategy>();

    [TestMethod]
    public Task TrendEnvelopes()
        => RunStrategy<TrendEnvelopesStrategy>();

    [TestMethod]
    public Task TrendFinder()
        => RunStrategy<TrendFinderStrategy>();

    [TestMethod]
    public Task TrendFollowerRainbow()
        => RunStrategy<TrendFollowerRainbowStrategy>();

    [TestMethod]
    public Task TrendFollowingAdxParabolicSar()
        => RunStrategy<TrendFollowingAdxParabolicSarStrategy>();

    [TestMethod]
    public Task TrendFollowingCandles()
        => RunStrategy<TrendFollowingCandlesStrategy>();

    [TestMethod]
    public Task TrendFollowingKnn()
        => RunStrategy<TrendFollowingKnnStrategy>();

    [TestMethod]
    public Task TrendFollowingMas3D()
        => RunStrategy<TrendFollowingMas3DStrategy>();

    [TestMethod]
    public Task TrendFollowingMm3HighLow()
        => RunStrategy<TrendFollowingMm3HighLowStrategy>();

    [TestMethod]
    public Task TrendFollowingMovingAverages()
        => RunStrategy<TrendFollowingMovingAveragesStrategy>();

    [TestMethod]
    public Task TrendFollowingParabolicBuySell()
        => RunStrategy<TrendFollowingParabolicBuySellStrategy>();

    [TestMethod]
    public Task TrendGuardFlagFinder()
        => RunStrategy<TrendGuardFlagFinderStrategy>();

    [TestMethod]
    public Task TrendGuardScalperSslHamaCandleWithConsolidationZones()
        => RunStrategy<TrendGuardScalperSslHamaCandleWithConsolidationZonesStrategy>();

    [TestMethod]
    public Task TrendImpulseTester()
        => RunStrategy<TrendImpulseTesterStrategy>();

    [TestMethod]
    public Task TrendIsYourFriend()
        => RunStrategy<TrendIsYourFriendStrategy>();

    [TestMethod]
    public Task TrendLineByAngle()
        => RunStrategy<TrendLineByAngleStrategy>();

    [TestMethod]
    public Task TrendLine()
        => RunStrategy<TrendLineStrategy>();

    [TestMethod]
    public Task TrendMagicWithEmaSmaAndAutoTrading()
        => RunStrategy<TrendMagicWithEmaSmaAndAutoTradingStrategy>();

    [TestMethod]
    public Task TrendManagerTmPlus()
        => RunStrategy<TrendManagerTmPlusStrategy>();

    [TestMethod]
    public Task TrendMasterPro23WithAlerts()
        => RunStrategy<TrendMasterPro23WithAlertsStrategy>();

    [TestMethod]
    public Task TrendMeLeaveMeChannel()
        => RunStrategy<TrendMeLeaveMeChannelStrategy>();

    [TestMethod]
    public Task TrendMeLeaveMe()
        => RunStrategy<TrendMeLeaveMeStrategy>();

    [TestMethod]
    public Task TrendRdsReversal()
        => RunStrategy<TrendRdsReversalStrategy>();

    [TestMethod]
    public Task TrendRds()
        => RunStrategy<TrendRdsStrategy>();

    [TestMethod]
    public Task TrendReversal()
        => RunStrategy<TrendReversalStrategy>();

    [TestMethod]
    public Task TrendScalper()
        => RunStrategy<TrendScalperStrategy>();

    [TestMethod]
    public Task TrendSignalsWithTpSlUAlgo()
        => RunStrategy<TrendSignalsWithTpSlUAlgoStrategy>();

    [TestMethod]
    public Task TrendSwitch()
        => RunStrategy<TrendSwitchStrategy>();

    [TestMethod]
    public Task TrendSyncProSmc()
        => RunStrategy<TrendSyncProSmcStrategy>();

    [TestMethod]
    public Task TrendTraderRemastered()
        => RunStrategy<TrendTraderRemasteredStrategy>();

    [TestMethod]
    public Task TrendTwisterV15()
        => RunStrategy<TrendTwisterV15Strategy>();

    [TestMethod]
    public Task TrendTypeIndicator()
        => RunStrategy<TrendTypeIndicatorStrategy>();

    [TestMethod]
    public Task TrendVanguard()
        => RunStrategy<TrendVanguardStrategy>();

    [TestMethod]
    public Task Trendcapture()
        => RunStrategy<TrendcaptureStrategy>();

    [TestMethod]
    public Task TrendlessAgHist()
        => RunStrategy<TrendlessAgHistStrategy>();

    [TestMethod]
    public Task TrendlineAlert()
        => RunStrategy<TrendlineAlertStrategy>();

    [TestMethod]
    public Task TrendlineBreaksWithMultiFibonacciSupertrend()
        => RunStrategy<TrendlineBreaksWithMultiFibonacciSupertrendStrategy>();

    [TestMethod]
    public Task TrendlineCrossAlert()
        => RunStrategy<TrendlineCrossAlertStrategy>();

    [TestMethod]
    public Task TriMonthlyBtcSwing()
        => RunStrategy<TriMonthlyBtcSwingStrategy>();

    [TestMethod]
    public Task TriangleBreakoutBtcMark804()
        => RunStrategy<TriangleBreakoutBtcMark804Strategy>();

    [TestMethod]
    public Task TriangleBreakoutTpSlEmaFilter()
        => RunStrategy<TriangleBreakoutTpSlEmaFilterStrategy>();

    [TestMethod]
    public Task Triangle()
        => RunStrategy<TriangleStrategy>();

    [TestMethod]
    public Task TriangularArbitrage()
        => RunStrategy<TriangularArbitrageStrategy>();

    [TestMethod]
    public Task TriangularHullMovingAverage()
        => RunStrategy<TriangularHullMovingAverageStrategy>();

    [TestMethod]
    public Task TrickerlessRhmp()
        => RunStrategy<TrickerlessRhmpStrategy>();

    [TestMethod]
    public Task TriggerLine()
        => RunStrategy<TriggerLineStrategy>();

    [TestMethod]
    public Task TrinArmsIndex()
        => RunStrategy<TrinArmsIndexStrategy>();

    [TestMethod]
    public Task TripleCciMfiConfirmed()
        => RunStrategy<TripleCciMfiConfirmedStrategy>();

    [TestMethod]
    public Task TripleEmaCrossover()
        => RunStrategy<TripleEmaCrossoverStrategy>();

    [TestMethod]
    public Task TripleEmaQqeTrendFollowing()
        => RunStrategy<TripleEmaQqeTrendFollowingStrategy>();

    [TestMethod]
    public Task TripleMaChannelCrossover()
        => RunStrategy<TripleMaChannelCrossoverStrategy>();

    [TestMethod]
    public Task TripleMaHtfDynamicSmoothing()
        => RunStrategy<TripleMaHtfDynamicSmoothingStrategy>();

    [TestMethod]
    public Task TripleRvi()
        => RunStrategy<TripleRviStrategy>();

    [TestMethod]
    public Task TripleSmaCrossover()
        => RunStrategy<TripleSmaCrossoverStrategy>();

    [TestMethod]
    public Task TripleSmaSpread()
        => RunStrategy<TripleSmaSpreadStrategy>();

    [TestMethod]
    public Task TripleSupertrend()
        => RunStrategy<TripleSupertrendStrategy>();

    [TestMethod]
    public Task TripleTopTripleBottom()
        => RunStrategy<TripleTopTripleBottomStrategy>();

    [TestMethod]
    public Task TrippleMacd()
        => RunStrategy<TrippleMacdStrategy>();

    [TestMethod]
    public Task TrixCandle()
        => RunStrategy<TrixCandleStrategy>();

    [TestMethod]
    public Task TrixCrossover()
        => RunStrategy<TrixCrossoverStrategy>();

    [TestMethod]
    public Task TrueScalperProfitLockBreakEven()
        => RunStrategy<TrueScalperProfitLockBreakEvenStrategy>();

    [TestMethod]
    public Task TrueScalperProfitLock()
        => RunStrategy<TrueScalperProfitLockStrategy>();

    [TestMethod]
    public Task TrueSort1001()
        => RunStrategy<TrueSort1001Strategy>();

    [TestMethod]
    public Task TrueSortTrend()
        => RunStrategy<TrueSortTrendStrategy>();

    [TestMethod]
    public Task TsiCloudCross()
        => RunStrategy<TsiCloudCrossStrategy>();

    [TestMethod]
    public Task TsiLongShortForBtc2H()
        => RunStrategy<TsiLongShortForBtc2HStrategy>();

    [TestMethod]
    public Task TsiMacdCrossover()
        => RunStrategy<TsiMacdCrossoverStrategy>();

    [TestMethod]
    public Task TsiSuperTrendDecision()
        => RunStrategy<TsiSuperTrendDecisionStrategy>();

}
