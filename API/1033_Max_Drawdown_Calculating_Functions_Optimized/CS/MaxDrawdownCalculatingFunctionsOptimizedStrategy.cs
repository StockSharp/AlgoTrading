using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sample demonstrating different drawdown calculation functions.
/// Collects last 10 closing prices and logs drawdown metrics.
/// </summary>
public class MaxDrawdownCalculatingFunctionsOptimizedStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _lastCloses = new decimal[10];
	private int _index;
	private bool _isFilled;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MaxDrawdownCalculatingFunctionsOptimizedStrategy"/>.
	/// </summary>
	public MaxDrawdownCalculatingFunctionsOptimizedStrategy()
	{
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
		Array.Clear(_lastCloses);
		_index = 0;
		_isFilled = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastCloses[_index] = candle.ClosePrice;
		_index++;
		if (_index >= _lastCloses.Length)
		{
			_index = 0;
			_isFilled = true;
		}

		if (!_isFilled)
			return;

		var length = _lastCloses.Length;

		var rdd = OptimizedMaximumRelativeDrawdown(_lastCloses, length);
		var dd = OptimizedMaximumDrawdown(_lastCloses, length);

		LogInfo($"Optimized Relative DD {rdd.max}% from {rdd.peak} to {rdd.trough} (iterations {rdd.iterations})");
		LogInfo($"Optimized DD {dd.max} from {dd.peak} to {dd.trough} (iterations {dd.iterations})");
	}

	private static (decimal max, decimal peak, decimal trough, int iterations) OptimizedMaximumRelativeDrawdown(decimal[] values, int length)
	{
		var peakIndex = 0;
		decimal? peakValue = null;
		decimal? troughValue = null;
		var maxDrawdown = 0m;
		var iterations = 0;

		for (var i = 1; i < length; i++)
		{
			var peak = values[peakIndex];
			var current = values[i];

			var diff = 100m * (peak - current) / peak;
			iterations++;

			if (diff > maxDrawdown)
			{
				maxDrawdown = diff;
				peakValue = peak;
				troughValue = current;
			}

			if (current > peak)
				peakIndex = i;
		}

		return (maxDrawdown, peakValue ?? 0m, troughValue ?? 0m, iterations);
	}

	private static (decimal max, decimal peak, decimal trough, int iterations) OptimizedMaximumDrawdown(decimal[] values, int length)
	{
		var peakIndex = 0;
		decimal? peakValue = null;
		decimal? troughValue = null;
		var maxDrawdown = 0m;
		var iterations = 0;

		for (var i = 1; i < length; i++)
		{
			var peak = values[peakIndex];
			var current = values[i];

			var diff = peak - current;
			iterations++;

			if (diff > maxDrawdown)
			{
				maxDrawdown = diff;
				peakValue = peak;
				troughValue = current;
			}

			if (current > peak)
				peakIndex = i;
		}

		return (maxDrawdown, peakValue ?? 0m, troughValue ?? 0m, iterations);
	}

	private static (decimal max, decimal peak, decimal trough, int iterations) MaximumRelativeDrawdown(decimal[] values, int length)
	{
		decimal? peakValue = null;
		decimal? troughValue = null;
		var maxDrawdown = 0m;
		var iterations = 0;

		for (var i = 0; i < length; i++)
		{
			for (var j = i; j < length; j++)
			{
				iterations++;
				var diff = 100m * (values[i] - values[j]) / values[i];
				if (diff > maxDrawdown)
				{
					maxDrawdown = diff;
					peakValue = values[i];
					troughValue = values[j];
				}
			}
		}

		return (maxDrawdown, peakValue ?? 0m, troughValue ?? 0m, iterations);
	}

	private static (decimal max, decimal peak, decimal trough, int iterations) MaximumDrawdown(decimal[] values, int length)
	{
		decimal? peakValue = null;
		decimal? troughValue = null;
		var maxDrawdown = 0m;
		var iterations = 0;

		for (var i = 0; i < length; i++)
		{
			for (var j = i; j < length; j++)
			{
				iterations++;
				var diff = values[i] - values[j];
				if (diff > maxDrawdown)
				{
					maxDrawdown = diff;
					peakValue = values[i];
					troughValue = values[j];
				}
			}
		}

		return (maxDrawdown, peakValue ?? 0m, troughValue ?? 0m, iterations);
	}
}
