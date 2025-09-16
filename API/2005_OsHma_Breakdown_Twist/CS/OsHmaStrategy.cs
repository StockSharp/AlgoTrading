using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the OsHMA oscillator.
/// It trades on zero crossings or direction changes of the oscillator.
/// </summary>
public class OsHmaStrategy : Strategy
{
	public enum OsHmaMode
	{
		Breakdown,
		Twist
	}

	private readonly StrategyParam<int> _fastHma;
	private readonly StrategyParam<int> _slowHma;
	private readonly StrategyParam<OsHmaMode> _mode;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private decimal _prevValue;
	private decimal _prevPrevValue;

	/// <summary>
	/// Period of fast Hull Moving Average.
	/// </summary>
	public int FastHma
	{
		get => _fastHma.Value;
		set => _fastHma.Value = value;
	}

	/// <summary>
	/// Period of slow Hull Moving Average.
	/// </summary>
	public int SlowHma
	{
		get => _slowHma.Value;
		set => _slowHma.Value = value;
	}

	/// <summary>
	/// Trading mode.
	/// </summary>
	public OsHmaMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Candle type to subscribe to.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public OsHmaStrategy()
	{
		_fastHma = Param(nameof(FastHma), 13)
		.SetDisplay("Fast HMA", "Length of fast Hull Moving Average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 20, 1);

		_slowHma = Param(nameof(SlowHma), 26)
		.SetDisplay("Slow HMA", "Length of slow Hull Moving Average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 2);

		_mode = Param(nameof(Mode), OsHmaMode.Twist)
		.SetDisplay("Mode", "Breakdown – zero crossing, Twist – direction change", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
		.SetDisplay("Take Profit", "Target profit in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(500m, 4000m, 500m);

		_stopLoss = Param(nameof(StopLoss), 1000m)
		.SetDisplay("Stop Loss", "Loss limit in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(300m, 3000m, 300m);
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
	_prevValue = 0m;
	_prevPrevValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	var fastHma = new HullMovingAverage { Length = FastHma };
	var slowHma = new HullMovingAverage { Length = SlowHma };

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(fastHma, slowHma, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, fastHma);
	DrawIndicator(area, slowHma);
	DrawOwnTrades(area);
	}

	StartProtection(
	takeProfit: new Unit(TakeProfit, UnitTypes.Point),
	stopLoss: new Unit(StopLoss, UnitTypes.Point),
	isStopTrailing: false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
	// Ignore unfinished candles
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var current = fastValue - slowValue;

	// Initialize on the first call
	if (_prevValue == 0m && _prevPrevValue == 0m)
	{
	_prevPrevValue = _prevValue;
	_prevValue = current;
	return;
	}

	var buySignal = false;
	var sellSignal = false;

	switch (Mode)
	{
	case OsHmaMode.Breakdown:
	buySignal = _prevValue > 0m && current <= 0m;
	sellSignal = _prevValue < 0m && current >= 0m;
	break;

	case OsHmaMode.Twist:
	buySignal = _prevPrevValue < _prevValue && current > _prevValue;
	sellSignal = _prevPrevValue > _prevValue && current < _prevValue;
	break;
	}

	if (buySignal && Position <= 0)
	{
	// Close short positions and open long
	var volume = Volume + Math.Abs(Position);
	BuyMarket(volume);
	}
	else if (sellSignal && Position >= 0)
	{
	// Close long positions and open short
	var volume = Volume + Math.Abs(Position);
	SellMarket(volume);
	}

	_prevPrevValue = _prevValue;
	_prevValue = current;
	}
}

