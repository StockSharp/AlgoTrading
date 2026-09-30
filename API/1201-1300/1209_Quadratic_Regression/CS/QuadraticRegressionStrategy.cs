using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades crossovers between price and a rolling quadratic least-squares regression line.
/// </summary>
public class QuadraticRegressionStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Queue<decimal> _closes = new();

	private decimal? _prevPrice;
	private decimal? _prevRegression;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public QuadraticRegressionStrategy()
	{
		_length = Param(nameof(Length), 54)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Number of bars in the quadratic regression.", "Regression");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_prevPrice = null;
		_prevRegression = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Enqueue(candle.ClosePrice);
		while (_closes.Count > Length)
			_closes.Dequeue();

		if (_closes.Count < Length)
			return;

		var price = candle.ClosePrice;
		var regression = FitLast(_closes);

		if (_prevPrice is decimal previousPrice && _prevRegression is decimal previousRegression)
		{
			var crossUp = previousPrice <= previousRegression && price > regression;
			var crossDown = previousPrice >= previousRegression && price < regression;

			if (crossUp && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (crossDown && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevPrice = price;
		_prevRegression = regression;
	}

	internal static decimal FitLast(IEnumerable<decimal> values)
	{
		var ys = new List<double>();
		foreach (var value in values)
			ys.Add((double)value);

		var n = ys.Count;
		if (n < 3)
			return ys.Count == 0 ? 0m : (decimal)ys[^1];

		double s0 = n, s1 = 0, s2 = 0, s3 = 0, s4 = 0;
		double t0 = 0, t1 = 0, t2 = 0;

		for (var i = 0; i < n; i++)
		{
			var x = (double)i;
			var x2 = x * x;
			var y = ys[i];

			s1 += x;
			s2 += x2;
			s3 += x2 * x;
			s4 += x2 * x2;
			t0 += y;
			t1 += x * y;
			t2 += x2 * y;
		}

		var a = new[,]
		{
			{ s0, s1, s2, t0 },
			{ s1, s2, s3, t1 },
			{ s2, s3, s4, t2 },
		};

		for (var col = 0; col < 3; col++)
		{
			var pivot = col;
			for (var row = col + 1; row < 3; row++)
			{
				if (Math.Abs(a[row, col]) > Math.Abs(a[pivot, col]))
					pivot = row;
			}

			if (Math.Abs(a[pivot, col]) < 1e-12)
				return ys.Count == 0 ? 0m : (decimal)ys[^1];

			if (pivot != col)
			{
				for (var k = col; k < 4; k++)
					(a[col, k], a[pivot, k]) = (a[pivot, k], a[col, k]);
			}

			var divisor = a[col, col];
			for (var k = col; k < 4; k++)
				a[col, k] /= divisor;

			for (var row = 0; row < 3; row++)
			{
				if (row == col)
					continue;

				var factor = a[row, col];
				for (var k = col; k < 4; k++)
					a[row, k] -= factor * a[col, k];
			}
		}

		var xLast = n - 1d;
		return (decimal)(a[0, 3] + a[1, 3] * xLast + a[2, 3] * xLast * xLast);
	}
}
