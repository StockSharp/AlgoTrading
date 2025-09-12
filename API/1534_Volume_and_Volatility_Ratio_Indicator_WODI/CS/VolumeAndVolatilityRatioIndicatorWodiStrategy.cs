using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified port of the "Volume and Volatility Ratio Indicator-WODI" strategy.
/// Detects increased volume and volatility to enter reversal trades.
/// </summary>
public class VolumeAndVolatilityRatioIndicatorWodiStrategy : Strategy
{
private readonly StrategyParam<int> _volLength;
private readonly StrategyParam<int> _indexShortLength;
private readonly StrategyParam<int> _indexLongLength;
private readonly StrategyParam<decimal> _indexThreshold;
private readonly StrategyParam<int> _lookbackBars;
private readonly StrategyParam<bool> _reversalMode;
private readonly StrategyParam<decimal> _stopLossFib;
private readonly StrategyParam<decimal> _takeProfitFib;
private readonly StrategyParam<DataType> _candleType;

private decimal _stopLoss;
private decimal _takeProfit;
private ICandleMessage _prevCandle;
private ICandleMessage _prevPrevCandle;

public int VolLength { get => _volLength.Value; set => _volLength.Value = value; }
public int IndexShortLength { get => _indexShortLength.Value; set => _indexShortLength.Value = value; }
public int IndexLongLength { get => _indexLongLength.Value; set => _indexLongLength.Value = value; }
public decimal IndexThreshold { get => _indexThreshold.Value; set => _indexThreshold.Value = value; }
public int LookbackBars { get => _lookbackBars.Value; set => _lookbackBars.Value = value; }
public bool ReversalMode { get => _reversalMode.Value; set => _reversalMode.Value = value; }
public decimal StopLossFib { get => _stopLossFib.Value; set => _stopLossFib.Value = value; }
public decimal TakeProfitFib { get => _takeProfitFib.Value; set => _takeProfitFib.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public VolumeAndVolatilityRatioIndicatorWodiStrategy()
{
_volLength = Param(nameof(VolLength), 48).SetDisplay("Volume MA Length", null, "Common").SetGreaterThanZero();
_indexShortLength = Param(nameof(IndexShortLength), 13).SetDisplay("Short Index MA", null, "Common").SetGreaterThanZero();
_indexLongLength = Param(nameof(IndexLongLength), 26).SetDisplay("Long Index MA", null, "Common").SetGreaterThanZero();
_indexThreshold = Param(nameof(IndexThreshold), 200m).SetDisplay("Index Threshold %", null, "Common").SetGreaterThanZero();
_lookbackBars = Param(nameof(LookbackBars), 3).SetDisplay("Lookback Bars", null, "Common").SetGreaterThanZero();
_reversalMode = Param(nameof(ReversalMode), false).SetDisplay("Reversal Mode", null, "Position");
_stopLossFib = Param(nameof(StopLossFib), 0m).SetDisplay("Stop Loss Fib", null, "Risk");
_takeProfitFib = Param(nameof(TakeProfitFib), 1.618m).SetDisplay("Take Profit Fib", null, "Risk");
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", null, "General");
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var volMa = new SMA { Length = VolLength };
var indexShortMa = new SMA { Length = IndexShortLength };
var indexLongMa = new SMA { Length = IndexLongLength };

var subscription = SubscribeCandles(CandleType);

subscription.Bind(candle =>
{
if (candle.State != CandleStates.Finished)
return;

var volMaValue = volMa.Process(candle.OpenTime, candle.TotalVolume);
var volatility = (candle.HighPrice - candle.LowPrice) / candle.ClosePrice * 100m;
var volatilityIndex = candle.TotalVolume * volatility;
var shortVal = indexShortMa.Process(candle.OpenTime, volatilityIndex);
var longVal = indexLongMa.Process(candle.OpenTime, volatilityIndex);

if (!volMaValue.IsFinal || !shortVal.IsFinal || !longVal.IsFinal)
return;

if (!volMaValue.TryGetValue(out var volAvg) ||
!shortVal.TryGetValue(out var _) ||
!longVal.TryGetValue(out var longAvg))
return;

var threshold = longAvg * IndexThreshold / 100m;

var isLongPattern = _prevCandle != null && _prevPrevCandle != null &&
volatilityIndex > threshold &&
candle.TotalVolume > volAvg &&
_prevCandle.ClosePrice < _prevPrevCandle.ClosePrice &&
candle.ClosePrice > _prevCandle.ClosePrice;

var isShortPattern = _prevCandle != null && _prevPrevCandle != null &&
volatilityIndex > threshold &&
candle.TotalVolume > volAvg &&
_prevCandle.ClosePrice > _prevPrevCandle.ClosePrice &&
candle.ClosePrice < _prevCandle.ClosePrice;

var longEntry = ReversalMode ? isShortPattern : isLongPattern;
var shortEntry = ReversalMode ? isLongPattern : isShortPattern;

if (longEntry && Position <= 0)
{
var range = candle.HighPrice - candle.LowPrice;
_stopLoss = candle.LowPrice - range * StopLossFib;
_takeProfit = candle.LowPrice + range * TakeProfitFib;
BuyMarket(Volume + Math.Abs(Position));
}
else if (shortEntry && Position >= 0)
{
var range = candle.HighPrice - candle.LowPrice;
_stopLoss = candle.HighPrice + range * StopLossFib;
_takeProfit = candle.HighPrice - range * TakeProfitFib;
SellMarket(Volume + Math.Abs(Position));
}

if (Position > 0)
{
if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
SellMarket(Math.Abs(Position));
}
else if (Position < 0)
{
if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _takeProfit)
BuyMarket(Math.Abs(Position));
}

_prevPrevCandle = _prevCandle;
_prevCandle = candle;
});

subscription.Start();
}
}
