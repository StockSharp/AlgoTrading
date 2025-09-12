using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified port of the "Volume by Session" indicator.
/// Calculates average volume for four intraday sessions and trades on deviations.
/// </summary>
public class VolumeBySessionStrategy : Strategy
{
private readonly StrategyParam<int> _smaLength;
private readonly StrategyParam<int> _session1Start;
private readonly StrategyParam<int> _session1End;
private readonly StrategyParam<int> _session2Start;
private readonly StrategyParam<int> _session2End;
private readonly StrategyParam<int> _session3Start;
private readonly StrategyParam<int> _session3End;
private readonly StrategyParam<int> _session4Start;
private readonly StrategyParam<int> _session4End;
private readonly StrategyParam<DataType> _candleType;

private SMA _sma1;
private SMA _sma2;
private SMA _sma3;
private SMA _sma4;

public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
public int Session1Start { get => _session1Start.Value; set => _session1Start.Value = value; }
public int Session1End { get => _session1End.Value; set => _session1End.Value = value; }
public int Session2Start { get => _session2Start.Value; set => _session2Start.Value = value; }
public int Session2End { get => _session2End.Value; set => _session2End.Value = value; }
public int Session3Start { get => _session3Start.Value; set => _session3Start.Value = value; }
public int Session3End { get => _session3End.Value; set => _session3End.Value = value; }
public int Session4Start { get => _session4Start.Value; set => _session4Start.Value = value; }
public int Session4End { get => _session4End.Value; set => _session4End.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public VolumeBySessionStrategy()
{
_smaLength = Param(nameof(SmaLength), 20).SetDisplay("SMA Length", null, "General").SetGreaterThanZero();
_session1Start = Param(nameof(Session1Start), 0).SetDisplay("Session1 Start", null, "Sessions");
_session1End = Param(nameof(Session1End), 6).SetDisplay("Session1 End", null, "Sessions");
_session2Start = Param(nameof(Session2Start), 6).SetDisplay("Session2 Start", null, "Sessions");
_session2End = Param(nameof(Session2End), 12).SetDisplay("Session2 End", null, "Sessions");
_session3Start = Param(nameof(Session3Start), 12).SetDisplay("Session3 Start", null, "Sessions");
_session3End = Param(nameof(Session3End), 18).SetDisplay("Session3 End", null, "Sessions");
_session4Start = Param(nameof(Session4Start), 18).SetDisplay("Session4 Start", null, "Sessions");
_session4End = Param(nameof(Session4End), 24).SetDisplay("Session4 End", null, "Sessions");
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", null, "General");
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_sma1 = new SMA { Length = SmaLength };
_sma2 = new SMA { Length = SmaLength };
_sma3 = new SMA { Length = SmaLength };
_sma4 = new SMA { Length = SmaLength };

var subscription = SubscribeCandles(CandleType);

subscription.Bind(candle =>
{
if (candle.State != CandleStates.Finished)
return;

var hour = candle.OpenTime.LocalDateTime.Hour;

ProcessSession(candle, hour, Session1Start, Session1End, _sma1);
ProcessSession(candle, hour, Session2Start, Session2End, _sma2);
ProcessSession(candle, hour, Session3Start, Session3End, _sma3);
ProcessSession(candle, hour, Session4Start, Session4End, _sma4);
});

subscription.Start();
}

private void ProcessSession(ICandleMessage candle, int hour, int start, int end, SMA sma)
{
if (hour < start || hour >= end)
return;

var val = sma.Process(candle.OpenTime, candle.TotalVolume);
if (!val.IsFormed || !val.TryGetValue(out var avg))
return;

if (candle.TotalVolume > avg && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
}
else if (candle.TotalVolume < avg && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
}
}
}
