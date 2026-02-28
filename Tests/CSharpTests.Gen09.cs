namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task DemaTrendOscillator()
        => RunStrategy<DemaTrendOscillatorStrategy>();

    [TestMethod]
    public Task DemarkerMartingale()
        => RunStrategy<DemarkerMartingaleStrategy>();

    [TestMethod]
    public Task Dematus()
        => RunStrategy<DematusStrategy>();

    [TestMethod]
    public Task DemoGptDayTradingScalping()
        => RunStrategy<DemoGptDayTradingScalpingStrategy>();

    [TestMethod]
    public Task DerivativeZeroCross()
        => RunStrategy<DerivativeZeroCrossStrategy>();

    [TestMethod]
    public Task DiNapoliStochastic()
        => RunStrategy<DiNapoliStochasticStrategy>();

    [TestMethod]
    public Task DiffTfMa()
        => RunStrategy<DiffTfMaStrategy>();

    [TestMethod]
    public Task DigVariation()
        => RunStrategy<DigVariationStrategy>();

    [TestMethod]
    public Task DigitalCciWoodies()
        => RunStrategy<DigitalCciWoodiesStrategy>();

    [TestMethod]
    public Task DigitalFilterT01()
        => RunStrategy<DigitalFilterT01Strategy>();

    [TestMethod]
    public Task DirectedMovementCandle()
        => RunStrategy<DirectedMovementCandleStrategy>();

    [TestMethod]
    public Task DirectedMovement()
        => RunStrategy<DirectedMovementStrategy>();

    [TestMethod]
    public Task DirectionalIndexKxDMI()
        => RunStrategy<DirectionalIndexKxDMIStrategy>();

    [TestMethod]
    public Task Disaster()
        => RunStrategy<DisasterStrategy>();

    [TestMethod]
    public Task DistanceToDemandVector()
        => RunStrategy<DistanceToDemandVectorStrategy>();

    [TestMethod]
    public Task Disturbed()
        => RunStrategy<DisturbedStrategy>();

    [TestMethod]
    public Task DivergenceEmaRsiCloseBuyOnly()
        => RunStrategy<DivergenceEmaRsiCloseBuyOnlyStrategy>();

    [TestMethod]
    public Task DivergenceExpert()
        => RunStrategy<DivergenceExpertStrategy>();

    [TestMethod]
    public Task DivergenceForManyIndicators()
        => RunStrategy<DivergenceForManyIndicatorsStrategy>();

    [TestMethod]
    public Task DivergenceForManyIndicatorsV4()
        => RunStrategy<DivergenceForManyIndicatorsV4Strategy>();

    [TestMethod]
    public Task DivergenceIndicatorAnyOscillator()
        => RunStrategy<DivergenceIndicatorAnyOscillatorStrategy>();

    [TestMethod]
    public Task DivergenceMacdStochastic()
        => RunStrategy<DivergenceMacdStochasticStrategy>();

    [TestMethod]
    public Task Divergence()
        => RunStrategy<DivergenceStrategy>();

    [TestMethod]
    public Task DivergenceTraderBasket()
        => RunStrategy<DivergenceTraderBasketStrategy>();

    [TestMethod]
    public Task DivergenceTraderClassic()
        => RunStrategy<DivergenceTraderClassicStrategy>();

    [TestMethod]
    public Task DivergenceTrader()
        => RunStrategy<DivergenceTraderStrategy>();

    [TestMethod]
    public Task DkoderwebRepaintingIssueFix()
        => RunStrategy<DkoderwebRepaintingIssueFixStrategy>();

    [TestMethod]
    public Task Dlmv1Grid()
        => RunStrategy<Dlmv1GridStrategy>();

    [TestMethod]
    public Task DlmvFxFishGrid()
        => RunStrategy<DlmvFxFishGridStrategy>();

    [TestMethod]
    public Task DnseVn301SmaEmaCross()
        => RunStrategy<DnseVn301SmaEmaCrossStrategy>();

    [TestMethod]
    public Task Doctor()
        => RunStrategy<DoctorStrategy>();

    [TestMethod]
    public Task DojiArrowsBreakout()
        => RunStrategy<DojiArrowsBreakoutStrategy>();

    [TestMethod]
    public Task DojiArrows()
        => RunStrategy<DojiArrowsStrategy>();

    [TestMethod]
    public Task DojiPatternAlert()
        => RunStrategy<DojiPatternAlertStrategy>();

    [TestMethod]
    public Task DojiTraderBreakout()
        => RunStrategy<DojiTraderBreakoutStrategy>();

    [TestMethod]
    public Task DojiTrader()
        => RunStrategy<DojiTraderStrategy>();

    [TestMethod]
    public Task DojiTrading()
        => RunStrategy<DojiTradingStrategy>();

    [TestMethod]
    public Task DominanceTagcloud()
        => RunStrategy<DominanceTagcloudStrategy>();

    [TestMethod]
    public Task DonchainCounterChannelSystem()
        => RunStrategy<DonchainCounterChannelSystemStrategy>();

    [TestMethod]
    public Task DonchainCounter()
        => RunStrategy<DonchainCounterStrategy>();

    [TestMethod]
    public Task DonchianBreakout()
        => RunStrategy<DonchianBreakoutStrategy>();

    [TestMethod]
    public Task DonchianChannels()
        => RunStrategy<DonchianChannelsStrategy>();

    [TestMethod]
    public Task DonchianChannelsSystem()
        => RunStrategy<DonchianChannelsSystemStrategy>();

    [TestMethod]
    public Task DonchianHlWidthCycleInformation()
        => RunStrategy<DonchianHlWidthCycleInformationStrategy>();

    [TestMethod]
    public Task DonchianQuestResearch()
        => RunStrategy<DonchianQuestResearchStrategy>();

    [TestMethod]
    public Task DonchianScalper()
        => RunStrategy<DonchianScalperStrategy>();

    [TestMethod]
    public Task DonchianWmaCrossover()
        => RunStrategy<DonchianWmaCrossoverStrategy>();

    [TestMethod]
    public Task DonchianZigZagLuxAlgo()
        => RunStrategy<DonchianZigZagLuxAlgoStrategy>();

    [TestMethod]
    public Task DonkyMaTpSl()
        => RunStrategy<DonkyMaTpSlStrategy>();

    [TestMethod]
    public Task DontMakeMeCross()
        => RunStrategy<DontMakeMeCrossStrategy>();

    [TestMethod]
    public Task Dots()
        => RunStrategy<DotsStrategy>();

    [TestMethod]
    public Task DoubleAiSuperTrendTrading()
        => RunStrategy<DoubleAiSuperTrendTradingStrategy>();

    [TestMethod]
    public Task DoubleBollingerBandsSignals()
        => RunStrategy<DoubleBollingerBandsSignalsStrategy>();

    [TestMethod]
    public Task DoubleBottomAndTopHunter()
        => RunStrategy<DoubleBottomAndTopHunterStrategy>();

    [TestMethod]
    public Task DoubleCciConfirmedHullMovingAverageReversal()
        => RunStrategy<DoubleCciConfirmedHullMovingAverageReversalStrategy>();

    [TestMethod]
    public Task DoubleChannelEa()
        => RunStrategy<DoubleChannelEaStrategy>();

    [TestMethod]
    public Task DoubleMaBreakout()
        => RunStrategy<DoubleMaBreakoutStrategy>();

    [TestMethod]
    public Task DoubleMaCrossover()
        => RunStrategy<DoubleMaCrossoverStrategy>();

    [TestMethod]
    public Task DoubleMacd()
        => RunStrategy<DoubleMacdStrategy>();

    [TestMethod]
    public Task DoubleTrading()
        => RunStrategy<DoubleTradingStrategy>();

    [TestMethod]
    public Task DoubleUp2Martingale()
        => RunStrategy<DoubleUp2MartingaleStrategy>();

    [TestMethod]
    public Task DoubleUp2()
        => RunStrategy<DoubleUp2Strategy>();

    [TestMethod]
    public Task DoubleUp()
        => RunStrategy<DoubleUpStrategy>();

    [TestMethod]
    public Task DoubleVegasSuperTrendEnhanced()
        => RunStrategy<DoubleVegasSuperTrendEnhancedStrategy>();

    [TestMethod]
    public Task DoubleZigZag()
        => RunStrategy<DoubleZigZagStrategy>();

    [TestMethod]
    public Task Doubler()
        => RunStrategy<DoublerStrategy>();

    [TestMethod]
    public Task DowTheoryTrend()
        => RunStrategy<DowTheoryTrendStrategy>();

    [TestMethod]
    public Task DragSlTp()
        => RunStrategy<DragSlTpStrategy>();

    [TestMethod]
    public Task DreamBot()
        => RunStrategy<DreamBotStrategy>();

    [TestMethod]
    public Task DroneoxEquityGuardian()
        => RunStrategy<DroneoxEquityGuardianStrategy>();

    [TestMethod]
    public Task DskyzAdaptiveFuturesElite()
        => RunStrategy<DskyzAdaptiveFuturesEliteStrategy>();

    [TestMethod]
    public Task DskyzAiAdaptiveRegimeBeginners()
        => RunStrategy<DskyzAiAdaptiveRegimeBeginnersStrategy>();

    [TestMethod]
    public Task DskyzDafeAdaptiveRegimeQuantMachinePro()
        => RunStrategy<DskyzDafeAdaptiveRegimeQuantMachineProStrategy>();

    [TestMethod]
    public Task DskyzDafeGenesis()
        => RunStrategy<DskyzDafeGenesisStrategy>();

    [TestMethod]
    public Task DskyzDafeMatrixAtrPrecision()
        => RunStrategy<DskyzDafeMatrixAtrPrecisionStrategy>();

    [TestMethod]
    public Task Dsl()
        => RunStrategy<DslStrategy>();

    [TestMethod]
    public Task DssBressert()
        => RunStrategy<DssBressertStrategy>();

    [TestMethod]
    public Task DtRsiExp1()
        => RunStrategy<DtRsiExp1Strategy>();

    [TestMethod]
    public Task DualKeltnerChannels()
        => RunStrategy<DualKeltnerChannelsStrategy>();

    [TestMethod]
    public Task DualLotStepHedge()
        => RunStrategy<DualLotStepHedgeStrategy>();

    [TestMethod]
    public Task DualMaTrendConfirmation()
        => RunStrategy<DualMaTrendConfirmationStrategy>();

    [TestMethod]
    public Task DualMacd()
        => RunStrategy<DualMacdStrategy>();

    [TestMethod]
    public Task DualMomentum()
        => RunStrategy<DualMomentumStrategy>();

    [TestMethod]
    public Task DualPhaseTrendRegime()
        => RunStrategy<DualPhaseTrendRegimeStrategy>();

    [TestMethod]
    public Task DualRsiDifferential()
        => RunStrategy<DualRsiDifferentialStrategy>();

    [TestMethod]
    public Task DualSelectorV2Cryptogyani()
        => RunStrategy<DualSelectorV2CryptogyaniStrategy>();

    [TestMethod]
    public Task DualStoploss()
        => RunStrategy<DualStoplossStrategy>();

    [TestMethod]
    public Task DualSuperTrendVixFilter()
        => RunStrategy<DualSuperTrendVixFilterStrategy>();

    [TestMethod]
    public Task DualSupertrendMacd()
        => RunStrategy<DualSupertrendMacdStrategy>();

    [TestMethod]
    public Task DubicEma()
        => RunStrategy<DubicEmaStrategy>();

    [TestMethod]
    public Task Dvd10050Cent()
        => RunStrategy<Dvd10050CentStrategy>();

    [TestMethod]
    public Task DvdLevel()
        => RunStrategy<DvdLevelStrategy>();

    [TestMethod]
    public Task DynamicAveraging()
        => RunStrategy<DynamicAveragingStrategy>();

    [TestMethod]
    public Task DynamicBreakoutMaster()
        => RunStrategy<DynamicBreakoutMasterStrategy>();

    [TestMethod]
    public Task DynamicDotsDashboard()
        => RunStrategy<DynamicDotsDashboardStrategy>();

    [TestMethod]
    public Task DynamicRsC()
        => RunStrategy<DynamicRsCStrategy>();

    [TestMethod]
    public Task DynamicStopLoss()
        => RunStrategy<DynamicStopLossStrategy>();

    [TestMethod]
    public Task DynamicSupportAndResistancePivot()
        => RunStrategy<DynamicSupportAndResistancePivotStrategy>();

    [TestMethod]
    public Task DynamicTicksOscillatorModel()
        => RunStrategy<DynamicTicksOscillatorModelStrategy>();

    [TestMethod]
    public Task DynamicVolatilityDifferentialModel()
        => RunStrategy<DynamicVolatilityDifferentialModelStrategy>();

}
