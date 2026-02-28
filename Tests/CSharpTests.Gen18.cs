namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task Lanz20Backtest()
        => RunStrategy<Lanz20BacktestStrategy>();

    [TestMethod]
    public Task Lanz30Backtest()
        => RunStrategy<Lanz30BacktestStrategy>();

    [TestMethod]
    public Task Lanz40Backtest()
        => RunStrategy<Lanz40BacktestStrategy>();

    [TestMethod]
    public Task Lanz50()
        => RunStrategy<Lanz50Strategy>();

    [TestMethod]
    public Task Lanz60Backtest()
        => RunStrategy<Lanz60BacktestStrategy>();

    [TestMethod]
    public Task Laptrend1()
        => RunStrategy<Laptrend1Strategy>();

    [TestMethod]
    public Task LarryConnersRsi2()
        => RunStrategy<LarryConnersRsi2Strategy>();

    [TestMethod]
    public Task LarryConnersSmtp()
        => RunStrategy<LarryConnersSmtpStrategy>();

    [TestMethod]
    public Task LarryConnersVixReversalII()
        => RunStrategy<LarryConnersVixReversalIIStrategy>();

    [TestMethod]
    public Task LarryConnors3DayHighLow()
        => RunStrategy<LarryConnors3DayHighLowStrategy>();

    [TestMethod]
    public Task LarryConnorsPercentB()
        => RunStrategy<LarryConnorsPercentBStrategy>();

    [TestMethod]
    public Task LarryConnorsRsi3()
        => RunStrategy<LarryConnorsRsi3Strategy>();

    [TestMethod]
    public Task LastPrice()
        => RunStrategy<LastPriceStrategy>();

    [TestMethod]
    public Task LastZz50()
        => RunStrategy<LastZz50Strategy>();

    [TestMethod]
    public Task Lavika100()
        => RunStrategy<Lavika100Strategy>();

    [TestMethod]
    public Task LayeredRiskProtector()
        => RunStrategy<LayeredRiskProtectorStrategy>();

    [TestMethod]
    public Task LazyBotV1()
        => RunStrategy<LazyBotV1Strategy>();

    [TestMethod]
    public Task Lbs()
        => RunStrategy<LbsStrategy>();

    [TestMethod]
    public Task LbsV12()
        => RunStrategy<LbsV12Strategy>();

    [TestMethod]
    public Task LcsMacdTrader()
        => RunStrategy<LcsMacdTraderStrategy>();

    [TestMethod]
    public Task LeManSignal()
        => RunStrategy<LeManSignalStrategy>();

    [TestMethod]
    public Task LeManTrendHist()
        => RunStrategy<LeManTrendHistStrategy>();

    [TestMethod]
    public Task LeManTrend()
        => RunStrategy<LeManTrendStrategy>();

    [TestMethod]
    public Task Lego4Beta()
        => RunStrategy<Lego4BetaStrategy>();

    [TestMethod]
    public Task LegoEa()
        => RunStrategy<LegoEaStrategy>();

    [TestMethod]
    public Task LegoV3()
        => RunStrategy<LegoV3Strategy>();

    [TestMethod]
    public Task LetItSnow()
        => RunStrategy<LetItSnowStrategy>();

    [TestMethod]
    public Task LevelsWithRevolve()
        => RunStrategy<LevelsWithRevolveStrategy>();

    [TestMethod]
    public Task LevelsWithTrail()
        => RunStrategy<LevelsWithTrailStrategy>();

    [TestMethod]
    public Task LibraryCOT()
        => RunStrategy<LibraryCOTStrategy>();

    [TestMethod]
    public Task LilithGoesToHollywood()
        => RunStrategy<LilithGoesToHollywoodStrategy>();

    [TestMethod]
    public Task LimitsBot()
        => RunStrategy<LimitsBotStrategy>();

    [TestMethod]
    public Task LimitsMartin()
        => RunStrategy<LimitsMartinStrategy>();

    [TestMethod]
    public Task LimitsRsiMomentumBot()
        => RunStrategy<LimitsRsiMomentumBotStrategy>();

    [TestMethod]
    public Task LineOrderDualLevel()
        => RunStrategy<LineOrderDualLevelStrategy>();

    [TestMethod]
    public Task LineOrderSingleEntry()
        => RunStrategy<LineOrderSingleEntryStrategy>();

    [TestMethod]
    public Task LineOrder()
        => RunStrategy<LineOrderStrategy>();

    [TestMethod]
    public Task LinearContinuation()
        => RunStrategy<LinearContinuationStrategy>();

    [TestMethod]
    public Task LinearCorrelationOscillator()
        => RunStrategy<LinearCorrelationOscillatorStrategy>();

    [TestMethod]
    public Task LinearCrossTrading()
        => RunStrategy<LinearCrossTradingStrategy>();

    [TestMethod]
    public Task LinearMeanReversion()
        => RunStrategy<LinearMeanReversionStrategy>();

    [TestMethod]
    public Task LinearOnMacd()
        => RunStrategy<LinearOnMacdStrategy>();

    [TestMethod]
    public Task LinearRegressionAllData()
        => RunStrategy<LinearRegressionAllDataStrategy>();

    [TestMethod]
    public Task LinearRegressionChannelFib()
        => RunStrategy<LinearRegressionChannelFibStrategy>();

    [TestMethod]
    public Task LinearRegressionChannel()
        => RunStrategy<LinearRegressionChannelStrategy>();

    [TestMethod]
    public Task LinearRegressionSlopeTrigger()
        => RunStrategy<LinearRegressionSlopeTriggerStrategy>();

    [TestMethod]
    public Task LinearRegressionSlopeV1()
        => RunStrategy<LinearRegressionSlopeV1Strategy>();

    [TestMethod]
    public Task LiquidPulse()
        => RunStrategy<LiquidPulseStrategy>();

    [TestMethod]
    public Task LiquidexKeltner()
        => RunStrategy<LiquidexKeltnerStrategy>();

    [TestMethod]
    public Task Liquidex()
        => RunStrategy<LiquidexStrategy>();

    [TestMethod]
    public Task LiquidexV1()
        => RunStrategy<LiquidexV1Strategy>();

    [TestMethod]
    public Task LiquidityBreakout()
        => RunStrategy<LiquidityBreakoutStrategy>();

    [TestMethod]
    public Task LiquidityEngulfment()
        => RunStrategy<LiquidityEngulfmentStrategy>();

    [TestMethod]
    public Task LiquidityGrabVolumeTrap()
        => RunStrategy<LiquidityGrabVolumeTrapStrategy>();

    [TestMethod]
    public Task LiquidityInternalMarketShift()
        => RunStrategy<LiquidityInternalMarketShiftStrategy>();

    [TestMethod]
    public Task LiquiditySweepFilter()
        => RunStrategy<LiquiditySweepFilterStrategy>();

    [TestMethod]
    public Task LiquiditySwings()
        => RunStrategy<LiquiditySwingsStrategy>();

    [TestMethod]
    public Task ListPositions()
        => RunStrategy<ListPositionsStrategy>();

    [TestMethod]
    public Task LitecoinTrailingStop()
        => RunStrategy<LitecoinTrailingStopStrategy>();

    [TestMethod]
    public Task LittleEa()
        => RunStrategy<LittleEaStrategy>();

    [TestMethod]
    public Task LiveAlligator()
        => RunStrategy<LiveAlligatorStrategy>();

    [TestMethod]
    public Task LiveRSI()
        => RunStrategy<LiveRSIStrategy>();

    [TestMethod]
    public Task LivermoreSeykotaBreakout()
        => RunStrategy<LivermoreSeykotaBreakoutStrategy>();

    [TestMethod]
    public Task Lock()
        => RunStrategy<LockStrategy>();

    [TestMethod]
    public Task LockerHedgingGrid()
        => RunStrategy<LockerHedgingGridStrategy>();

    [TestMethod]
    public Task Locker()
        => RunStrategy<LockerStrategy>();

    [TestMethod]
    public Task Loco()
        => RunStrategy<LocoStrategy>();

    [TestMethod]
    public Task LogisticRsiStochRocAo()
        => RunStrategy<LogisticRsiStochRocAoStrategy>();

    [TestMethod]
    public Task LondonBreakOutClassic()
        => RunStrategy<LondonBreakOutClassicStrategy>();

    [TestMethod]
    public Task LongAndShortWithMultiIndicators()
        => RunStrategy<LongAndShortWithMultiIndicatorsStrategy>();

    [TestMethod]
    public Task LongEmaAdvancedExit()
        => RunStrategy<LongEmaAdvancedExitStrategy>();

    [TestMethod]
    public Task LongExplosiveV1()
        => RunStrategy<LongExplosiveV1Strategy>();

    [TestMethod]
    public Task LongLegDojiBreakout()
        => RunStrategy<LongLegDojiBreakoutStrategy>();

    [TestMethod]
    public Task LongOnlyMtfEmaCloud()
        => RunStrategy<LongOnlyMtfEmaCloudStrategy>();

    [TestMethod]
    public Task LongOnlyOpeningRangeBreakoutWithPivotPoints()
        => RunStrategy<LongOnlyOpeningRangeBreakoutWithPivotPointsStrategy>();

    [TestMethod]
    public Task LongShortExitRiskManagement()
        => RunStrategy<LongShortExitRiskManagementStrategy>();

    [TestMethod]
    public Task LongShortExpertMacd()
        => RunStrategy<LongShortExpertMacdStrategy>();

    [TestMethod]
    public Task LongTermProfitableSwingAbbas()
        => RunStrategy<LongTermProfitableSwingAbbasStrategy>();

    [TestMethod]
    public Task LoongClock()
        => RunStrategy<LoongClockStrategy>();

    [TestMethod]
    public Task LorenzoSuperScalp()
        => RunStrategy<LorenzoSuperScalpStrategy>();

    [TestMethod]
    public Task LosslessMa()
        => RunStrategy<LosslessMaStrategy>();

    [TestMethod]
    public Task LotScalp()
        => RunStrategy<LotScalpStrategy>();

    [TestMethod]
    public Task LsmaAngle()
        => RunStrategy<LsmaAngleStrategy>();

    [TestMethod]
    public Task LsmaFastSimpleAlternativeCalculation()
        => RunStrategy<LsmaFastSimpleAlternativeCalculationStrategy>();

    [TestMethod]
    public Task Lube()
        => RunStrategy<LubeStrategy>();

    [TestMethod]
    public Task LuckyCode()
        => RunStrategy<LuckyCodeStrategy>();

    [TestMethod]
    public Task LuckyJump()
        => RunStrategy<LuckyJumpStrategy>();

    [TestMethod]
    public Task LuckyShiftLimit()
        => RunStrategy<LuckyShiftLimitStrategy>();

    [TestMethod]
    public Task Lucky()
        => RunStrategy<LuckyStrategy>();

    [TestMethod]
    public Task LunarCalendarDayCryptoTrading()
        => RunStrategy<LunarCalendarDayCryptoTradingStrategy>();

    [TestMethod]
    public Task LuxClaraEmaVwap()
        => RunStrategy<LuxClaraEmaVwapStrategy>();

    [TestMethod]
    public Task MA2CCI()
        => RunStrategy<MA2CCIStrategy>();

    [TestMethod]
    public Task MABreakImpulseBuy()
        => RunStrategy<MABreakImpulseBuyStrategy>();

    [TestMethod]
    public Task MKCustomeAdaptiveSuperTrend()
        => RunStrategy<MKCustomeAdaptiveSuperTrendStrategy>();

    [TestMethod]
    public Task MLTrendE()
        => RunStrategy<MLTrendEStrategy>();

    [TestMethod]
    public Task MNQEMA()
        => RunStrategy<MNQEMAStrategy>();

    [TestMethod]
    public Task MT45()
        => RunStrategy<MT45Strategy>();

    [TestMethod]
    public Task MTrainer()
        => RunStrategy<MTrainerStrategy>();

    [TestMethod]
    public Task MTrendLine()
        => RunStrategy<MTrendLineStrategy>();

    [TestMethod]
    public Task Ma2CciAdaptiveVolume()
        => RunStrategy<Ma2CciAdaptiveVolumeStrategy>();

}
