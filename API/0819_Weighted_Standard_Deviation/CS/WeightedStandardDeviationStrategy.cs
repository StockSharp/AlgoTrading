using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on volume weighted mean and standard deviation.
/// Buys when price falls below lower band and sells when above upper band.
/// Exits positions when price returns to the mean.
/// </summary>
public class WeightedStandardDeviationStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<(decimal value, decimal weight)> _values = new();

	/// <summary>
	/// Number of samples.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	/// Initializes a new instance of the strategy.
	/// </summary>
	public WeightedStandardDeviationStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Number of samples", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Std Mult", "Standard deviation multiplier", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

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
		_values.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var weight = candle.TotalVolume ?? 0m;

		_values.Enqueue((candle.ClosePrice, weight));
		if (_values.Count > Length)
			_values.Dequeue();

		if (_values.Count < Length)
			return;

		var sumWeight = 0m;
		var sumWeighted = 0m;
		var nonZero = 0;
		foreach (var (value, w) in _values)
		{
			sumWeight += w;
			sumWeighted += value * w;
			if (w != 0m)
				nonZero++;
		}

		if (sumWeight == 0m || nonZero <= 1)
			return;

		var mean = sumWeighted / sumWeight;

		var sqErrorSum = 0m;
		foreach (var (value, w) in _values)
		{
			var diff = mean - value;
			sqErrorSum += diff * diff * w;
		}

		var variance = sqErrorSum / ((nonZero - 1) * sumWeight / nonZero);
		var deviation = (decimal)Math.Sqrt((double)variance);

		var upper = mean + deviation * Multiplier;
		var lower = mean - deviation * Multiplier;

		if (Position <= 0 && candle.ClosePrice < lower)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Buy: price {candle.ClosePrice} below lower band {lower}");
		}
		else if (Position >= 0 && candle.ClosePrice > upper)
		{
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Sell: price {candle.ClosePrice} above upper band {upper}");
		}
		else if (Position > 0 && candle.ClosePrice >= mean)
		{
			SellMarket(Math.Abs(Position));
			LogInfo($"Exit long: price {candle.ClosePrice} above mean {mean}");
		}
		else if (Position < 0 && candle.ClosePrice <= mean)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short: price {candle.ClosePrice} below mean {mean}");
		}
	}
}
