using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with ATR based exits and partial profit.
/// </summary>
public class AtrExitStrategyStrategy : Strategy
{
private readonly StrategyParam<int> _fastLen;
private readonly StrategyParam<int> _slowLen;
private readonly StrategyParam<int> _atrLen;
private readonly StrategyParam<DataType> _candleType;

private decimal _entryPrice;
private decimal _stopPrice;
private decimal _takeProfitPrice;
private decimal _atrRef;
private decimal _atrDown;
private decimal _atrUp;
private bool _tookProfit;

public int FastLength { get => _fastLen.Value; set => _fastLen.Value = value; }
public int SlowLength { get => _slowLen.Value; set => _slowLen.Value = value; }
public int AtrLength { get => _atrLen.Value; set => _atrLen.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public AtrExitStrategyStrategy()
{
_fastLen = Param(nameof(FastLength), 5).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "General");
_slowLen = Param(nameof(SlowLength), 20).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "General");
_atrLen = Param(nameof(AtrLength), 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "General");
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to process", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_entryPrice = 0m;
_stopPrice = 0m;
_takeProfitPrice = 0m;
_atrRef = 0m;
_atrDown = 0m;
_atrUp = 0m;
_tookProfit = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var fast = new EMA { Length = FastLength };
var slow = new EMA { Length = SlowLength };
var atr = new Atr { Length = AtrLength };

subscribe();

void subscribe()
{
var sub = SubscribeCandles(CandleType);
sub.Bind(fast, slow, atr, Process).Start();
}
}

private void Process(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var avg = (candle.HighPrice + candle.LowPrice) / 2m;

if (Position == 0)
{
if (fast > slow)
{
_entryPrice = avg;
_atrRef = atr;
_stopPrice = _entryPrice - 1.5m * _atrRef;
_takeProfitPrice = _entryPrice + 3m * _atrRef;
_atrDown = _entryPrice - 1.5m * _atrRef;
_atrUp = _entryPrice + 1m * _atrRef;
_tookProfit = false;
BuyMarket(2);
}
return;
}

var stopCondition = avg < _stopPrice;
var takeProfitCondition = avg > _takeProfitPrice;

if (avg < _atrDown)
stopCondition = true;

if (avg > _atrUp)
{
if (_tookProfit)
_atrRef = atr;
var atrDiv = Math.Floor((avg - _entryPrice) / _atrRef);
_atrDown = _entryPrice + _atrRef * (atrDiv - 1m);
_atrUp = _entryPrice + _atrRef * (atrDiv + 1m);
}

if (takeProfitCondition && !_tookProfit)
{
SellMarket(1);
_tookProfit = true;
}

if (stopCondition && Position > 0)
{
SellMarket(Position);
_tookProfit = false;
}
}
}
