using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Detects new swing highs and lows and calculates Fibonacci and Gann retracement levels.
/// </summary>
public class AutoFiboGannFanRetracementsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookbackPeriod;

	private decimal _highestPrice;
	private decimal _lowestPrice;
	private bool _waitingForHigh = true;
	private bool _isInitialized;

	/// <summary>
	/// Constructor.
	/// </summary>
	public AutoFiboGannFanRetracementsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Bars for swing detection", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 10);
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bars for swing detection.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var highest = new Highest { Length = LookbackPeriod };
		var lowest = new Lowest { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_highestPrice = highestValue;
			_lowestPrice = lowestValue;
			_isInitialized = true;
			return;
		}

		if (_waitingForHigh)
		{
			if (highestValue > _highestPrice)
			{
				_highestPrice = highestValue;
				CalcLevels(_lowestPrice, _highestPrice);
				_waitingForHigh = false;
			}
		}
		else
		{
			if (lowestValue < _lowestPrice)
			{
				_lowestPrice = lowestValue;
				CalcLevels(_highestPrice, _lowestPrice);
				_waitingForHigh = true;
			}
		}
	}

	private void CalcLevels(decimal start, decimal end)
	{
		var range = end - start;
		if (range == 0)
			return;

		decimal[] fibo = { 0.236m, 0.382m, 0.5m, 0.618m, 0.786m };
		foreach (var r in fibo)
		{
			var level = start + range * r;
			LogInfo($"Fibo {r:P0}: {level}");
		}

		decimal[] gann = { 0.125m, 0.25m, 0.5m, 0.75m, 0.875m };
		foreach (var r in gann)
		{
			var level = start + range * r;
			LogInfo($"Gann {r:P0}: {level}");
		}
	}
}
