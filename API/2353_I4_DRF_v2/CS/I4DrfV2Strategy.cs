namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// I4 DRF v2 strategy.
/// Uses a custom indicator counting up and down closes to generate signals.
/// </summary>
public class I4DrfV2Strategy : Strategy
{
private readonly StrategyParam<int> _period;
private readonly StrategyParam<bool> _buyPosOpen;
private readonly StrategyParam<bool> _sellPosOpen;
private readonly StrategyParam<bool> _buyPosClose;
private readonly StrategyParam<bool> _sellPosClose;
private readonly StrategyParam<TrendMode> _trendMode;
private readonly StrategyParam<int> _stopLoss;
private readonly StrategyParam<int> _takeProfit;
private readonly StrategyParam<DataType> _candleType;

private DrfIndicator _drf = null!;
private int? _prevColor;
private decimal _entryPrice;
private decimal _stopPrice;
private decimal _takePrice;

/// <summary>
/// Indicator period.
/// </summary>
public int Period
{
get => _period.Value;
set => _period.Value = value;
}

/// <summary>
/// Allow opening long positions.
/// </summary>
public bool BuyPosOpen
{
get => _buyPosOpen.Value;
set => _buyPosOpen.Value = value;
}

/// <summary>
/// Allow opening short positions.
/// </summary>
public bool SellPosOpen
{
get => _sellPosOpen.Value;
set => _sellPosOpen.Value = value;
}

/// <summary>
/// Allow closing long positions on signal.
/// </summary>
public bool BuyPosClose
{
get => _buyPosClose.Value;
set => _buyPosClose.Value = value;
}

/// <summary>
/// Allow closing short positions on signal.
/// </summary>
public bool SellPosClose
{
get => _sellPosClose.Value;
set => _sellPosClose.Value = value;
}

/// <summary>
/// Trend mode.
/// </summary>
public TrendMode TrendMode
{
get => _trendMode.Value;
set => _trendMode.Value = value;
}

/// <summary>
/// Stop loss in price steps.
/// </summary>
public int StopLoss
{
get => _stopLoss.Value;
set => _stopLoss.Value = value;
}

/// <summary>
/// Take profit in price steps.
/// </summary>
public int TakeProfit
{
get => _takeProfit.Value;
set => _takeProfit.Value = value;
}

/// <summary>
/// Candle type for processing.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="I4DrfV2Strategy"/>.
/// </summary>
public I4DrfV2Strategy()
{
_period = Param(nameof(Period), 11)
.SetDisplay("Period", "Indicator period", "Indicator")
.SetGreaterThanZero();

_buyPosOpen = Param(nameof(BuyPosOpen), true)
.SetDisplay("Buy Open", "Allow opening longs", "Trading");

_sellPosOpen = Param(nameof(SellPosOpen), true)
.SetDisplay("Sell Open", "Allow opening shorts", "Trading");

_buyPosClose = Param(nameof(BuyPosClose), true)
.SetDisplay("Buy Close", "Allow closing longs", "Trading");

_sellPosClose = Param(nameof(SellPosClose), true)
.SetDisplay("Sell Close", "Allow closing shorts", "Trading");

_trendMode = Param(nameof(TrendMode), TrendMode.Direct)
.SetDisplay("Trend Mode", "DIRECT - contrarian, NOTDIRECT - trend following", "General");

_stopLoss = Param(nameof(StopLoss), 1000)
.SetDisplay("Stop Loss", "Stop loss in price steps", "Risk");

_takeProfit = Param(nameof(TakeProfit), 2000)
.SetDisplay("Take Profit", "Take profit in price steps", "Risk");

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Candle Type", "Timeframe of candles", "General");
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
_drf?.Reset();
_prevColor = null;
_entryPrice = 0m;
_stopPrice = 0m;
_takePrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

StartProtection();

_drf = new DrfIndicator { Length = Period };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_drf, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _drf);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal drfValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!_drf.IsFormed)
{
_prevColor = drfValue > 0 ? 1 : 0;
return;
}

var currentColor = drfValue > 0 ? 1 : 0;
var step = Security.PriceStep ?? 1m;

// Handle protective stops
if (Position > 0)
{
if ((StopLoss > 0 && candle.ClosePrice <= _stopPrice) || (TakeProfit > 0 && candle.ClosePrice >= _takePrice))
{
SellMarket(Position);
_prevColor = currentColor;
return;
}
}
else if (Position < 0)
{
if ((StopLoss > 0 && candle.ClosePrice >= _stopPrice) || (TakeProfit > 0 && candle.ClosePrice <= _takePrice))
{
BuyMarket(-Position);
_prevColor = currentColor;
return;
}
}

if (_prevColor == null)
{
_prevColor = currentColor;
return;
}

if (TrendMode == TrendMode.Direct)
{
if (_prevColor == 1 && currentColor == 0)
{
if (SellPosClose && Position < 0)
BuyMarket(-Position);
if (BuyPosOpen && Position <= 0)
{
BuyMarket();
_entryPrice = candle.ClosePrice;
_stopPrice = _entryPrice - StopLoss * step;
_takePrice = _entryPrice + TakeProfit * step;
}
}
else if (_prevColor == 0 && currentColor == 1)
{
if (BuyPosClose && Position > 0)
SellMarket(Position);
if (SellPosOpen && Position >= 0)
{
SellMarket();
_entryPrice = candle.ClosePrice;
_stopPrice = _entryPrice + StopLoss * step;
_takePrice = _entryPrice - TakeProfit * step;
}
}
}
else
{
if (_prevColor == 0 && currentColor == 1)
{
if (SellPosClose && Position < 0)
BuyMarket(-Position);
if (BuyPosOpen && Position <= 0)
{
BuyMarket();
_entryPrice = candle.ClosePrice;
_stopPrice = _entryPrice - StopLoss * step;
_takePrice = _entryPrice + TakeProfit * step;
}
}
else if (_prevColor == 1 && currentColor == 0)
{
if (BuyPosClose && Position > 0)
SellMarket(Position);
if (SellPosOpen && Position >= 0)
{
SellMarket();
_entryPrice = candle.ClosePrice;
_stopPrice = _entryPrice + StopLoss * step;
_takePrice = _entryPrice - TakeProfit * step;
}
}
}

_prevColor = currentColor;
}

/// <summary>
/// Trend mode options.
/// </summary>
public enum TrendMode
{
/// <summary>Contrarian trading.</summary>
Direct,
/// <summary>Trend following.</summary>
NotDirect
}

private sealed class DrfIndicator : LengthIndicator<decimal>
{
private readonly Queue<int> _signs = new();
private decimal? _prevPrice;
private int _sum;

protected override IIndicatorValue OnProcess(IIndicatorValue input)
{
var price = input.GetValue<decimal>();

if (_prevPrice is null)
{
_prevPrice = price;
return new DecimalIndicatorValue(this, 0m, input.Time);
}

var sign = price > _prevPrice ? 1 : -1;
_prevPrice = price;

_signs.Enqueue(sign);
_sum += sign;

if (_signs.Count > Length)
_sum -= _signs.Dequeue();

if (_signs.Count < Length)
{
IsFormed = false;
return new DecimalIndicatorValue(this, 0m, input.Time);
}

IsFormed = true;
var value = (decimal)_sum / Length * 100m;
return new DecimalIndicatorValue(this, value, input.Time);
}

public override void Reset()
{
base.Reset();
_signs.Clear();
_prevPrice = null;
_sum = 0;
}
}
}

