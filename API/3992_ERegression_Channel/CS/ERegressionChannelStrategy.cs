using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Polynomial regression channel strategy. Calculates a regression midline with
/// standard deviation bands and trades mean reversion between the bands and midline.
/// </summary>
public class ERegressionChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _regressionLength;
	private readonly StrategyParam<int> _degree;
	private readonly StrategyParam<decimal> _stdMultiplier;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _closes = new();
	private ExponentialMovingAverage _ema;

	private decimal? _previousMid;

	/// <summary>
	/// Initializes a new instance of the <see cref="ERegressionChannelStrategy"/> class.
	/// </summary>
	public ERegressionChannelStrategy()
	{
		_regressionLength = Param(nameof(RegressionLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Regression Length", "Number of bars used for regression", "Regression");

		_degree = Param(nameof(Degree), 3)
			.SetGreaterThanZero()
			.SetDisplay("Degree", "Polynomial degree for the regression", "Regression");

		_stdMultiplier = Param(nameof(StdDevMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Std Dev Multiplier", "Width multiplier for the regression bands", "Regression");

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Protective stop in absolute points (0 disables)", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Target in absolute points (0 disables)", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type used for trading", "General");

		Volume = 1;
	}

	/// <summary>
	/// Number of bars used for regression.
	/// </summary>
	public int RegressionLength
	{
		get => _regressionLength.Value;
		set => _regressionLength.Value = value;
	}

	/// <summary>
	/// Polynomial degree for the regression.
	/// </summary>
	public int Degree
	{
		get => _degree.Value;
		set => _degree.Value = value;
	}

	/// <summary>
	/// Width multiplier for the regression bands.
	/// </summary>
	public decimal StdDevMultiplier
	{
		get => _stdMultiplier.Value;
		set => _stdMultiplier.Value = value;
	}

	/// <summary>
	/// Protective stop in absolute points (0 disables).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Target in absolute points (0 disables).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Primary candle type used for trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_closes.Clear();
		_previousMid = null;
		_ema = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		_ema = new ExponentialMovingAverage { Length = Math.Max(2, RegressionLength) };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}

		var tp = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null;
		var sl = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Absolute) : null;
		if (tp != null || sl != null)
			StartProtection(tp, sl);

		base.OnStarted2(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Enqueue(candle.ClosePrice);
		if (_closes.Count > RegressionLength)
			_closes.Dequeue();

		if (_closes.Count < RegressionLength)
			return;

		var prices = new List<decimal>(_closes);
		var coeffs = PolyFit(prices, Degree);
		var currentIndex = prices.Count - 1;
		var mid = PolyEval(coeffs, currentIndex);
		var std = CalcStd(prices, coeffs) * StdDevMultiplier;
		var upper = mid + std;
		var lower = mid - std;

		// Exit at midline
		if (Position > 0 && candle.ClosePrice >= mid)
		{
			SellMarket(Position);
			_previousMid = mid;
			return;
		}
		if (Position < 0 && candle.ClosePrice <= mid)
		{
			BuyMarket(Math.Abs(Position));
			_previousMid = mid;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousMid = mid;
			return;
		}

		// Entry signals: mean reversion from bands
		if (candle.LowPrice <= lower && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		else if (candle.HighPrice >= upper && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket(Volume);
		}

		_previousMid = mid;
	}

	private static decimal[] PolyFit(IReadOnlyList<decimal> y, int degree)
	{
		var n = y.Count;
		var actualDegree = Math.Min(degree, Math.Max(1, n - 1));
		var size = actualDegree + 1;
		var matrix = new decimal[size, size + 1];

		for (var row = 0; row < size; row++)
		{
			for (var col = 0; col < size; col++)
			{
				decimal sum = 0m;
				for (var i = 0; i < n; i++)
					sum += (decimal)Math.Pow(i, row + col);

				matrix[row, col] = sum;
			}

			decimal sumY = 0m;
			for (var i = 0; i < n; i++)
				sumY += y[i] * (decimal)Math.Pow(i, row);

			matrix[row, size] = sumY;
		}

		for (var i = 0; i < size; i++)
		{
			var pivot = matrix[i, i];
			if (pivot == 0m)
			{
				for (var k = i + 1; k < size; k++)
				{
					if (matrix[k, i] == 0m)
						continue;

					for (var j = i; j < size + 1; j++)
						(matrix[i, j], matrix[k, j]) = (matrix[k, j], matrix[i, j]);

					pivot = matrix[i, i];
					break;
				}
			}

			if (pivot == 0m)
				continue;

			for (var j = i; j < size + 1; j++)
				matrix[i, j] /= pivot;

			for (var row = 0; row < size; row++)
			{
				if (row == i)
					continue;

				var factor = matrix[row, i];
				if (factor == 0m)
					continue;

				for (var col = i; col < size + 1; col++)
					matrix[row, col] -= factor * matrix[i, col];
			}
		}

		var coeffs = new decimal[size];
		for (var i = 0; i < size; i++)
			coeffs[i] = matrix[i, size];

		return coeffs;
	}

	private static decimal PolyEval(IReadOnlyList<decimal> coeffs, decimal x)
	{
		decimal result = 0m;
		decimal power = 1m;
		for (var i = 0; i < coeffs.Count; i++)
		{
			result += coeffs[i] * power;
			power *= x;
		}

		return result;
	}

	private static decimal CalcStd(IReadOnlyList<decimal> values, decimal[] coeffs)
	{
		var n = values.Count;
		if (n == 0)
			return 0m;

		decimal sum = 0m;
		for (var i = 0; i < n; i++)
		{
			var fitted = PolyEval(coeffs, i);
			var diff = values[i] - fitted;
			sum += diff * diff;
		}

		return (decimal)Math.Sqrt((double)(sum / n));
	}
}
