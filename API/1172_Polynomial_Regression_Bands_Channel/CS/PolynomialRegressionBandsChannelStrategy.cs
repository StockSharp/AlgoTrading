using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Polynomial Regression Bands Channel strategy.
/// Fits polynomial regression and trades when price touches the bands.
/// </summary>
public class PolynomialRegressionBandsChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _degree;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _prices = new();
	private decimal? _prevMid;
	private decimal? _prevUpper;
	private decimal? _prevLower;
	private DateTimeOffset _prevTime;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public int Degree { get => _degree.Value; set => _degree.Value = value; }
	public decimal StdDevMultiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PolynomialRegressionBandsChannelStrategy()
	{
		_length = Param(nameof(Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Lookback period for regression", "General")
			.SetCanOptimize();

		_degree = Param(nameof(Degree), 2)
			.SetGreaterThanZero()
			.SetDisplay("Degree", "Polynomial degree", "General")
			.SetCanOptimize();

		_multiplier = Param(nameof(StdDevMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Std Dev Multiplier", "Band width multiplier", "General")
			.SetCanOptimize();

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

		_prices.Clear();
		_prevMid = null;
		_prevUpper = null;
		_prevLower = null;
		_prevTime = default;
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

		_prices.Enqueue(candle.ClosePrice);
		if (_prices.Count > Length)
			_prices.Dequeue();

		if (_prices.Count < Length)
			return;

		var pricesList = new List<decimal>(_prices);
		var coeffs = PolyFit(pricesList, Degree);
		var mid = PolyEval(coeffs, Length - 1);
		var stdev = CalcStd(pricesList, coeffs);

		var upper = mid + stdev * StdDevMultiplier;
		var lower = mid - stdev * StdDevMultiplier;

		if (_prevMid != null)
		{
			DrawLine(_prevTime, _prevMid.Value, candle.OpenTime, mid);
			DrawLine(_prevTime, _prevUpper!.Value, candle.OpenTime, upper);
			DrawLine(_prevTime, _prevLower!.Value, candle.OpenTime, lower);
		}

		_prevMid = mid;
		_prevUpper = upper;
		_prevLower = lower;
		_prevTime = candle.OpenTime;

		if (candle.ClosePrice > upper && Position <= 0)
			BuyMarket();
		else if (candle.ClosePrice < lower && Position >= 0)
			SellMarket();
	}

	private static decimal[] PolyFit(IReadOnlyList<decimal> y, int degree)
	{
		var n = y.Count;
		var order = degree;
		var size = order + 1;
		var a = new decimal[size, size + 1];

		for (var row = 0; row < size; row++)
		{
			for (var col = 0; col < size; col++)
			{
				decimal sum = 0m;
				for (var i = 0; i < n; i++)
					sum += (decimal)Math.Pow(i, row + col);

				a[row, col] = sum;
			}

			decimal sumY = 0m;
			for (var i = 0; i < n; i++)
				sumY += y[i] * (decimal)Math.Pow(i, row);

			a[row, size] = sumY;
		}

		for (var i = 0; i < size; i++)
		{
			var pivot = a[i, i];
			if (pivot == 0)
				continue;

			for (var j = i; j < size + 1; j++)
				a[i, j] /= pivot;

			for (var k = 0; k < size; k++)
			{
				if (k == i)
					continue;

				var factor = a[k, i];
				for (var j = i; j < size + 1; j++)
					a[k, j] -= factor * a[i, j];
			}
		}

		var coeffs = new decimal[size];
		for (var i = 0; i < size; i++)
			coeffs[i] = a[i, size];

		return coeffs;
	}

	private static decimal PolyEval(decimal[] coef, decimal x)
	{
		decimal y = 0m;
		decimal power = 1m;
		foreach (var c in coef)
		{
			y += c * power;
			power *= x;
		}
		return y;
	}

	private static decimal CalcStd(IReadOnlyList<decimal> y, decimal[] coef)
	{
		var n = y.Count;
		decimal sum = 0m;
		for (var i = 0; i < n; i++)
		{
			var diff = y[i] - PolyEval(coef, i);
			sum += diff * diff;
		}
		return (decimal)Math.Sqrt((double)(sum / n));
	}
}

