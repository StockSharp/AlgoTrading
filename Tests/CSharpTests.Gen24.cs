namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task PauseTradingOnConsecutiveLoss()
        => RunStrategy<PauseTradingOnConsecutiveLossStrategy>();

    [TestMethod]
    public Task PavanCpr()
        => RunStrategy<PavanCprStrategy>();

    [TestMethod]
    public Task PaydayAnomaly2()
        => RunStrategy<PaydayAnomaly2Strategy>();

    [TestMethod]
    public Task Pead()
        => RunStrategy<PeadStrategy>();

    [TestMethod]
    public Task PearsonsROscillator()
        => RunStrategy<PearsonsROscillatorStrategy>();

    [TestMethod]
    public Task PedroMod()
        => RunStrategy<PedroModStrategy>();

    [TestMethod]
    public Task PendingLimitGrid()
        => RunStrategy<PendingLimitGridStrategy>();

    [TestMethod]
    public Task PendingOrderGrid()
        => RunStrategy<PendingOrderGridStrategy>();

    [TestMethod]
    public Task PendingOrder()
        => RunStrategy<PendingOrderStrategy>();

    [TestMethod]
    public Task PendingOrdersByTime2()
        => RunStrategy<PendingOrdersByTime2Strategy>();

    [TestMethod]
    public Task PendingOrdersByTime()
        => RunStrategy<PendingOrdersByTimeStrategy>();

    [TestMethod]
    public Task PendingStopGrid()
        => RunStrategy<PendingStopGridStrategy>();

    [TestMethod]
    public Task PendingTread()
        => RunStrategy<PendingTreadStrategy>();

    [TestMethod]
    public Task Pendulum()
        => RunStrategy<PendulumStrategy>();

    [TestMethod]
    public Task PendulumSwing()
        => RunStrategy<PendulumSwingStrategy>();

    [TestMethod]
    public Task PenroseDiagram()
        => RunStrategy<PenroseDiagramStrategy>();

    [TestMethod]
    public Task PercentStopTakeProfit()
        => RunStrategy<PercentStopTakeProfitStrategy>();

    [TestMethod]
    public Task PercentXTrendFollower()
        => RunStrategy<PercentXTrendFollowerStrategy>();

    [TestMethod]
    public Task PercentageCrossoverChannel()
        => RunStrategy<PercentageCrossoverChannelStrategy>();

    [TestMethod]
    public Task PercentageCrossoverChannelSystem()
        => RunStrategy<PercentageCrossoverChannelSystemStrategy>();

    [TestMethod]
    public Task PercentageCrossover()
        => RunStrategy<PercentageCrossoverStrategy>();

    [TestMethod]
    public Task PerceptronAc()
        => RunStrategy<PerceptronAcStrategy>();

    [TestMethod]
    public Task PerceptronAdaptive()
        => RunStrategy<PerceptronAdaptiveStrategy>();

    [TestMethod]
    public Task PerceptronMult()
        => RunStrategy<PerceptronMultStrategy>();

    [TestMethod]
    public Task PersonalAssistantAlert()
        => RunStrategy<PersonalAssistantAlertStrategy>();

    [TestMethod]
    public Task PersonalAssistantMns()
        => RunStrategy<PersonalAssistantMnsStrategy>();

    [TestMethod]
    public Task PersonalAssistant()
        => RunStrategy<PersonalAssistantStrategy>();

    [TestMethod]
    public Task PeterPanel()
        => RunStrategy<PeterPanelStrategy>();

    [TestMethod]
    public Task PfeExtremes()
        => RunStrategy<PfeExtremesStrategy>();

    [TestMethod]
    public Task PhaseCrossWithZone()
        => RunStrategy<PhaseCrossWithZoneStrategy>();

    [TestMethod]
    public Task PianoMultiTimeframeBarState()
        => RunStrategy<PianoMultiTimeframeBarStateStrategy>();

    [TestMethod]
    public Task PinBarReversal()
        => RunStrategy<PinBarReversalStrategy>();

    [TestMethod]
    public Task PinballMachineRandomDraw()
        => RunStrategy<PinballMachineRandomDrawStrategy>();

    [TestMethod]
    public Task PinballMachine()
        => RunStrategy<PinballMachineStrategy>();

    [TestMethod]
    public Task PineconnectorTemplate()
        => RunStrategy<PineconnectorTemplateStrategy>();

    [TestMethod]
    public Task PipsoNightBreakout()
        => RunStrategy<PipsoNightBreakoutStrategy>();

    [TestMethod]
    public Task Pipso()
        => RunStrategy<PipsoStrategy>();

    [TestMethod]
    public Task Pipsover8167()
        => RunStrategy<Pipsover8167Strategy>();

    [TestMethod]
    public Task PipsoverChaikinHedge()
        => RunStrategy<PipsoverChaikinHedgeStrategy>();

    [TestMethod]
    public Task Pipsover()
        => RunStrategy<PipsoverStrategy>();

    [TestMethod]
    public Task PivotEma3RlhV4()
        => RunStrategy<PivotEma3RlhV4Strategy>();

    [TestMethod]
    public Task PivotHeiken()
        => RunStrategy<PivotHeikenStrategy>();

    [TestMethod]
    public Task PivotPercentileTrend()
        => RunStrategy<PivotPercentileTrendStrategy>();

    [TestMethod]
    public Task PivotPointSuperTrendTrendFilter()
        => RunStrategy<PivotPointSuperTrendTrendFilterStrategy>();

    [TestMethod]
    public Task PivotPointSupertrend()
        => RunStrategy<PivotPointSupertrendStrategy>();

    [TestMethod]
    public Task Pivots()
        => RunStrategy<PivotsStrategy>();

    [TestMethod]
    public Task PixelArt()
        => RunStrategy<PixelArtStrategy>();

    [TestMethod]
    public Task PlanXBreakout()
        => RunStrategy<PlanXBreakoutStrategy>();

    [TestMethod]
    public Task PlanX()
        => RunStrategy<PlanXStrategy>();

    [TestMethod]
    public Task Plateau()
        => RunStrategy<PlateauStrategy>();

    [TestMethod]
    public Task Plc()
        => RunStrategy<PlcStrategy>();

    [TestMethod]
    public Task PokerShow()
        => RunStrategy<PokerShowStrategy>();

    [TestMethod]
    public Task PolarizedFractalEfficiency()
        => RunStrategy<PolarizedFractalEfficiencyStrategy>();

    [TestMethod]
    public Task PolishLayerExpertAdvisorSystemEfficient()
        => RunStrategy<PolishLayerExpertAdvisorSystemEfficientStrategy>();

    [TestMethod]
    public Task PolishLayer()
        => RunStrategy<PolishLayerStrategy>();

    [TestMethod]
    public Task PolynomialRegressionBandsChannel()
        => RunStrategy<PolynomialRegressionBandsChannelStrategy>();

    [TestMethod]
    public Task PortfolioAlphaBetaStdevVarianceMeanMaxDrawdown()
        => RunStrategy<PortfolioAlphaBetaStdevVarianceMeanMaxDrawdownStrategy>();

    [TestMethod]
    public Task PortfolioTrackerV2()
        => RunStrategy<PortfolioTrackerV2Strategy>();

    [TestMethod]
    public Task PosNegDiCrossover()
        => RunStrategy<PosNegDiCrossoverStrategy>();

    [TestMethod]
    public Task PositionsChangeInformer()
        => RunStrategy<PositionsChangeInformerStrategy>();

    [TestMethod]
    public Task PositiveSwapInformer()
        => RunStrategy<PositiveSwapInformerStrategy>();

    [TestMethod]
    public Task PostEarningsAnnouncementDrift()
        => RunStrategy<PostEarningsAnnouncementDriftStrategy>();

    [TestMethod]
    public Task PostOpenLongAtrStopLossTakeProfit()
        => RunStrategy<PostOpenLongAtrStopLossTakeProfitStrategy>();

    [TestMethod]
    public Task PotentialEntries()
        => RunStrategy<PotentialEntriesStrategy>();

    [TestMethod]
    public Task PowerHourMoney()
        => RunStrategy<PowerHourMoneyStrategy>();

    [TestMethod]
    public Task PowerHouseSwiftEdgeAiV210()
        => RunStrategy<PowerHouseSwiftEdgeAiV210Strategy>();

    [TestMethod]
    public Task PowerZone()
        => RunStrategy<PowerZoneStrategy>();

    [TestMethod]
    public Task PowertrendVolumeRangeFilter()
        => RunStrategy<PowertrendVolumeRangeFilterStrategy>();

    [TestMethod]
    public Task PpoCloud()
        => RunStrategy<PpoCloudStrategy>();

    [TestMethod]
    public Task PrecipiceMartin()
        => RunStrategy<PrecipiceMartinStrategy>();

    [TestMethod]
    public Task Precipice()
        => RunStrategy<PrecipiceStrategy>();

    [TestMethod]
    public Task PrecisionTradingGoldenEdge()
        => RunStrategy<PrecisionTradingGoldenEdgeStrategy>();

    [TestMethod]
    public Task PremarketGapMomoTrader()
        => RunStrategy<PremarketGapMomoTraderStrategy>();

    [TestMethod]
    public Task PresentTrendRmiSynergy()
        => RunStrategy<PresentTrendRmiSynergyStrategy>();

    [TestMethod]
    public Task PresentTrend()
        => RunStrategy<PresentTrendStrategy>();

    [TestMethod]
    public Task PreviousCandleBreakdown2()
        => RunStrategy<PreviousCandleBreakdown2Strategy>();

    [TestMethod]
    public Task PreviousCandleBreakdownLevels()
        => RunStrategy<PreviousCandleBreakdownLevelsStrategy>();

    [TestMethod]
    public Task PreviousCandleBreakdown()
        => RunStrategy<PreviousCandleBreakdownStrategy>();

    [TestMethod]
    public Task PreviousCandleBreakout()
        => RunStrategy<PreviousCandleBreakoutStrategy>();

    [TestMethod]
    public Task PreviousDayHighLowLong()
        => RunStrategy<PreviousDayHighLowLongStrategy>();

    [TestMethod]
    public Task PreviousHighLowBreakout()
        => RunStrategy<PreviousHighLowBreakoutStrategy>();

    [TestMethod]
    public Task PreviousPeriodLevelsXAlerts()
        => RunStrategy<PreviousPeriodLevelsXAlertsStrategy>();

    [TestMethod]
    public Task PriceActionFractal()
        => RunStrategy<PriceActionFractalStrategy>();

    [TestMethod]
    public Task PriceAction()
        => RunStrategy<PriceActionStrategy>();

    [TestMethod]
    public Task PriceAndVolumeBreakoutBuy()
        => RunStrategy<PriceAndVolumeBreakoutBuyStrategy>();

    [TestMethod]
    public Task PriceBasedZTrend()
        => RunStrategy<PriceBasedZTrendStrategy>();

    [TestMethod]
    public Task PriceChannelSignalV2()
        => RunStrategy<PriceChannelSignalV2Strategy>();

    [TestMethod]
    public Task PriceChannelStop()
        => RunStrategy<PriceChannelStopStrategy>();

    [TestMethod]
    public Task PriceConvergence()
        => RunStrategy<PriceConvergenceStrategy>();

    [TestMethod]
    public Task PriceExtreme()
        => RunStrategy<PriceExtremeStrategy>();

    [TestMethod]
    public Task PriceFlip()
        => RunStrategy<PriceFlipStrategy>();

    [TestMethod]
    public Task PriceImpulse()
        => RunStrategy<PriceImpulseStrategy>();

    [TestMethod]
    public Task PriceRollback()
        => RunStrategy<PriceRollbackStrategy>();

    [TestMethod]
    public Task PriceStatisticalZScore()
        => RunStrategy<PriceStatisticalZScoreStrategy>();

    [TestMethod]
    public Task PricerEa()
        => RunStrategy<PricerEaStrategy>();

    [TestMethod]
    public Task ProMartMacdMartingale()
        => RunStrategy<ProMartMacdMartingaleStrategy>();

    [TestMethod]
    public Task ProbabilityOfAtrIndex()
        => RunStrategy<ProbabilityOfAtrIndexStrategy>();

    [TestMethod]
    public Task Probe()
        => RunStrategy<ProbeStrategy>();

    [TestMethod]
    public Task ProfessionalOrb()
        => RunStrategy<ProfessionalOrbStrategy>();

    [TestMethod]
    public Task ProffessorV3()
        => RunStrategy<ProffessorV3Strategy>();

}
