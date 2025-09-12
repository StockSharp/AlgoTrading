using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA crossover strategy with trailing stop and daily profit target.
/// </summary>
public class VaranormalMacNCheezStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _dailyTarget;
	private readonly StrategyParam<decimal> _stopLossAmount;
	private readonly StrategyParam<decimal> _trailOffset;
	private readonly StrategyParam<DataType> _candleType;
	
	private SMA _fastMa;
	private SMA _slowMa;
	
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;
	private bool _dailyTargetReached;
	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private bool _isLong;
	
	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	
	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	
	/// <summary>
	/// Profit target to stop trading for the day.
	/// </summary>
	public decimal DailyTarget { get => _dailyTarget.Value; set => _dailyTarget.Value = value; }
	
	/// <summary>
	/// Fixed stop loss amount.
	/// </summary>
	public decimal StopLossAmount { get => _stopLossAmount.Value; set => _stopLossAmount.Value = value; }
	
	/// <summary>
	/// Trailing stop offset.
	/// </summary>
	public decimal TrailOffset { get => _trailOffset.Value; set => _trailOffset.Value = value; }
	
	/// <summary>
	/// Candle type to use for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public VaranormalMacNCheezStrategy()
	{
		_fastLength = Param(nameof(FastLength), 9)
		.SetDisplay("Fast MA Length", "Fast moving average period", "Parameters")
		.SetCanOptimize(true);
		_slowLength = Param(nameof(SlowLength), 21)
		.SetDisplay("Slow MA Length", "Slow moving average period", "Parameters")
		.SetCanOptimize(true);
		_dailyTarget = Param(nameof(DailyTarget), 200m)
		.SetDisplay("Daily Profit Target", "Close positions when profit reaches this value (0 to disable)", "Risk")
		.SetCanOptimize(true);
		_stopLossAmount = Param(nameof(StopLossAmount), 100m)
		.SetDisplay("Stop Loss Amount", "Fixed stop loss distance", "Risk")
		.SetCanOptimize(true);
		_trailOffset = Param(nameof(TrailOffset), 20m)
		.SetDisplay("Trailing Stop Offset", "Distance for trailing stop", "Risk")
		.SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for analysis", "General");
		
		_prevFast = 0m;
		_prevSlow = 0m;
		_initialized = false;
		_dailyTargetReached = false;
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_isLong = false;
	}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);
	
	_fastMa = new SMA { Length = FastLength };
	_slowMa = new SMA { Length = SlowLength };
	
	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_fastMa, _slowMa, ProcessCandle)
	.Start();
	
	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawIndicator(area, _fastMa);
		DrawIndicator(area, _slowMa);
		DrawOwnTrades(area);
	}
}

private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (!_initialized && _fastMa.IsFormed && _slowMa.IsFormed)
{
_prevFast = fast;
_prevSlow = slow;
_initialized = true;
return;
}

if (!_initialized)
return;

if (DailyTarget > 0m && !_dailyTargetReached && PnL >= DailyTarget)
{
ClosePositions();
_dailyTargetReached = true;
return;
}

var longCondition = _prevFast <= _prevSlow && fast > slow;
var shortCondition = _prevFast >= _prevSlow && fast < slow;

if (!_dailyTargetReached)
{
if (longCondition && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
_entryPrice = candle.ClosePrice;
_highestPrice = candle.HighPrice;
_lowestPrice = candle.LowPrice;
_isLong = true;
}
else if (shortCondition && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
_entryPrice = candle.ClosePrice;
_highestPrice = candle.HighPrice;
_lowestPrice = candle.LowPrice;
_isLong = false;
}
}

if (Position > 0)
{
_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
var hardStop = _entryPrice - StopLossAmount;
var trailStop = _highestPrice - TrailOffset;
var exitLevel = Math.Max(hardStop, trailStop);

if (candle.ClosePrice <= exitLevel)
{
SellMarket(Math.Abs(Position));
ResetPositionData();
}
}
else if (Position < 0)
{
_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);
var hardStop = _entryPrice + StopLossAmount;
var trailStop = _lowestPrice + TrailOffset;
var exitLevel = Math.Min(hardStop, trailStop);

if (candle.ClosePrice >= exitLevel)
{
BuyMarket(Math.Abs(Position));
ResetPositionData();
}
}

_prevFast = fast;
_prevSlow = slow;
}

private void ClosePositions()
{
if (Position > 0)
SellMarket(Math.Abs(Position));
else if (Position < 0)
BuyMarket(Math.Abs(Position));

ResetPositionData();
}

private void ResetPositionData()
{
_entryPrice = 0m;
_highestPrice = 0m;
_lowestPrice = 0m;
_isLong = false;
}
}
