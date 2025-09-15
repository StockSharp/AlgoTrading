using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on fast and slow TRIX indicator signals.
/// A long position opens when the fast TRIX forms a local bottom and the slow TRIX is rising.
/// A short position opens when the fast TRIX forms a local top and the slow TRIX is falling.
/// </summary>
public class TrixCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	// Store previous TRIX values for decision making
	private decimal _fastTrixPrev1;
	private decimal _fastTrixPrev2;
	private decimal _slowTrixPrev;
	private decimal _prevFastTema;
	private decimal _prevSlowTema;
	private TripleExponentialMovingAverage _fastTema = null!;
	private TripleExponentialMovingAverage _slowTema = null!;

	/// <summary>
	/// Fast TRIX period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
}

/// <summary>
/// Slow TRIX period.
/// </summary>
public int SlowPeriod
{
	get => _slowPeriod.Value;
	set => _slowPeriod.Value = value;
}

/// <summary>
/// Take profit size in absolute price units.
/// </summary>
public decimal TakeProfit
{
	get => _takeProfit.Value;
	set => _takeProfit.Value = value;
}

/// <summary>
/// Stop loss size in absolute price units.
/// </summary>
public decimal StopLoss
{
	get => _stopLoss.Value;
	set => _stopLoss.Value = value;
}

/// <summary>
/// Candle type.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
}

/// <summary>
/// Initializes <see cref="TrixCrossoverStrategy"/>.
/// </summary>
public TrixCrossoverStrategy()
{
	_fastPeriod = Param(nameof(FastPeriod), 9)
	.SetGreaterThanZero()
	.SetDisplay("Fast TRIX Period", "Period for the fast TRIX indicator", "Indicators");

	_slowPeriod = Param(nameof(SlowPeriod), 9)
	.SetGreaterThanZero()
	.SetDisplay("Slow TRIX Period", "Period for the slow TRIX indicator", "Indicators");

	_takeProfit = Param(nameof(TakeProfit), 1500m)
	.SetNotNegative()
	.SetDisplay("Take Profit", "Take profit in absolute price units", "Risk Management");

	_stopLoss = Param(nameof(StopLoss), 500m)
	.SetNotNegative()
	.SetDisplay("Stop Loss", "Stop loss in absolute price units", "Risk Management");

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles to use", "General");
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

	_fastTrixPrev1 = 0m;
	_fastTrixPrev2 = 0m;
	_slowTrixPrev = 0m;
	_prevFastTema = 0m;
	_prevSlowTema = 0m;
	_fastTema = null!;
	_slowTema = null!;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	_fastTema = new TripleExponentialMovingAverage { Length = FastPeriod };
	_slowTema = new TripleExponentialMovingAverage { Length = SlowPeriod };

	var subscription = SubscribeCandles(CandleType);

	subscription
	.Bind(_fastTema, _slowTema, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawIndicator(area, _fastTema);
		DrawIndicator(area, _slowTema);
		DrawOwnTrades(area);
}

StartProtection(new Unit(TakeProfit, UnitTypes.Absolute), new Unit(StopLoss, UnitTypes.Absolute));
}

private void ProcessCandle(ICandleMessage candle, decimal fastTemaValue, decimal slowTemaValue)
{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (_prevFastTema == 0m || _prevSlowTema == 0m)
	{
		_prevFastTema = fastTemaValue;
		_prevSlowTema = slowTemaValue;
		return;
}

var fastTrix = (fastTemaValue - _prevFastTema) / _prevFastTema;
var slowTrix = (slowTemaValue - _prevSlowTema) / _prevSlowTema;

_prevFastTema = fastTemaValue;
_prevSlowTema = slowTemaValue;

// Shift stored values for fast TRIX
_fastTrixPrev2 = _fastTrixPrev1;
_fastTrixPrev1 = fastTrix;

// Store previous slow TRIX value
var slowTrixPrev = _slowTrixPrev;
_slowTrixPrev = slowTrix;

// Need enough history to make decisions
if (_fastTrixPrev2 == 0m || slowTrixPrev == 0m)
return;

// Detect long signal
if (fastTrix > _fastTrixPrev1 && _fastTrixPrev1 < _fastTrixPrev2 && slowTrix > slowTrixPrev && Position <= 0)
{
	BuyMarket(Volume + Math.Abs(Position));
}
// Detect short signal
else if (fastTrix < _fastTrixPrev1 && _fastTrixPrev1 > _fastTrixPrev2 && slowTrix < slowTrixPrev && Position >= 0)
{
	SellMarket(Volume + Math.Abs(Position));
}
}
}
