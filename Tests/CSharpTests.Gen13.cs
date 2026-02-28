namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task FibonacciLevelsWithHighLowCriteriaAynet()
        => RunStrategy<FibonacciLevelsWithHighLowCriteriaAynetStrategy>();

    [TestMethod]
    public Task FibonacciOnly()
        => RunStrategy<FibonacciOnlyStrategy>();

    [TestMethod]
    public Task FibonacciOnlyV2()
        => RunStrategy<FibonacciOnlyV2Strategy>();

    [TestMethod]
    public Task FibonacciPotentialEntriesRetracement()
        => RunStrategy<FibonacciPotentialEntriesRetracementStrategy>();

    [TestMethod]
    public Task FibonacciPotentialEntries()
        => RunStrategy<FibonacciPotentialEntriesStrategy>();

    [TestMethod]
    public Task FibonacciRetracementCrypto()
        => RunStrategy<FibonacciRetracementCryptoStrategy>();

    [TestMethod]
    public Task FibonacciRetracementMomentum()
        => RunStrategy<FibonacciRetracementMomentumStrategy>();

    [TestMethod]
    public Task FibonacciRetracement()
        => RunStrategy<FibonacciRetracementStrategy>();

    [TestMethod]
    public Task FibonacciSwingTradingBot()
        => RunStrategy<FibonacciSwingTradingBotStrategy>();

    [TestMethod]
    public Task FibonacciTimeZones()
        => RunStrategy<FibonacciTimeZonesStrategy>();

    [TestMethod]
    public Task FibonacciTpSl()
        => RunStrategy<FibonacciTpSlStrategy>();

    [TestMethod]
    public Task FibonacciTrendReversal()
        => RunStrategy<FibonacciTrendReversalStrategy>();

    [TestMethod]
    public Task FibonacciTrend()
        => RunStrategy<FibonacciTrendStrategy>();

    [TestMethod]
    public Task FifteenMinuteScalper()
        => RunStrategy<FifteenMinuteScalperStrategy>();

    [TestMethod]
    public Task FiftyFiveMaBarComparison()
        => RunStrategy<FiftyFiveMaBarComparisonStrategy>();

    [TestMethod]
    public Task FiftyFiveMedianSlope()
        => RunStrategy<FiftyFiveMedianSlopeStrategy>();

    [TestMethod]
    public Task FigurelliSeries()
        => RunStrategy<FigurelliSeriesStrategy>();

    [TestMethod]
    public Task FinancialRatiosFundamental()
        => RunStrategy<FinancialRatiosFundamentalStrategy>();

    [TestMethod]
    public Task FineClock()
        => RunStrategy<FineClockStrategy>();

    [TestMethod]
    public Task FineTuneGannLaplaceVzo()
        => RunStrategy<FineTuneGannLaplaceVzoStrategy>();

    [TestMethod]
    public Task FineTuneInputsFourierSmoothedHybridVolumeSpreadAnalysis()
        => RunStrategy<FineTuneInputsFourierSmoothedHybridVolumeSpreadAnalysisStrategy>();

    [TestMethod]
    public Task FineTuningMaCandleDuplex()
        => RunStrategy<FineTuningMaCandleDuplexStrategy>();

    [TestMethod]
    public Task FineTuningMa()
        => RunStrategy<FineTuningMaStrategy>();

    [TestMethod]
    public Task FinishRenaming()
        => RunStrategy<FinishRenamingStrategy>();

    [TestMethod]
    public Task FirebirdChannelAveraging()
        => RunStrategy<FirebirdChannelAveragingStrategy>();

    [TestMethod]
    public Task FirebirdMaEnvelopeExhaustion()
        => RunStrategy<FirebirdMaEnvelopeExhaustionStrategy>();

    [TestMethod]
    public Task FirstFriday()
        => RunStrategy<FirstFridayStrategy>();

    [TestMethod]
    public Task FisherCrossover()
        => RunStrategy<FisherCrossoverStrategy>();

    [TestMethod]
    public Task FisherCyberCycle()
        => RunStrategy<FisherCyberCycleStrategy>();

    [TestMethod]
    public Task FisherOrgSign()
        => RunStrategy<FisherOrgSignStrategy>();

    [TestMethod]
    public Task FisherOrgV1()
        => RunStrategy<FisherOrgV1Strategy>();

    [TestMethod]
    public Task FisherTransformX2()
        => RunStrategy<FisherTransformX2Strategy>();

    [TestMethod]
    public Task FitFul13()
        => RunStrategy<FitFul13Strategy>();

    [TestMethod]
    public Task FitFul13TimeGated()
        => RunStrategy<FitFul13TimeGatedStrategy>();

    [TestMethod]
    public Task FiveEightMaCrossProtect()
        => RunStrategy<FiveEightMaCrossProtectStrategy>();

    [TestMethod]
    public Task FiveEightMaCross()
        => RunStrategy<FiveEightMaCrossStrategy>();

    [TestMethod]
    public Task FiveEmaNoTouchBreakout()
        => RunStrategy<FiveEmaNoTouchBreakoutStrategy>();

    [TestMethod]
    public Task FiveEma()
        => RunStrategy<FiveEmaStrategy>();

    [TestMethod]
    public Task FiveMaMultiTimeframe()
        => RunStrategy<FiveMaMultiTimeframeStrategy>();

    [TestMethod]
    public Task FiveMinRsiQualified()
        => RunStrategy<FiveMinRsiQualifiedStrategy>();

    [TestMethod]
    public Task FiveMinScalping()
        => RunStrategy<FiveMinScalpingStrategy>();

    [TestMethod]
    public Task FiveMinsEnvelopes()
        => RunStrategy<FiveMinsEnvelopesStrategy>();

    [TestMethod]
    public Task FiveMinuteRsiCci()
        => RunStrategy<FiveMinuteRsiCciStrategy>();

    [TestMethod]
    public Task FiveMinutesScalpingEaV11()
        => RunStrategy<FiveMinutesScalpingEaV11Strategy>();

    [TestMethod]
    public Task FlashMinerviniQualifier()
        => RunStrategy<FlashMinerviniQualifierStrategy>();

    [TestMethod]
    public Task Flat001a()
        => RunStrategy<Flat001aStrategy>();

    [TestMethod]
    public Task FlatChannelBreakout()
        => RunStrategy<FlatChannelBreakoutStrategy>();

    [TestMethod]
    public Task FlatChannel()
        => RunStrategy<FlatChannelStrategy>();

    [TestMethod]
    public Task FlatTrendEa()
        => RunStrategy<FlatTrendEaStrategy>();

    [TestMethod]
    public Task FlatTrend()
        => RunStrategy<FlatTrendStrategy>();

    [TestMethod]
    public Task FlexAtr()
        => RunStrategy<FlexAtrStrategy>();

    [TestMethod]
    public Task FlexiMaVarianceTracker()
        => RunStrategy<FlexiMaVarianceTrackerStrategy>();

    [TestMethod]
    public Task FlexiMaXFlexiSt()
        => RunStrategy<FlexiMaXFlexiStStrategy>();

    [TestMethod]
    public Task FlexiSuperTrend()
        => RunStrategy<FlexiSuperTrendStrategy>();

    [TestMethod]
    public Task FlexibleMovingAverage()
        => RunStrategy<FlexibleMovingAverageStrategy>();

    [TestMethod]
    public Task Fluctuate()
        => RunStrategy<FluctuateStrategy>();

    [TestMethod]
    public Task FlySystemScalp()
        => RunStrategy<FlySystemScalpStrategy>();

    [TestMethod]
    public Task FmOneScalping()
        => RunStrategy<FmOneScalpingStrategy>();

    [TestMethod]
    public Task FollowLine()
        => RunStrategy<FollowLineStrategy>();

    [TestMethod]
    public Task FollowLineTrend()
        => RunStrategy<FollowLineTrendStrategy>();

    [TestMethod]
    public Task FollowYourHeart()
        => RunStrategy<FollowYourHeartStrategy>();

    [TestMethod]
    public Task Fon60Dk()
        => RunStrategy<Fon60DkStrategy>();

    [TestMethod]
    public Task Footprint()
        => RunStrategy<FootprintStrategy>();

    [TestMethod]
    public Task ForMaxV2()
        => RunStrategy<ForMaxV2Strategy>();

    [TestMethod]
    public Task ForceDiverSign()
        => RunStrategy<ForceDiverSignStrategy>();

    [TestMethod]
    public Task ForceTrend()
        => RunStrategy<ForceTrendStrategy>();

    [TestMethod]
    public Task ForecastOscillator()
        => RunStrategy<ForecastOscillatorStrategy>();

    [TestMethod]
    public Task ForexFireEmaMaRsi()
        => RunStrategy<ForexFireEmaMaRsiStrategy>();

    [TestMethod]
    public Task ForexFraus4ForM1s()
        => RunStrategy<ForexFraus4ForM1sStrategy>();

    [TestMethod]
    public Task ForexFrausM1()
        => RunStrategy<ForexFrausM1Strategy>();

    [TestMethod]
    public Task ForexFrausPortfolio()
        => RunStrategy<ForexFrausPortfolioStrategy>();

    [TestMethod]
    public Task ForexFrausSlogger()
        => RunStrategy<ForexFrausSloggerStrategy>();

    [TestMethod]
    public Task ForexHammerAndHangingMan()
        => RunStrategy<ForexHammerAndHangingManStrategy>();

    [TestMethod]
    public Task ForexLine()
        => RunStrategy<ForexLineStrategy>();

    [TestMethod]
    public Task ForexPairYieldMomentum()
        => RunStrategy<ForexPairYieldMomentumStrategy>();

    [TestMethod]
    public Task ForexProfitBoost()
        => RunStrategy<ForexProfitBoostStrategy>();

    [TestMethod]
    public Task ForexProfit()
        => RunStrategy<ForexProfitStrategy>();

    [TestMethod]
    public Task ForexProfitSystem()
        => RunStrategy<ForexProfitSystemStrategy>();

    [TestMethod]
    public Task ForexSky()
        => RunStrategy<ForexSkyStrategy>();

    [TestMethod]
    public Task Fortrader10Pips()
        => RunStrategy<Fortrader10PipsStrategy>();

    [TestMethod]
    public Task FourBarMomentumReversal()
        => RunStrategy<FourBarMomentumReversalStrategy>();

    [TestMethod]
    public Task FourHourSwing()
        => RunStrategy<FourHourSwingStrategy>();

    [TestMethod]
    public Task FourScreens()
        => RunStrategy<FourScreensStrategy>();

    [TestMethod]
    public Task FourSma()
        => RunStrategy<FourSmaStrategy>();

    [TestMethod]
    public Task FourWmaTpSl()
        => RunStrategy<FourWmaTpSlStrategy>();

    [TestMethod]
    public Task FourierSmoothedVzo()
        => RunStrategy<FourierSmoothedVzoStrategy>();

    [TestMethod]
    public Task FrBestExp02MalomaMod()
        => RunStrategy<FrBestExp02MalomaModStrategy>();

    [TestMethod]
    public Task FractalAdxCloud()
        => RunStrategy<FractalAdxCloudStrategy>();

    [TestMethod]
    public Task FractalAmaMbk()
        => RunStrategy<FractalAmaMbkStrategy>();

    [TestMethod]
    public Task FractalBreakoutTrendFollowing()
        => RunStrategy<FractalBreakoutTrendFollowingStrategy>();

    [TestMethod]
    public Task FractalForceIndex()
        => RunStrategy<FractalForceIndexStrategy>();

    [TestMethod]
    public Task FractalIdentifier20()
        => RunStrategy<FractalIdentifier20Strategy>();

    [TestMethod]
    public Task FractalMfi()
        => RunStrategy<FractalMfiStrategy>();

    [TestMethod]
    public Task FractalRsi()
        => RunStrategy<FractalRsiStrategy>();

    [TestMethod]
    public Task FractalWeightOscillator()
        => RunStrategy<FractalWeightOscillatorStrategy>();

    [TestMethod]
    public Task FractalWpr()
        => RunStrategy<FractalWprStrategy>();

    [TestMethod]
    public Task FractalZigZag()
        => RunStrategy<FractalZigZagStrategy>();

    [TestMethod]
    public Task FractalsAlligator()
        => RunStrategy<FractalsAlligatorStrategy>();

    [TestMethod]
    public Task FractalsAtClosePrices()
        => RunStrategy<FractalsAtClosePricesStrategy>();

    [TestMethod]
    public Task FractalsMartingale()
        => RunStrategy<FractalsMartingaleStrategy>();

}
