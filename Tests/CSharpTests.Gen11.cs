namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task EmaTrendHeikinAshiEntry()
        => RunStrategy<EmaTrendHeikinAshiEntryStrategy>();

    [TestMethod]
    public Task EmaWmaContrarian()
        => RunStrategy<EmaWmaContrarianStrategy>();

    [TestMethod]
    public Task EmaWmaCrossover()
        => RunStrategy<EmaWmaCrossoverStrategy>();

    [TestMethod]
    public Task EmaWmaRisk()
        => RunStrategy<EmaWmaRiskStrategy>();

    [TestMethod]
    public Task EmaWmaRsi()
        => RunStrategy<EmaWmaRsiStrategy>();

    [TestMethod]
    public Task EmaWprRetracement()
        => RunStrategy<EmaWprRetracementStrategy>();

    [TestMethod]
    public Task EmaWprTrend()
        => RunStrategy<EmaWprTrendStrategy>();

    [TestMethod]
    public Task Emagic1()
        => RunStrategy<Emagic1Strategy>();

    [TestMethod]
    public Task EnergyAdvancedPolicy()
        => RunStrategy<EnergyAdvancedPolicyStrategy>();

    [TestMethod]
    public Task EngulfingCandlestick()
        => RunStrategy<EngulfingCandlestickStrategy>();

    [TestMethod]
    public Task EngulfingMfiConfirmation()
        => RunStrategy<EngulfingMfiConfirmationStrategy>();

    [TestMethod]
    public Task EngulfingMomentum()
        => RunStrategy<EngulfingMomentumStrategy>();

    [TestMethod]
    public Task EngulfingPinBarBreakout()
        => RunStrategy<EngulfingPinBarBreakoutStrategy>();

    [TestMethod]
    public Task EngulfingWithTrend()
        => RunStrategy<EngulfingWithTrendStrategy>();

    [TestMethod]
    public Task EnhancedBarUpDn()
        => RunStrategy<EnhancedBarUpDnStrategy>();

    [TestMethod]
    public Task EnhancedBollingerBands()
        => RunStrategy<EnhancedBollingerBandsStrategy>();

    [TestMethod]
    public Task EnhancedDojiCandle()
        => RunStrategy<EnhancedDojiCandleStrategy>();

    [TestMethod]
    public Task EnhancedIchimokuCloud()
        => RunStrategy<EnhancedIchimokuCloudStrategy>();

    [TestMethod]
    public Task EnhancedMarketStructure()
        => RunStrategy<EnhancedMarketStructureStrategy>();

    [TestMethod]
    public Task EnhancedRangeFilterAtrTpSl()
        => RunStrategy<EnhancedRangeFilterAtrTpSlStrategy>();

    [TestMethod]
    public Task EnhancedTimeSegmentedVolume()
        => RunStrategy<EnhancedTimeSegmentedVolumeStrategy>();

    [TestMethod]
    public Task EntryFragger()
        => RunStrategy<EntryFraggerStrategy>();

    [TestMethod]
    public Task EnvelopeLimitLadder()
        => RunStrategy<EnvelopeLimitLadderStrategy>();

    [TestMethod]
    public Task EnvelopeMaShort()
        => RunStrategy<EnvelopeMaShortStrategy>();

    [TestMethod]
    public Task EnvelopesEa()
        => RunStrategy<EnvelopesEaStrategy>();

    [TestMethod]
    public Task EqualVolumeRangeBars()
        => RunStrategy<EqualVolumeRangeBarsStrategy>();

    [TestMethod]
    public Task EquidistantChannel()
        => RunStrategy<EquidistantChannelStrategy>();

    [TestMethod]
    public Task EquilibriumCandlesPattern()
        => RunStrategy<EquilibriumCandlesPatternStrategy>();

    [TestMethod]
    public Task EquityPercentLock()
        => RunStrategy<EquityPercentLockStrategy>();

    [TestMethod]
    public Task EquivolumeBars()
        => RunStrategy<EquivolumeBarsStrategy>();

    [TestMethod]
    public Task EquivolumeOverlayVolumeBars()
        => RunStrategy<EquivolumeOverlayVolumeBarsStrategy>();

    [TestMethod]
    public Task ErgodicTicksVolumeIndicator()
        => RunStrategy<ErgodicTicksVolumeIndicatorStrategy>();

    [TestMethod]
    public Task ErgodicTicksVolumeOsma()
        => RunStrategy<ErgodicTicksVolumeOsmaStrategy>();

    [TestMethod]
    public Task ErrorEa()
        => RunStrategy<ErrorEaStrategy>();

    [TestMethod]
    public Task EscapeMeanReversion()
        => RunStrategy<EscapeMeanReversionStrategy>();

    [TestMethod]
    public Task Escape()
        => RunStrategy<EscapeStrategy>();

    [TestMethod]
    public Task EscortTrend()
        => RunStrategy<EscortTrendStrategy>();

    [TestMethod]
    public Task Et4MtcV1()
        => RunStrategy<Et4MtcV1Strategy>();

    [TestMethod]
    public Task EthSignal15m()
        => RunStrategy<EthSignal15mStrategy>();

    [TestMethod]
    public Task EthUsdtEmaCrossover()
        => RunStrategy<EthUsdtEmaCrossoverStrategy>();

    [TestMethod]
    public Task EugeneCandlePattern()
        => RunStrategy<EugeneCandlePatternStrategy>();

    [TestMethod]
    public Task EugeneInsideBreakout()
        => RunStrategy<EugeneInsideBreakoutStrategy>();

    [TestMethod]
    public Task Eugene()
        => RunStrategy<EugeneStrategy>();

    [TestMethod]
    public Task EurGbpEa()
        => RunStrategy<EurGbpEaStrategy>();

    [TestMethod]
    public Task EurUsdMultiLayerStatisticalRegression()
        => RunStrategy<EurUsdMultiLayerStatisticalRegressionStrategy>();

    [TestMethod]
    public Task EurUsdSessionBreakout()
        => RunStrategy<EurUsdSessionBreakoutStrategy>();

    [TestMethod]
    public Task EuroSurgeSimplified()
        => RunStrategy<EuroSurgeSimplifiedStrategy>();

    [TestMethod]
    public Task EurusdV20()
        => RunStrategy<EurusdV20Strategy>();

    [TestMethod]
    public Task EveningStarReversal()
        => RunStrategy<EveningStarReversalStrategy>();

    [TestMethod]
    public Task ExFractals()
        => RunStrategy<ExFractalsStrategy>();

    [TestMethod]
    public Task ExampleOfMacdAutomated()
        => RunStrategy<ExampleOfMacdAutomatedStrategy>();

    [TestMethod]
    public Task ExchangePrice()
        => RunStrategy<ExchangePriceStrategy>();

    [TestMethod]
    public Task ExecuterAc()
        => RunStrategy<ExecuterAcStrategy>();

    [TestMethod]
    public Task ExecutorAo()
        => RunStrategy<ExecutorAoStrategy>();

    [TestMethod]
    public Task ExecutorCandles()
        => RunStrategy<ExecutorCandlesStrategy>();

    [TestMethod]
    public Task Exodus()
        => RunStrategy<ExodusStrategy>();

    [TestMethod]
    public Task Exp2XmaIchimokuOscillator()
        => RunStrategy<Exp2XmaIchimokuOscillatorStrategy>();

    [TestMethod]
    public Task Exp3Sto()
        => RunStrategy<Exp3StoStrategy>();

    [TestMethod]
    public Task Exp3XmaIshimoku()
        => RunStrategy<Exp3XmaIshimokuStrategy>();

    [TestMethod]
    public Task ExpAdaptiveRenkoMmrecDuplex()
        => RunStrategy<ExpAdaptiveRenkoMmrecDuplexStrategy>();

    [TestMethod]
    public Task ExpAdxCrossHullStyle()
        => RunStrategy<ExpAdxCrossHullStyleStrategy>();

    [TestMethod]
    public Task ExpAfirma()
        => RunStrategy<ExpAfirmaStrategy>();

    [TestMethod]
    public Task ExpAmstell()
        => RunStrategy<ExpAmstellStrategy>();

    [TestMethod]
    public Task ExpAtrTrailing()
        => RunStrategy<ExpAtrTrailingStrategy>();

    [TestMethod]
    public Task ExpBlauCmi()
        => RunStrategy<ExpBlauCmiStrategy>();

    [TestMethod]
    public Task ExpBlauCsi()
        => RunStrategy<ExpBlauCsiStrategy>();

    [TestMethod]
    public Task ExpBlauHlm()
        => RunStrategy<ExpBlauHlmStrategy>();

    [TestMethod]
    public Task ExpBrainTrend2AbsolutelyNoLagLwmaX2MACandleMmrec()
        => RunStrategy<ExpBrainTrend2AbsolutelyNoLagLwmaX2MACandleMmrecStrategy>();

    [TestMethod]
    public Task ExpBreakoutSignals()
        => RunStrategy<ExpBreakoutSignalsStrategy>();

    [TestMethod]
    public Task ExpBuySellSide()
        => RunStrategy<ExpBuySellSideStrategy>();

    [TestMethod]
    public Task ExpCandlesXSmoothed()
        => RunStrategy<ExpCandlesXSmoothedStrategy>();

    [TestMethod]
    public Task ExpCandlesticksBwTime()
        => RunStrategy<ExpCandlesticksBwTimeStrategy>();

    [TestMethod]
    public Task ExpColorMetroMmrecDuplex()
        => RunStrategy<ExpColorMetroMmrecDuplexStrategy>();

    [TestMethod]
    public Task ExpColorPemaDigitTmPlusMmrecDuplex()
        => RunStrategy<ExpColorPemaDigitTmPlusMmrecDuplexStrategy>();

    [TestMethod]
    public Task ExpColorPemaDigitTmPlus()
        => RunStrategy<ExpColorPemaDigitTmPlusStrategy>();

    [TestMethod]
    public Task ExpColorTsiOscillator()
        => RunStrategy<ExpColorTsiOscillatorStrategy>();

    [TestMethod]
    public Task ExpColorX2MaX2()
        => RunStrategy<ExpColorX2MaX2Strategy>();

    [TestMethod]
    public Task ExpCronexAO()
        => RunStrategy<ExpCronexAOStrategy>();

    [TestMethod]
    public Task ExpCronexChaikin()
        => RunStrategy<ExpCronexChaikinStrategy>();

    [TestMethod]
    public Task ExpCronexMfi()
        => RunStrategy<ExpCronexMfiStrategy>();

    [TestMethod]
    public Task ExpCyclePeriod()
        => RunStrategy<ExpCyclePeriodStrategy>();

    [TestMethod]
    public Task ExpDemaRangeChannelTmPlus()
        => RunStrategy<ExpDemaRangeChannelTmPlusStrategy>();

    [TestMethod]
    public Task ExpDigitalMacd()
        => RunStrategy<ExpDigitalMacdStrategy>();

    [TestMethod]
    public Task ExpExtremum()
        => RunStrategy<ExpExtremumStrategy>();

    [TestMethod]
    public Task ExpFiboZz()
        => RunStrategy<ExpFiboZzStrategy>();

    [TestMethod]
    public Task ExpFineTuningMaCandle()
        => RunStrategy<ExpFineTuningMaCandleStrategy>();

    [TestMethod]
    public Task ExpFisherCgOscillator()
        => RunStrategy<ExpFisherCgOscillatorStrategy>();

    [TestMethod]
    public Task ExpFishing()
        => RunStrategy<ExpFishingStrategy>();

    [TestMethod]
    public Task ExpHansIndicatorCloudSystemTmPlus()
        => RunStrategy<ExpHansIndicatorCloudSystemTmPlusStrategy>();

    [TestMethod]
    public Task ExpHighsLowsSignal()
        => RunStrategy<ExpHighsLowsSignalStrategy>();

    [TestMethod]
    public Task ExpHlrSign()
        => RunStrategy<ExpHlrSignStrategy>();

    [TestMethod]
    public Task ExpHullTrend()
        => RunStrategy<ExpHullTrendStrategy>();

    [TestMethod]
    public Task ExpICustomV1()
        => RunStrategy<ExpICustomV1Strategy>();

    [TestMethod]
    public Task ExpIKlPriceVolDirect()
        => RunStrategy<ExpIKlPriceVolDirectStrategy>();

    [TestMethod]
    public Task ExpIKlPriceVol()
        => RunStrategy<ExpIKlPriceVolStrategy>();

    [TestMethod]
    public Task ExpIinMaSignalMmrec()
        => RunStrategy<ExpIinMaSignalMmrecStrategy>();

    [TestMethod]
    public Task ExpKwanNrp()
        => RunStrategy<ExpKwanNrpStrategy>();

    [TestMethod]
    public Task ExpLeading()
        => RunStrategy<ExpLeadingStrategy>();

    [TestMethod]
    public Task ExpMaRoundingCandleMmrec()
        => RunStrategy<ExpMaRoundingCandleMmrecStrategy>();

    [TestMethod]
    public Task ExpMaRoundingChannel()
        => RunStrategy<ExpMaRoundingChannelStrategy>();

}
