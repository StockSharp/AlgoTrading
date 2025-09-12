using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Murrey Math Fibonacci breakout strategy.
/// Calculates Fibonacci levels based on Murrey Math and trades breakouts.
/// </summary>
public class MmFibonacciStrategy : Strategy
{
	private readonly StrategyParam<int> _frame;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	private int _sinceHigh;
	private int _sinceLow;

	/// <summary>
	/// Frame size.
	/// </summary>
	public int Frame
	{
		get => _frame.Value;
		set => _frame.Value = value;
	}

	/// <summary>
	/// Frame multiplier.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	/// Initializes a new instance of <see cref="MmFibonacciStrategy"/>.
	/// </summary>
	public MmFibonacciStrategy()
	{
		_frame = Param(nameof(Frame), 64)
			.SetGreaterThanZero()
			.SetDisplay("Frame Size", "Murrey frame size", "General")
			.SetCanOptimize(true);

		_multiplier = Param(nameof(Multiplier), 1.5m)
			.SetDisplay("Multiplier", "Frame multiplier", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_highest = default;
		_lowest = default;
		_sinceHigh = 0;
		_sinceLow = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var length = (int)Math.Round(Frame * Multiplier);

		_highest = new Highest { Length = length };
		_lowest = new Lowest { Length = length };

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highValue = _highest.Process(candle.HighPrice);
		var lowValue = _lowest.Process(candle.LowPrice);

		if (!highValue.IsFinal || !lowValue.IsFinal)
			return;

		var nHigh = highValue.ToDecimal();
		var nLow = lowValue.ToDecimal();

		var range = nHigh - nLow;
		if (range == 0m)
			return;

		decimal fractal;

		if (nHigh <= 250000m && nHigh > 25000m)
			fractal = 100000m;
		else if (nHigh <= 25000m && nHigh > 2500m)
			fractal = 10000m;
		else if (nHigh <= 2500m && nHigh > 250m)
			fractal = 1000m;
		else if (nHigh <= 250m && nHigh > 25m)
			fractal = 100m;
		else if (nHigh <= 25m && nHigh > 6.25m)
			fractal = 12.5m;
		else if (nHigh <= 6.25m && nHigh > 3.125m)
			fractal = 6.25m;
		else if (nHigh <= 3.125m && nHigh > 1.5625m)
			fractal = 3.125m;
		else if (nHigh <= 1.5625m && nHigh > 0.390625m)
			fractal = 1.5625m;
		else
			fractal = 0.1953125m;

		var sum = (decimal)Math.Floor(Math.Log((double)(fractal / range), 2));
		var octave = fractal * (decimal)Math.Pow(0.5, (double)sum);
		var minimum = Math.Floor(nLow / octave) * octave;
		var maximum = (minimum + octave) > nHigh ? minimum + octave : minimum + 2m * octave;

		decimal t1 = 0m, t2 = 0m, t3 = 0m, t4 = 0m, t5 = 0m;
		var diff = maximum - minimum;

		if (nLow >= (3m * diff / 16m + minimum) && nHigh <= (9m * diff / 16m + minimum))
			t2 = minimum + diff / 2m;

		if (nLow >= (minimum - diff / 8m) && nHigh <= (5m * diff / 8m + minimum) && t2 == 0m)
			t1 = minimum + diff / 2m;

		if (nLow >= (minimum + 7m * diff / 16m) && nHigh <= (13m * diff / 16m + minimum))
			t4 = minimum + 3m * diff / 4m;

		if (nLow >= (minimum + 3m * diff / 8m) && nHigh <= (9m * diff / 8m + minimum) && t4 == 0m)
			t5 = maximum;

		if (nLow >= (minimum + diff / 8m) && nHigh <= (7m * diff / 8m + minimum) && t1 == 0m && t2 == 0m && t4 == 0m && t5 == 0m)
			t3 = minimum + 3m * diff / 4m;

		var t6 = (t1 + t2 + t3 + t4 + t5) == 0m ? maximum : 0m;

		var top = t1 + t2 + t3 + t4 + t5 + t6;

		decimal b1 = 0m, b2 = 0m, b3 = 0m, b4 = 0m, b5 = 0m;
		if (t1 > 0m)
			b1 = minimum;
		if (t2 > 0m)
			b2 = minimum + diff / 4m;
		if (t3 > 0m)
			b3 = minimum + diff / 4m;
		if (t4 > 0m)
			b4 = minimum + diff / 2m;
		if (t5 > 0m)
			b5 = minimum + diff / 2m;
		var b6 = (top > 0m && (b1 + b2 + b3 + b4 + b5) == 0m) ? minimum : 0m;

		var bottom = b1 + b2 + b3 + b4 + b5 + b6;
		var fibRange = top - bottom;
		if (fibRange == 0m)
			return;

		_sinceHigh = candle.HighPrice >= top ? 0 : _sinceHigh + 1;
		_sinceLow = candle.LowPrice <= bottom ? 0 : _sinceLow + 1;

		var fibDirUp = _sinceHigh > _sinceLow;
		var fibDirDn = _sinceHigh < _sinceLow;

		var fib100 = fibDirUp ? top : bottom;
		var fib0 = fibDirUp ? bottom : top;
		var fib50 = fibDirUp ? bottom + fibRange * 0.5m : top - fibRange * 0.5m;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (fibDirUp)
		{
			if (candle.ClosePrice > fib100 && Position <= 0)
				BuyMarket();
			else if (Position > 0 && candle.ClosePrice < fib50)
				SellMarket(Math.Abs(Position));
		}
		else if (fibDirDn)
		{
			if (candle.ClosePrice < fib0 && Position >= 0)
				SellMarket();
			else if (Position < 0 && candle.ClosePrice > fib50)
				BuyMarket(Math.Abs(Position));
		}
	}
}
