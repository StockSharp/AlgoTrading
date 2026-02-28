namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task BitcoinLiquidityBreakout()
        => RunStrategy<BitcoinLiquidityBreakoutStrategy>();

    [TestMethod]
    public Task BitcoinMomentum()
        => RunStrategy<BitcoinMomentumStrategy>();

    [TestMethod]
    public Task BitexOneMarketMaker()
        => RunStrategy<BitexOneMarketMakerStrategy>();

    [TestMethod]
    public Task BjCandlePatterns()
        => RunStrategy<BjCandlePatternsStrategy>();

    [TestMethod]
    public Task BjorgumDoubleTap()
        => RunStrategy<BjorgumDoubleTapStrategy>();

    [TestMethod]
    public Task BlackScholesDeltaHedge()
        => RunStrategy<BlackScholesDeltaHedgeStrategy>();

    [TestMethod]
    public Task BlackScholesOptionPricing()
        => RunStrategy<BlackScholesOptionPricingStrategy>();

    [TestMethod]
    public Task BladeRunner()
        => RunStrategy<BladeRunnerStrategy>();

    [TestMethod]
    public Task BlauCMomentum()
        => RunStrategy<BlauCMomentumStrategy>();

    [TestMethod]
    public Task BlauErgodicMdi()
        => RunStrategy<BlauErgodicMdiStrategy>();

    [TestMethod]
    public Task BlauErgodicMdiTime()
        => RunStrategy<BlauErgodicMdiTimeStrategy>();

    [TestMethod]
    public Task BlauErgodic()
        => RunStrategy<BlauErgodicStrategy>();

    [TestMethod]
    public Task BlauSmStochastic()
        => RunStrategy<BlauSmStochasticStrategy>();

    [TestMethod]
    public Task BlauTStochIndicator()
        => RunStrategy<BlauTStochIndicatorStrategy>();

    [TestMethod]
    public Task BlauTsStochastic()
        => RunStrategy<BlauTsStochasticStrategy>();

    [TestMethod]
    public Task BlauTvi()
        => RunStrategy<BlauTviStrategy>();

    [TestMethod]
    public Task BlauTviTimedReversal()
        => RunStrategy<BlauTviTimedReversalStrategy>();

    [TestMethod]
    public Task Bleris()
        => RunStrategy<BlerisStrategy>();

    [TestMethod]
    public Task BlockbusterBollinger()
        => RunStrategy<BlockbusterBollingerStrategy>();

    [TestMethod]
    public Task BlondeTrader()
        => RunStrategy<BlondeTraderStrategy>();

    [TestMethod]
    public Task BloodInTheStreets()
        => RunStrategy<BloodInTheStreetsStrategy>();

    [TestMethod]
    public Task BnB()
        => RunStrategy<BnBStrategy>();

    [TestMethod]
    public Task BoberXm()
        => RunStrategy<BoberXmStrategy>();

    [TestMethod]
    public Task Bobnaley()
        => RunStrategy<BobnaleyStrategy>();

    [TestMethod]
    public Task BoilerplateConfigurable()
        => RunStrategy<BoilerplateConfigurableStrategy>();

    [TestMethod]
    public Task BollTradeBollingerReversion()
        => RunStrategy<BollTradeBollingerReversionStrategy>();

    [TestMethod]
    public Task BollTrade()
        => RunStrategy<BollTradeStrategy>();

    [TestMethod]
    public Task BollingerBandPendingStops()
        => RunStrategy<BollingerBandPendingStopsStrategy>();

    [TestMethod]
    public Task BollingerBandSqueezeBreakout()
        => RunStrategy<BollingerBandSqueezeBreakoutStrategy>();

    [TestMethod]
    public Task BollingerBandTouchSmiMacdAngle()
        => RunStrategy<BollingerBandTouchSmiMacdAngleStrategy>();

    [TestMethod]
    public Task BollingerBandTwoMaZigZag()
        => RunStrategy<BollingerBandTwoMaZigZagStrategy>();

    [TestMethod]
    public Task BollingerBandsAutomated()
        => RunStrategy<BollingerBandsAutomatedStrategy>();

    [TestMethod]
    public Task BollingerBandsDema()
        => RunStrategy<BollingerBandsDemaStrategy>();

    [TestMethod]
    public Task BollingerBandsDistance()
        => RunStrategy<BollingerBandsDistanceStrategy>();

    [TestMethod]
    public Task BollingerBandsEnhanced()
        => RunStrategy<BollingerBandsEnhancedStrategy>();

    [TestMethod]
    public Task BollingerBandsFibonacci()
        => RunStrategy<BollingerBandsFibonacciStrategy>();

    [TestMethod]
    public Task BollingerBandsLong()
        => RunStrategy<BollingerBandsLongStrategy>();

    [TestMethod]
    public Task BollingerBandsMeanReversion()
        => RunStrategy<BollingerBandsMeanReversionStrategy>();

    [TestMethod]
    public Task BollingerBandsModified()
        => RunStrategy<BollingerBandsModifiedStrategy>();

    [TestMethod]
    public Task BollingerBandsNPositions()
        => RunStrategy<BollingerBandsNPositionsStrategy>();

    [TestMethod]
    public Task BollingerBandsNPositionsV2()
        => RunStrategy<BollingerBandsNPositionsV2Strategy>();

    [TestMethod]
    public Task BollingerBandsRsi()
        => RunStrategy<BollingerBandsRsiStrategy>();

    [TestMethod]
    public Task BollingerBandsRsiZones()
        => RunStrategy<BollingerBandsRsiZonesStrategy>();

    [TestMethod]
    public Task BollingerBandsSessionReversal()
        => RunStrategy<BollingerBandsSessionReversalStrategy>();

    [TestMethod]
    public Task BollingerBandsSma202()
        => RunStrategy<BollingerBandsSma202Strategy>();

    [TestMethod]
    public Task BollingerBands()
        => RunStrategy<BollingerBandsStrategy>();

    [TestMethod]
    public Task BollingerBandsTrailingStop()
        => RunStrategy<BollingerBandsTrailingStopStrategy>();

    [TestMethod]
    public Task BollingerBounceReversal()
        => RunStrategy<BollingerBounceReversalStrategy>();

    [TestMethod]
    public Task BollingerBreakout2()
        => RunStrategy<BollingerBreakout2Strategy>();

    [TestMethod]
    public Task BollingerBreakoutDc2008()
        => RunStrategy<BollingerBreakoutDc2008Strategy>();

    [TestMethod]
    public Task BollingerBreakoutDirection()
        => RunStrategy<BollingerBreakoutDirectionStrategy>();

    [TestMethod]
    public Task BollingerBreakoutMomentum()
        => RunStrategy<BollingerBreakoutMomentumStrategy>();

    [TestMethod]
    public Task BollingerChannelRebound()
        => RunStrategy<BollingerChannelReboundStrategy>();

    [TestMethod]
    public Task BollingerEmaStats()
        => RunStrategy<BollingerEmaStatsStrategy>();

    [TestMethod]
    public Task BollingerHeikinAshiEntry()
        => RunStrategy<BollingerHeikinAshiEntryStrategy>();

    [TestMethod]
    public Task BollingerRsiCountertrendSol()
        => RunStrategy<BollingerRsiCountertrendSolStrategy>();

    [TestMethod]
    public Task BollingerRsiMa()
        => RunStrategy<BollingerRsiMaStrategy>();

    [TestMethod]
    public Task BollingerStochasticTrailingStop()
        => RunStrategy<BollingerStochasticTrailingStopStrategy>();

    [TestMethod]
    public Task BonkLongVolatility()
        => RunStrategy<BonkLongVolatilityStrategy>();

    [TestMethod]
    public Task BoringEa2()
        => RunStrategy<BoringEa2Strategy>();

    [TestMethod]
    public Task BotForSpotMarketCustomGrid()
        => RunStrategy<BotForSpotMarketCustomGridStrategy>();

    [TestMethod]
    public Task BounceNumber()
        => RunStrategy<BounceNumberStrategy>();

    [TestMethod]
    public Task BounceStrengthIndex()
        => RunStrategy<BounceStrengthIndexStrategy>();

    [TestMethod]
    public Task BrainTrend2AbsolutelyNoLagLwmaMmrec()
        => RunStrategy<BrainTrend2AbsolutelyNoLagLwmaMmrecStrategy>();

    [TestMethod]
    public Task BrainTrend2AbsolutelyNoLagLwma()
        => RunStrategy<BrainTrend2AbsolutelyNoLagLwmaStrategy>();

    [TestMethod]
    public Task BrainTrend2V2Duplex()
        => RunStrategy<BrainTrend2V2DuplexStrategy>();

    [TestMethod]
    public Task BrakeExpChannel()
        => RunStrategy<BrakeExpChannelStrategy>();

    [TestMethod]
    public Task BrakeExp()
        => RunStrategy<BrakeExpStrategy>();

    [TestMethod]
    public Task BrakeParabolic()
        => RunStrategy<BrakeParabolicStrategy>();

    [TestMethod]
    public Task BrakeoutTraderV1()
        => RunStrategy<BrakeoutTraderV1Strategy>();

    [TestMethod]
    public Task Brandy()
        => RunStrategy<BrandyStrategy>();

    [TestMethod]
    public Task BrandyV12()
        => RunStrategy<BrandyV12Strategy>();

    [TestMethod]
    public Task Breadandbutter2AdxAma()
        => RunStrategy<Breadandbutter2AdxAmaStrategy>();

    [TestMethod]
    public Task Breadandbutter2()
        => RunStrategy<Breadandbutter2Strategy>();

    [TestMethod]
    public Task BreadthThrustVolatilityStop()
        => RunStrategy<BreadthThrustVolatilityStopStrategy>();

    [TestMethod]
    public Task BreakOut15()
        => RunStrategy<BreakOut15Strategy>();

    [TestMethod]
    public Task BreakRevertPro()
        => RunStrategy<BreakRevertProStrategy>();

    [TestMethod]
    public Task BreakTheRangeBound()
        => RunStrategy<BreakTheRangeBoundStrategy>();

    [TestMethod]
    public Task BreakThrough()
        => RunStrategy<BreakThroughStrategy>();

    [TestMethod]
    public Task BreakdownCatcher()
        => RunStrategy<BreakdownCatcherStrategy>();

    [TestMethod]
    public Task BreakdownLevelDay()
        => RunStrategy<BreakdownLevelDayStrategy>();

    [TestMethod]
    public Task BreakdownLevelIntraday()
        => RunStrategy<BreakdownLevelIntradayStrategy>();

    [TestMethod]
    public Task BreakdownPendingStop()
        => RunStrategy<BreakdownPendingStopStrategy>();

    [TestMethod]
    public Task BreakevenTrailingStop()
        => RunStrategy<BreakevenTrailingStopStrategy>();

    [TestMethod]
    public Task BreakevenTrailingStopTick()
        => RunStrategy<BreakevenTrailingStopTickStrategy>();

    [TestMethod]
    public Task BreakevenV3()
        => RunStrategy<BreakevenV3Strategy>();

    [TestMethod]
    public Task Breakout04()
        => RunStrategy<Breakout04Strategy>();

    [TestMethod]
    public Task BreakoutBarsTrend()
        => RunStrategy<BreakoutBarsTrendStrategy>();

    [TestMethod]
    public Task BreakoutNiftyBn()
        => RunStrategy<BreakoutNiftyBnStrategy>();

    [TestMethod]
    public Task Breakout()
        => RunStrategy<BreakoutStrategy>();

    [TestMethod]
    public Task BreakoutsWithTimeFilter()
        => RunStrategy<BreakoutsWithTimeFilterStrategy>();

    [TestMethod]
    public Task BreaksAndRetests()
        => RunStrategy<BreaksAndRetestsStrategy>();

    [TestMethod]
    public Task BreakthroughBb()
        => RunStrategy<BreakthroughBbStrategy>();

    [TestMethod]
    public Task BreakthroughVolatility()
        => RunStrategy<BreakthroughVolatilityStrategy>();

    [TestMethod]
    public Task BroadeningTop()
        => RunStrategy<BroadeningTopStrategy>();

    [TestMethod]
    public Task BronzePan()
        => RunStrategy<BronzePanStrategy>();

    [TestMethod]
    public Task BronzeWarrioir()
        => RunStrategy<BronzeWarrioirStrategy>();

    [TestMethod]
    public Task Bruno()
        => RunStrategy<BrunoStrategy>();

    [TestMethod]
    public Task BrunoTrend()
        => RunStrategy<BrunoTrendStrategy>();

    [TestMethod]
    public Task BssTripleEmaSeparation()
        => RunStrategy<BssTripleEmaSeparationStrategy>();

}
