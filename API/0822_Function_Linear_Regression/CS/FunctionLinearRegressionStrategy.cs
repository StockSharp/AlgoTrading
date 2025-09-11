using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that draws a linear regression channel using pivot points.
/// </summary>
public class FunctionLinearRegressionStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _prices = new();
	private readonly Queue<decimal> _x = new();
	private readonly Queue<DateTimeOffset> _times = new();
	private readonly List<ICandleMessage> _window = new();
	private int _barIndex;

	/// <summary>
	/// Number of pivot points for regression.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FunctionLinearRegressionStrategy()
	{
		_length = Param(nameof(Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Number of points used for regression", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prices.Clear();
		_x.Clear();
		_times.Clear();
		_window.Clear();
		_barIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
			DrawCandles(area, subscription);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;
		_window.Add(candle);

		if (_window.Count < 5)
			return;

		var c0 = _window[0];
		var c1 = _window[1];
		var c2 = _window[2];
		var c3 = _window[3];
		var c4 = _window[4];

		var pivotHigh = c2.HighPrice > c0.HighPrice && c2.HighPrice > c1.HighPrice &&
			c2.HighPrice > c3.HighPrice && c2.HighPrice > c4.HighPrice;
		var pivotLow = c2.LowPrice < c0.LowPrice && c2.LowPrice < c1.LowPrice &&
			c2.LowPrice < c3.LowPrice && c2.LowPrice < c4.LowPrice;

		if (pivotHigh || pivotLow)
		{
			if (_prices.Count >= Length)
			{
				_prices.Dequeue();
				_x.Dequeue();
				_times.Dequeue();
			}

			var point = pivotHigh ? c2.HighPrice : c2.LowPrice;
			_prices.Enqueue(point);
			_x.Enqueue(_barIndex - 2);
			_times.Enqueue(c2.OpenTime);

			if (_prices.Count >= 2)
			{
				var (a, b, maxDev, minDev, avgDev) = ComputeRegression(_x, _prices);

				var firstX = _x.Peek();
				var firstTime = _times.Peek();
				var lastX = _barIndex - 2;
				var lastTime = c2.OpenTime;

				var firstY = a + b * firstX;
				var lastY = a + b * lastX;

				DrawLine(firstTime, firstY, lastTime, lastY);
				DrawLine(firstTime, firstY + maxDev, lastTime, lastY + maxDev);
				DrawLine(firstTime, firstY + minDev, lastTime, lastY + minDev);
				DrawLine(firstTime, firstY + avgDev, lastTime, lastY + avgDev);
				DrawLine(firstTime, firstY - avgDev, lastTime, lastY - avgDev);
			}
		}

		_window.RemoveAt(0);
	}

	private static (decimal a, decimal b, decimal maxDev, decimal minDev, decimal avgDev) ComputeRegression(IEnumerable<decimal> xs, IEnumerable<decimal> ys)
	{
		var xList = new List<decimal>(xs);
		var yList = new List<decimal>(ys);
		var n = xList.Count;

		decimal sumX = 0;
		decimal sumY = 0;
		decimal sumXY = 0;
		decimal sumX2 = 0;

		for (var i = 0; i < n; i++)
		{
			sumX += xList[i];
			sumY += yList[i];
			sumXY += xList[i] * yList[i];
			sumX2 += xList[i] * xList[i];
		}

		var denom = n * sumX2 - sumX * sumX;
		if (denom == 0)
			return (0, 0, 0, 0, 0);

		var a = (sumY * sumX2 - sumX * sumXY) / denom;
		var b = (n * sumXY - sumX * sumY) / denom;

		decimal maxDev = decimal.MinValue;
		decimal minDev = decimal.MaxValue;
		decimal absDev = 0;

		for (var i = 0; i < n; i++)
		{
			var predicted = a + b * xList[i];
			var diff = yList[i] - predicted;
			if (diff > maxDev)
				maxDev = diff;
			if (diff < minDev)
				minDev = diff;
			absDev += Math.Abs(diff);
		}

		return (a, b, maxDev, minDev, absDev / n);
	}
}
