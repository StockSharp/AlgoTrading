using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading pullbacks using a linear regression channel.
/// </summary>
public class LinearRegressionChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _deviation;

	private readonly Queue<decimal> _closes = new();

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Number of bars used in regression.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Channel width multiplier.
	/// </summary>
	public decimal Deviation { get => _deviation.Value; set => _deviation.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="LinearRegressionChannelStrategy"/> class.
	/// </summary>
	public LinearRegressionChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_length = Param(nameof(Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Bars for regression", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 50);

		_deviation = Param(nameof(Deviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation", "Channel width multiplier", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);
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

		_closes.Enqueue(candle.ClosePrice);
		if (_closes.Count > Length)
			_closes.Dequeue();

		if (_closes.Count < Length)
			return;

		var n = Length;
		decimal sumX = 0;
		decimal sumY = 0;
		decimal sumXY = 0;
		decimal sumX2 = 0;
		var i = 0;

		foreach (var y in _closes)
		{
			var x = (decimal)i;
			sumX += x;
			sumY += y;
			sumXY += x * y;
			sumX2 += x * x;
			i++;
		}

		var denom = n * sumX2 - sumX * sumX;
		if (denom == 0)
			return;

		var slope = (n * sumXY - sumX * sumY) / denom;
		var intercept = (sumY - slope * sumX) / n;

		var lastX = n - 1;
		var line = intercept + slope * lastX;

		decimal devSum = 0;
		i = 0;
		foreach (var y in _closes)
		{
			var x = (decimal)i;
			var fitted = intercept + slope * x;
			var diff = y - fitted;
			devSum += diff * diff;
			i++;
		}
		var deviation = (decimal)Math.Sqrt((double)(devSum / n));

		var upper = line + deviation * Deviation;
		var lower = line - deviation * Deviation;

		if (slope > 0 && candle.ClosePrice < lower && Position <= 0)
		{
			BuyMarket();
		}
		else if (slope < 0 && candle.ClosePrice > upper && Position >= 0)
		{
			SellMarket();
		}
		else if (Position > 0 && candle.ClosePrice >= line)
		{
			SellMarket();
		}
		else if (Position < 0 && candle.ClosePrice <= line)
		{
			BuyMarket();
		}
	}
}
