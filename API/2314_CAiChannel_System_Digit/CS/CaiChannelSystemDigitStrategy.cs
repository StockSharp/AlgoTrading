using System;
using System.Collections.Generic;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified port of the i-CAiChannel System Digit expert.
/// </summary>
public class CaiChannelSystemDigitStrategy : Strategy
{
private readonly StrategyParam<int> _length;
private readonly StrategyParam<decimal> _width;
private readonly StrategyParam<DataType> _candle;
private bool _prevUp;
private bool _prevDown;

public int Length { get => _length.Value; set => _length.Value = value; }
public decimal Width { get => _width.Value; set => _width.Value = value; }
public DataType CandleType { get => _candle.Value; set => _candle.Value = value; }

public CaiChannelSystemDigitStrategy()
{
_length = Param(nameof(Length), 12).SetGreaterThan(0);
_width = Param(nameof(Width), 2m).SetGreaterThanZero();
_candle = Param(nameof(CandleType), TimeSpan.FromHours(12).TimeFrame());
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);
var bb = new BollingerBands { Length = Length, Width = Width };
var sub = SubscribeCandles(CandleType);
sub.Bind(bb, Process).Start();
}

private void Process(ICandleMessage candle, decimal mid, decimal up, decimal down)
{
if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
return;
if (_prevUp && candle.ClosePrice <= up && Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
else if (_prevDown && candle.ClosePrice >= down && Position >= 0)
SellMarket(Volume + Math.Abs(Position));
_prevUp = candle.ClosePrice > up;
_prevDown = candle.ClosePrice < down;
}
}
