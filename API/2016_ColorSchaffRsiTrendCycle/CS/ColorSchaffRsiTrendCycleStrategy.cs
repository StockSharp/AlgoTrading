using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Color Schaff RSI Trend Cycle indicator.
/// Reacts to color transitions of the indicator to open or close positions.
/// </summary>
public class ColorSchaffRsiTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastRsi;
	private readonly StrategyParam<int> _slowRsi;
	private readonly StrategyParam<int> _cycle;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevColor;
	private decimal? _prevPrevColor;

	/// <summary>
	/// Fast RSI period.
	/// </summary>
	public int FastRsi
	{
		get => _fastRsi.Value;
		set => _fastRsi.Value = value;
	}

/// <summary>
/// Slow RSI period.
/// </summary>
public int SlowRsi
{
	get => _slowRsi.Value;
	set => _slowRsi.Value = value;
}

/// <summary>
/// Cycle length used in STC calculation.
/// </summary>
public int Cycle
{
	get => _cycle.Value;
	set => _cycle.Value = value;
}

/// <summary>
/// Upper level for color computation.
/// </summary>
public int HighLevel
{
	get => _highLevel.Value;
	set => _highLevel.Value = value;
}

/// <summary>
/// Lower level for color computation.
/// </summary>
public int LowLevel
{
	get => _lowLevel.Value;
	set => _lowLevel.Value = value;
}

/// <summary>
/// Candle type for subscription.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="ColorSchaffRsiTrendCycleStrategy"/>.
/// </summary>
public ColorSchaffRsiTrendCycleStrategy()
{
	_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles for calculations", "General");

	_fastRsi = Param(nameof(FastRsi), 23)
	.SetGreaterThanZero()
	.SetDisplay("Fast RSI", "Fast RSI period", "Parameters")
	.SetCanOptimize(true)
	.SetOptimize(10, 30, 5);

	_slowRsi = Param(nameof(SlowRsi), 50)
	.SetGreaterThanZero()
	.SetDisplay("Slow RSI", "Slow RSI period", "Parameters")
	.SetCanOptimize(true)
	.SetOptimize(30, 70, 5);

	_cycle = Param(nameof(Cycle), 10)
	.SetGreaterThanZero()
	.SetDisplay("Cycle", "Cycle length", "Parameters")
	.SetCanOptimize(true)
	.SetOptimize(5, 20, 1);

	_highLevel = Param(nameof(HighLevel), 60)
	.SetDisplay("High Level", "Upper level for STC", "Parameters")
	.SetCanOptimize(true)
	.SetOptimize(40, 80, 5);

	_lowLevel = Param(nameof(LowLevel), -60)
	.SetDisplay("Low Level", "Lower level for STC", "Parameters")
	.SetCanOptimize(true)
	.SetOptimize(-80, -40, 5);
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	var stc = new SchaffRsiTrendCycle
	{
		FastRsi = FastRsi,
		SlowRsi = SlowRsi,
		Cycle = Cycle,
		HighLevel = HighLevel,
		LowLevel = LowLevel
	};

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(stc, ProcessCandle).Start();

	StartProtection();
}

private void ProcessCandle(ICandleMessage candle, decimal color)
{
	if (candle.State != CandleStates.Finished)
	return;

	_prevPrevColor = _prevColor;
	_prevColor = color;

	if (_prevPrevColor is null)
	return;

	var prev2 = _prevPrevColor.Value;
	var prev1 = _prevColor.Value;

	if (prev2 > 5)
	{
		if (Position < 0)
		BuyMarket();
		if (prev1 < 6 && Position <= 0)
		BuyMarket();
	}
else if (prev2 < 2)
{
	if (Position > 0)
	SellMarket();
	if (prev1 > 1 && Position >= 0)
	SellMarket();
}
}

private class SchaffRsiTrendCycle : Indicator<decimal>
{
	public int FastRsi { get; set; }
public int SlowRsi { get; set; }
public int Cycle { get; set; }
public int HighLevel { get; set; }
public int LowLevel { get; set; }

private readonly Rsi _fast = new();
private readonly Rsi _slow = new();
private readonly Queue<decimal> _macd = new();
private readonly Queue<decimal> _st = new();
private decimal? _prevSt;
private decimal? _prevStc;
private bool _st1Pass;
private bool _st2Pass;

protected override IIndicatorValue OnProcess(IIndicatorValue input)
{
	var price = input.GetValue<decimal>();

	var fastVal = _fast.Process(new DecimalIndicatorValue(_fast, price, input.Time)).GetValue<decimal>();
	var slowVal = _slow.Process(new DecimalIndicatorValue(_slow, price, input.Time)).GetValue<decimal>();

	if (!_fast.IsFormed || !_slow.IsFormed)
	return new DecimalIndicatorValue(this, default, input.Time);

	var macd = fastVal - slowVal;
	_macd.Enqueue(macd);
	if (_macd.Count > Cycle)
	_macd.Dequeue();

	if (_macd.Count < Cycle)
	return new DecimalIndicatorValue(this, default, input.Time);

	var llv = _macd.Min();
	var hhv = _macd.Max();

	var st = hhv != llv ? (macd - llv) / (hhv - llv) * 100m : _prevSt ?? 0m;

	if (_st1Pass && _prevSt.HasValue)
	st = 0.5m * (st - _prevSt.Value) + _prevSt.Value;

	_st1Pass = true;
	_prevSt = st;

	_st.Enqueue(st);
	if (_st.Count > Cycle)
	_st.Dequeue();

	if (_st.Count < Cycle)
	return new DecimalIndicatorValue(this, default, input.Time);

	llv = _st.Min();
	hhv = _st.Max();

	var stc = hhv != llv ? (st - llv) / (hhv - llv) * 200m - 100m : _prevStc ?? 0m;

	if (_st2Pass && _prevStc.HasValue)
	stc = 0.5m * (stc - _prevStc.Value) + _prevStc.Value;

	var diff = _prevStc.HasValue ? stc - _prevStc.Value : 0m;
	_st2Pass = true;
	_prevStc = stc;

	decimal clr = 4m;
	if (stc > 0)
	{
		if (stc > HighLevel)
		clr = diff >= 0 ? 7m : 6m;
		else
		clr = diff >= 0 ? 5m : 4m;
	}
else if (stc < 0)
{
	if (stc < LowLevel)
	clr = diff < 0 ? 0m : 1m;
	else
	clr = diff < 0 ? 2m : 3m;
}

return new DecimalIndicatorValue(this, clr, input.Time);
}

public override void Reset()
{
	base.Reset();
	_fast.Length = FastRsi;
	_slow.Length = SlowRsi;
	_fast.Reset();
	_slow.Reset();
	_macd.Clear();
	_st.Clear();
	_prevSt = null;
	_prevStc = null;
	_st1Pass = false;
	_st2Pass = false;
}
}
}
