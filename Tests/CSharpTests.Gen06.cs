namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task CenterOfGravityMeanReversion()
        => RunStrategy<CenterOfGravityMeanReversionStrategy>();

    [TestMethod]
    public Task CenterOfGravityOsma()
        => RunStrategy<CenterOfGravityOsmaStrategy>();

    [TestMethod]
    public Task CenterOfGravity()
        => RunStrategy<CenterOfGravityStrategy>();

    [TestMethod]
    public Task Ch2010Structure()
        => RunStrategy<Ch2010StructureStrategy>();

    [TestMethod]
    public Task ChaikinMomentumScalper()
        => RunStrategy<ChaikinMomentumScalperStrategy>();

    [TestMethod]
    public Task ChaikinVolatilityStochastic()
        => RunStrategy<ChaikinVolatilityStochasticStrategy>();

    [TestMethod]
    public Task Chameleon()
        => RunStrategy<ChameleonStrategy>();

    [TestMethod]
    public Task Champion()
        => RunStrategy<ChampionStrategy>();

    [TestMethod]
    public Task ChandeKrollTrend()
        => RunStrategy<ChandeKrollTrendStrategy>();

    [TestMethod]
    public Task ChandeMomentumOscillator()
        => RunStrategy<ChandeMomentumOscillatorStrategy>();

    [TestMethod]
    public Task ChandelExitReopen()
        => RunStrategy<ChandelExitReopenStrategy>();

    [TestMethod]
    public Task ChandelierExitWith200EmaFilter()
        => RunStrategy<ChandelierExitWith200EmaFilterStrategy>();

    [TestMethod]
    public Task ChangeTpslByPercentage()
        => RunStrategy<ChangeTpslByPercentageStrategy>();

    [TestMethod]
    public Task ChannelEa2()
        => RunStrategy<ChannelEa2Strategy>();

    [TestMethod]
    public Task ChannelEaLimits()
        => RunStrategy<ChannelEaLimitsStrategy>();

    [TestMethod]
    public Task ChannelScalper()
        => RunStrategy<ChannelScalperStrategy>();

    [TestMethod]
    public Task ChannelTrailingStop()
        => RunStrategy<ChannelTrailingStopStrategy>();

    [TestMethod]
    public Task ChannelsEnvelopeCross()
        => RunStrategy<ChannelsEnvelopeCrossStrategy>();

    [TestMethod]
    public Task Channels()
        => RunStrategy<ChannelsStrategy>();

    [TestMethod]
    public Task ChannelsWithNvi()
        => RunStrategy<ChannelsWithNviStrategy>();

    [TestMethod]
    public Task ChaosTraderLite()
        => RunStrategy<ChaosTraderLiteStrategy>();

    [TestMethod]
    public Task Charles137()
        => RunStrategy<Charles137Strategy>();

    [TestMethod]
    public Task CharlesBreakout()
        => RunStrategy<CharlesBreakoutStrategy>();

    [TestMethod]
    public Task CharlesSmaTrailing()
        => RunStrategy<CharlesSmaTrailingStrategy>();

    [TestMethod]
    public Task Charles()
        => RunStrategy<CharlesStrategy>();

    [TestMethod]
    public Task ChartOscillator()
        => RunStrategy<ChartOscillatorStrategy>();

    [TestMethod]
    public Task ChartPatterns()
        => RunStrategy<ChartPatternsStrategy>();

    [TestMethod]
    public Task CheckExecution()
        => RunStrategy<CheckExecutionStrategy>();

    [TestMethod]
    public Task CheckOpenOrders()
        => RunStrategy<CheckOpenOrdersStrategy>();

    [TestMethod]
    public Task CheduecoglioniAlternating()
        => RunStrategy<CheduecoglioniAlternatingStrategy>();

    [TestMethod]
    public Task ChoSmoothedEa()
        => RunStrategy<ChoSmoothedEaStrategy>();

    [TestMethod]
    public Task ChoWithFlat()
        => RunStrategy<ChoWithFlatStrategy>();

    [TestMethod]
    public Task ChopFlowAtrScalp()
        => RunStrategy<ChopFlowAtrScalpStrategy>();

    [TestMethod]
    public Task Cidomo()
        => RunStrategy<CidomoStrategy>();

    [TestMethod]
    public Task CidomoV1()
        => RunStrategy<CidomoV1Strategy>();

    [TestMethod]
    public Task ClassicNackedZScoreArbitrage()
        => RunStrategy<ClassicNackedZScoreArbitrageStrategy>();

    [TestMethod]
    public Task ClassicVirtualTrailing()
        => RunStrategy<ClassicVirtualTrailingStrategy>();

    [TestMethod]
    public Task CleanerScreenersLibrary()
        => RunStrategy<CleanerScreenersLibraryStrategy>();

    [TestMethod]
    public Task CloseAgent()
        => RunStrategy<CloseAgentStrategy>();

    [TestMethod]
    public Task CloseAllPositionsByTime()
        => RunStrategy<CloseAllPositionsByTimeStrategy>();

    [TestMethod]
    public Task CloseAllPositions()
        => RunStrategy<CloseAllPositionsStrategy>();

    [TestMethod]
    public Task CloseAtProfit()
        => RunStrategy<CloseAtProfitStrategy>();

    [TestMethod]
    public Task CloseBasketPairs()
        => RunStrategy<CloseBasketPairsStrategy>();

    [TestMethod]
    public Task CloseByEquityPercent()
        => RunStrategy<CloseByEquityPercentStrategy>();

    [TestMethod]
    public Task CloseCrossKijunSen()
        => RunStrategy<CloseCrossKijunSenStrategy>();

    [TestMethod]
    public Task CloseCrossMa()
        => RunStrategy<CloseCrossMaStrategy>();

    [TestMethod]
    public Task CloseDeleteEa()
        => RunStrategy<CloseDeleteEaStrategy>();

    [TestMethod]
    public Task CloseOnLoss()
        => RunStrategy<CloseOnLossStrategy>();

    [TestMethod]
    public Task CloseOnProfitOrLossInAccountCurrency()
        => RunStrategy<CloseOnProfitOrLossInAccountCurrencyStrategy>();

    [TestMethod]
    public Task CloseOrdersRiskControl()
        => RunStrategy<CloseOrdersRiskControlStrategy>();

    [TestMethod]
    public Task CloseOrders()
        => RunStrategy<CloseOrdersStrategy>();

    [TestMethod]
    public Task ClosePanel()
        => RunStrategy<ClosePanelStrategy>();

    [TestMethod]
    public Task ClosePositionsByTime()
        => RunStrategy<ClosePositionsByTimeStrategy>();

    [TestMethod]
    public Task ClosePositions()
        => RunStrategy<ClosePositionsStrategy>();

    [TestMethod]
    public Task CloseProfitEndOfWeek()
        => RunStrategy<CloseProfitEndOfWeekStrategy>();

    [TestMethod]
    public Task CloseProfitV2()
        => RunStrategy<CloseProfitV2Strategy>();

    [TestMethod]
    public Task CloseVsPreviousOpen()
        => RunStrategy<CloseVsPreviousOpenStrategy>();

    [TestMethod]
    public Task CloudsTrade2()
        => RunStrategy<CloudsTrade2Strategy>();

    [TestMethod]
    public Task CloudzsTrade2()
        => RunStrategy<CloudzsTrade2Strategy>();

    [TestMethod]
    public Task CmFishing()
        => RunStrategy<CmFishingStrategy>();

    [TestMethod]
    public Task CmManualGrid()
        => RunStrategy<CmManualGridStrategy>();

    [TestMethod]
    public Task CmPanel()
        => RunStrategy<CmPanelStrategy>();

    [TestMethod]
    public Task CmRsi()
        => RunStrategy<CmRsiStrategy>();

    [TestMethod]
    public Task CmeEquityFuturesPriceLimits()
        => RunStrategy<CmeEquityFuturesPriceLimitsStrategy>();

    [TestMethod]
    public Task CmoDuplex()
        => RunStrategy<CmoDuplexStrategy>();

    [TestMethod]
    public Task CmoZeroCross()
        => RunStrategy<CmoZeroCrossStrategy>();

    [TestMethod]
    public Task CnagdaFixedSwing()
        => RunStrategy<CnagdaFixedSwingStrategy>();

    [TestMethod]
    public Task CoeffofLineTrue()
        => RunStrategy<CoeffofLineTrueStrategy>();

    [TestMethod]
    public Task CoensioSwingTrader()
        => RunStrategy<CoensioSwingTraderStrategy>();

    [TestMethod]
    public Task CoensioSwingTraderV06()
        => RunStrategy<CoensioSwingTraderV06Strategy>();

    [TestMethod]
    public Task CoensioTrader1V06()
        => RunStrategy<CoensioTrader1V06Strategy>();

    [TestMethod]
    public Task CoinFlipMartingale()
        => RunStrategy<CoinFlipMartingaleStrategy>();

    [TestMethod]
    public Task CoinFlip()
        => RunStrategy<CoinFlipStrategy>();

    [TestMethod]
    public Task CoinFlipping()
        => RunStrategy<CoinFlippingStrategy>();

    [TestMethod]
    public Task ColibriGridManager()
        => RunStrategy<ColibriGridManagerStrategy>();

    [TestMethod]
    public Task CollectorV10()
        => RunStrategy<CollectorV10Strategy>();

    [TestMethod]
    public Task Color3rdGenXma()
        => RunStrategy<Color3rdGenXmaStrategy>();

    [TestMethod]
    public Task ColorBbCandles()
        => RunStrategy<ColorBbCandlesStrategy>();

    [TestMethod]
    public Task ColorBearsGap()
        => RunStrategy<ColorBearsGapStrategy>();

    [TestMethod]
    public Task ColorBears()
        => RunStrategy<ColorBearsStrategy>();

    [TestMethod]
    public Task ColorBullsGap()
        => RunStrategy<ColorBullsGapStrategy>();

    [TestMethod]
    public Task ColorBulls()
        => RunStrategy<ColorBullsStrategy>();

    [TestMethod]
    public Task ColorCodeOverlay()
        => RunStrategy<ColorCodeOverlayStrategy>();

    [TestMethod]
    public Task ColorCoppock()
        => RunStrategy<ColorCoppockStrategy>();

    [TestMethod]
    public Task ColorFisherM11()
        => RunStrategy<ColorFisherM11Strategy>();

    [TestMethod]
    public Task ColorGradientFramework()
        => RunStrategy<ColorGradientFrameworkStrategy>();

    [TestMethod]
    public Task ColorHmaReversal()
        => RunStrategy<ColorHmaReversalStrategy>();

    [TestMethod]
    public Task ColorHmaStDev()
        => RunStrategy<ColorHmaStDevStrategy>();

    [TestMethod]
    public Task ColorJ2JmaStdDev()
        => RunStrategy<ColorJ2JmaStdDevStrategy>();

    [TestMethod]
    public Task ColorJFatlDigitNn3MmRec()
        => RunStrategy<ColorJFatlDigitNn3MmRecStrategy>();

    [TestMethod]
    public Task ColorJFatlDigitReOpen()
        => RunStrategy<ColorJFatlDigitReOpenStrategy>();

    [TestMethod]
    public Task ColorJFatlDigit()
        => RunStrategy<ColorJFatlDigitStrategy>();

    [TestMethod]
    public Task ColorJFatlStDev()
        => RunStrategy<ColorJFatlStDevStrategy>();

    [TestMethod]
    public Task ColorJLaguerre()
        => RunStrategy<ColorJLaguerreStrategy>();

    [TestMethod]
    public Task ColorJMomentum()
        => RunStrategy<ColorJMomentumStrategy>();

    [TestMethod]
    public Task ColorJVariation()
        => RunStrategy<ColorJVariationStrategy>();

    [TestMethod]
    public Task ColorJfatlDigitDuplex()
        => RunStrategy<ColorJfatlDigitDuplexStrategy>();

    [TestMethod]
    public Task ColorJfatlDigitTmPlus()
        => RunStrategy<ColorJfatlDigitTmPlusStrategy>();

    [TestMethod]
    public Task ColorJfatlDigitTm()
        => RunStrategy<ColorJfatlDigitTmStrategy>();

    [TestMethod]
    public Task ColorJjrsxTimePlus()
        => RunStrategy<ColorJjrsxTimePlusStrategy>();

}
