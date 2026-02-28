namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task GoCandleBodyReversal()
        => RunStrategy<GoCandleBodyReversalStrategy>();

    [TestMethod]
    public Task GoRiskManaged()
        => RunStrategy<GoRiskManagedStrategy>();

    [TestMethod]
    public Task Go()
        => RunStrategy<GoStrategy>();

    [TestMethod]
    public Task Godbot()
        => RunStrategy<GodbotStrategy>();

    [TestMethod]
    public Task GoldBreakoutRr4()
        => RunStrategy<GoldBreakoutRr4Strategy>();

    [TestMethod]
    public Task GoldDust()
        => RunStrategy<GoldDustStrategy>();

    [TestMethod]
    public Task GoldEurUsd()
        => RunStrategy<GoldEurUsdStrategy>();

    [TestMethod]
    public Task GoldFridayAnomaly()
        => RunStrategy<GoldFridayAnomalyStrategy>();

    [TestMethod]
    public Task GoldOrb()
        => RunStrategy<GoldOrbStrategy>();

    [TestMethod]
    public Task GoldPro()
        => RunStrategy<GoldProStrategy>();

    [TestMethod]
    public Task GoldPullback()
        => RunStrategy<GoldPullbackStrategy>();

    [TestMethod]
    public Task GoldRsiDivergence()
        => RunStrategy<GoldRsiDivergenceStrategy>();

    [TestMethod]
    public Task GoldScalpingBosChoch()
        => RunStrategy<GoldScalpingBosChochStrategy>();

    [TestMethod]
    public Task GoldScalpingWithPreciseEntries()
        => RunStrategy<GoldScalpingWithPreciseEntriesStrategy>();

    [TestMethod]
    public Task GoldTradeSetup()
        => RunStrategy<GoldTradeSetupStrategy>();

    [TestMethod]
    public Task GoldVolumeBasedEntry()
        => RunStrategy<GoldVolumeBasedEntryStrategy>();

    [TestMethod]
    public Task GoldWarrior02bImpulse()
        => RunStrategy<GoldWarrior02bImpulseStrategy>();

    [TestMethod]
    public Task GoldWarrior02b()
        => RunStrategy<GoldWarrior02bStrategy>();

    [TestMethod]
    public Task GoldenCrossVwmaEma()
        => RunStrategy<GoldenCrossVwmaEmaStrategy>();

    [TestMethod]
    public Task GoldenRatioCubes()
        => RunStrategy<GoldenRatioCubesStrategy>();

    [TestMethod]
    public Task GoldenTransform()
        => RunStrategy<GoldenTransformStrategy>();

    [TestMethod]
    public Task GonnaScalp()
        => RunStrategy<GonnaScalpStrategy>();

    [TestMethod]
    public Task GoodGbbi()
        => RunStrategy<GoodGbbiStrategy>();

    [TestMethod]
    public Task GoodModeRsiV2()
        => RunStrategy<GoodModeRsiV2Strategy>();

    [TestMethod]
    public Task GordagoEa()
        => RunStrategy<GordagoEaStrategy>();

    [TestMethod]
    public Task GpfTcpPivotLimit()
        => RunStrategy<GpfTcpPivotLimitStrategy>();

    [TestMethod]
    public Task GraalEmaMomentum()
        => RunStrategy<GraalEmaMomentumStrategy>();

    [TestMethod]
    public Task GraalFractalChannel()
        => RunStrategy<GraalFractalChannelStrategy>();

    [TestMethod]
    public Task GradientTrendFilter()
        => RunStrategy<GradientTrendFilterStrategy>();

    [TestMethod]
    public Task GrailExpertMa()
        => RunStrategy<GrailExpertMaStrategy>();

    [TestMethod]
    public Task GraphStyle4thDimensionRSI()
        => RunStrategy<GraphStyle4thDimensionRSIStrategy>();

    [TestMethod]
    public Task GreaseTrap()
        => RunStrategy<GreaseTrapStrategy>();

    [TestMethod]
    public Task GreenTrade()
        => RunStrategy<GreenTradeStrategy>();

    [TestMethod]
    public Task GridBotBacktesting()
        => RunStrategy<GridBotBacktestingStrategy>();

    [TestMethod]
    public Task GridEaPro()
        => RunStrategy<GridEaProStrategy>();

    [TestMethod]
    public Task GridLike()
        => RunStrategy<GridLikeStrategy>();

    [TestMethod]
    public Task GridRebalance()
        => RunStrategy<GridRebalanceStrategy>();

    [TestMethod]
    public Task Grid()
        => RunStrategy<GridStrategy>();

    [TestMethod]
    public Task GridTLongV1()
        => RunStrategy<GridTLongV1Strategy>();

    [TestMethod]
    public Task GridTemplate()
        => RunStrategy<GridTemplateStrategy>();

    [TestMethod]
    public Task GridTendenceV1()
        => RunStrategy<GridTendenceV1Strategy>();

    [TestMethod]
    public Task GridTradingAtVolatileMarket()
        => RunStrategy<GridTradingAtVolatileMarketStrategy>();

    [TestMethod]
    public Task GridderEa()
        => RunStrategy<GridderEaStrategy>();

    [TestMethod]
    public Task Grim309CallPut()
        => RunStrategy<Grim309CallPutStrategy>();

    [TestMethod]
    public Task GrimSlash()
        => RunStrategy<GrimSlashStrategy>();

    [TestMethod]
    public Task GroverLlorensActivator()
        => RunStrategy<GroverLlorensActivatorStrategy>();

    [TestMethod]
    public Task GrrAlBreakout()
        => RunStrategy<GrrAlBreakoutStrategy>();

    [TestMethod]
    public Task GselectorPatternProbability()
        => RunStrategy<GselectorPatternProbabilityStrategy>();

    [TestMethod]
    public Task Guage()
        => RunStrategy<GuageStrategy>();

    [TestMethod]
    public Task H4L4Breakout()
        => RunStrategy<H4L4BreakoutStrategy>();

    [TestMethod]
    public Task HaMaZi()
        => RunStrategy<HaMaZiStrategy>();

    [TestMethod]
    public Task HammerEmaTickSlTp()
        => RunStrategy<HammerEmaTickSlTpStrategy>();

    [TestMethod]
    public Task HammerHangingManCci()
        => RunStrategy<HammerHangingManCciStrategy>();

    [TestMethod]
    public Task HammerHangingStochastic()
        => RunStrategy<HammerHangingStochasticStrategy>();

    [TestMethod]
    public Task HammerShootingStar()
        => RunStrategy<HammerShootingStarStrategy>();

    [TestMethod]
    public Task HamsterBotMrs2()
        => RunStrategy<HamsterBotMrs2Strategy>();

    [TestMethod]
    public Task HancockRsiVolume()
        => RunStrategy<HancockRsiVolumeStrategy>();

    [TestMethod]
    public Task Hans123TraderRangeBreakout()
        => RunStrategy<Hans123TraderRangeBreakoutStrategy>();

    [TestMethod]
    public Task Hans123Trader()
        => RunStrategy<Hans123TraderStrategy>();

    [TestMethod]
    public Task Hans123TraderV2()
        => RunStrategy<Hans123TraderV2Strategy>();

    [TestMethod]
    public Task HansIndicatorCloudSystem()
        => RunStrategy<HansIndicatorCloudSystemStrategy>();

    [TestMethod]
    public Task HarVesteRMacdTrend()
        => RunStrategy<HarVesteRMacdTrendStrategy>();

    [TestMethod]
    public Task HarVesteR()
        => RunStrategy<HarVesteRStrategy>();

    [TestMethod]
    public Task HaramiCciConfirmation()
        => RunStrategy<HaramiCciConfirmationStrategy>();

    [TestMethod]
    public Task Harami()
        => RunStrategy<HaramiStrategy>();

    [TestMethod]
    public Task HardProfit()
        => RunStrategy<HardProfitStrategy>();

    [TestMethod]
    public Task HardcoreFx()
        => RunStrategy<HardcoreFxStrategy>();

    [TestMethod]
    public Task HarmonicPattern()
        => RunStrategy<HarmonicPatternStrategy>();

    [TestMethod]
    public Task HarmonySignalFlowByArun()
        => RunStrategy<HarmonySignalFlowByArunStrategy>();

    [TestMethod]
    public Task HawaiianTsunamiSurfer()
        => RunStrategy<HawaiianTsunamiSurferStrategy>();

    [TestMethod]
    public Task HbsSystem()
        => RunStrategy<HbsSystemStrategy>();

    [TestMethod]
    public Task HeadAndShoulders()
        => RunStrategy<HeadAndShouldersStrategy>();

    [TestMethod]
    public Task HeatmapMacd2()
        => RunStrategy<HeatmapMacd2Strategy>();

    [TestMethod]
    public Task HeatmapMacd()
        => RunStrategy<HeatmapMacdStrategy>();

    [TestMethod]
    public Task HedgeAnyPositions()
        => RunStrategy<HedgeAnyPositionsStrategy>();

    [TestMethod]
    public Task HedgeAverage()
        => RunStrategy<HedgeAverageStrategy>();

    [TestMethod]
    public Task HedgerDrawdown()
        => RunStrategy<HedgerDrawdownStrategy>();

    [TestMethod]
    public Task Hedger()
        => RunStrategy<HedgerStrategy>();

    [TestMethod]
    public Task HedgingMartingale()
        => RunStrategy<HedgingMartingaleStrategy>();

    [TestMethod]
    public Task HeikenAshiEngulf()
        => RunStrategy<HeikenAshiEngulfStrategy>();

    [TestMethod]
    public Task HeikenAshiIdea()
        => RunStrategy<HeikenAshiIdeaStrategy>();

    [TestMethod]
    public Task HeikenAshiNoWick()
        => RunStrategy<HeikenAshiNoWickStrategy>();

    [TestMethod]
    public Task HeikenAshiSimplifiedEa()
        => RunStrategy<HeikenAshiSimplifiedEaStrategy>();

    [TestMethod]
    public Task HeikenAshiSmoothedMtf()
        => RunStrategy<HeikenAshiSmoothedMtfStrategy>();

    [TestMethod]
    public Task HeikenAshiSmoothedTrend()
        => RunStrategy<HeikenAshiSmoothedTrendStrategy>();

    [TestMethod]
    public Task HeikenAshiSupertrendAdx()
        => RunStrategy<HeikenAshiSupertrendAdxStrategy>();

    [TestMethod]
    public Task HeikenAshiSupertrendAtrSl()
        => RunStrategy<HeikenAshiSupertrendAtrSlStrategy>();

    [TestMethod]
    public Task HeikenAshiWaves()
        => RunStrategy<HeikenAshiWavesStrategy>();

    [TestMethod]
    public Task HeikinAshiRocPercentile()
        => RunStrategy<HeikinAshiRocPercentileStrategy>();

    [TestMethod]
    public Task HeikinAshiTrader()
        => RunStrategy<HeikinAshiTraderStrategy>();

    [TestMethod]
    public Task HelloSmart()
        => RunStrategy<HelloSmartStrategy>();

    [TestMethod]
    public Task HerculesATC2006()
        => RunStrategy<HerculesATC2006Strategy>();

    [TestMethod]
    public Task Hercules()
        => RunStrategy<HerculesStrategy>();

    [TestMethod]
    public Task HftSpreaderForForts()
        => RunStrategy<HftSpreaderForFortsStrategy>();

    [TestMethod]
    public Task HiddenSl()
        => RunStrategy<HiddenSlStrategy>();

    [TestMethod]
    public Task HiddenStopLossTakeProfit()
        => RunStrategy<HiddenStopLossTakeProfitStrategy>();

    [TestMethod]
    public Task HierarchicalKMeansClustering()
        => RunStrategy<HierarchicalKMeansClusteringStrategy>();

    [TestMethod]
    public Task HighAndLowLast24Hours()
        => RunStrategy<HighAndLowLast24HoursStrategy>();

    [TestMethod]
    public Task HighLowBreakoutAtrTrailingStop()
        => RunStrategy<HighLowBreakoutAtrTrailingStopStrategy>();

    [TestMethod]
    public Task HighLowBreakoutStatisticalAnalysis()
        => RunStrategy<HighLowBreakoutStatisticalAnalysisStrategy>();

}
