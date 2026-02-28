namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task SessionBreakout()
        => RunStrategy<SessionBreakoutStrategy>();

    [TestMethod]
    public Task SessionInputParser()
        => RunStrategy<SessionInputParserStrategy>();

    [TestMethod]
    public Task SessionOrderSentiment()
        => RunStrategy<SessionOrderSentimentStrategy>();

    [TestMethod]
    public Task SetupSmoothGaussianAdaptiveSupertrendManualVol()
        => RunStrategy<SetupSmoothGaussianAdaptiveSupertrendManualVolStrategy>();

    [TestMethod]
    public Task SharpeRatioForcedSelling()
        => RunStrategy<SharpeRatioForcedSellingStrategy>();

    [TestMethod]
    public Task SheKanskigorDaily()
        => RunStrategy<SheKanskigorDailyStrategy>();

    [TestMethod]
    public Task SheKanskigor()
        => RunStrategy<SheKanskigorStrategy>();

    [TestMethod]
    public Task ShortOnly10BarLowPullback()
        => RunStrategy<ShortOnly10BarLowPullbackStrategy>();

    [TestMethod]
    public Task ShurikenLite()
        => RunStrategy<ShurikenLiteStrategy>();

    [TestMethod]
    public Task SidusAlligator()
        => RunStrategy<SidusAlligatorStrategy>();

    [TestMethod]
    public Task SidusEmaRsi()
        => RunStrategy<SidusEmaRsiStrategy>();

    [TestMethod]
    public Task Sidus()
        => RunStrategy<SidusStrategy>();

    [TestMethod]
    public Task SidusV1()
        => RunStrategy<SidusV1Strategy>();

    [TestMethod]
    public Task SigmaSpikeFilteredBinnedOpr()
        => RunStrategy<SigmaSpikeFilteredBinnedOprStrategy>();

    [TestMethod]
    public Task SignalCountWithArray()
        => RunStrategy<SignalCountWithArrayStrategy>();

    [TestMethod]
    public Task SignalTester()
        => RunStrategy<SignalTesterStrategy>();

    [TestMethod]
    public Task SilverMidnightCandleColor()
        => RunStrategy<SilverMidnightCandleColorStrategy>();

    [TestMethod]
    public Task SilverTrendColorJFatlDigit()
        => RunStrategy<SilverTrendColorJFatlDigitStrategy>();

    [TestMethod]
    public Task SilverTrendColorJfatlDigitMmrec()
        => RunStrategy<SilverTrendColorJfatlDigitMmrecStrategy>();

    [TestMethod]
    public Task SilverTrendCrazyChart()
        => RunStrategy<SilverTrendCrazyChartStrategy>();

    [TestMethod]
    public Task SilverTrendDuplex()
        => RunStrategy<SilverTrendDuplexStrategy>();

    [TestMethod]
    public Task SilverTrendSignalReOpen()
        => RunStrategy<SilverTrendSignalReOpenStrategy>();

    [TestMethod]
    public Task SilverTrend()
        => RunStrategy<SilverTrendStrategy>();

    [TestMethod]
    public Task SilverTrendV3Jtpo()
        => RunStrategy<SilverTrendV3JtpoStrategy>();

    [TestMethod]
    public Task SilverTrendV3()
        => RunStrategy<SilverTrendV3Strategy>();

    [TestMethod]
    public Task SimilarityMeasures()
        => RunStrategy<SimilarityMeasuresStrategy>();

    [TestMethod]
    public Task Simple2MaI()
        => RunStrategy<Simple2MaIStrategy>();

    [TestMethod]
    public Task SimpleApfBacktesting()
        => RunStrategy<SimpleApfBacktestingStrategy>();

    [TestMethod]
    public Task SimpleBars()
        => RunStrategy<SimpleBarsStrategy>();

    [TestMethod]
    public Task SimpleDca()
        => RunStrategy<SimpleDcaStrategy>();

    [TestMethod]
    public Task SimpleEaMaPlusMacd()
        => RunStrategy<SimpleEaMaPlusMacdStrategy>();

    [TestMethod]
    public Task SimpleEmaCrossover()
        => RunStrategy<SimpleEmaCrossoverStrategy>();

    [TestMethod]
    public Task SimpleEngulfing()
        => RunStrategy<SimpleEngulfingStrategy>();

    [TestMethod]
    public Task SimpleFibonacciRetracement()
        => RunStrategy<SimpleFibonacciRetracementStrategy>();

    [TestMethod]
    public Task SimpleForecastKeltnerWorms()
        => RunStrategy<SimpleForecastKeltnerWormsStrategy>();

    [TestMethod]
    public Task SimpleFxCrossover()
        => RunStrategy<SimpleFxCrossoverStrategy>();

    [TestMethod]
    public Task SimpleFx()
        => RunStrategy<SimpleFxStrategy>();

    [TestMethod]
    public Task SimpleHedgePanel()
        => RunStrategy<SimpleHedgePanelStrategy>();

    [TestMethod]
    public Task SimpleLevels()
        => RunStrategy<SimpleLevelsStrategy>();

    [TestMethod]
    public Task SimpleMaAdxEa()
        => RunStrategy<SimpleMaAdxEaStrategy>();

    [TestMethod]
    public Task SimpleMacdEa()
        => RunStrategy<SimpleMacdEaStrategy>();

    [TestMethod]
    public Task SimpleMacd()
        => RunStrategy<SimpleMacdStrategy>();

    [TestMethod]
    public Task SimpleMartingaleTemplate()
        => RunStrategy<SimpleMartingaleTemplateStrategy>();

    [TestMethod]
    public Task SimpleMultipleTimeFrameMovingAverage()
        => RunStrategy<SimpleMultipleTimeFrameMovingAverageStrategy>();

    [TestMethod]
    public Task SimpleNews()
        => RunStrategy<SimpleNewsStrategy>();

    [TestMethod]
    public Task SimpleOrderPanel()
        => RunStrategy<SimpleOrderPanelStrategy>();

    [TestMethod]
    public Task SimplePivotFlip()
        => RunStrategy<SimplePivotFlipStrategy>();

    [TestMethod]
    public Task SimplePivot()
        => RunStrategy<SimplePivotStrategy>();

    [TestMethod]
    public Task SimpleProfitByPeriodsPanel2Extended()
        => RunStrategy<SimpleProfitByPeriodsPanel2ExtendedStrategy>();

    [TestMethod]
    public Task SimplePullBackTjlv26()
        => RunStrategy<SimplePullBackTjlv26Strategy>();

    [TestMethod]
    public Task SimpleRsiStock1D()
        => RunStrategy<SimpleRsiStock1DStrategy>();

    [TestMethod]
    public Task Simple()
        => RunStrategy<SimpleStrategy>();

    [TestMethod]
    public Task SimpleTradeFlip()
        => RunStrategy<SimpleTradeFlipStrategy>();

    [TestMethod]
    public Task SimpleTrade()
        => RunStrategy<SimpleTradeStrategy>();

    [TestMethod]
    public Task SimpleTradingSystem()
        => RunStrategy<SimpleTradingSystemStrategy>();

    [TestMethod]
    public Task SimpleTrailingStop()
        => RunStrategy<SimpleTrailingStopStrategy>();

    [TestMethod]
    public Task SimpleTrendlines()
        => RunStrategy<SimpleTrendlinesStrategy>();

    [TestMethod]
    public Task SimplestDeMarker()
        => RunStrategy<SimplestDeMarkerStrategy>();

    [TestMethod]
    public Task SimplifiedGapWithSmaFilter()
        => RunStrategy<SimplifiedGapWithSmaFilterStrategy>();

    [TestMethod]
    public Task SimplisticAutomaticGrowthModels()
        => RunStrategy<SimplisticAutomaticGrowthModelsStrategy>();

    [TestMethod]
    public Task Simulator()
        => RunStrategy<SimulatorStrategy>();

    [TestMethod]
    public Task SixIndicatorsMomentum()
        => RunStrategy<SixIndicatorsMomentumStrategy>();

    [TestMethod]
    public Task SjNifty()
        => RunStrategy<SjNiftyStrategy>();

    [TestMethod]
    public Task SlimeMoldRsi()
        => RunStrategy<SlimeMoldRsiStrategy>();

    [TestMethod]
    public Task SlopeDirectionLine()
        => RunStrategy<SlopeDirectionLineStrategy>();

    [TestMethod]
    public Task SlopeRsiMtf()
        => RunStrategy<SlopeRsiMtfStrategy>();

    [TestMethod]
    public Task SlowStochasticMode()
        => RunStrategy<SlowStochasticModeStrategy>();

    [TestMethod]
    public Task SmaBuffer()
        => RunStrategy<SmaBufferStrategy>();

    [TestMethod]
    public Task SmaCrossover()
        => RunStrategy<SmaCrossoverStrategy>();

    [TestMethod]
    public Task SmaMultiHedge2()
        => RunStrategy<SmaMultiHedge2Strategy>();

    [TestMethod]
    public Task SmaPullbackAtrExits()
        => RunStrategy<SmaPullbackAtrExitsStrategy>();

    [TestMethod]
    public Task SmaRsiVolumeAtr()
        => RunStrategy<SmaRsiVolumeAtrStrategy>();

    [TestMethod]
    public Task SmaSlopeDynamicTpSl()
        => RunStrategy<SmaSlopeDynamicTpSlStrategy>();

    [TestMethod]
    public Task SmaTrendFilter()
        => RunStrategy<SmaTrendFilterStrategy>();

    [TestMethod]
    public Task SmallInsideBar()
        => RunStrategy<SmallInsideBarStrategy>();

    [TestMethod]
    public Task SmartAcTrader()
        => RunStrategy<SmartAcTraderStrategy>();

    [TestMethod]
    public Task SmartAssTrade()
        => RunStrategy<SmartAssTradeStrategy>();

    [TestMethod]
    public Task SmartAssTradeV2()
        => RunStrategy<SmartAssTradeV2Strategy>();

    [TestMethod]
    public Task SmartFib()
        => RunStrategy<SmartFibStrategy>();

    [TestMethod]
    public Task SmartForexSystem()
        => RunStrategy<SmartForexSystemStrategy>();

    [TestMethod]
    public Task SmartGridScalpingPullback()
        => RunStrategy<SmartGridScalpingPullbackStrategy>();

    [TestMethod]
    public Task SmartMaCrossoverBacktester()
        => RunStrategy<SmartMaCrossoverBacktesterStrategy>();

    [TestMethod]
    public Task SmartMoneyConceptUncleSam()
        => RunStrategy<SmartMoneyConceptUncleSamStrategy>();

    [TestMethod]
    public Task SmartMoneyPivot()
        => RunStrategy<SmartMoneyPivotStrategy>();

    [TestMethod]
    public Task SmartScaleEnvelopeDca()
        => RunStrategy<SmartScaleEnvelopeDcaStrategy>();

    [TestMethod]
    public Task SmartTrendFollower()
        => RunStrategy<SmartTrendFollowerStrategy>();

    [TestMethod]
    public Task SmbMagic()
        => RunStrategy<SmbMagicStrategy>();

    [TestMethod]
    public Task SmcBbBreakout()
        => RunStrategy<SmcBbBreakoutStrategy>();

    [TestMethod]
    public Task SmcBtc1HObFvg()
        => RunStrategy<SmcBtc1HObFvgStrategy>();

    [TestMethod]
    public Task SmcHiloMaxMin()
        => RunStrategy<SmcHiloMaxMinStrategy>();

    [TestMethod]
    public Task SmcOrderBlockZones()
        => RunStrategy<SmcOrderBlockZonesStrategy>();

    [TestMethod]
    public Task Smc()
        => RunStrategy<SmcStrategy>();

    [TestMethod]
    public Task SmcTraderCamelCciMacd1()
        => RunStrategy<SmcTraderCamelCciMacd1Strategy>();

    [TestMethod]
    public Task SmiCorrect()
        => RunStrategy<SmiCorrectStrategy>();

    [TestMethod]
    public Task SmoothedHeikenAshiLongOnly()
        => RunStrategy<SmoothedHeikenAshiLongOnlyStrategy>();

    [TestMethod]
    public Task SmoothedHeikenAshi()
        => RunStrategy<SmoothedHeikenAshiStrategy>();

    [TestMethod]
    public Task SmoothedMaDirectional()
        => RunStrategy<SmoothedMaDirectionalStrategy>();

    [TestMethod]
    public Task SmoothingAverageCrossover()
        => RunStrategy<SmoothingAverageCrossoverStrategy>();

    [TestMethod]
    public Task SmoothingAverage()
        => RunStrategy<SmoothingAverageStrategy>();

    [TestMethod]
    public Task SmuStdevCandles()
        => RunStrategy<SmuStdevCandlesStrategy>();

}
