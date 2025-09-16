using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on a parabolic brake indicator.
/// Opens long when trend flips to bullish and short when it turns bearish.
/// </summary>
public class BrakeParabolicStrategy : Strategy
{
	private readonly StrategyParam<decimal> _a;
	private readonly StrategyParam<decimal> _b;
	private readonly StrategyParam<decimal> _beginShift;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _maxPrice;
	private decimal _minPrice;
	private decimal _beginPrice;
	private bool _isLong;
	private int _barsFromBegin;
	private decimal _bCoef;
	private decimal _shiftValue;

	/// <summary>
	/// Exponent that defines curve shape.
	/// </summary>
	public decimal A
	{
		get => _a.Value;
		set => _a.Value = value;
	}

	/// <summary>
	/// Multiplier affecting curve speed.
	/// </summary>
	public decimal B
	{
		get => _b.Value;
		set => _b.Value = value;
	}

	/// <summary>
	/// Initial shift in points.
	/// </summary>
	public decimal BeginShift
	{
		get => _beginShift.Value;
		set => _beginShift.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public BrakeParabolicStrategy()
	{
		_a = Param(nameof(A), 1.5m)
		.SetDisplay("A", "Curve exponent", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);

		_b = Param(nameof(B), 1.0m)
		.SetDisplay("B", "Curve speed", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2m, 0.1m);

		_beginShift = Param(nameof(BeginShift), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Shift", "Initial shift in points", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5m, 20m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();

		// Pre-calculate coefficients based on security settings
		var priceStep = Security.PriceStep ?? 1m;
		var seconds = (decimal)((TimeSpan)CandleType.Arg).TotalSeconds;
		_bCoef = B * priceStep * seconds * 0.1m / 60m;
		_shiftValue = BeginShift * priceStep;

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Do(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}

		StartProtection();
	}

	private void ResetState()
	{
		_maxPrice = decimal.MinValue;
		_minPrice = decimal.MaxValue;
		_beginPrice = 0m;
		_isLong = true;
		_barsFromBegin = 0;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Initialize start price on first call
		if (_beginPrice == 0m)
		{
			_beginPrice = candle.LowPrice;
		}

		if (candle.HighPrice > _maxPrice)
			_maxPrice = candle.HighPrice;
		if (candle.LowPrice < _minPrice)
			_minPrice = candle.LowPrice;

		// Calculate parabolic level
		var parab = (decimal)Math.Pow(_barsFromBegin, (double)A) * _bCoef;
		var level = _isLong ? _beginPrice + parab : _beginPrice - parab;

		var buySignal = false;
		var sellSignal = false;

		// Check for trend change
		if (_isLong && level > candle.LowPrice)
		{
			// Bullish trend broken - switch to short
			_isLong = false;
			_beginPrice = _maxPrice + _shiftValue;
			level = _beginPrice;
			_barsFromBegin = 0;
			_maxPrice = decimal.MinValue;
			_minPrice = decimal.MaxValue;
			sellSignal = true;
		}
		else if (!_isLong && level < candle.HighPrice)
		{
			// Bearish trend broken - switch to long
			_isLong = true;
			_beginPrice = _minPrice - _shiftValue;
			level = _beginPrice;
			_barsFromBegin = 0;
			_maxPrice = decimal.MinValue;
			_minPrice = decimal.MaxValue;
			buySignal = true;
		}
		else
		{
			_barsFromBegin++;
		}

		// Execute trading actions
		if (buySignal)
		{
			if (Position < 0)
				BuyMarket(-Position);
			BuyMarket(Volume);
		}
		else if (sellSignal)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket(Volume);
		}
		else
		{
			// Close opposite positions if trend opposes
			if (_isLong && Position < 0)
				BuyMarket(-Position);
			else if (!_isLong && Position > 0)
				SellMarket(Position);
		}
	}
}