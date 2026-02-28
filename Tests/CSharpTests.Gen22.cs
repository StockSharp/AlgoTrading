namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task MultiTimeFrameRegression()
        => RunStrategy<MultiTimeFrameRegressionStrategy>();

    [TestMethod]
    public Task MultiTimeFrameTrader()
        => RunStrategy<MultiTimeFrameTraderStrategy>();

    [TestMethod]
    public Task MultiTimeframeEmaAlignment()
        => RunStrategy<MultiTimeframeEmaAlignmentStrategy>();

    [TestMethod]
    public Task MultiTimeframeMacd()
        => RunStrategy<MultiTimeframeMacdStrategy>();

    [TestMethod]
    public Task MultiTimeframeParabolicSar()
        => RunStrategy<MultiTimeframeParabolicSarStrategy>();

    [TestMethod]
    public Task MultiTimeframeRsiBuySell()
        => RunStrategy<MultiTimeframeRsiBuySellStrategy>();

    [TestMethod]
    public Task MultiTimeframeRsiGridWithArrows()
        => RunStrategy<MultiTimeframeRsiGridWithArrowsStrategy>();

    [TestMethod]
    public Task MultiTimeframeTrend200EmaFilterLongsOnly()
        => RunStrategy<MultiTimeframeTrend200EmaFilterLongsOnlyStrategy>();

    [TestMethod]
    public Task MultiTrader()
        => RunStrategy<MultiTraderStrategy>();

    [TestMethod]
    public Task MulticurrencyOverlayHedge()
        => RunStrategy<MulticurrencyOverlayHedgeStrategy>();

    [TestMethod]
    public Task MulticurrencyTradingPanel()
        => RunStrategy<MulticurrencyTradingPanelStrategy>();

    [TestMethod]
    public Task MultikSmaExp()
        => RunStrategy<MultikSmaExpStrategy>();

    [TestMethod]
    public Task MurreyBBandStochastic()
        => RunStrategy<MurreyBBandStochasticStrategy>();

    [TestMethod]
    public Task MustangAlgoChannel()
        => RunStrategy<MustangAlgoChannelStrategy>();

    [TestMethod]
    public Task MutanabbyAiAlgoPro()
        => RunStrategy<MutanabbyAiAlgoProStrategy>();

    [TestMethod]
    public Task MvoMaSignal()
        => RunStrategy<MvoMaSignalStrategy>();

    [TestMethod]
    public Task MyLineOrder()
        => RunStrategy<MyLineOrderStrategy>();

    [TestMethod]
    public Task MySystem()
        => RunStrategy<MySystemStrategy>();

    [TestMethod]
    public Task MyTs15()
        => RunStrategy<MyTs15Strategy>();

    [TestMethod]
    public Task MyfriendForexInstruments()
        => RunStrategy<MyfriendForexInstrumentsStrategy>();

    [TestMethod]
    public Task N7SAo772012()
        => RunStrategy<N7SAo772012Strategy>();

    [TestMethod]
    public Task NCandlesSequence()
        => RunStrategy<NCandlesSequenceStrategy>();

    [TestMethod]
    public Task NCandlesSequenceStreak()
        => RunStrategy<NCandlesSequenceStreakStrategy>();

    [TestMethod]
    public Task NCandles()
        => RunStrategy<NCandlesStrategy>();

    [TestMethod]
    public Task NCandlesV2()
        => RunStrategy<NCandlesV2Strategy>();

    [TestMethod]
    public Task NCandlesV3()
        => RunStrategy<NCandlesV3Strategy>();

    [TestMethod]
    public Task NCandlesV5()
        => RunStrategy<NCandlesV5Strategy>();

    [TestMethod]
    public Task NCandlesV6()
        => RunStrategy<NCandlesV6Strategy>();

    [TestMethod]
    public Task NRTRATRStop()
        => RunStrategy<NRTRATRStopStrategy>();

    [TestMethod]
    public Task NRatioSign()
        => RunStrategy<NRatioSignStrategy>();

    [TestMethod]
    public Task NSecondsNPoints()
        => RunStrategy<NSecondsNPointsStrategy>();

    [TestMethod]
    public Task NTradesPerSetMartingale()
        => RunStrategy<NTradesPerSetMartingaleStrategy>();

    [TestMethod]
    public Task NUp1Down()
        => RunStrategy<NUp1DownStrategy>();

    [TestMethod]
    public Task NadarayaWatsonEnvelope()
        => RunStrategy<NadarayaWatsonEnvelopeStrategy>();

    [TestMethod]
    public Task NarrowRange()
        => RunStrategy<NarrowRangeStrategy>();

    [TestMethod]
    public Task Nas100AndGoldSmartScalpingProEnhancedV2()
        => RunStrategy<Nas100AndGoldSmartScalpingProEnhancedV2Strategy>();

    [TestMethod]
    public Task Nasdaq100PeakHours()
        => RunStrategy<Nasdaq100PeakHoursStrategy>();

    [TestMethod]
    public Task NasdaqDayAndNightBreakdown()
        => RunStrategy<NasdaqDayAndNightBreakdownStrategy>();

    [TestMethod]
    public Task NatusekoProtrader4H()
        => RunStrategy<NatusekoProtrader4HStrategy>();

    [TestMethod]
    public Task NegativeSpread()
        => RunStrategy<NegativeSpreadStrategy>();

    [TestMethod]
    public Task NegroniOpeningRange()
        => RunStrategy<NegroniOpeningRangeStrategy>();

    [TestMethod]
    public Task NeoImacdSlTp()
        => RunStrategy<NeoImacdSlTpStrategy>();

    [TestMethod]
    public Task NeonMomentumWaves()
        => RunStrategy<NeonMomentumWavesStrategy>();

    [TestMethod]
    public Task NeuralNetworkAtr()
        => RunStrategy<NeuralNetworkAtrStrategy>();

    [TestMethod]
    public Task NeuralNetworkMacd()
        => RunStrategy<NeuralNetworkMacdStrategy>();

    [TestMethod]
    public Task NeuralNetworkTemplate()
        => RunStrategy<NeuralNetworkTemplateStrategy>();

    [TestMethod]
    public Task NeuroNirvamanEa2()
        => RunStrategy<NeuroNirvamanEa2Strategy>();

    [TestMethod]
    public Task NeuroNirvamanMq4()
        => RunStrategy<NeuroNirvamanMq4Strategy>();

    [TestMethod]
    public Task NeuroNirvaman()
        => RunStrategy<NeuroNirvamanStrategy>();

    [TestMethod]
    public Task NevalyashkaBreakdownLevel()
        => RunStrategy<NevalyashkaBreakdownLevelStrategy>();

    [TestMethod]
    public Task NevalyashkaDirection()
        => RunStrategy<NevalyashkaDirectionStrategy>();

    [TestMethod]
    public Task NevalyashkaFlip()
        => RunStrategy<NevalyashkaFlipStrategy>();

    [TestMethod]
    public Task NevalyashkaMartingale()
        => RunStrategy<NevalyashkaMartingaleStrategy>();

    [TestMethod]
    public Task NevalyashkaStopup()
        => RunStrategy<NevalyashkaStopupStrategy>();

    [TestMethod]
    public Task Nevalyashka()
        => RunStrategy<NevalyashkaStrategy>();

    [TestMethod]
    public Task NewBarEvent()
        => RunStrategy<NewBarEventStrategy>();

    [TestMethod]
    public Task NewBar()
        => RunStrategy<NewBarStrategy>();

    [TestMethod]
    public Task NewFscea()
        => RunStrategy<NewFsceaStrategy>();

    [TestMethod]
    public Task NewIntradayHighWithWeakBar()
        => RunStrategy<NewIntradayHighWithWeakBarStrategy>();

    [TestMethod]
    public Task NewMartin()
        => RunStrategy<NewMartinStrategy>();

    [TestMethod]
    public Task NewRandom()
        => RunStrategy<NewRandomStrategy>();

    [TestMethod]
    public Task NewsHourTrade()
        => RunStrategy<NewsHourTradeStrategy>();

    [TestMethod]
    public Task NewsPendingOrders()
        => RunStrategy<NewsPendingOrdersStrategy>();

    [TestMethod]
    public Task NewsRelease()
        => RunStrategy<NewsReleaseStrategy>();

    [TestMethod]
    public Task NewsTemplateUniversal()
        => RunStrategy<NewsTemplateUniversalStrategy>();

    [TestMethod]
    public Task NewsTrader()
        => RunStrategy<NewsTraderStrategy>();

    [TestMethod]
    public Task NewsTradingEa()
        => RunStrategy<NewsTradingEaStrategy>();

    [TestMethod]
    public Task NextBarMomentum()
        => RunStrategy<NextBarMomentumStrategy>();

    [TestMethod]
    public Task Nextbar()
        => RunStrategy<NextbarStrategy>();

    [TestMethod]
    public Task Nifty505mint()
        => RunStrategy<Nifty505mintStrategy>();

    [TestMethod]
    public Task NiftyOptionsTrendyMarketsWithTsl()
        => RunStrategy<NiftyOptionsTrendyMarketsWithTslStrategy>();

    [TestMethod]
    public Task NightFlatTrade()
        => RunStrategy<NightFlatTradeStrategy>();

    [TestMethod]
    public Task NightScalper()
        => RunStrategy<NightScalperStrategy>();

    [TestMethod]
    public Task Night()
        => RunStrategy<NightStrategy>();

    [TestMethod]
    public Task NinaEa()
        => RunStrategy<NinaEaStrategy>();

    [TestMethod]
    public Task NirvamanImax()
        => RunStrategy<NirvamanImaxStrategy>();

    [TestMethod]
    public Task NnfxAutoTrade()
        => RunStrategy<NnfxAutoTradeStrategy>();

    [TestMethod]
    public Task NoNonsenseTester()
        => RunStrategy<NoNonsenseTesterStrategy>();

    [TestMethod]
    public Task Noah10Pips2006()
        => RunStrategy<Noah10Pips2006Strategy>();

    [TestMethod]
    public Task NonLagDot()
        => RunStrategy<NonLagDotStrategy>();

    [TestMethod]
    public Task NonRepaintingRenkoEmulation()
        => RunStrategy<NonRepaintingRenkoEmulationStrategy>();

    [TestMethod]
    public Task NormalizedOscillatorsSpiderChart()
        => RunStrategy<NormalizedOscillatorsSpiderChartStrategy>();

    [TestMethod]
    public Task NovaBarra()
        => RunStrategy<NovaBarraStrategy>();

    [TestMethod]
    public Task NovaFuturesProSafeV6()
        => RunStrategy<NovaFuturesProSafeV6Strategy>();

    [TestMethod]
    public Task Nova()
        => RunStrategy<NovaStrategy>();

    [TestMethod]
    public Task NqPhantomScalperPro()
        => RunStrategy<NqPhantomScalperProStrategy>();

    [TestMethod]
    public Task NrtrAtrStop()
        => RunStrategy<NrtrAtrStopStrategy>();

    [TestMethod]
    public Task NrtrExtr()
        => RunStrategy<NrtrExtrStrategy>();

    [TestMethod]
    public Task NrtrRevers()
        => RunStrategy<NrtrReversStrategy>();

    [TestMethod]
    public Task NrtrReversal()
        => RunStrategy<NrtrReversalStrategy>();

    [TestMethod]
    public Task NrtrTrailingStop()
        => RunStrategy<NrtrTrailingStopStrategy>();

    [TestMethod]
    public Task NseIndexWithEntryExitMarkers()
        => RunStrategy<NseIndexWithEntryExitMarkersStrategy>();

    [TestMethod]
    public Task Ntk07RangeTrader()
        => RunStrategy<Ntk07RangeTraderStrategy>();

    [TestMethod]
    public Task Ntk07()
        => RunStrategy<Ntk07Strategy>();

    [TestMethod]
    public Task NtoQf()
        => RunStrategy<NtoQfStrategy>();

    [TestMethod]
    public Task Nunchucks()
        => RunStrategy<NunchucksStrategy>();

    [TestMethod]
    public Task NyBreakout()
        => RunStrategy<NyBreakoutStrategy>();

    [TestMethod]
    public Task NyFirstCandleBreakAndRetest()
        => RunStrategy<NyFirstCandleBreakAndRetestStrategy>();

    [TestMethod]
    public Task NyOpeningRangeBreakoutMaStop()
        => RunStrategy<NyOpeningRangeBreakoutMaStopStrategy>();

    [TestMethod]
    public Task NyOrbCp()
        => RunStrategy<NyOrbCpStrategy>();

}
