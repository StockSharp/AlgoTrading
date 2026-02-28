namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task ExpMama()
        => RunStrategy<ExpMamaStrategy>();

    [TestMethod]
    public Task ExpMartinV2()
        => RunStrategy<ExpMartinV2Strategy>();

    [TestMethod]
    public Task ExpMovingAverageFn()
        => RunStrategy<ExpMovingAverageFnStrategy>();

    [TestMethod]
    public Task ExpMultic()
        => RunStrategy<ExpMulticStrategy>();

    [TestMethod]
    public Task ExpMultitrendSignalKvn()
        => RunStrategy<ExpMultitrendSignalKvnStrategy>();

    [TestMethod]
    public Task ExpMuvNorDiffCloud()
        => RunStrategy<ExpMuvNorDiffCloudStrategy>();

    [TestMethod]
    public Task ExpOracle()
        => RunStrategy<ExpOracleStrategy>();

    [TestMethod]
    public Task ExpPricePosition()
        => RunStrategy<ExpPricePositionStrategy>();

    [TestMethod]
    public Task ExpQqeCloud()
        => RunStrategy<ExpQqeCloudStrategy>();

    [TestMethod]
    public Task ExpRjSlidingRangeRjDigitSystemTmPlus()
        => RunStrategy<ExpRjSlidingRangeRjDigitSystemTmPlusStrategy>();

    [TestMethod]
    public Task ExpRjtxMatchesSmoothedDuplex()
        => RunStrategy<ExpRjtxMatchesSmoothedDuplexStrategy>();

    [TestMethod]
    public Task ExpRsioma()
        => RunStrategy<ExpRsiomaStrategy>();

    [TestMethod]
    public Task ExpRsiomaV2()
        => RunStrategy<ExpRsiomaV2Strategy>();

    [TestMethod]
    public Task ExpSarTmPlus()
        => RunStrategy<ExpSarTmPlusStrategy>();

    [TestMethod]
    public Task ExpSinewave2X2()
        => RunStrategy<ExpSinewave2X2Strategy>();

    [TestMethod]
    public Task ExpSkyscraperFixColorAmlMmrec()
        => RunStrategy<ExpSkyscraperFixColorAmlMmrecStrategy>();

    [TestMethod]
    public Task ExpSkyscraperFixColorAml()
        => RunStrategy<ExpSkyscraperFixColorAmlStrategy>();

    [TestMethod]
    public Task ExpSkyscraperFixColorAmlX2MaCandleMmRec()
        => RunStrategy<ExpSkyscraperFixColorAmlX2MaCandleMmRecStrategy>();

    [TestMethod]
    public Task ExpSkyscraperFixDuplex()
        => RunStrategy<ExpSkyscraperFixDuplexStrategy>();

    [TestMethod]
    public Task ExpSlowStochDuplex()
        => RunStrategy<ExpSlowStochDuplexStrategy>();

    [TestMethod]
    public Task ExpSpearmanRankCorrelationHistogram()
        => RunStrategy<ExpSpearmanRankCorrelationHistogramStrategy>();

    [TestMethod]
    public Task ExpSslNrtrTmPlus()
        => RunStrategy<ExpSslNrtrTmPlusStrategy>();

    [TestMethod]
    public Task ExpSuperTrend()
        => RunStrategy<ExpSuperTrendStrategy>();

    [TestMethod]
    public Task ExpT3Trix()
        => RunStrategy<ExpT3TrixStrategy>();

    [TestMethod]
    public Task ExpTema()
        => RunStrategy<ExpTemaStrategy>();

    [TestMethod]
    public Task ExpTimeZonePivotsOpenSystemTmPlus()
        => RunStrategy<ExpTimeZonePivotsOpenSystemTmPlusStrategy>();

    [TestMethod]
    public Task ExpToCloseProfit()
        => RunStrategy<ExpToCloseProfitStrategy>();

    [TestMethod]
    public Task ExpTradingChannelIndex()
        => RunStrategy<ExpTradingChannelIndexStrategy>();

    [TestMethod]
    public Task ExpTrendIntensityIndex()
        => RunStrategy<ExpTrendIntensityIndexStrategy>();

    [TestMethod]
    public Task ExpTrendMagic()
        => RunStrategy<ExpTrendMagicStrategy>();

    [TestMethod]
    public Task ExpTrendValue()
        => RunStrategy<ExpTrendValueStrategy>();

    [TestMethod]
    public Task ExpTsiCci()
        => RunStrategy<ExpTsiCciStrategy>();

    [TestMethod]
    public Task ExpUltraFatlDuplex()
        => RunStrategy<ExpUltraFatlDuplexStrategy>();

    [TestMethod]
    public Task ExpX2MaCandleMmRec()
        => RunStrategy<ExpX2MaCandleMmRecStrategy>();

    [TestMethod]
    public Task ExpX2Ma()
        => RunStrategy<ExpX2MaStrategy>();

    [TestMethod]
    public Task ExpXBullsBearsEyesVolDirect()
        => RunStrategy<ExpXBullsBearsEyesVolDirectStrategy>();

    [TestMethod]
    public Task ExpXBullsBearsEyesVol()
        => RunStrategy<ExpXBullsBearsEyesVolStrategy>();

    [TestMethod]
    public Task ExpXFisherOrgV1()
        => RunStrategy<ExpXFisherOrgV1Strategy>();

    [TestMethod]
    public Task ExpXHullTrendDigit()
        => RunStrategy<ExpXHullTrendDigitStrategy>();

    [TestMethod]
    public Task ExpXPeriodCandle()
        => RunStrategy<ExpXPeriodCandleStrategy>();

    [TestMethod]
    public Task ExpXPeriodCandleX2()
        => RunStrategy<ExpXPeriodCandleX2Strategy>();

    [TestMethod]
    public Task ExpXmaRangeBands()
        => RunStrategy<ExpXmaRangeBandsStrategy>();

    [TestMethod]
    public Task ExpXpvt()
        => RunStrategy<ExpXpvtStrategy>();

    [TestMethod]
    public Task ExpXrsiHistogramVol()
        => RunStrategy<ExpXrsiHistogramVolStrategy>();

    [TestMethod]
    public Task ExpXwamiMmRec()
        => RunStrategy<ExpXwamiMmRecStrategy>();

    [TestMethod]
    public Task ExpXwprHistogramVolDirect()
        => RunStrategy<ExpXwprHistogramVolDirectStrategy>();

    [TestMethod]
    public Task ExpXwprHistogramVol()
        => RunStrategy<ExpXwprHistogramVolStrategy>();

    [TestMethod]
    public Task Expert610Breakout()
        => RunStrategy<Expert610BreakoutStrategy>();

    [TestMethod]
    public Task ExpertAdcPlStoch()
        => RunStrategy<ExpertAdcPlStochStrategy>();

    [TestMethod]
    public Task ExpertAlligator()
        => RunStrategy<ExpertAlligatorStrategy>();

    [TestMethod]
    public Task ExpertAmlMfi()
        => RunStrategy<ExpertAmlMfiStrategy>();

    [TestMethod]
    public Task ExpertCandles()
        => RunStrategy<ExpertCandlesStrategy>();

    [TestMethod]
    public Task ExpertClor2MaStopAtr()
        => RunStrategy<ExpertClor2MaStopAtrStrategy>();

    [TestMethod]
    public Task ExpertIchimoku()
        => RunStrategy<ExpertIchimokuStrategy>();

    [TestMethod]
    public Task ExpertMacdEurusd1Hour()
        => RunStrategy<ExpertMacdEurusd1HourStrategy>();

    [TestMethod]
    public Task ExpertMasterEurusd()
        => RunStrategy<ExpertMasterEurusdStrategy>();

    [TestMethod]
    public Task ExpertNews()
        => RunStrategy<ExpertNewsStrategy>();

    [TestMethod]
    public Task ExpertRsiStochasticMa()
        => RunStrategy<ExpertRsiStochasticMaStrategy>();

    [TestMethod]
    public Task ExpertZzlwa()
        => RunStrategy<ExpertZzlwaStrategy>();

    [TestMethod]
    public Task ExplosionRangeExpansion()
        => RunStrategy<ExplosionRangeExpansionStrategy>();

    [TestMethod]
    public Task Explosion()
        => RunStrategy<ExplosionStrategy>();

    [TestMethod]
    public Task Expotest()
        => RunStrategy<ExpotestStrategy>();

    [TestMethod]
    public Task ExpressGenerator()
        => RunStrategy<ExpressGeneratorStrategy>();

    [TestMethod]
    public Task ExternalLevel()
        => RunStrategy<ExternalLevelStrategy>();

    [TestMethod]
    public Task ExternalSignalsTester()
        => RunStrategy<ExternalSignalsTesterStrategy>();

    [TestMethod]
    public Task ExtrapolatedPivotConnector()
        => RunStrategy<ExtrapolatedPivotConnectorStrategy>();

    [TestMethod]
    public Task ExtremN()
        => RunStrategy<ExtremNStrategy>();

    [TestMethod]
    public Task ExtremeEa()
        => RunStrategy<ExtremeEaStrategy>();

    [TestMethod]
    public Task ExtremeStrengthReversal()
        => RunStrategy<ExtremeStrengthReversalStrategy>();

    [TestMethod]
    public Task F2aAo()
        => RunStrategy<F2aAoStrategy>();

    [TestMethod]
    public Task FTBillWillamsTrader()
        => RunStrategy<FTBillWillamsTraderStrategy>();

    [TestMethod]
    public Task FaithIndicator()
        => RunStrategy<FaithIndicatorStrategy>();

    [TestMethod]
    public Task FalconLiquidityGrab()
        => RunStrategy<FalconLiquidityGrabStrategy>();

    [TestMethod]
    public Task FancyBollingerBands()
        => RunStrategy<FancyBollingerBandsStrategy>();

    [TestMethod]
    public Task FarhadCrab1()
        => RunStrategy<FarhadCrab1Strategy>();

    [TestMethod]
    public Task FarhadCrab()
        => RunStrategy<FarhadCrabStrategy>();

    [TestMethod]
    public Task FarhadHillVersion2()
        => RunStrategy<FarhadHillVersion2Strategy>();

    [TestMethod]
    public Task Fast2Crossover()
        => RunStrategy<Fast2CrossoverStrategy>();

    [TestMethod]
    public Task FastSlowMaCrossover()
        => RunStrategy<FastSlowMaCrossoverStrategy>();

    [TestMethod]
    public Task FastSlowRviCrossover()
        => RunStrategy<FastSlowRviCrossoverStrategy>();

    [TestMethod]
    public Task FatPanelVisualBuilder()
        => RunStrategy<FatPanelVisualBuilderStrategy>();

    [TestMethod]
    public Task FatlMacd()
        => RunStrategy<FatlMacdStrategy>();

    [TestMethod]
    public Task FatlSatlOsma()
        => RunStrategy<FatlSatlOsmaStrategy>();

    [TestMethod]
    public Task FearzonePanel()
        => RunStrategy<FearzonePanelStrategy>();

    [TestMethod]
    public Task FetchNews()
        => RunStrategy<FetchNewsStrategy>();

    [TestMethod]
    public Task FibHurstBreakout()
        => RunStrategy<FibHurstBreakoutStrategy>();

    [TestMethod]
    public Task Fibo1()
        => RunStrategy<Fibo1Strategy>();

    [TestMethod]
    public Task FiboArcMomentum()
        => RunStrategy<FiboArcMomentumStrategy>();

    [TestMethod]
    public Task FiboAvg001a()
        => RunStrategy<FiboAvg001aStrategy>();

    [TestMethod]
    public Task FiboCandlesTrend()
        => RunStrategy<FiboCandlesTrendStrategy>();

    [TestMethod]
    public Task FiboChannelLine()
        => RunStrategy<FiboChannelLineStrategy>();

    [TestMethod]
    public Task FiboIsar()
        => RunStrategy<FiboIsarStrategy>();

    [TestMethod]
    public Task FiboPivotMultiVal()
        => RunStrategy<FiboPivotMultiValStrategy>();

    [TestMethod]
    public Task FiboStop()
        => RunStrategy<FiboStopStrategy>();

    [TestMethod]
    public Task FibonacciAtrFusion()
        => RunStrategy<FibonacciAtrFusionStrategy>();

    [TestMethod]
    public Task FibonacciAutoTrendScouter()
        => RunStrategy<FibonacciAutoTrendScouterStrategy>();

    [TestMethod]
    public Task FibonacciBands()
        => RunStrategy<FibonacciBandsStrategy>();

    [TestMethod]
    public Task FibonacciBollingerBands()
        => RunStrategy<FibonacciBollingerBandsStrategy>();

    [TestMethod]
    public Task FibonacciCounterTrendTrading()
        => RunStrategy<FibonacciCounterTrendTradingStrategy>();

    [TestMethod]
    public Task FibonacciLevelsHighLow()
        => RunStrategy<FibonacciLevelsHighLowStrategy>();

}
