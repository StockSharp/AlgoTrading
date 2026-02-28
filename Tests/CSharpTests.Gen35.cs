namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task Vwap()
        => RunStrategy<VwapStrategy>();

    [TestMethod]
    public Task WaddahAttarTrend()
        => RunStrategy<WaddahAttarTrendStrategy>();

    [TestMethod]
    public Task WaddahAttarWinGrid()
        => RunStrategy<WaddahAttarWinGridStrategy>();

    [TestMethod]
    public Task WaddahAttarWin()
        => RunStrategy<WaddahAttarWinStrategy>();

    [TestMethod]
    public Task WaindropsMakit0()
        => RunStrategy<WaindropsMakit0Strategy>();

    [TestMethod]
    public Task WajdyssIchimokuCandleMmrec()
        => RunStrategy<WajdyssIchimokuCandleMmrecStrategy>();

    [TestMethod]
    public Task WajdyssMaExpert()
        => RunStrategy<WajdyssMaExpertStrategy>();

    [TestMethod]
    public Task WamiCloudX2()
        => RunStrategy<WamiCloudX2Strategy>();

    [TestMethod]
    public Task WarriorTradingMomentum()
        => RunStrategy<WarriorTradingMomentumStrategy>();

    [TestMethod]
    public Task WavePowerEA()
        => RunStrategy<WavePowerEAStrategy>();

    [TestMethod]
    public Task WeTrustChannel()
        => RunStrategy<WeTrustChannelStrategy>();

    [TestMethod]
    public Task WedgePattern()
        => RunStrategy<WedgePatternStrategy>();

    [TestMethod]
    public Task WeeklyReboundCorridor()
        => RunStrategy<WeeklyReboundCorridorStrategy>();

    [TestMethod]
    public Task WeightOscillatorDirect()
        => RunStrategy<WeightOscillatorDirectStrategy>();

    [TestMethod]
    public Task WeightOscillator()
        => RunStrategy<WeightOscillatorStrategy>();

    [TestMethod]
    public Task WeightedHarrellDavisQuantileEstimatorWithAbsoluteDeviation()
        => RunStrategy<WeightedHarrellDavisQuantileEstimatorWithAbsoluteDeviationStrategy>();

    [TestMethod]
    public Task WeightedIchimoku()
        => RunStrategy<WeightedIchimokuStrategy>();

    [TestMethod]
    public Task WeightedStandardDeviation()
        => RunStrategy<WeightedStandardDeviationStrategy>();

    [TestMethod]
    public Task WellMartin()
        => RunStrategy<WellMartinStrategy>();

    [TestMethod]
    public Task WilliamsAlligatorAtr()
        => RunStrategy<WilliamsAlligatorAtrStrategy>();

    [TestMethod]
    public Task WilliamsAoAc()
        => RunStrategy<WilliamsAoAcStrategy>();

    [TestMethod]
    public Task WilliamsFractalTrailingStops()
        => RunStrategy<WilliamsFractalTrailingStopsStrategy>();

    [TestMethod]
    public Task WilliamsPercentDirectionalIndex()
        => RunStrategy<WilliamsPercentDirectionalIndexStrategy>();

    [TestMethod]
    public Task WilliamsRCrossWith200MaFilter()
        => RunStrategy<WilliamsRCrossWith200MaFilterStrategy>();

    [TestMethod]
    public Task WilliamsR()
        => RunStrategy<WilliamsRStrategy>();

    [TestMethod]
    public Task WilliamsRZoneScalper()
        => RunStrategy<WilliamsRZoneScalperStrategy>();

    [TestMethod]
    public Task WlxBw5Zone()
        => RunStrategy<WlxBw5ZoneStrategy>();

    [TestMethod]
    public Task Woc012()
        => RunStrategy<Woc012Strategy>();

    [TestMethod]
    public Task WodismaTripleMaCrossover()
        => RunStrategy<WodismaTripleMaCrossoverStrategy>();

    [TestMethod]
    public Task WprCustomCloudSimple()
        => RunStrategy<WprCustomCloudSimpleStrategy>();

    [TestMethod]
    public Task WprHistogram()
        => RunStrategy<WprHistogramStrategy>();

    [TestMethod]
    public Task WprLevelCross()
        => RunStrategy<WprLevelCrossStrategy>();

    [TestMethod]
    public Task WprSlowdown()
        => RunStrategy<WprSlowdownStrategy>();

    [TestMethod]
    public Task WprsiSignal()
        => RunStrategy<WprsiSignalStrategy>();

    [TestMethod]
    public Task WssTrader()
        => RunStrategy<WssTraderStrategy>();

    [TestMethod]
    public Task X2MADigitDm361()
        => RunStrategy<X2MADigitDm361Strategy>();

    [TestMethod]
    public Task X2MaJfatl()
        => RunStrategy<X2MaJfatlStrategy>();

    [TestMethod]
    public Task X2maJjrsx()
        => RunStrategy<X2maJjrsxStrategy>();

    [TestMethod]
    public Task X3MaEaV20()
        => RunStrategy<X3MaEaV20Strategy>();

    [TestMethod]
    public Task XAlert3()
        => RunStrategy<XAlert3Strategy>();

    [TestMethod]
    public Task XAngZadCTmMmRec()
        => RunStrategy<XAngZadCTmMmRecStrategy>();

    [TestMethod]
    public Task XBug()
        => RunStrategy<XBugStrategy>();

    [TestMethod]
    public Task XDeMarkerHistogramVolDirect()
        => RunStrategy<XDeMarkerHistogramVolDirectStrategy>();

    [TestMethod]
    public Task XDeMarkerHistogramVol()
        => RunStrategy<XDeMarkerHistogramVolStrategy>();

    [TestMethod]
    public Task XDerivative()
        => RunStrategy<XDerivativeStrategy>();

    [TestMethod]
    public Task XDidiIndexCloudDuplex()
        => RunStrategy<XDidiIndexCloudDuplexStrategy>();

    [TestMethod]
    public Task XFatlXSatlCloudDuplex()
        => RunStrategy<XFatlXSatlCloudDuplexStrategy>();

    [TestMethod]
    public Task XFatlXSatlCloud()
        => RunStrategy<XFatlXSatlCloudStrategy>();

    [TestMethod]
    public Task XMACandles()
        => RunStrategy<XMACandlesStrategy>();

    [TestMethod]
    public Task XMan()
        => RunStrategy<XManStrategy>();

    [TestMethod]
    public Task XPTradeManager()
        => RunStrategy<XPTradeManagerStrategy>();

    [TestMethod]
    public Task XPeriodCandleSystemTmPlus()
        => RunStrategy<XPeriodCandleSystemTmPlusStrategy>();

    [TestMethod]
    public Task XTrader()
        => RunStrategy<XTraderStrategy>();

    [TestMethod]
    public Task XTraderV2()
        => RunStrategy<XTraderV2Strategy>();

    [TestMethod]
    public Task XTraderV3()
        => RunStrategy<XTraderV3Strategy>();

    [TestMethod]
    public Task XTrail2()
        => RunStrategy<XTrail2Strategy>();

    [TestMethod]
    public Task XTrail()
        => RunStrategy<XTrailStrategy>();

    [TestMethod]
    public Task XauUsdAdxBollinger()
        => RunStrategy<XauUsdAdxBollingerStrategy>();

    [TestMethod]
    public Task Xauusd10Minute()
        => RunStrategy<Xauusd10MinuteStrategy>();

    [TestMethod]
    public Task XauusdSimple20Profit100Loss()
        => RunStrategy<XauusdSimple20Profit100LossStrategy>();

    [TestMethod]
    public Task XauusdTrend()
        => RunStrategy<XauusdTrendStrategy>();

    [TestMethod]
    public Task XbugFree()
        => RunStrategy<XbugFreeStrategy>();

    [TestMethod]
    public Task XbugFreeV4()
        => RunStrategy<XbugFreeV4Strategy>();

    [TestMethod]
    public Task XcciHistogramVolDirect()
        => RunStrategy<XcciHistogramVolDirectStrategy>();

    [TestMethod]
    public Task XcciHistogramVol()
        => RunStrategy<XcciHistogramVolStrategy>();

    [TestMethod]
    public Task XdRangeSwitch()
        => RunStrategy<XdRangeSwitchStrategy>();

    [TestMethod]
    public Task XdpoCandle()
        => RunStrategy<XdpoCandleStrategy>();

    [TestMethod]
    public Task XdpoHistogram()
        => RunStrategy<XdpoHistogramStrategy>();

    [TestMethod]
    public Task XitThreeMaCross()
        => RunStrategy<XitThreeMaCrossStrategy>();

    [TestMethod]
    public Task XkriHistogram()
        => RunStrategy<XkriHistogramStrategy>();

    [TestMethod]
    public Task XmaIchimokuChannel()
        => RunStrategy<XmaIchimokuChannelStrategy>();

    [TestMethod]
    public Task XmaRangeChannel()
        => RunStrategy<XmaRangeChannelStrategy>();

    [TestMethod]
    public Task XmacdModes()
        => RunStrategy<XmacdModesStrategy>();

    [TestMethod]
    public Task XoSignalReopen()
        => RunStrategy<XoSignalReopenStrategy>();

    [TestMethod]
    public Task XpTradeManagerGrid()
        => RunStrategy<XpTradeManagerGridStrategy>();

    [TestMethod]
    public Task XpTradeManager()
        => RunStrategy<XpTradeManagerStrategy>();

    [TestMethod]
    public Task Xroc2VgTm()
        => RunStrategy<Xroc2VgTmStrategy>();

    [TestMethod]
    public Task Xroc2VgX2()
        => RunStrategy<Xroc2VgX2Strategy>();

    [TestMethod]
    public Task XrpAi15mAdaptiveV31()
        => RunStrategy<XrpAi15mAdaptiveV31Strategy>();

    [TestMethod]
    public Task XrsiHistogramVolDirect()
        => RunStrategy<XrsiHistogramVolDirectStrategy>();

    [TestMethod]
    public Task XrsidDeMarkerHistogram()
        => RunStrategy<XrsidDeMarkerHistogramStrategy>();

    [TestMethod]
    public Task XrviCrossover()
        => RunStrategy<XrviCrossoverStrategy>();

    [TestMethod]
    public Task XwamiMultiLayerMmrec()
        => RunStrategy<XwamiMultiLayerMmrecStrategy>();

    [TestMethod]
    public Task YenTrader051()
        => RunStrategy<YenTrader051Strategy>();

    [TestMethod]
    public Task YeongRrg()
        => RunStrategy<YeongRrgStrategy>();

    [TestMethod]
    public Task YesterdayToday()
        => RunStrategy<YesterdayTodayStrategy>();

    [TestMethod]
    public Task YesterdaysHigh()
        => RunStrategy<YesterdaysHighStrategy>();

    [TestMethod]
    public Task YinYangRsiVolumeTrend()
        => RunStrategy<YinYangRsiVolumeTrendStrategy>();

    [TestMethod]
    public Task YtgAdxLevelCross()
        => RunStrategy<YtgAdxLevelCrossStrategy>();

    [TestMethod]
    public Task YuriGarciaSmartMoney()
        => RunStrategy<YuriGarciaSmartMoneyStrategy>();

    [TestMethod]
    public Task ZScore2()
        => RunStrategy<ZScore2Strategy>();

    [TestMethod]
    public Task ZScoreBuySell()
        => RunStrategy<ZScoreBuySellStrategy>();

    [TestMethod]
    public Task ZScoreNormalizedVix()
        => RunStrategy<ZScoreNormalizedVixStrategy>();

    [TestMethod]
    public Task ZScoreRsi()
        => RunStrategy<ZScoreRsiStrategy>();

    [TestMethod]
    public Task ZStrikeRecovery()
        => RunStrategy<ZStrikeRecoveryStrategy>();

    [TestMethod]
    public Task ZZFiboTrader()
        => RunStrategy<ZZFiboTraderStrategy>();

    [TestMethod]
    public Task ZahorchakMeasure()
        => RunStrategy<ZahorchakMeasureStrategy>();

    [TestMethod]
    public Task Zakryvator()
        => RunStrategy<ZakryvatorStrategy>();

    [TestMethod]
    public Task ZapTeamProV6Ema()
        => RunStrategy<ZapTeamProV6EmaStrategy>();

    [TestMethod]
    public Task ZeeZeeLevel()
        => RunStrategy<ZeeZeeLevelStrategy>();

}
