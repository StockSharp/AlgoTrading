namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task HighLowMaBreakout()
        => RunStrategy<HighLowMaBreakoutStrategy>();

    [TestMethod]
    public Task HighYieldSpreadSmaFilter()
        => RunStrategy<HighYieldSpreadSmaFilterStrategy>();

    [TestMethod]
    public Task HighYieldSpreadWithSmaFilter()
        => RunStrategy<HighYieldSpreadWithSmaFilterStrategy>();

    [TestMethod]
    public Task HigherOrderPivots()
        => RunStrategy<HigherOrderPivotsStrategy>();

    [TestMethod]
    public Task HighsLows()
        => RunStrategy<HighsLowsStrategy>();

    [TestMethod]
    public Task HistoScalper()
        => RunStrategy<HistoScalperStrategy>();

    [TestMethod]
    public Task HistoryInfoEa()
        => RunStrategy<HistoryInfoEaStrategy>();

    [TestMethod]
    public Task Hma200Ema20Crossover()
        => RunStrategy<Hma200Ema20CrossoverStrategy>();

    [TestMethod]
    public Task HmaCrossoverAtrCurvature()
        => RunStrategy<HmaCrossoverAtrCurvatureStrategy>();

    [TestMethod]
    public Task HmaCrossoverRsiStochasticTrailingStop()
        => RunStrategy<HmaCrossoverRsiStochasticTrailingStopStrategy>();

    [TestMethod]
    public Task HodLodPmhPmlPdhPdl()
        => RunStrategy<HodLodPmhPmlPdhPdlStrategy>();

    [TestMethod]
    public Task HoffmanHeikenBias()
        => RunStrategy<HoffmanHeikenBiasStrategy>();

    [TestMethod]
    public Task HonestVolatilityGrid()
        => RunStrategy<HonestVolatilityGridStrategy>();

    [TestMethod]
    public Task HoopMasterBreakout()
        => RunStrategy<HoopMasterBreakoutStrategy>();

    [TestMethod]
    public Task HoopMaster()
        => RunStrategy<HoopMasterStrategy>();

    [TestMethod]
    public Task HorizontalLineLevels()
        => RunStrategy<HorizontalLineLevelsStrategy>();

    [TestMethod]
    public Task HorizontalRay()
        => RunStrategy<HorizontalRayStrategy>();

    [TestMethod]
    public Task HowToSetBacktestTimeRanges()
        => RunStrategy<HowToSetBacktestTimeRangesStrategy>();

    [TestMethod]
    public Task HowToUseLeverageAndMargin()
        => RunStrategy<HowToUseLeverageAndMarginStrategy>();

    [TestMethod]
    public Task HpcsInter4()
        => RunStrategy<HpcsInter4Strategy>();

    [TestMethod]
    public Task HpcsInter5()
        => RunStrategy<HpcsInter5Strategy>();

    [TestMethod]
    public Task HpcsInter6Rsi()
        => RunStrategy<HpcsInter6RsiStrategy>();

    [TestMethod]
    public Task HpcsInter7()
        => RunStrategy<HpcsInter7Strategy>();

    [TestMethod]
    public Task Hsi1First30mCandle()
        => RunStrategy<Hsi1First30mCandleStrategy>();

    [TestMethod]
    public Task HsiFirst30mCandle()
        => RunStrategy<HsiFirst30mCandleStrategy>();

    [TestMethod]
    public Task HsvAndHslGradientTools()
        => RunStrategy<HsvAndHslGradientToolsStrategy>();

    [TestMethod]
    public Task HtfCandlesLib()
        => RunStrategy<HtfCandlesLibStrategy>();

    [TestMethod]
    public Task HthTrader()
        => RunStrategy<HthTraderStrategy>();

    [TestMethod]
    public Task HugeIncome()
        => RunStrategy<HugeIncomeStrategy>();

    [TestMethod]
    public Task HulkGridAlgorithmV2()
        => RunStrategy<HulkGridAlgorithmV2Strategy>();

    [TestMethod]
    public Task HullCandles()
        => RunStrategy<HullCandlesStrategy>();

    [TestMethod]
    public Task HullSuite1RiskNoSlTp()
        => RunStrategy<HullSuite1RiskNoSlTpStrategy>();

    [TestMethod]
    public Task HullSuiteByMRS()
        => RunStrategy<HullSuiteByMRSStrategy>();

    [TestMethod]
    public Task HullSuiteNoSlTp()
        => RunStrategy<HullSuiteNoSlTpStrategy>();

    [TestMethod]
    public Task HullTrendOsma()
        => RunStrategy<HullTrendOsmaStrategy>();

    [TestMethod]
    public Task HurstExponent()
        => RunStrategy<HurstExponentStrategy>();

    [TestMethod]
    public Task HurstFutureLinesOfDemarcation()
        => RunStrategy<HurstFutureLinesOfDemarcationStrategy>();

    [TestMethod]
    public Task Hvr()
        => RunStrategy<HvrStrategy>();

    [TestMethod]
    public Task HybridEa()
        => RunStrategy<HybridEaStrategy>();

    [TestMethod]
    public Task HybridRsiBreakoutDashboard()
        => RunStrategy<HybridRsiBreakoutDashboardStrategy>();

    [TestMethod]
    public Task HybridScalper()
        => RunStrategy<HybridScalperStrategy>();

    [TestMethod]
    public Task HybridScalpingBot()
        => RunStrategy<HybridScalpingBotStrategy>();

    [TestMethod]
    public Task I4Drf()
        => RunStrategy<I4DrfStrategy>();

    [TestMethod]
    public Task I4DrfV2()
        => RunStrategy<I4DrfV2Strategy>();

    [TestMethod]
    public Task ICai()
        => RunStrategy<ICaiStrategy>();

    [TestMethod]
    public Task IGap()
        => RunStrategy<IGapStrategy>();

    [TestMethod]
    public Task IMAIStochasticCustom()
        => RunStrategy<IMAIStochasticCustomStrategy>();

    [TestMethod]
    public Task IStochasticTrading()
        => RunStrategy<IStochasticTradingStrategy>();

    [TestMethod]
    public Task ITrade()
        => RunStrategy<ITradeStrategy>();

    [TestMethod]
    public Task ITrend()
        => RunStrategy<ITrendStrategy>();

    [TestMethod]
    public Task IUBreakOfAnySession()
        => RunStrategy<IUBreakOfAnySessionStrategy>();

    [TestMethod]
    public Task IUGapFill()
        => RunStrategy<IUGapFillStrategy>();

    [TestMethod]
    public Task IUHigherTimeframeMACross()
        => RunStrategy<IUHigherTimeframeMACrossStrategy>();

    [TestMethod]
    public Task IUOpeningRangeBreakout()
        => RunStrategy<IUOpeningRangeBreakoutStrategy>();

    [TestMethod]
    public Task IbsInternalBarStrength()
        => RunStrategy<IbsInternalBarStrengthStrategy>();

    [TestMethod]
    public Task IbsRsiCciV4()
        => RunStrategy<IbsRsiCciV4Strategy>();

    [TestMethod]
    public Task IbsRsiCciV4X2()
        => RunStrategy<IbsRsiCciV4X2Strategy>();

    [TestMethod]
    public Task IcciIma()
        => RunStrategy<IcciImaStrategy>();

    [TestMethod]
    public Task IcciIrsi()
        => RunStrategy<IcciIrsiStrategy>();

    [TestMethod]
    public Task IchiOscillator()
        => RunStrategy<IchiOscillatorStrategy>();

    [TestMethod]
    public Task Ichimoku2005()
        => RunStrategy<Ichimoku2005Strategy>();

    [TestMethod]
    public Task IchimokuBarabashkakvn()
        => RunStrategy<IchimokuBarabashkakvnStrategy>();

    [TestMethod]
    public Task IchimokuByFarmerBtc()
        => RunStrategy<IchimokuByFarmerBtcStrategy>();

    [TestMethod]
    public Task IchimokuChinkouCross()
        => RunStrategy<IchimokuChinkouCrossStrategy>();

    [TestMethod]
    public Task IchimokuCloudBreakoutOnlyLong()
        => RunStrategy<IchimokuCloudBreakoutOnlyLongStrategy>();

    [TestMethod]
    public Task IchimokuCloudBuyCustomEmaExit()
        => RunStrategy<IchimokuCloudBuyCustomEmaExitStrategy>();

    [TestMethod]
    public Task IchimokuCloudBuySell()
        => RunStrategy<IchimokuCloudBuySellStrategy>();

    [TestMethod]
    public Task IchimokuCloudRetrace()
        => RunStrategy<IchimokuCloudRetraceStrategy>();

    [TestMethod]
    public Task IchimokuCloudsLongAndShort()
        => RunStrategy<IchimokuCloudsLongAndShortStrategy>();

    [TestMethod]
    public Task IchimokuDailyCandleXHullMaXMacd()
        => RunStrategy<IchimokuDailyCandleXHullMaXMacdStrategy>();

    [TestMethod]
    public Task IchimokuMomentumMacd()
        => RunStrategy<IchimokuMomentumMacdStrategy>();

    [TestMethod]
    public Task IchimokuOscillator()
        => RunStrategy<IchimokuOscillatorStrategy>();

    [TestMethod]
    public Task IchimokuPriceAction()
        => RunStrategy<IchimokuPriceActionStrategy>();

    [TestMethod]
    public Task IchimokuRetracement()
        => RunStrategy<IchimokuRetracementStrategy>();

    [TestMethod]
    public Task IchimokuRsiMacd()
        => RunStrategy<IchimokuRsiMacdStrategy>();

    [TestMethod]
    public Task IctBreadAndButterSellSetup()
        => RunStrategy<IctBreadAndButterSellSetupStrategy>();

    [TestMethod]
    public Task IctIndicatorWithPaperTrading()
        => RunStrategy<IctIndicatorWithPaperTradingStrategy>();

    [TestMethod]
    public Task IctMasterSuiteTradingIq()
        => RunStrategy<IctMasterSuiteTradingIqStrategy>();

    [TestMethod]
    public Task IctNyKillZoneAutoTrading()
        => RunStrategy<IctNyKillZoneAutoTradingStrategy>();

    [TestMethod]
    public Task IdEmarsiOnChart()
        => RunStrategy<IdEmarsiOnChartStrategy>();

    [TestMethod]
    public Task IfsFractals()
        => RunStrategy<IfsFractalsStrategy>();

    [TestMethod]
    public Task IiOutbreak()
        => RunStrategy<IiOutbreakStrategy>();

    [TestMethod]
    public Task IinMaSignal()
        => RunStrategy<IinMaSignalStrategy>();

    [TestMethod]
    public Task Ilan14Grid()
        => RunStrategy<Ilan14GridStrategy>();

    [TestMethod]
    public Task Ilan14()
        => RunStrategy<Ilan14Strategy>();

    [TestMethod]
    public Task Ilan16Dynamic()
        => RunStrategy<Ilan16DynamicStrategy>();

    [TestMethod]
    public Task IlanDynamicHt()
        => RunStrategy<IlanDynamicHtStrategy>();

    [TestMethod]
    public Task IlanIma()
        => RunStrategy<IlanImaStrategy>();

    [TestMethod]
    public Task ImaExpert()
        => RunStrategy<ImaExpertStrategy>();

    [TestMethod]
    public Task ImaIsarEa()
        => RunStrategy<ImaIsarEaStrategy>();

    [TestMethod]
    public Task ImacdSniper()
        => RunStrategy<ImacdSniperStrategy>();

    [TestMethod]
    public Task Imlib()
        => RunStrategy<ImlibStrategy>();

    [TestMethod]
    public Task ImproveMaRsiHedge()
        => RunStrategy<ImproveMaRsiHedgeStrategy>();

    [TestMethod]
    public Task ImprovedEmaCdcTrailingStop()
        => RunStrategy<ImprovedEmaCdcTrailingStopStrategy>();

    [TestMethod]
    public Task IndicatorBuffers()
        => RunStrategy<IndicatorBuffersStrategy>();

    [TestMethod]
    public Task IndicatorPanel()
        => RunStrategy<IndicatorPanelStrategy>();

    [TestMethod]
    public Task IndicatorTestWithConditionsTable()
        => RunStrategy<IndicatorTestWithConditionsTableStrategy>();

    [TestMethod]
    public Task IndicesSectorSigmaSpikes()
        => RunStrategy<IndicesSectorSigmaSpikesStrategy>();

    [TestMethod]
    public Task IndicesTester()
        => RunStrategy<IndicesTesterStrategy>();

    [TestMethod]
    public Task InformativeDashboard()
        => RunStrategy<InformativeDashboardStrategy>();

}
