namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task ProfitHunterHsiWithFibonacci()
        => RunStrategy<ProfitHunterHsiWithFibonacciStrategy>();

    [TestMethod]
    public Task ProfitLabels()
        => RunStrategy<ProfitLabelsStrategy>();

    [TestMethod]
    public Task ProfitLossTrail()
        => RunStrategy<ProfitLossTrailStrategy>();

    [TestMethod]
    public Task ProfitViewTemplate()
        => RunStrategy<ProfitViewTemplateStrategy>();

    [TestMethod]
    public Task ProfitablePullbackMark804()
        => RunStrategy<ProfitablePullbackMark804Strategy>();

    [TestMethod]
    public Task ProfitableSuperTrendMAStoch()
        => RunStrategy<ProfitableSuperTrendMAStochStrategy>();

    [TestMethod]
    public Task Projection()
        => RunStrategy<ProjectionStrategy>();

    [TestMethod]
    public Task PropFirmBusinessSimulator()
        => RunStrategy<PropFirmBusinessSimulatorStrategy>();

    [TestMethod]
    public Task PropFirmHelper()
        => RunStrategy<PropFirmHelperStrategy>();

    [TestMethod]
    public Task ProperBot()
        => RunStrategy<ProperBotStrategy>();

    [TestMethod]
    public Task Prophet()
        => RunStrategy<ProphetStrategy>();

    [TestMethod]
    public Task PrototypeIx()
        => RunStrategy<PrototypeIxStrategy>();

    [TestMethod]
    public Task ProxyFinancialStressIndex()
        => RunStrategy<ProxyFinancialStressIndexStrategy>();

    [TestMethod]
    public Task PsarBug6()
        => RunStrategy<PsarBug6Strategy>();

    [TestMethod]
    public Task PsarBug()
        => RunStrategy<PsarBugStrategy>();

    [TestMethod]
    public Task PsarMultiTimeframe()
        => RunStrategy<PsarMultiTimeframeStrategy>();

    [TestMethod]
    public Task PsarTrader()
        => RunStrategy<PsarTraderStrategy>();

    [TestMethod]
    public Task PsarTraderTicks()
        => RunStrategy<PsarTraderTicksStrategy>();

    [TestMethod]
    public Task PsarTraderV2()
        => RunStrategy<PsarTraderV2Strategy>();

    [TestMethod]
    public Task PsiProcEmaMacd()
        => RunStrategy<PsiProcEmaMacdStrategy>();

    [TestMethod]
    public Task PullAllTicks()
        => RunStrategy<PullAllTicksStrategy>();

    [TestMethod]
    public Task PullBack()
        => RunStrategy<PullBackStrategy>();

    [TestMethod]
    public Task PullbackProDow()
        => RunStrategy<PullbackProDowStrategy>();

    [TestMethod]
    public Task PulseWave()
        => RunStrategy<PulseWaveStrategy>();

    [TestMethod]
    public Task Puncher()
        => RunStrategy<PuncherStrategy>();

    [TestMethod]
    public Task PureMartingale()
        => RunStrategy<PureMartingaleStrategy>();

    [TestMethod]
    public Task PurePriceActionBreakoutWith15RR()
        => RunStrategy<PurePriceActionBreakoutWith15RRStrategy>();

    [TestMethod]
    public Task PurePriceActionFractal()
        => RunStrategy<PurePriceActionFractalStrategy>();

    [TestMethod]
    public Task PurePriceAction()
        => RunStrategy<PurePriceActionStrategy>();

    [TestMethod]
    public Task PuriaMethod()
        => RunStrategy<PuriaMethodStrategy>();

    [TestMethod]
    public Task Puria()
        => RunStrategy<PuriaStrategy>();

    [TestMethod]
    public Task PvsraV5()
        => RunStrategy<PvsraV5Strategy>();

    [TestMethod]
    public Task PvtCrossover()
        => RunStrategy<PvtCrossoverStrategy>();

    [TestMethod]
    public Task PzParabolicSarEa()
        => RunStrategy<PzParabolicSarEaStrategy>();

    [TestMethod]
    public Task PzReversalTrendFollowing()
        => RunStrategy<PzReversalTrendFollowingStrategy>();

    [TestMethod]
    public Task Q2maCross()
        => RunStrategy<Q2maCrossStrategy>();

    [TestMethod]
    public Task QqqV2EslEasyPeasyX()
        => RunStrategy<QqqV2EslEasyPeasyXStrategy>();

    [TestMethod]
    public Task QuadraticRegression()
        => RunStrategy<QuadraticRegressionStrategy>();

    [TestMethod]
    public Task QualityScreen()
        => RunStrategy<QualityScreenStrategy>();

    [TestMethod]
    public Task QuantitativeTrendUptrendLong()
        => RunStrategy<QuantitativeTrendUptrendLongStrategy>();

    [TestMethod]
    public Task QuantumReversal()
        => RunStrategy<QuantumReversalStrategy>();

    [TestMethod]
    public Task QuantumSentimentFluxBeginners()
        => RunStrategy<QuantumSentimentFluxBeginnersStrategy>();

    [TestMethod]
    public Task QuantumStochastic()
        => RunStrategy<QuantumStochasticStrategy>();

    [TestMethod]
    public Task QuatroSma()
        => RunStrategy<QuatroSmaStrategy>();

    [TestMethod]
    public Task QuickTradeKeys()
        => RunStrategy<QuickTradeKeysStrategy>();

    [TestMethod]
    public Task RBasedTemplate()
        => RunStrategy<RBasedTemplateStrategy>();

    [TestMethod]
    public Task RPoint250()
        => RunStrategy<RPoint250Strategy>();

    [TestMethod]
    public Task RSIMartingale()
        => RunStrategy<RSIMartingaleStrategy>();

    [TestMethod]
    public Task Rabbit3()
        => RunStrategy<Rabbit3Strategy>();

    [TestMethod]
    public Task RabbitM2RegimeSwing()
        => RunStrategy<RabbitM2RegimeSwingStrategy>();

    [TestMethod]
    public Task RabbitM2()
        => RunStrategy<RabbitM2Strategy>();

    [TestMethod]
    public Task RabbitM3()
        => RunStrategy<RabbitM3Strategy>();

    [TestMethod]
    public Task RallyBaseDropSndPivots()
        => RunStrategy<RallyBaseDropSndPivotsStrategy>();

    [TestMethod]
    public Task RampokScalp()
        => RunStrategy<RampokScalpStrategy>();

    [TestMethod]
    public Task RandomAtrBybit()
        => RunStrategy<RandomAtrBybitStrategy>();

    [TestMethod]
    public Task RandomBiasTrader()
        => RunStrategy<RandomBiasTraderStrategy>();

    [TestMethod]
    public Task RandomCoinTossBaseline()
        => RunStrategy<RandomCoinTossBaselineStrategy>();

    [TestMethod]
    public Task RandomCoinToss()
        => RunStrategy<RandomCoinTossStrategy>();

    [TestMethod]
    public Task RandomEntryAndExit()
        => RunStrategy<RandomEntryAndExitStrategy>();

    [TestMethod]
    public Task RandomHedg()
        => RunStrategy<RandomHedgStrategy>();

    [TestMethod]
    public Task RandomStateMachine()
        => RunStrategy<RandomStateMachineStrategy>();

    [TestMethod]
    public Task RandomSyntheticAssetGeneration()
        => RunStrategy<RandomSyntheticAssetGenerationStrategy>();

    [TestMethod]
    public Task RandomT()
        => RunStrategy<RandomTStrategy>();

    [TestMethod]
    public Task RandomTrader()
        => RunStrategy<RandomTraderStrategy>();

    [TestMethod]
    public Task RandomTrailingStop()
        => RunStrategy<RandomTrailingStopStrategy>();

    [TestMethod]
    public Task RangeBreakout2()
        => RunStrategy<RangeBreakout2Strategy>();

    [TestMethod]
    public Task RangeBreakout()
        => RunStrategy<RangeBreakoutStrategy>();

    [TestMethod]
    public Task RangeBreakoutWeekly()
        => RunStrategy<RangeBreakoutWeeklyStrategy>();

    [TestMethod]
    public Task RangeEa()
        => RunStrategy<RangeEaStrategy>();

    [TestMethod]
    public Task RangeExpansionIndex()
        => RunStrategy<RangeExpansionIndexStrategy>();

    [TestMethod]
    public Task RangeFilterAtrLowDrawdown()
        => RunStrategy<RangeFilterAtrLowDrawdownStrategy>();

    [TestMethod]
    public Task RangeFilterAtrTpSl()
        => RunStrategy<RangeFilterAtrTpSlStrategy>();

    [TestMethod]
    public Task RangeFilterDw()
        => RunStrategy<RangeFilterDwStrategy>();

    [TestMethod]
    public Task RangeFilter()
        => RunStrategy<RangeFilterStrategy>();

    [TestMethod]
    public Task RangeFollower()
        => RunStrategy<RangeFollowerStrategy>();

    [TestMethod]
    public Task RangeWeeklyGrid()
        => RunStrategy<RangeWeeklyGridStrategy>();

    [TestMethod]
    public Task RapidDoji()
        => RunStrategy<RapidDojiStrategy>();

    [TestMethod]
    public Task RateOfChange()
        => RunStrategy<RateOfChangeStrategy>();

    [TestMethod]
    public Task RaviAo()
        => RunStrategy<RaviAoStrategy>();

    [TestMethod]
    public Task RaviHistogram()
        => RunStrategy<RaviHistogramStrategy>();

    [TestMethod]
    public Task RaviIao()
        => RunStrategy<RaviIaoStrategy>();

    [TestMethod]
    public Task Rawstocks15MinuteModel()
        => RunStrategy<Rawstocks15MinuteModelStrategy>();

    [TestMethod]
    public Task RaymondCloudyDay()
        => RunStrategy<RaymondCloudyDayStrategy>();

    [TestMethod]
    public Task Rci()
        => RunStrategy<RciStrategy>();

    [TestMethod]
    public Task RdTrendTrigger()
        => RunStrategy<RdTrendTriggerStrategy>();

    [TestMethod]
    public Task ReInitChart()
        => RunStrategy<ReInitChartStrategy>();

    [TestMethod]
    public Task ReOpenPositions()
        => RunStrategy<ReOpenPositionsStrategy>();

    [TestMethod]
    public Task RealtimeDeltaVolumeAction()
        => RunStrategy<RealtimeDeltaVolumeActionStrategy>();

    [TestMethod]
    public Task RecoRsiGrid()
        => RunStrategy<RecoRsiGridStrategy>();

    [TestMethod]
    public Task RectangleTest()
        => RunStrategy<RectangleTestStrategy>();

    [TestMethod]
    public Task RedkCompoundRatioMa()
        => RunStrategy<RedkCompoundRatioMaStrategy>();

    [TestMethod]
    public Task RedkSlowSmoothAverageRssWma()
        => RunStrategy<RedkSlowSmoothAverageRssWmaStrategy>();

    [TestMethod]
    public Task ReduceRisks()
        => RunStrategy<ReduceRisksStrategy>();

    [TestMethod]
    public Task RefinedMaEngulfing()
        => RunStrategy<RefinedMaEngulfingStrategy>();

    [TestMethod]
    public Task RefinedSmaEmaCrossoverWithIchimokuAnd200SmaFilter()
        => RunStrategy<RefinedSmaEmaCrossoverWithIchimokuAnd200SmaFilterStrategy>();

    [TestMethod]
    public Task ReflectedEmaDifferenceRed()
        => RunStrategy<ReflectedEmaDifferenceRedStrategy>();

    [TestMethod]
    public Task ReflexOscillator()
        => RunStrategy<ReflexOscillatorStrategy>();

    [TestMethod]
    public Task ReflexTrendflex()
        => RunStrategy<ReflexTrendflexStrategy>();

    [TestMethod]
    public Task RegressionChannelBreakout()
        => RunStrategy<RegressionChannelBreakoutStrategy>();

    [TestMethod]
    public Task RegularitiesOfExchangeRates()
        => RunStrategy<RegularitiesOfExchangeRatesStrategy>();

}
