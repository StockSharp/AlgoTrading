using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Game Theory Trading Strategy combines herd behavior, liquidity traps,
/// institutional flow and Nash equilibrium analysis.
/// </summary>
public class GameTheoryTradingStrategy : Strategy
{
private readonly StrategyParam<int> _rsiLength;
private readonly StrategyParam<int> _volumeMaLength;
private readonly StrategyParam<decimal> _herdThreshold;
private readonly StrategyParam<int> _liquidityLookback;
private readonly StrategyParam<decimal> _instVolumeMultiplier;
private readonly StrategyParam<int> _instMaLength;
private readonly StrategyParam<int> _nashPeriod;
private readonly StrategyParam<decimal> _nashDeviation;
private readonly StrategyParam<bool> _useStopLoss;
private readonly StrategyParam<decimal> _stopLossPercent;
private readonly StrategyParam<bool> _useTakeProfit;
private readonly StrategyParam<decimal> _takeProfitPercent;
private readonly StrategyParam<DataType> _candleType;

private readonly SimpleMovingAverage _volumeSma = new();
private readonly SimpleMovingAverage _momentumSma = new();
private readonly SimpleMovingAverage _adMa = new();
private readonly SimpleMovingAverage _smartMoneySma = new();

private decimal _prevClose;
private decimal _prevRecentHigh;
private decimal _prevRecentLow;
private bool _initialized;

/// <summary>
/// RSI period.
/// </summary>
public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

/// <summary>
/// Volume MA period.
/// </summary>
public int VolumeMaLength { get => _volumeMaLength.Value; set => _volumeMaLength.Value = value; }

/// <summary>
/// Volume spike multiplier for herd detection.
/// </summary>
public decimal HerdThreshold { get => _herdThreshold.Value; set => _herdThreshold.Value = value; }

/// <summary>
/// Lookback period for liquidity traps.
/// </summary>
public int LiquidityLookback { get => _liquidityLookback.Value; set => _liquidityLookback.Value = value; }

/// <summary>
/// Institutional volume multiplier.
/// </summary>
public decimal InstVolumeMultiplier { get => _instVolumeMultiplier.Value; set => _instVolumeMultiplier.Value = value; }

/// <summary>
/// MA period for accumulation/distribution.
/// </summary>
public int InstMaLength { get => _instMaLength.Value; set => _instMaLength.Value = value; }

/// <summary>
/// Period for Nash equilibrium.
/// </summary>
public int NashPeriod { get => _nashPeriod.Value; set => _nashPeriod.Value = value; }

/// <summary>
/// Deviation multiplier for Nash bands.
/// </summary>
public decimal NashDeviation { get => _nashDeviation.Value; set => _nashDeviation.Value = value; }

/// <summary>
/// Enable stop loss.
/// </summary>
public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }

/// <summary>
/// Stop loss percent.
/// </summary>
public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

/// <summary>
/// Enable take profit.
/// </summary>
public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }

/// <summary>
/// Take profit percent.
/// </summary>
public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

/// <summary>
/// Candle type for strategy.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Initializes a new instance of <see cref="GameTheoryTradingStrategy"/>.
/// </summary>
public GameTheoryTradingStrategy()
{
_rsiLength = Param(nameof(RsiLength), 14)
.SetGreaterThanZero()
.SetDisplay("RSI Period", "RSI period", "Herd Behavior");

_volumeMaLength = Param(nameof(VolumeMaLength), 20)
.SetGreaterThanZero()
.SetDisplay("Volume MA", "Volume MA period", "Herd Behavior");

_herdThreshold = Param(nameof(HerdThreshold), 2m)
.SetGreaterThanZero()
.SetDisplay("Herd Threshold", "Volume spike multiplier", "Herd Behavior");

_liquidityLookback = Param(nameof(LiquidityLookback), 50)
.SetGreaterThanZero()
.SetDisplay("Liquidity Lookback", "Lookback for traps", "Liquidity");

_instVolumeMultiplier = Param(nameof(InstVolumeMultiplier), 2.5m)
.SetGreaterThanZero()
.SetDisplay("Inst Volume Mult", "Institutional volume multiplier", "Institutions");

_instMaLength = Param(nameof(InstMaLength), 21)
.SetGreaterThanZero()
.SetDisplay("Inst MA", "AD moving average period", "Institutions");

_nashPeriod = Param(nameof(NashPeriod), 100)
.SetGreaterThanZero()
.SetDisplay("Nash Period", "Nash equilibrium period", "Nash");

_nashDeviation = Param(nameof(NashDeviation), 0.02m)
.SetGreaterThanZero()
.SetDisplay("Nash Deviation", "Deviation multiplier", "Nash");

_useStopLoss = Param(nameof(UseStopLoss), true)
.SetDisplay("Use SL", "Enable stop loss", "Risk");

_stopLossPercent = Param(nameof(StopLossPercent), 2m)
.SetGreaterThanZero()
.SetDisplay("SL %", "Stop loss percent", "Risk");

_useTakeProfit = Param(nameof(UseTakeProfit), true)
.SetDisplay("Use TP", "Enable take profit", "Risk");

_takeProfitPercent = Param(nameof(TakeProfitPercent), 5m)
.SetGreaterThanZero()
.SetDisplay("TP %", "Take profit percent", "Risk");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_prevClose = 0m;
_prevRecentHigh = 0m;
_prevRecentLow = 0m;
_initialized = false;
}
/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_volumeSma.Length = VolumeMaLength;
_momentumSma.Length = 20;
_adMa.Length = InstMaLength;
_smartMoneySma.Length = 20;

var rsi = new RelativeStrengthIndex { Length = RsiLength };
var momentum = new Momentum { Length = 10 };
var ad = new AccumulationDistribution();
var priceMean = new SimpleMovingAverage { Length = NashPeriod };
var stdDev = new StandardDeviation { Length = NashPeriod };
var highest = new Highest { Length = LiquidityLookback };
var lowest = new Lowest { Length = LiquidityLookback };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(rsi, momentum, ad, priceMean, stdDev, highest, lowest, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawOwnTrades(area);
}

StartProtection(
takeProfit: UseTakeProfit ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(0m),
stopLoss: UseStopLoss ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(0m));
}

private void ProcessCandle(
ICandleMessage candle,
decimal rsi,
decimal momentum,
decimal ad,
decimal priceMean,
decimal priceStd,
decimal recentHigh,
decimal recentLow)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var volumeMa = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
var momentumMa = _momentumSma.Process(momentum, candle.ServerTime, true).ToDecimal();
var adAverage = _adMa.Process(ad, candle.ServerTime, true).ToDecimal();

var range = candle.HighPrice - candle.LowPrice;
var smartMoney = range > 0m ? (candle.ClosePrice - candle.OpenPrice) / range * candle.TotalVolume : 0m;
var smartMoneyMa = _smartMoneySma.Process(smartMoney, candle.ServerTime, true).ToDecimal();
var smartMoneyPositive = smartMoney > smartMoneyMa;

var volumeSpike = candle.TotalVolume > volumeMa * HerdThreshold;
var rsiExtremeHigh = rsi > 70m;
var rsiExtremeLow = rsi < 30m;

var herdBuying = rsiExtremeHigh && volumeSpike && momentum > momentumMa;
var herdSelling = rsiExtremeLow && volumeSpike && momentum < momentumMa;

var liquidityTrapUp = _initialized && candle.HighPrice > _prevRecentHigh && candle.ClosePrice < _prevRecentHigh && volumeSpike;
var liquidityTrapDown = _initialized && candle.LowPrice < _prevRecentLow && candle.ClosePrice > _prevRecentLow && volumeSpike;

_prevRecentHigh = recentHigh;
_prevRecentLow = recentLow;

var institutionalVolume = candle.TotalVolume > volumeMa * InstVolumeMultiplier;
var accumulation = ad > adAverage && institutionalVolume;
var distribution = ad < adAverage && institutionalVolume;

var upperNash = priceMean + priceStd * NashDeviation;
var lowerNash = priceMean - priceStd * NashDeviation;
var nearNash = candle.ClosePrice > lowerNash && candle.ClosePrice < upperNash;
var aboveNash = candle.ClosePrice > upperNash;
var belowNash = candle.ClosePrice < lowerNash;

var contrarianBuy = herdSelling && (accumulation || liquidityTrapDown);
var contrarianSell = herdBuying && (distribution || liquidityTrapUp);
var momentumBuy = belowNash && smartMoneyPositive && !herdBuying;
var momentumSell = aboveNash && !smartMoneyPositive && !herdSelling;
var nashReversionBuy = belowNash && candle.ClosePrice > _prevClose && candle.TotalVolume > volumeMa;
var nashReversionSell = aboveNash && candle.ClosePrice < _prevClose && candle.TotalVolume > volumeMa;
var longSignal = contrarianBuy || momentumBuy || nashReversionBuy;
var shortSignal = contrarianSell || momentumSell || nashReversionSell;

var positionSize = 1m;
if (nearNash)
positionSize = 0.5m;
else if (institutionalVolume)
positionSize = 1.5m;

if (longSignal && Position <= 0)
{
var volume = Volume * positionSize + Math.Abs(Position);
BuyMarket(volume);
}
else if (shortSignal && Position >= 0)
{
var volume = Volume * positionSize + Math.Abs(Position);
SellMarket(volume);
}

_prevClose = candle.ClosePrice;
_initialized = true;
}
}
