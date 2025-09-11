using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades on price crossovers with a quadratic regression line.
/// </summary>
public class QuadraticRegressionStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _prices = new();
	private bool? _wasAbove;

	/// <summary>
	/// Regression length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
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
	/// Initializes a new instance of <see cref="QuadraticRegressionStrategy"/>.
	/// </summary>
	public QuadraticRegressionStrategy()
	{
		_length = Param(nameof(Length), 54)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Number of bars for regression", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 10);

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
		_prices.Clear();
		_wasAbove = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prices.Enqueue(candle.ClosePrice);
		if (_prices.Count > Length)
			_prices.Dequeue();

		if (_prices.Count < Length)
			return;

		var regression = CalculateRegression();

		var isAbove = candle.ClosePrice > regression;

		if (_wasAbove == null)
		{
			_wasAbove = isAbove;
			return;
		}

		if (isAbove != _wasAbove)
		{
			if (isAbove && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (!isAbove && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}

			_wasAbove = isAbove;
		}
	}

	private decimal CalculateRegression()
	{
		var prices = _prices.ToArray();
		var n = prices.Length;

		double sx = 0;
		double sxx = 0;
		double sxxx = 0;
		double sxxxx = 0;
		double sy = 0;
		double sxy = 0;
		double sxxy = 0;

		for (var i = 0; i < n; i++)
		{
			var x = (double)i;
			var y = (double)prices[i];
			sx += x;
			sxx += x * x;
			sxxx += x * x * x;
			sxxxx += x * x * x * x;
			sy += y;
			sxy += x * y;
			sxxy += x * x * y;
		}

		var det = Det(sxxxx, sxxx, sxx, sxxx, sxx, sx, sxx, sx, n);
		if (det == 0)
			return prices[^1];

		var a = Det(sxxy, sxxx, sxx, sxy, sxx, sx, sy, sx, n) / det;
		var b = Det(sxxxx, sxxy, sxx, sxxx, sxy, sx, sxx, sy, n) / det;
		var c = Det(sxxxx, sxxx, sxxy, sxxx, sxx, sxy, sxx, sx, sy) / det;

		var xLast = n - 1;
		return (decimal)(a * xLast * xLast + b * xLast + c);
	}

	private static double Det(double a1, double a2, double a3,
	double b1, double b2, double b3,
	double c1, double c2, double c3)
	{
	return a1 * (b2 * c3 - b3 * c2)
	- a2 * (b1 * c3 - b3 * c1)
	+ a3 * (b1 * c2 - b2 * c1);
	}
}
