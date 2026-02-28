namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task Ma2CciClassic()
        => RunStrategy<Ma2CciClassicStrategy>();

    [TestMethod]
    public Task Ma2CciEma()
        => RunStrategy<Ma2CciEmaStrategy>();

    [TestMethod]
    public Task Ma2Cci()
        => RunStrategy<Ma2CciStrategy>();

    [TestMethod]
    public Task MaBbSupertrend()
        => RunStrategy<MaBbSupertrendStrategy>();

    [TestMethod]
    public Task MaBreak()
        => RunStrategy<MaBreakStrategy>();

    [TestMethod]
    public Task MaByMa()
        => RunStrategy<MaByMaStrategy>();

    [TestMethod]
    public Task MaChannel()
        => RunStrategy<MaChannelStrategy>();

    [TestMethod]
    public Task MaCrossMethodPriceMode()
        => RunStrategy<MaCrossMethodPriceModeStrategy>();

    [TestMethod]
    public Task MaCross()
        => RunStrategy<MaCrossStrategy>();

    [TestMethod]
    public Task MaCrossoverAdx()
        => RunStrategy<MaCrossoverAdxStrategy>();

    [TestMethod]
    public Task MaCrossoverDemandSupplyZonesSltp()
        => RunStrategy<MaCrossoverDemandSupplyZonesSltpStrategy>();

    [TestMethod]
    public Task MaCrossoverMultiTimeframe()
        => RunStrategy<MaCrossoverMultiTimeframeStrategy>();

    [TestMethod]
    public Task MaCrossoverTpSl5EmaFilter()
        => RunStrategy<MaCrossoverTpSl5EmaFilterStrategy>();

    [TestMethod]
    public Task MaCrossoverTpSl()
        => RunStrategy<MaCrossoverTpSlStrategy>();

    [TestMethod]
    public Task MaDelta()
        => RunStrategy<MaDeltaStrategy>();

    [TestMethod]
    public Task MaEnvelopes()
        => RunStrategy<MaEnvelopesStrategy>();

    [TestMethod]
    public Task MaGrid()
        => RunStrategy<MaGridStrategy>();

    [TestMethod]
    public Task MaLWorld()
        => RunStrategy<MaLWorldStrategy>();

    [TestMethod]
    public Task MaMacdBbBackTester()
        => RunStrategy<MaMacdBbBackTesterStrategy>();

    [TestMethod]
    public Task MaMacdPositionAveraging()
        => RunStrategy<MaMacdPositionAveragingStrategy>();

    [TestMethod]
    public Task MaMacdPositionAveragingV2()
        => RunStrategy<MaMacdPositionAveragingV2Strategy>();

    [TestMethod]
    public Task MaMirror()
        => RunStrategy<MaMirrorStrategy>();

    [TestMethod]
    public Task MaOnMomentumMinProfit()
        => RunStrategy<MaOnMomentumMinProfitStrategy>();

    [TestMethod]
    public Task MaOscillatorHistogram()
        => RunStrategy<MaOscillatorHistogramStrategy>();

    [TestMethod]
    public Task MaPriceCross()
        => RunStrategy<MaPriceCrossStrategy>();

    [TestMethod]
    public Task MaPsarAtrTrend()
        => RunStrategy<MaPsarAtrTrendStrategy>();

    [TestMethod]
    public Task MaReverse()
        => RunStrategy<MaReverseStrategy>();

    [TestMethod]
    public Task MaRobot()
        => RunStrategy<MaRobotStrategy>();

    [TestMethod]
    public Task MaRoundingCandle()
        => RunStrategy<MaRoundingCandleStrategy>();

    [TestMethod]
    public Task MaRsiEa()
        => RunStrategy<MaRsiEaStrategy>();

    [TestMethod]
    public Task MaRsiTrigger()
        => RunStrategy<MaRsiTriggerStrategy>();

    [TestMethod]
    public Task MaRsiWizard()
        => RunStrategy<MaRsiWizardStrategy>();

    [TestMethod]
    public Task MaSarAdxBind()
        => RunStrategy<MaSarAdxBindStrategy>();

    [TestMethod]
    public Task MaSarAdx()
        => RunStrategy<MaSarAdxStrategy>();

    [TestMethod]
    public Task MaShiftPuriaMethod()
        => RunStrategy<MaShiftPuriaMethodStrategy>();

    [TestMethod]
    public Task MaSrTrading()
        => RunStrategy<MaSrTradingStrategy>();

    [TestMethod]
    public Task MaTrend2()
        => RunStrategy<MaTrend2Strategy>();

    [TestMethod]
    public Task MaTrend()
        => RunStrategy<MaTrendStrategy>();

    [TestMethod]
    public Task MaWithLogistic()
        => RunStrategy<MaWithLogisticStrategy>();

    [TestMethod]
    public Task Macd1MinScalper()
        => RunStrategy<Macd1MinScalperStrategy>();

    [TestMethod]
    public Task MacdAggressiveScalpSimple()
        => RunStrategy<MacdAggressiveScalpSimpleStrategy>();

    [TestMethod]
    public Task MacdAndSar()
        => RunStrategy<MacdAndSarStrategy>();

    [TestMethod]
    public Task MacdAoPattern()
        => RunStrategy<MacdAoPatternStrategy>();

    [TestMethod]
    public Task MacdCandle()
        => RunStrategy<MacdCandleStrategy>();

    [TestMethod]
    public Task MacdCciLotfy()
        => RunStrategy<MacdCciLotfyStrategy>();

    [TestMethod]
    public Task MacdCleaner()
        => RunStrategy<MacdCleanerStrategy>();

    [TestMethod]
    public Task MacdCrossAudusdD1()
        => RunStrategy<MacdCrossAudusdD1Strategy>();

    [TestMethod]
    public Task MacdCrossover()
        => RunStrategy<MacdCrossoverStrategy>();

    [TestMethod]
    public Task MacdDiverAndRsi()
        => RunStrategy<MacdDiverAndRsiStrategy>();

    [TestMethod]
    public Task MacdDivergenceRsi()
        => RunStrategy<MacdDivergenceRsiStrategy>();

    [TestMethod]
    public Task MacdEa()
        => RunStrategy<MacdEaStrategy>();

    [TestMethod]
    public Task MacdEmaSarBollingerBullBear()
        => RunStrategy<MacdEmaSarBollingerBullBearStrategy>();

    [TestMethod]
    public Task MacdEnhancedMtfWithStopLoss()
        => RunStrategy<MacdEnhancedMtfWithStopLossStrategy>();

    [TestMethod]
    public Task MacdFixedPsar()
        => RunStrategy<MacdFixedPsarStrategy>();

    [TestMethod]
    public Task MacdFourColors2Martingale()
        => RunStrategy<MacdFourColors2MartingaleStrategy>();

    [TestMethod]
    public Task MacdLiquidityTracker()
        => RunStrategy<MacdLiquidityTrackerStrategy>();

    [TestMethod]
    public Task MacdMomentumReversal()
        => RunStrategy<MacdMomentumReversalStrategy>();

    [TestMethod]
    public Task MacdMultiTimeframeExpert()
        => RunStrategy<MacdMultiTimeframeExpertStrategy>();

    [TestMethod]
    public Task MacdNoSample()
        => RunStrategy<MacdNoSampleStrategy>();

    [TestMethod]
    public Task MacdNotSoSample()
        => RunStrategy<MacdNotSoSampleStrategy>();

    [TestMethod]
    public Task MacdOfRelativeStrenght()
        => RunStrategy<MacdOfRelativeStrenghtStrategy>();

    [TestMethod]
    public Task MacdParabolicSarWizard()
        => RunStrategy<MacdParabolicSarWizardStrategy>();

    [TestMethod]
    public Task MacdPatternTraderAdvancedMultiPattern()
        => RunStrategy<MacdPatternTraderAdvancedMultiPatternStrategy>();

    [TestMethod]
    public Task MacdPatternTraderAll()
        => RunStrategy<MacdPatternTraderAllStrategy>();

    [TestMethod]
    public Task MacdPatternTraderAllV001()
        => RunStrategy<MacdPatternTraderAllV001Strategy>();

    [TestMethod]
    public Task MacdPatternTraderDoubleTop()
        => RunStrategy<MacdPatternTraderDoubleTopStrategy>();

    [TestMethod]
    public Task MacdPatternTraderSession()
        => RunStrategy<MacdPatternTraderSessionStrategy>();

    [TestMethod]
    public Task MacdPatternTraderTrigger()
        => RunStrategy<MacdPatternTraderTriggerStrategy>();

    [TestMethod]
    public Task MacdPatternTraderV01()
        => RunStrategy<MacdPatternTraderV01Strategy>();

    [TestMethod]
    public Task MacdPatternTraderV02()
        => RunStrategy<MacdPatternTraderV02Strategy>();

    [TestMethod]
    public Task MacdPatternTraderV03()
        => RunStrategy<MacdPatternTraderV03Strategy>();

    [TestMethod]
    public Task MacdPower()
        => RunStrategy<MacdPowerStrategy>();

    [TestMethod]
    public Task MacdRsiEmaBbAtrDayTrading()
        => RunStrategy<MacdRsiEmaBbAtrDayTradingStrategy>();

    [TestMethod]
    public Task MacdSample1010()
        => RunStrategy<MacdSample1010Strategy>();

    [TestMethod]
    public Task MacdSampleClassic()
        => RunStrategy<MacdSampleClassicStrategy>();

    [TestMethod]
    public Task MacdSampleHedgingGrid()
        => RunStrategy<MacdSampleHedgingGridStrategy>();

    [TestMethod]
    public Task MacdSample()
        => RunStrategy<MacdSampleStrategy>();

    [TestMethod]
    public Task MacdSampleTrendFilter()
        => RunStrategy<MacdSampleTrendFilterStrategy>();

    [TestMethod]
    public Task MacdSecrets()
        => RunStrategy<MacdSecretsStrategy>();

    [TestMethod]
    public Task MacdSignalAtr()
        => RunStrategy<MacdSignalAtrStrategy>();

    [TestMethod]
    public Task MacdSignalCrossover()
        => RunStrategy<MacdSignalCrossoverStrategy>();

    [TestMethod]
    public Task MacdSignal()
        => RunStrategy<MacdSignalStrategy>();

    [TestMethod]
    public Task MacdSimpleReshetov()
        => RunStrategy<MacdSimpleReshetovStrategy>();

    [TestMethod]
    public Task MacdStochastic2()
        => RunStrategy<MacdStochastic2Strategy>();

    [TestMethod]
    public Task MacdStochasticConfirmationReversal()
        => RunStrategy<MacdStochasticConfirmationReversalStrategy>();

    [TestMethod]
    public Task MacdStochasticFilter()
        => RunStrategy<MacdStochasticFilterStrategy>();

    [TestMethod]
    public Task MacdStochastic()
        => RunStrategy<MacdStochasticStrategy>();

    [TestMethod]
    public Task MacdStochasticTrailing()
        => RunStrategy<MacdStochasticTrailingStrategy>();

    [TestMethod]
    public Task MacdTrendMode()
        => RunStrategy<MacdTrendModeStrategy>();

    [TestMethod]
    public Task MacdVolumeBboReversal()
        => RunStrategy<MacdVolumeBboReversalStrategy>();

    [TestMethod]
    public Task MacdVolumeXauusd()
        => RunStrategy<MacdVolumeXauusdStrategy>();

    [TestMethod]
    public Task MacdVsSignal()
        => RunStrategy<MacdVsSignalStrategy>();

    [TestMethod]
    public Task MacdWaterlineCrossExpectator()
        => RunStrategy<MacdWaterlineCrossExpectatorStrategy>();

    [TestMethod]
    public Task MacdZeroFilterTakeProfit()
        => RunStrategy<MacdZeroFilterTakeProfitStrategy>();

    [TestMethod]
    public Task MacdZeroFilteredCross()
        => RunStrategy<MacdZeroFilteredCrossStrategy>();

    [TestMethod]
    public Task Macfibo()
        => RunStrategy<MacfiboStrategy>();

    [TestMethod]
    public Task MachineLearningLogisticRegression()
        => RunStrategy<MachineLearningLogisticRegressionStrategy>();

    [TestMethod]
    public Task MachineLearningSuperTrend()
        => RunStrategy<MachineLearningSuperTrendStrategy>();

    [TestMethod]
    public Task Macross()
        => RunStrategy<MacrossStrategy>();

    [TestMethod]
    public Task MadTrader()
        => RunStrategy<MadTraderStrategy>();

}
