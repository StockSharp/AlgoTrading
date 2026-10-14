namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Mean reversion outside a polynomial regression endpoint and its residual deviation bands.
/// </summary>
public class PolynomialRegressionBandsChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _degree;
	private readonly StrategyParam<decimal> _stdDevMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly List<decimal> _prices = [];
	private double[][] _basis;

	/// <summary>
	/// Initializes the published regression length, degree and band multiplier.
	/// </summary>
	public PolynomialRegressionBandsChannelStrategy()
	{
		_length = Param(nameof(Length), 100).SetGreaterThanZero()
			.SetDisplay("Length", "Rolling regression window, including the current close", "Indicators");
		_degree = Param(nameof(Degree), 2).SetRange(0, 6)
			.SetDisplay("Degree", "Polynomial degree from zero through six", "Indicators");
		_stdDevMultiplier = Param(nameof(StdDevMultiplier), 2m).SetGreaterThanZero()
			.SetDisplay("Std Dev Multiplier", "Population deviation multiplier of fitted residuals", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of finished candles", "General");
	}

	/// <summary>
	/// Number of finished closes in the regression window.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Polynomial degree from zero through six; the window needs at least two more observations.
	/// </summary>
	public int Degree { get => _degree.Value; set => _degree.Value = value; }

	/// <summary>
	/// Positive multiplier of the population residual deviation.
	/// </summary>
	public decimal StdDevMultiplier { get => _stdDevMultiplier.Value; set => _stdDevMultiplier.Value = value; }

	/// <summary>
	/// Candle type; five minutes by default.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prices.Clear();
		_basis = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (Degree < 0 || Degree > 6 || Length < Degree + 2)
			throw new ArgumentException("Degree must be between zero and six and Length at least Degree + 2.");
		_prices.Clear();
		_basis = BuildBasis(Length, Degree);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private static double[][] BuildBasis(int length, int degree)
	{
		// The fixed grid permits one QR basis for every rolling window.
		var basis = new double[degree + 1][];
		for (var column = 0; column <= degree; column++)
		{
			var values = new double[length];
			for (var row = 0; row < length; row++)
				values[row] = Math.Pow(2.0 * row / (length - 1) - 1.0, column);
			for (var pass = 0; pass < 2; pass++)
			{
				for (var previous = 0; previous < column; previous++)
				{
					var projection = 0.0;
					for (var row = 0; row < length; row++)
						projection += values[row] * basis[previous][row];
					for (var row = 0; row < length; row++)
						values[row] -= projection * basis[previous][row];
				}
			}
			var squaredNorm = 0.0;
			foreach (var value in values)
				squaredNorm += value * value;
			var norm = Math.Sqrt(squaredNorm);
			for (var row = 0; row < length; row++)
				values[row] /= norm;
			basis[column] = values;
		}
		return basis;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		_prices.Add(candle.ClosePrice);
		if (_prices.Count > Length)
			_prices.RemoveAt(0);
		if (_prices.Count < Length || !IsFormedAndOnlineAndAllowTrading())
			return;
		var values = new double[Length];
		for (var row = 0; row < Length; row++)
			values[row] = (double)(_prices[row] - _prices[0]);
		var coefficients = new double[_basis.Length];
		for (var column = 0; column < _basis.Length; column++)
		{
			for (var row = 0; row < Length; row++)
				coefficients[column] += values[row] * _basis[column][row];
		}
		var squaredError = 0.0;
		var center = 0.0;
		for (var row = 0; row < Length; row++)
		{
			var fitted = 0.0;
			for (var column = 0; column < _basis.Length; column++)
				fitted += coefficients[column] * _basis[column][row];
			var residual = values[row] - fitted;
			squaredError += residual * residual;
			if (row == Length - 1)
				center = fitted;
		}
		var width = (double)StdDevMultiplier * Math.Sqrt(squaredError / Length);
		if (values[^1] < center - width && Position <= 0m)
			BuyMarket(Volume + Math.Abs(Position));
		else if (values[^1] > center + width && Position >= 0m)
			SellMarket(Volume + Math.Abs(Position));
	}
}
