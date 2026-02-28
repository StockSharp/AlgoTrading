namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task ColorJjrsxTrend()
        => RunStrategy<ColorJjrsxTrendStrategy>();

    [TestMethod]
    public Task ColorJsatlDigit()
        => RunStrategy<ColorJsatlDigitStrategy>();

    [TestMethod]
    public Task ColorLaguerre()
        => RunStrategy<ColorLaguerreStrategy>();

    [TestMethod]
    public Task ColorLemanTrend()
        => RunStrategy<ColorLemanTrendStrategy>();

    [TestMethod]
    public Task ColorMaRsiTriggerDuplex()
        => RunStrategy<ColorMaRsiTriggerDuplexStrategy>();

    [TestMethod]
    public Task ColorMaRsiTriggerMmRecDuplex()
        => RunStrategy<ColorMaRsiTriggerMmRecDuplexStrategy>();

    [TestMethod]
    public Task ColorMaRsiTrigger()
        => RunStrategy<ColorMaRsiTriggerStrategy>();

    [TestMethod]
    public Task ColorMetroDeMarker()
        => RunStrategy<ColorMetroDeMarkerStrategy>();

    [TestMethod]
    public Task ColorMetroDuplex()
        => RunStrategy<ColorMetroDuplexStrategy>();

    [TestMethod]
    public Task ColorMetroStochastic()
        => RunStrategy<ColorMetroStochasticStrategy>();

    [TestMethod]
    public Task ColorMetro()
        => RunStrategy<ColorMetroStrategy>();

    [TestMethod]
    public Task ColorMetroWpr()
        => RunStrategy<ColorMetroWprStrategy>();

    [TestMethod]
    public Task ColorMetroXrsx()
        => RunStrategy<ColorMetroXrsxStrategy>();

    [TestMethod]
    public Task ColorMomentumAma()
        => RunStrategy<ColorMomentumAmaStrategy>();

    [TestMethod]
    public Task ColorNonLagDotMacd()
        => RunStrategy<ColorNonLagDotMacdStrategy>();

    [TestMethod]
    public Task ColorPemaEnvelopesDigitSystem()
        => RunStrategy<ColorPemaEnvelopesDigitSystemStrategy>();

    [TestMethod]
    public Task ColorRsiMacd()
        => RunStrategy<ColorRsiMacdStrategy>();

    [TestMethod]
    public Task ColorSchaffDeMarkerTrendCycle()
        => RunStrategy<ColorSchaffDeMarkerTrendCycleStrategy>();

    [TestMethod]
    public Task ColorSchaffJccxTrendCycleMmrecDuplex()
        => RunStrategy<ColorSchaffJccxTrendCycleMmrecDuplexStrategy>();

    [TestMethod]
    public Task ColorSchaffJccxTrendCycle()
        => RunStrategy<ColorSchaffJccxTrendCycleStrategy>();

    [TestMethod]
    public Task ColorSchaffJjrsxMmrecDuplex()
        => RunStrategy<ColorSchaffJjrsxMmrecDuplexStrategy>();

    [TestMethod]
    public Task ColorSchaffJjrsxTrendCycle()
        => RunStrategy<ColorSchaffJjrsxTrendCycleStrategy>();

    [TestMethod]
    public Task ColorSchaffMfiTrendCycle()
        => RunStrategy<ColorSchaffMfiTrendCycleStrategy>();

    [TestMethod]
    public Task ColorSchaffMomentumTrendCycle()
        => RunStrategy<ColorSchaffMomentumTrendCycleStrategy>();

    [TestMethod]
    public Task ColorSchaffRsiTrendCycle()
        => RunStrategy<ColorSchaffRsiTrendCycleStrategy>();

    [TestMethod]
    public Task ColorSchaffRviTrendCycle()
        => RunStrategy<ColorSchaffRviTrendCycleStrategy>();

    [TestMethod]
    public Task ColorSchaffTrendCycle()
        => RunStrategy<ColorSchaffTrendCycleStrategy>();

    [TestMethod]
    public Task ColorSchaffTrixTrendCycle()
        => RunStrategy<ColorSchaffTrixTrendCycleStrategy>();

    [TestMethod]
    public Task ColorSchaffWprTrendCycle()
        => RunStrategy<ColorSchaffWprTrendCycleStrategy>();

    [TestMethod]
    public Task ColorStepXccx()
        => RunStrategy<ColorStepXccxStrategy>();

    [TestMethod]
    public Task ColorStochNr()
        => RunStrategy<ColorStochNrStrategy>();

    [TestMethod]
    public Task Color()
        => RunStrategy<ColorStrategy>();

    [TestMethod]
    public Task ColorTrendCf()
        => RunStrategy<ColorTrendCfStrategy>();

    [TestMethod]
    public Task ColorX2MaDigitNn3Mmrec()
        => RunStrategy<ColorX2MaDigitNn3MmrecStrategy>();

    [TestMethod]
    public Task ColorX2MaDigit()
        => RunStrategy<ColorX2MaDigitStrategy>();

    [TestMethod]
    public Task ColorXAdx()
        => RunStrategy<ColorXAdxStrategy>();

    [TestMethod]
    public Task ColorXDerivative()
        => RunStrategy<ColorXDerivativeStrategy>();

    [TestMethod]
    public Task ColorXMacdCandle()
        => RunStrategy<ColorXMacdCandleStrategy>();

    [TestMethod]
    public Task ColorXXDPO()
        => RunStrategy<ColorXXDPOStrategy>();

    [TestMethod]
    public Task ColorXccxCandle()
        => RunStrategy<ColorXccxCandleStrategy>();

    [TestMethod]
    public Task ColorXdinMAStDev()
        => RunStrategy<ColorXdinMAStDevStrategy>();

    [TestMethod]
    public Task ColorXdinMA()
        => RunStrategy<ColorXdinMAStrategy>();

    [TestMethod]
    public Task ColorXmuvTime()
        => RunStrategy<ColorXmuvTimeStrategy>();

    [TestMethod]
    public Task ColorXpWmaDigitMmRec()
        => RunStrategy<ColorXpWmaDigitMmRecStrategy>();

    [TestMethod]
    public Task ColorXpWmaDigitMultiTimeframe()
        => RunStrategy<ColorXpWmaDigitMultiTimeframeStrategy>();

    [TestMethod]
    public Task ColorXtrixHistogram()
        => RunStrategy<ColorXtrixHistogramStrategy>();

    [TestMethod]
    public Task ColorXvaMADigit()
        => RunStrategy<ColorXvaMADigitStrategy>();

    [TestMethod]
    public Task ColorXvaMaDigitStDev()
        => RunStrategy<ColorXvaMaDigitStDevStrategy>();

    [TestMethod]
    public Task ColorZeroLagMa()
        => RunStrategy<ColorZeroLagMaStrategy>();

    [TestMethod]
    public Task ColorZerolagDeMarker()
        => RunStrategy<ColorZerolagDeMarkerStrategy>();

    [TestMethod]
    public Task ColorZerolagHlr()
        => RunStrategy<ColorZerolagHlrStrategy>();

    [TestMethod]
    public Task ColorZerolagJccx()
        => RunStrategy<ColorZerolagJccxStrategy>();

    [TestMethod]
    public Task ColorZerolagJjrsx()
        => RunStrategy<ColorZerolagJjrsxStrategy>();

    [TestMethod]
    public Task ColorZerolagMomentumOsma()
        => RunStrategy<ColorZerolagMomentumOsmaStrategy>();

    [TestMethod]
    public Task ColorZerolagMomentumX2()
        => RunStrategy<ColorZerolagMomentumX2Strategy>();

    [TestMethod]
    public Task ColorZerolagRsiOsma()
        => RunStrategy<ColorZerolagRsiOsmaStrategy>();

    [TestMethod]
    public Task ColorZerolagRvi()
        => RunStrategy<ColorZerolagRviStrategy>();

    [TestMethod]
    public Task ColorZerolagTrixOsma()
        => RunStrategy<ColorZerolagTrixOsmaStrategy>();

    [TestMethod]
    public Task ColorZerolagTrix()
        => RunStrategy<ColorZerolagTrixStrategy>();

    [TestMethod]
    public Task ColorZerolagX10Ma()
        => RunStrategy<ColorZerolagX10MaStrategy>();

    [TestMethod]
    public Task ComFractiFractalRsi()
        => RunStrategy<ComFractiFractalRsiStrategy>();

    [TestMethod]
    public Task ComFracti()
        => RunStrategy<ComFractiStrategy>();

    [TestMethod]
    public Task Combo123ReversalFractalChaosBands()
        => RunStrategy<Combo123ReversalFractalChaosBandsStrategy>();

    [TestMethod]
    public Task Combo220EmaBandpassFilter()
        => RunStrategy<Combo220EmaBandpassFilterStrategy>();

    [TestMethod]
    public Task Combo220EmaCci()
        => RunStrategy<Combo220EmaCciStrategy>();

    [TestMethod]
    public Task ComboEa4FsfrUpdated5()
        => RunStrategy<ComboEa4FsfrUpdated5Strategy>();

    [TestMethod]
    public Task ComboRightPerceptron()
        => RunStrategy<ComboRightPerceptronStrategy>();

    [TestMethod]
    public Task ComboRight()
        => RunStrategy<ComboRightStrategy>();

    [TestMethod]
    public Task CommissionCalculator()
        => RunStrategy<CommissionCalculatorStrategy>();

    [TestMethod]
    public Task CommitmentOfTraderR()
        => RunStrategy<CommitmentOfTraderRStrategy>();

    [TestMethod]
    public Task CommonLabelLineArrayFunctions()
        => RunStrategy<CommonLabelLineArrayFunctionsStrategy>();

    [TestMethod]
    public Task CompassLine()
        => RunStrategy<CompassLineStrategy>();

    [TestMethod]
    public Task ConditionalPositionOpener()
        => RunStrategy<ConditionalPositionOpenerStrategy>();

    [TestMethod]
    public Task ConnectDisconnectSoundAlert()
        => RunStrategy<ConnectDisconnectSoundAlertStrategy>();

    [TestMethod]
    public Task Connectable()
        => RunStrategy<ConnectableStrategy>();

    [TestMethod]
    public Task ConnorsVixReversalIII()
        => RunStrategy<ConnorsVixReversalIIIStrategy>();

    [TestMethod]
    public Task ConsecutiveBarsAboveBelowEMABuyTheDip()
        => RunStrategy<ConsecutiveBarsAboveBelowEMABuyTheDipStrategy>();

    [TestMethod]
    public Task ConsecutiveBarsAboveMa()
        => RunStrategy<ConsecutiveBarsAboveMaStrategy>();

    [TestMethod]
    public Task ConsecutiveBearishCandle()
        => RunStrategy<ConsecutiveBearishCandleStrategy>();

    [TestMethod]
    public Task ConsecutiveCloseHigh1MeanReversion()
        => RunStrategy<ConsecutiveCloseHigh1MeanReversionStrategy>();

    [TestMethod]
    public Task ConsolidationBreakout()
        => RunStrategy<ConsolidationBreakoutStrategy>();

    [TestMethod]
    public Task ConstituentsEa()
        => RunStrategy<ConstituentsEaStrategy>();

    [TestMethod]
    public Task ContrarianDc()
        => RunStrategy<ContrarianDcStrategy>();

    [TestMethod]
    public Task ContrarianTradeMaMonday()
        => RunStrategy<ContrarianTradeMaMondayStrategy>();

    [TestMethod]
    public Task ContrarianTradeMa()
        => RunStrategy<ContrarianTradeMaStrategy>();

    [TestMethod]
    public Task ContrarianTradeMaWeekly()
        => RunStrategy<ContrarianTradeMaWeeklyStrategy>();

    [TestMethod]
    public Task ControlPanel()
        => RunStrategy<ControlPanelStrategy>();

    [TestMethod]
    public Task CoppockHistogram()
        => RunStrategy<CoppockHistogramStrategy>();

    [TestMethod]
    public Task CorrTime()
        => RunStrategy<CorrTimeStrategy>();

    [TestMethod]
    public Task CorrectedAverageBreakout()
        => RunStrategy<CorrectedAverageBreakoutStrategy>();

    [TestMethod]
    public Task CorrectedAverageChannel()
        => RunStrategy<CorrectedAverageChannelStrategy>();

    [TestMethod]
    public Task CorrelationArrays()
        => RunStrategy<CorrelationArraysStrategy>();

    [TestMethod]
    public Task CorrelationCycle()
        => RunStrategy<CorrelationCycleStrategy>();

    [TestMethod]
    public Task CorrelationMatrixHeatmap()
        => RunStrategy<CorrelationMatrixHeatmapStrategy>();

    [TestMethod]
    public Task Costar()
        => RunStrategy<CostarStrategy>();

    [TestMethod]
    public Task CountAndWait()
        => RunStrategy<CountAndWaitStrategy>();

    [TestMethod]
    public Task CountOrders()
        => RunStrategy<CountOrdersStrategy>();

    [TestMethod]
    public Task CoupleHedgeBasket()
        => RunStrategy<CoupleHedgeBasketStrategy>();

    [TestMethod]
    public Task CoupleHedge()
        => RunStrategy<CoupleHedgeStrategy>();

    [TestMethod]
    public Task CovidStatisticsTracker()
        => RunStrategy<CovidStatisticsTrackerStrategy>();

}
