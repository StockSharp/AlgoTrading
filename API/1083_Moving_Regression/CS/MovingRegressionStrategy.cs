using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on polynomial moving regression.
/// </summary>
public class MovingRegressionStrategy : Strategy
{
	private readonly StrategyParam<int> _degree;
	private readonly StrategyParam<int> _window;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _prices = new();

	/// <summary>
	/// Polynomial degree.
	/// </summary>
	public int Degree
	{
		get => _degree.Value;
		set => _degree.Value = value;
	}

	/// <summary>
	/// Regression window length.
	/// </summary>
	public int Window
	{
		get => _window.Value;
		set => _window.Value = value;
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
	/// Initializes a new instance of the <see cref="MovingRegressionStrategy"/> class.
	/// </summary>
	public MovingRegressionStrategy()
	{
		_degree = Param(nameof(Degree), 2)
		.SetRange(0, 5)
		.SetDisplay("Degree", "Polynomial degree", "General")
		.SetCanOptimize(true);

		_window = Param(nameof(Window), 18)
		.SetRange(2, 100)
		.SetDisplay("Window", "Regression window length", "General")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prices.Add(candle.ClosePrice);
		if (_prices.Count > Window)
			_prices.RemoveAt(0);

		if (_prices.Count < Window)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var coeffs = CalculateCoefficients(_prices, Degree);
		var current = Evaluate(coeffs, Window - 1);
		var forecast = Evaluate(coeffs, Window);

		if (forecast > current && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (forecast < current && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}

	private static decimal Evaluate(decimal[] coeffs, int x)
	{
		decimal result = 0m;
		decimal pow = 1m;
		for (var i = 0; i < coeffs.Length; i++)
		{
			result += coeffs[i] * pow;
			pow *= x;
		}
		return result;
	}

	private static decimal[] CalculateCoefficients(IReadOnlyList<decimal> y, int degree)
	{
		var n = y.Count;
		var m = degree + 1;

		var sumX = new double[2 * degree + 1];
		for (var k = 0; k < sumX.Length; k++)
		{
			double s = 0;
			for (var i = 0; i < n; i++)
			{
				s += Math.Pow(i, k);
			}
			sumX[k] = s;
		}

		var sumYX = new double[m];
		for (var k = 0; k < m; k++)
		{
			double s = 0;
			for (var i = 0; i < n; i++)
			{
				s += (double)y[i] * Math.Pow(i, k);
			}
			sumYX[k] = s;
		}

		var aug = new double[m, m + 1];
		for (var i = 0; i < m; i++)
		{
			for (var j = 0; j < m; j++)
			{
				aug[i, j] = sumX[i + j];
			}
			aug[i, m] = sumYX[i];
		}

		var coeffs = SolveGaussian(aug, m);
		var result = new decimal[m];
		for (var i = 0; i < m; i++)
		{
			result[i] = (decimal)coeffs[i];
		}
		return result;
	}

	private static double[] SolveGaussian(double[,] a, int n)
	{
		for (var i = 0; i < n; i++)
		{
			var maxRow = i;
			for (var k = i + 1; k < n; k++)
			{
				if (Math.Abs(a[k, i]) > Math.Abs(a[maxRow, i]))
				maxRow = k;
			}

			for (var k = i; k <= n; k++)
			{
				var tmp = a[maxRow, k];
				a[maxRow, k] = a[i, k];
				a[i, k] = tmp;
			}

			var pivot = a[i, i];
			if (pivot == 0)
				continue;
			for (var k = i; k <= n; k++)
			a[i, k] /= pivot;

			for (var j = 0; j < n; j++)
			{
				if (j == i)
					continue;
				double factor = a[j, i];
				for (var k = i; k <= n; k++)
				a[j, k] -= factor * a[i, k];
			}
		}

		var x = new double[n];
		for (var i = 0; i < n; i++)
		x[i] = a[i, n];
		return x;
	}
}
