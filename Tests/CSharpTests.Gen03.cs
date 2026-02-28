namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task AutotradePendingStops()
        => RunStrategy<AutotradePendingStopsStrategy>();

    [TestMethod]
    public Task AutotraderMomentum()
        => RunStrategy<AutotraderMomentumStrategy>();

    [TestMethod]
    public Task AvalancheAv()
        => RunStrategy<AvalancheAvStrategy>();

    [TestMethod]
    public Task Avalanche()
        => RunStrategy<AvalancheStrategy>();

    [TestMethod]
    public Task Aver4StochPostZigZag()
        => RunStrategy<Aver4StochPostZigZagStrategy>();

    [TestMethod]
    public Task AverageCandleCross()
        => RunStrategy<AverageCandleCrossStrategy>();

    [TestMethod]
    public Task AverageChangeCandle()
        => RunStrategy<AverageChangeCandleStrategy>();

    [TestMethod]
    public Task AverageForce()
        => RunStrategy<AverageForceStrategy>();

    [TestMethod]
    public Task AverageHighLowRangeIbsReversal()
        => RunStrategy<AverageHighLowRangeIbsReversalStrategy>();

    [TestMethod]
    public Task AveragePipMovementTickSeconds()
        => RunStrategy<AveragePipMovementTickSecondsStrategy>();

    [TestMethod]
    public Task AveragedStochWpr()
        => RunStrategy<AveragedStochWprStrategy>();

    [TestMethod]
    public Task AveragingBySignal()
        => RunStrategy<AveragingBySignalStrategy>();

    [TestMethod]
    public Task AveragingDown2()
        => RunStrategy<AveragingDown2Strategy>();

    [TestMethod]
    public Task AveragingDown()
        => RunStrategy<AveragingDownStrategy>();

    [TestMethod]
    public Task AwesomeFxTrader()
        => RunStrategy<AwesomeFxTraderStrategy>();

    [TestMethod]
    public Task AwesomeOscTrader()
        => RunStrategy<AwesomeOscTraderStrategy>();

    [TestMethod]
    public Task AwesomeOscillatorTrader()
        => RunStrategy<AwesomeOscillatorTraderStrategy>();

    [TestMethod]
    public Task BB()
        => RunStrategy<BBStrategy>();

    [TestMethod]
    public Task BBandsStop()
        => RunStrategy<BBandsStopStrategy>();

    [TestMethod]
    public Task BWWiseMan2()
        => RunStrategy<BWWiseMan2Strategy>();

    [TestMethod]
    public Task BabySharkVwap()
        => RunStrategy<BabySharkVwapStrategy>();

    [TestMethod]
    public Task BackKick()
        => RunStrategy<BackKickStrategy>();

    [TestMethod]
    public Task BackToTheFuture()
        => RunStrategy<BackToTheFutureStrategy>();

    [TestMethod]
    public Task BackboneBasket()
        => RunStrategy<BackboneBasketStrategy>();

    [TestMethod]
    public Task Backbone()
        => RunStrategy<BackboneStrategy>();

    [TestMethod]
    public Task BacktestUtBotRsi()
        => RunStrategy<BacktestUtBotRsiStrategy>();

    [TestMethod]
    public Task BacktestingModule()
        => RunStrategy<BacktestingModuleStrategy>();

    [TestMethod]
    public Task BacktestingTradeAssistantPanel()
        => RunStrategy<BacktestingTradeAssistantPanelStrategy>();

    [TestMethod]
    public Task BackwardNumberOfBars()
        => RunStrategy<BackwardNumberOfBarsStrategy>();

    [TestMethod]
    public Task BadOrders()
        => RunStrategy<BadOrdersStrategy>();

    [TestMethod]
    public Task BadxAdxBollinger()
        => RunStrategy<BadxAdxBollingerStrategy>();

    [TestMethod]
    public Task BagoEaClassic()
        => RunStrategy<BagoEaClassicStrategy>();

    [TestMethod]
    public Task BagoEa()
        => RunStrategy<BagoEaStrategy>();

    [TestMethod]
    public Task BalanceDrawdownInMt4()
        => RunStrategy<BalanceDrawdownInMt4Strategy>();

    [TestMethod]
    public Task BalanceOfPowerHistogram()
        => RunStrategy<BalanceOfPowerHistogramStrategy>();

    [TestMethod]
    public Task BalanceOfPower()
        => RunStrategy<BalanceOfPowerStrategy>();

    [TestMethod]
    public Task BandOsMaCustom()
        => RunStrategy<BandOsMaCustomStrategy>();

    [TestMethod]
    public Task BandOsMa()
        => RunStrategy<BandOsMaStrategy>();

    [TestMethod]
    public Task BandsPendingBreakout()
        => RunStrategy<BandsPendingBreakoutStrategy>();

    [TestMethod]
    public Task BandsPrice()
        => RunStrategy<BandsPriceStrategy>();

    [TestMethod]
    public Task Bands()
        => RunStrategy<BandsStrategy>();

    [TestMethod]
    public Task BarBalance()
        => RunStrategy<BarBalanceStrategy>();

    [TestMethod]
    public Task BarCounterTrendReversal()
        => RunStrategy<BarCounterTrendReversalStrategy>();

    [TestMethod]
    public Task BarRange()
        => RunStrategy<BarRangeStrategy>();

    [TestMethod]
    public Task BarsAlligator()
        => RunStrategy<BarsAlligatorStrategy>();

    [TestMethod]
    public Task Baseline2()
        => RunStrategy<Baseline2Strategy>();

    [TestMethod]
    public Task BasicAtrStopTake()
        => RunStrategy<BasicAtrStopTakeStrategy>();

    [TestMethod]
    public Task BasicCciRsi()
        => RunStrategy<BasicCciRsiStrategy>();

    [TestMethod]
    public Task BasicMaTemplate()
        => RunStrategy<BasicMaTemplateStrategy>();

    [TestMethod]
    public Task BasicMartingaleEa3()
        => RunStrategy<BasicMartingaleEa3Strategy>();

    [TestMethod]
    public Task BasicRsiEaTemplate()
        => RunStrategy<BasicRsiEaTemplateStrategy>();

    [TestMethod]
    public Task BasicTrailingStop()
        => RunStrategy<BasicTrailingStopStrategy>();

    [TestMethod]
    public Task BasketClose()
        => RunStrategy<BasketCloseStrategy>();

    [TestMethod]
    public Task BatmanAtrTrailingStop()
        => RunStrategy<BatmanAtrTrailingStopStrategy>();

    [TestMethod]
    public Task BayesianBbsmaOscillator()
        => RunStrategy<BayesianBbsmaOscillatorStrategy>();

    [TestMethod]
    public Task BbBreakoutMomentumSqueeze()
        => RunStrategy<BbBreakoutMomentumSqueezeStrategy>();

    [TestMethod]
    public Task BbHeikinAshiEntry()
        => RunStrategy<BbHeikinAshiEntryStrategy>();

    [TestMethod]
    public Task BbRsi()
        => RunStrategy<BbRsiStrategy>();

    [TestMethod]
    public Task BbRsiTrailingStop()
        => RunStrategy<BbRsiTrailingStopStrategy>();

    [TestMethod]
    public Task BbSqueeze()
        => RunStrategy<BbSqueezeStrategy>();

    [TestMethod]
    public Task BbSwing()
        => RunStrategy<BbSwingStrategy>();

    [TestMethod]
    public Task BbsrExtreme()
        => RunStrategy<BbsrExtremeStrategy>();

    [TestMethod]
    public Task BbtrendSupertrendDecision()
        => RunStrategy<BbtrendSupertrendDecisionStrategy>();

    [TestMethod]
    public Task BearBullsPower()
        => RunStrategy<BearBullsPowerStrategy>();

    [TestMethod]
    public Task BearishWickReversal()
        => RunStrategy<BearishWickReversalStrategy>();

    [TestMethod]
    public Task BedoOsaimiIstr()
        => RunStrategy<BedoOsaimiIstrStrategy>();

    [TestMethod]
    public Task BeerGodEmaTiming()
        => RunStrategy<BeerGodEmaTimingStrategy>();

    [TestMethod]
    public Task BeginnerBreakout()
        => RunStrategy<BeginnerBreakoutStrategy>();

    [TestMethod]
    public Task Bench()
        => RunStrategy<BenchStrategy>();

    [TestMethod]
    public Task BerlinCandles()
        => RunStrategy<BerlinCandlesStrategy>();

    [TestMethod]
    public Task BerlinRangeIndex()
        => RunStrategy<BerlinRangeIndexStrategy>();

    [TestMethod]
    public Task BestDollarCostAverage()
        => RunStrategy<BestDollarCostAverageStrategy>();

    [TestMethod]
    public Task BetaWeightedMa()
        => RunStrategy<BetaWeightedMaStrategy>();

    [TestMethod]
    public Task BezierReOpen()
        => RunStrategy<BezierReOpenStrategy>();

    [TestMethod]
    public Task BezierStDev()
        => RunStrategy<BezierStDevStrategy>();

    [TestMethod]
    public Task BhsSystem()
        => RunStrategy<BhsSystemStrategy>();

    [TestMethod]
    public Task BiasRatio()
        => RunStrategy<BiasRatioStrategy>();

    [TestMethod]
    public Task BiasSentimentStrength()
        => RunStrategy<BiasSentimentStrengthStrategy>();

    [TestMethod]
    public Task BigBarSound()
        => RunStrategy<BigBarSoundStrategy>();

    [TestMethod]
    public Task BigCandleRsiDivergence()
        => RunStrategy<BigCandleRsiDivergenceStrategy>();

    [TestMethod]
    public Task BigDog()
        => RunStrategy<BigDogStrategy>();

    [TestMethod]
    public Task BigMoverCatcher()
        => RunStrategy<BigMoverCatcherStrategy>();

    [TestMethod]
    public Task BigRunner()
        => RunStrategy<BigRunnerStrategy>();

    [TestMethod]
    public Task BillWilliamsAlligator()
        => RunStrategy<BillWilliamsAlligatorStrategy>();

    [TestMethod]
    public Task BillWilliams()
        => RunStrategy<BillWilliamsStrategy>();

    [TestMethod]
    public Task BillWilliamsTrader()
        => RunStrategy<BillWilliamsTraderStrategy>();

    [TestMethod]
    public Task BillyExpertReversal()
        => RunStrategy<BillyExpertReversalStrategy>();

    [TestMethod]
    public Task BillyExpert()
        => RunStrategy<BillyExpertStrategy>();

    [TestMethod]
    public Task Binario31()
        => RunStrategy<Binario31Strategy>();

    [TestMethod]
    public Task Binario3()
        => RunStrategy<Binario3Strategy>();

    [TestMethod]
    public Task Binario()
        => RunStrategy<BinarioStrategy>();

    [TestMethod]
    public Task BinaryWaveStdDev()
        => RunStrategy<BinaryWaveStdDevStrategy>();

    [TestMethod]
    public Task BinaryWave()
        => RunStrategy<BinaryWaveStrategy>();

    [TestMethod]
    public Task BinomialOptionPricingModel()
        => RunStrategy<BinomialOptionPricingModelStrategy>();

    [TestMethod]
    public Task Bitcoin1H15MBreakout()
        => RunStrategy<Bitcoin1H15MBreakoutStrategy>();

    [TestMethod]
    public Task BitcoinBullishPercentIndex()
        => RunStrategy<BitcoinBullishPercentIndexStrategy>();

    [TestMethod]
    public Task BitcoinCmeSpotSpread()
        => RunStrategy<BitcoinCmeSpotSpreadStrategy>();

    [TestMethod]
    public Task BitcoinExponentialProfit()
        => RunStrategy<BitcoinExponentialProfitStrategy>();

    [TestMethod]
    public Task BitcoinFuturesSpotTriFrame()
        => RunStrategy<BitcoinFuturesSpotTriFrameStrategy>();

    [TestMethod]
    public Task BitcoinLeverageSentiment()
        => RunStrategy<BitcoinLeverageSentimentStrategy>();

}
