namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task RelativeCandle()
        => RunStrategy<RelativeCandleStrategy>();

    [TestMethod]
    public Task RelativeCurrencyStrength()
        => RunStrategy<RelativeCurrencyStrengthStrategy>();

    [TestMethod]
    public Task RelativeStrengthRsmkPlusPerk()
        => RunStrategy<RelativeStrengthRsmkPlusPerkStrategy>();

    [TestMethod]
    public Task RelativeStrength()
        => RunStrategy<RelativeStrengthStrategy>();

    [TestMethod]
    public Task RelativeVolumeAtTime()
        => RunStrategy<RelativeVolumeAtTimeStrategy>();

    [TestMethod]
    public Task ReminderMessageWithColorPicker()
        => RunStrategy<ReminderMessageWithColorPickerStrategy>();

    [TestMethod]
    public Task RenkoChartFromTicks()
        => RunStrategy<RenkoChartFromTicksStrategy>();

    [TestMethod]
    public Task RenkoChart()
        => RunStrategy<RenkoChartStrategy>();

    [TestMethod]
    public Task RenkoFractalsGrid()
        => RunStrategy<RenkoFractalsGridStrategy>();

    [TestMethod]
    public Task RenkoLevelEa()
        => RunStrategy<RenkoLevelEaStrategy>();

    [TestMethod]
    public Task RenkoLevel()
        => RunStrategy<RenkoLevelStrategy>();

    [TestMethod]
    public Task RenkoLineBreakVsRsi()
        => RunStrategy<RenkoLineBreakVsRsiStrategy>();

    [TestMethod]
    public Task RenkoLiveChart()
        => RunStrategy<RenkoLiveChartStrategy>();

    [TestMethod]
    public Task RenkoLiveChartsPimped()
        => RunStrategy<RenkoLiveChartsPimpedStrategy>();

    [TestMethod]
    public Task RenkoRsi()
        => RunStrategy<RenkoRsiStrategy>();

    [TestMethod]
    public Task RenkoScalper()
        => RunStrategy<RenkoScalperStrategy>();

    [TestMethod]
    public Task Renko()
        => RunStrategy<RenkoStrategy>();

    [TestMethod]
    public Task RenkoTrendReversal()
        => RunStrategy<RenkoTrendReversalStrategy>();

    [TestMethod]
    public Task RenkoTrendReversalV2()
        => RunStrategy<RenkoTrendReversalV2Strategy>();

    [TestMethod]
    public Task ResamplingFilterPack()
        => RunStrategy<ResamplingFilterPackStrategy>();

    [TestMethod]
    public Task ResamplingReverseEngineeringBands()
        => RunStrategy<ResamplingReverseEngineeringBandsStrategy>();

    [TestMethod]
    public Task ResonanceHunter()
        => RunStrategy<ResonanceHunterStrategy>();

    [TestMethod]
    public Task ResponsiveLinearRegressionChannels()
        => RunStrategy<ResponsiveLinearRegressionChannelsStrategy>();

    [TestMethod]
    public Task Return()
        => RunStrategy<ReturnStrategy>();

    [TestMethod]
    public Task Revelations()
        => RunStrategy<RevelationsStrategy>();

    [TestMethod]
    public Task ReversalBreakoutOrb()
        => RunStrategy<ReversalBreakoutOrbStrategy>();

    [TestMethod]
    public Task ReversalCatcher()
        => RunStrategy<ReversalCatcherStrategy>();

    [TestMethod]
    public Task ReversalFinder()
        => RunStrategy<ReversalFinderStrategy>();

    [TestMethod]
    public Task ReversalTradingBot()
        => RunStrategy<ReversalTradingBotStrategy>();

    [TestMethod]
    public Task ReversalTrapSniper()
        => RunStrategy<ReversalTrapSniperStrategy>();

    [TestMethod]
    public Task ReversalsWithPinBars()
        => RunStrategy<ReversalsWithPinBarsStrategy>();

    [TestMethod]
    public Task ReverseDayFractal()
        => RunStrategy<ReverseDayFractalStrategy>();

    [TestMethod]
    public Task ReverseKeltnerChannel()
        => RunStrategy<ReverseKeltnerChannelStrategy>();

    [TestMethod]
    public Task Reverse()
        => RunStrategy<ReverseStrategy>();

    [TestMethod]
    public Task ReversingMartingale()
        => RunStrategy<ReversingMartingaleStrategy>();

    [TestMethod]
    public Task RevisedSelfAdaptiveEa()
        => RunStrategy<RevisedSelfAdaptiveEaStrategy>();

    [TestMethod]
    public Task RevolutionVolatilityBandsWithRangeContractionSignalVII()
        => RunStrategy<RevolutionVolatilityBandsWithRangeContractionSignalVIIStrategy>();

    [TestMethod]
    public Task RgtEaRsi()
        => RunStrategy<RgtEaRsiStrategy>();

    [TestMethod]
    public Task RgtRsiBollinger()
        => RunStrategy<RgtRsiBollingerStrategy>();

    [TestMethod]
    public Task RichKohonenMap()
        => RunStrategy<RichKohonenMapStrategy>();

    [TestMethod]
    public Task RideAlligator()
        => RunStrategy<RideAlligatorStrategy>();

    [TestMethod]
    public Task RideAlligatorWilliams()
        => RunStrategy<RideAlligatorWilliamsStrategy>();

    [TestMethod]
    public Task RijfiePyramid()
        => RunStrategy<RijfiePyramidStrategy>();

    [TestMethod]
    public Task RingSystemEa()
        => RunStrategy<RingSystemEaStrategy>();

    [TestMethod]
    public Task RingSystem()
        => RunStrategy<RingSystemStrategy>();

    [TestMethod]
    public Task RiskBasedTrailingManager()
        => RunStrategy<RiskBasedTrailingManagerStrategy>();

    [TestMethod]
    public Task RiskFixedMargin()
        => RunStrategy<RiskFixedMarginStrategy>();

    [TestMethod]
    public Task RiskManagementAndPositionsizeMacdExample()
        => RunStrategy<RiskManagementAndPositionsizeMacdExampleStrategy>();

    [TestMethod]
    public Task RiskManagementAtr()
        => RunStrategy<RiskManagementAtrStrategy>();

    [TestMethod]
    public Task RiskManagerInfoPanel()
        => RunStrategy<RiskManagerInfoPanelStrategy>();

    [TestMethod]
    public Task RiskManager()
        => RunStrategy<RiskManagerStrategy>();

    [TestMethod]
    public Task RiskMonitor()
        => RunStrategy<RiskMonitorStrategy>();

    [TestMethod]
    public Task RiskProfitCloser()
        => RunStrategy<RiskProfitCloserStrategy>();

    [TestMethod]
    public Task RiskRewardRatio()
        => RunStrategy<RiskRewardRatioStrategy>();

    [TestMethod]
    public Task RiskToRewardFixedSlBacktester()
        => RunStrategy<RiskToRewardFixedSlBacktesterStrategy>();

    [TestMethod]
    public Task RksFrameworkAutoColorGradient()
        => RunStrategy<RksFrameworkAutoColorGradientStrategy>();

    [TestMethod]
    public Task RmStochasticBand()
        => RunStrategy<RmStochasticBandStrategy>();

    [TestMethod]
    public Task RmacdReversal()
        => RunStrategy<RmacdReversalStrategy>();

    [TestMethod]
    public Task RmiTrendSync()
        => RunStrategy<RmiTrendSyncStrategy>();

    [TestMethod]
    public Task RndTradeRandomHold()
        => RunStrategy<RndTradeRandomHoldStrategy>();

    [TestMethod]
    public Task RndTrade()
        => RunStrategy<RndTradeStrategy>();

    [TestMethod]
    public Task RnnProbability()
        => RunStrategy<RnnProbabilityStrategy>();

    [TestMethod]
    public Task RoBoost()
        => RunStrategy<RoBoostStrategy>();

    [TestMethod]
    public Task RoNzAutoSlTsTp()
        => RunStrategy<RoNzAutoSlTsTpStrategy>();

    [TestMethod]
    public Task RoNzRapidFire()
        => RunStrategy<RoNzRapidFireStrategy>();

    [TestMethod]
    public Task RobotAdxTwoMa()
        => RunStrategy<RobotAdxTwoMaStrategy>();

    [TestMethod]
    public Task RobotDanu()
        => RunStrategy<RobotDanuStrategy>();

    [TestMethod]
    public Task RobotPowerM5Meta4V12()
        => RunStrategy<RobotPowerM5Meta4V12Strategy>();

    [TestMethod]
    public Task RobotPowerM5()
        => RunStrategy<RobotPowerM5Strategy>();

    [TestMethod]
    public Task RobotiAdxProfit()
        => RunStrategy<RobotiAdxProfitStrategy>();

    [TestMethod]
    public Task RobustEaTemplate()
        => RunStrategy<RobustEaTemplateStrategy>();

    [TestMethod]
    public Task Roc2Vg()
        => RunStrategy<Roc2VgStrategy>();

    [TestMethod]
    public Task Roc()
        => RunStrategy<RocStrategy>();

    [TestMethod]
    public Task RockTraderNeuro()
        => RunStrategy<RockTraderNeuroStrategy>();

    [TestMethod]
    public Task RollbackRebound()
        => RunStrategy<RollbackReboundStrategy>();

    [TestMethod]
    public Task RollbackSystem()
        => RunStrategy<RollbackSystemStrategy>();

    [TestMethod]
    public Task RomanDirectionFlip()
        => RunStrategy<RomanDirectionFlipStrategy>();

    [TestMethod]
    public Task RonzAutoSltp()
        => RunStrategy<RonzAutoSltpStrategy>();

    [TestMethod]
    public Task RouletteGame()
        => RunStrategy<RouletteGameStrategy>();

    [TestMethod]
    public Task Rpm5BullsBearsEyes()
        => RunStrategy<Rpm5BullsBearsEyesStrategy>();

    [TestMethod]
    public Task RrsChaotic()
        => RunStrategy<RrsChaoticStrategy>();

    [TestMethod]
    public Task RrsImpulse()
        => RunStrategy<RrsImpulseStrategy>();

    [TestMethod]
    public Task RrsNonDirectional()
        => RunStrategy<RrsNonDirectionalStrategy>();

    [TestMethod]
    public Task RrsRandomness()
        => RunStrategy<RrsRandomnessStrategy>();

    [TestMethod]
    public Task RrsTangledEa()
        => RunStrategy<RrsTangledEaStrategy>();

    [TestMethod]
    public Task Rsi3070()
        => RunStrategy<Rsi3070Strategy>();

    [TestMethod]
    public Task RsiAdaptiveT3SqueezeMomentum()
        => RunStrategy<RsiAdaptiveT3SqueezeMomentumStrategy>();

    [TestMethod]
    public Task RsiAdaptiveT3()
        => RunStrategy<RsiAdaptiveT3Strategy>();

    [TestMethod]
    public Task RsiAdxLongShort()
        => RunStrategy<RsiAdxLongShortStrategy>();

    [TestMethod]
    public Task RsiAlert()
        => RunStrategy<RsiAlertStrategy>();

    [TestMethod]
    public Task RsiAndAtrTrendReversalSlTp()
        => RunStrategy<RsiAndAtrTrendReversalSlTpStrategy>();

    [TestMethod]
    public Task RsiAutomated()
        => RunStrategy<RsiAutomatedStrategy>();

    [TestMethod]
    public Task RsiBackedWeightedMa()
        => RunStrategy<RsiBackedWeightedMaStrategy>();

    [TestMethod]
    public Task RsiBollingerBandsEa()
        => RunStrategy<RsiBollingerBandsEaStrategy>();

    [TestMethod]
    public Task RsiBollingerBands()
        => RunStrategy<RsiBollingerBandsStrategy>();

    [TestMethod]
    public Task RsiBollingerFractalBreakout()
        => RunStrategy<RsiBollingerFractalBreakoutStrategy>();

    [TestMethod]
    public Task RsiBooster()
        => RunStrategy<RsiBoosterStrategy>();

    [TestMethod]
    public Task RsiBoxPseudoGridBot()
        => RunStrategy<RsiBoxPseudoGridBotStrategy>();

    [TestMethod]
    public Task RsiBuySellForce()
        => RunStrategy<RsiBuySellForceStrategy>();

    [TestMethod]
    public Task RsiCciDivergence()
        => RunStrategy<RsiCciDivergenceStrategy>();

}
