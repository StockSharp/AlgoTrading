namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task MissionImpossiblePowerTwoOpen()
        => RunStrategy<MissionImpossiblePowerTwoOpenStrategy>();

    [TestMethod]
    public Task MmFibonacci()
        => RunStrategy<MmFibonacciStrategy>();

    [TestMethod]
    public Task MmaBreakoutVolumeI()
        => RunStrategy<MmaBreakoutVolumeIStrategy>();

    [TestMethod]
    public Task MnistPatternClassifier()
        => RunStrategy<MnistPatternClassifierStrategy>();

    [TestMethod]
    public Task MoBidir()
        => RunStrategy<MoBidirStrategy>();

    [TestMethod]
    public Task MoStAsHaR15PivotLine()
        => RunStrategy<MoStAsHaR15PivotLineStrategy>();

    [TestMethod]
    public Task MocDeltaMooEntry()
        => RunStrategy<MocDeltaMooEntryStrategy>();

    [TestMethod]
    public Task MocDeltaMooEntryV2Reverse()
        => RunStrategy<MocDeltaMooEntryV2ReverseStrategy>();

    [TestMethod]
    public Task MocDeltaMooEntryV2()
        => RunStrategy<MocDeltaMooEntryV2Strategy>();

    [TestMethod]
    public Task ModifiedObvWithDivergenceDetection()
        => RunStrategy<ModifiedObvWithDivergenceDetectionStrategy>();

    [TestMethod]
    public Task ModifiedOptimumEllipticFilter()
        => RunStrategy<ModifiedOptimumEllipticFilterStrategy>();

    [TestMethod]
    public Task ModularRangeTrading()
        => RunStrategy<ModularRangeTradingStrategy>();

    [TestMethod]
    public Task MollyEtfEmaCrossover()
        => RunStrategy<MollyEtfEmaCrossoverStrategy>();

    [TestMethod]
    public Task MomentumAlligator4hBitcoin()
        => RunStrategy<MomentumAlligator4hBitcoinStrategy>();

    [TestMethod]
    public Task MomentumCandleSign()
        => RunStrategy<MomentumCandleSignStrategy>();

    [TestMethod]
    public Task MomentumKeltnerStochasticCombo()
        => RunStrategy<MomentumKeltnerStochasticComboStrategy>();

    [TestMethod]
    public Task MomentumLongShort()
        => RunStrategy<MomentumLongShortStrategy>();

    [TestMethod]
    public Task MomentumM15()
        => RunStrategy<MomentumM15Strategy>();

    [TestMethod]
    public Task MomentumSyncPsarRsiAdxFiltered3TierExit()
        => RunStrategy<MomentumSyncPsarRsiAdxFiltered3TierExitStrategy>();

    [TestMethod]
    public Task MomoTrades()
        => RunStrategy<MomoTradesStrategy>();

    [TestMethod]
    public Task MomoTradesV3()
        => RunStrategy<MomoTradesV3Strategy>();

    [TestMethod]
    public Task MondayOpen()
        => RunStrategy<MondayOpenStrategy>();

    [TestMethod]
    public Task MondayTypicalBreakout()
        => RunStrategy<MondayTypicalBreakoutStrategy>();

    [TestMethod]
    public Task MoneyFixedMargin()
        => RunStrategy<MoneyFixedMarginStrategy>();

    [TestMethod]
    public Task MoneyFixedRisk()
        => RunStrategy<MoneyFixedRiskStrategy>();

    [TestMethod]
    public Task MoneyManager()
        => RunStrategy<MoneyManagerStrategy>();

    [TestMethod]
    public Task MoneyRainRecovery()
        => RunStrategy<MoneyRainRecoveryStrategy>();

    [TestMethod]
    public Task MoneyRain()
        => RunStrategy<MoneyRainStrategy>();

    [TestMethod]
    public Task MonteCarloRangeForecast()
        => RunStrategy<MonteCarloRangeForecastStrategy>();

    [TestMethod]
    public Task MonteCarloSimulationRandomWalk()
        => RunStrategy<MonteCarloSimulationRandomWalkStrategy>();

    [TestMethod]
    public Task MonthlyBreakout()
        => RunStrategy<MonthlyBreakoutStrategy>();

    [TestMethod]
    public Task MonthlyDayLongVix()
        => RunStrategy<MonthlyDayLongVixStrategy>();

    [TestMethod]
    public Task MonthlyPerformanceTable()
        => RunStrategy<MonthlyPerformanceTableStrategy>();

    [TestMethod]
    public Task MonthlyPurchaseWithDynamicContractSize()
        => RunStrategy<MonthlyPurchaseWithDynamicContractSizeStrategy>();

    [TestMethod]
    public Task MonthlyReturns()
        => RunStrategy<MonthlyReturnsStrategy>();

    [TestMethod]
    public Task MoreOrdersAfterBreakEven()
        => RunStrategy<MoreOrdersAfterBreakEvenStrategy>();

    [TestMethod]
    public Task MorningEveningMfi()
        => RunStrategy<MorningEveningMfiStrategy>();

    [TestMethod]
    public Task MorningEveningStarCci()
        => RunStrategy<MorningEveningStarCciStrategy>();

    [TestMethod]
    public Task MorningEveningStochastic()
        => RunStrategy<MorningEveningStochasticStrategy>();

    [TestMethod]
    public Task MorningPullbackCorridor()
        => RunStrategy<MorningPullbackCorridorStrategy>();

    [TestMethod]
    public Task MorseCode()
        => RunStrategy<MorseCodeStrategy>();

    [TestMethod]
    public Task MostPowerfulTqqqEmaCrossover()
        => RunStrategy<MostPowerfulTqqqEmaCrossoverStrategy>();

    [TestMethod]
    public Task MostasHar15Pivot()
        => RunStrategy<MostasHar15PivotStrategy>();

    [TestMethod]
    public Task Motion()
        => RunStrategy<MotionStrategy>();

    [TestMethod]
    public Task MoveCross()
        => RunStrategy<MoveCrossStrategy>();

    [TestMethod]
    public Task MoveStopLoss()
        => RunStrategy<MoveStopLossStrategy>();

    [TestMethod]
    public Task MovingAverageCrossoverSpread()
        => RunStrategy<MovingAverageCrossoverSpreadStrategy>();

    [TestMethod]
    public Task MovingAverageCrossover()
        => RunStrategy<MovingAverageCrossoverStrategy>();

    [TestMethod]
    public Task MovingAverageCrossoverSwing()
        => RunStrategy<MovingAverageCrossoverSwingStrategy>();

    [TestMethod]
    public Task MovingAverageEntanglement()
        => RunStrategy<MovingAverageEntanglementStrategy>();

    [TestMethod]
    public Task MovingAverageMartingale()
        => RunStrategy<MovingAverageMartingaleStrategy>();

    [TestMethod]
    public Task MovingAverageMoney()
        => RunStrategy<MovingAverageMoneyStrategy>();

    [TestMethod]
    public Task MovingAveragePositionSystem()
        => RunStrategy<MovingAveragePositionSystemStrategy>();

    [TestMethod]
    public Task MovingAveragePriceCross()
        => RunStrategy<MovingAveragePriceCrossStrategy>();

    [TestMethod]
    public Task MovingAverageRainbowStormer()
        => RunStrategy<MovingAverageRainbowStormerStrategy>();

    [TestMethod]
    public Task MovingAverageShift()
        => RunStrategy<MovingAverageShiftStrategy>();

    [TestMethod]
    public Task MovingAverageShiftWaveTrend()
        => RunStrategy<MovingAverageShiftWaveTrendStrategy>();

    [TestMethod]
    public Task MovingAverage()
        => RunStrategy<MovingAverageStrategy>();

    [TestMethod]
    public Task MovingAverageTradeSystem()
        => RunStrategy<MovingAverageTradeSystemStrategy>();

    [TestMethod]
    public Task MovingAverageWithFrames()
        => RunStrategy<MovingAverageWithFramesStrategy>();

    [TestMethod]
    public Task MovingAveragesCrossover()
        => RunStrategy<MovingAveragesCrossoverStrategy>();

    [TestMethod]
    public Task MovingAverages()
        => RunStrategy<MovingAveragesStrategy>();

    [TestMethod]
    public Task MovingRegression()
        => RunStrategy<MovingRegressionStrategy>();

    [TestMethod]
    public Task MovingUp()
        => RunStrategy<MovingUpStrategy>();

    [TestMethod]
    public Task MpCandlestick()
        => RunStrategy<MpCandlestickStrategy>();

    [TestMethod]
    public Task Mpm()
        => RunStrategy<MpmStrategy>();

    [TestMethod]
    public Task Mslea()
        => RunStrategy<MsleaStrategy>();

    [TestMethod]
    public Task MtcComboV2()
        => RunStrategy<MtcComboV2Strategy>();

    [TestMethod]
    public Task MtfOscillatorFramework()
        => RunStrategy<MtfOscillatorFrameworkStrategy>();

    [TestMethod]
    public Task MtfRsiSar()
        => RunStrategy<MtfRsiSarStrategy>();

    [TestMethod]
    public Task MtfSecondsValuesJD()
        => RunStrategy<MtfSecondsValuesJDStrategy>();

    [TestMethod]
    public Task MultiArbitration()
        => RunStrategy<MultiArbitrationStrategy>();

    [TestMethod]
    public Task MultiBandComparison()
        => RunStrategy<MultiBandComparisonStrategy>();

    [TestMethod]
    public Task MultiBreakoutV001k()
        => RunStrategy<MultiBreakoutV001kStrategy>();

    [TestMethod]
    public Task MultiCombo()
        => RunStrategy<MultiComboStrategy>();

    [TestMethod]
    public Task MultiConditionsCurveFitting()
        => RunStrategy<MultiConditionsCurveFittingStrategy>();

    [TestMethod]
    public Task MultiConfluenceSwingHunterV1()
        => RunStrategy<MultiConfluenceSwingHunterV1Strategy>();

    [TestMethod]
    public Task MultiCurrencyTemplateMt5()
        => RunStrategy<MultiCurrencyTemplateMt5Strategy>();

    [TestMethod]
    public Task MultiCurrencyTemplate()
        => RunStrategy<MultiCurrencyTemplateStrategy>();

    [TestMethod]
    public Task MultiEaV12()
        => RunStrategy<MultiEaV12Strategy>();

    [TestMethod]
    public Task MultiEmaCrossover()
        => RunStrategy<MultiEmaCrossoverStrategy>();

    [TestMethod]
    public Task MultiFactor()
        => RunStrategy<MultiFactorStrategy>();

    [TestMethod]
    public Task MultiHedgingScheduler()
        => RunStrategy<MultiHedgingSchedulerStrategy>();

    [TestMethod]
    public Task MultiIndicatorOptimizer()
        => RunStrategy<MultiIndicatorOptimizerStrategy>();

    [TestMethod]
    public Task MultiIndicatorSwing()
        => RunStrategy<MultiIndicatorSwingStrategy>();

    [TestMethod]
    public Task MultiIndicatorTrendFollowing()
        => RunStrategy<MultiIndicatorTrendFollowingStrategy>();

    [TestMethod]
    public Task MultiLayerAccelerationDeceleration()
        => RunStrategy<MultiLayerAccelerationDecelerationStrategy>();

    [TestMethod]
    public Task MultiLayerAwesomeOscillatorSaucer()
        => RunStrategy<MultiLayerAwesomeOscillatorSaucerStrategy>();

    [TestMethod]
    public Task MultiLotScalper()
        => RunStrategy<MultiLotScalperStrategy>();

    [TestMethod]
    public Task MultiMartin()
        => RunStrategy<MultiMartinStrategy>();

    [TestMethod]
    public Task MultiOrders()
        => RunStrategy<MultiOrdersStrategy>();

    [TestMethod]
    public Task MultiPairCloser()
        => RunStrategy<MultiPairCloserStrategy>();

    [TestMethod]
    public Task MultiRegression()
        => RunStrategy<MultiRegressionStrategy>();

    [TestMethod]
    public Task MultiStepFlexiMa()
        => RunStrategy<MultiStepFlexiMaStrategy>();

    [TestMethod]
    public Task MultiStepFlexiSuperTrend()
        => RunStrategy<MultiStepFlexiSuperTrendStrategy>();

    [TestMethod]
    public Task MultiStepVegasSuperTrend()
        => RunStrategy<MultiStepVegasSuperTrendStrategy>();

    [TestMethod]
    public Task MultiStochastic()
        => RunStrategy<MultiStochasticStrategy>();

    [TestMethod]
    public Task MultiTfAiSuperTrendWithAdx()
        => RunStrategy<MultiTfAiSuperTrendWithAdxStrategy>();

    [TestMethod]
    public Task MultiTimeFrameCandles()
        => RunStrategy<MultiTimeFrameCandlesStrategy>();

    [TestMethod]
    public Task MultiTimeFrameCandlesWithVolumeInfo3D()
        => RunStrategy<MultiTimeFrameCandlesWithVolumeInfo3DStrategy>();

}
