namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RD Trend Trigger oscillator based strategy.
/// </summary>
public class RdTrendTriggerStrategy : Strategy
{
	private readonly StrategyParam<int> _regress;
	private readonly StrategyParam<int> _t3Length;
	private readonly StrategyParam<decimal> _t3VolumeFactor;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<TriggerMode> _mode;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();
	private decimal? _prev1;
	private decimal? _prev2;
	private decimal? _e1, _e2, _e3, _e4, _e5, _e6;

	/// <summary>
	/// Length for high/low segments.
	/// </summary>
	public int Regress
	{
		get => _regress.Value;
		set => _regress.Value = value;
	}

	/// <summary>
	/// T3 smoothing length.
	/// </summary>
	public int T3Length
	{
		get => _t3Length.Value;
		set => _t3Length.Value = value;
	}

	/// <summary>
	/// T3 volume factor.
	/// </summary>
	public decimal T3VolumeFactor
	{
		get => _t3VolumeFactor.Value;
		set => _t3VolumeFactor.Value = value;
	}

	/// <summary>
	/// Upper threshold for disposition mode.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for disposition mode.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Trading mode.
	/// </summary>
	public TriggerMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="RdTrendTriggerStrategy"/>.
	/// </summary>
	public RdTrendTriggerStrategy()
	{
		_regress = Param(nameof(Regress), 15)
			.SetGreaterThanZero()
			.SetDisplay("Regress", "Length for high/low segments", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_t3Length = Param(nameof(T3Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("T3 Length", "Tillson T3 smoothing depth", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_t3VolumeFactor = Param(nameof(T3VolumeFactor), 0.7m)
			.SetDisplay("T3 Volume Factor", "Tillson T3 volume factor", "Indicator");

		_highLevel = Param(nameof(HighLevel), 50m)
			.SetDisplay("High Level", "Upper threshold", "Signal");

		_lowLevel = Param(nameof(LowLevel), -50m)
			.SetDisplay("Low Level", "Lower threshold", "Signal");

		_mode = Param(nameof(Mode), TriggerMode.Twist)
			.SetDisplay("Mode", "Trading mode", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
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

		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);

		var maxCount = Regress * 2;
		if (_highs.Count > maxCount)
		{
			_highs.Dequeue();
			_lows.Dequeue();
		}

		if (_highs.Count < maxCount)
			return;

		var highestRecent = GetMax(_highs, 0, Regress);
		var highestOlder = GetMax(_highs, Regress, Regress);
		var lowestRecent = GetMin(_lows, 0, Regress);
		var lowestOlder = GetMin(_lows, Regress, Regress);

		var buyPower = highestRecent - lowestOlder;
		var sellPower = highestOlder - lowestRecent;
		var res = buyPower + sellPower;
		var ttf = res == 0m ? 0m : (buyPower - sellPower) / (0.5m * res) * 100m;

		var e1 = UpdateEma(ref _e1, ttf, T3Length);
		var e2 = UpdateEma(ref _e2, e1, T3Length);
		var e3 = UpdateEma(ref _e3, e2, T3Length);
		var e4 = UpdateEma(ref _e4, e3, T3Length);
		var e5 = UpdateEma(ref _e5, e4, T3Length);
		var e6 = UpdateEma(ref _e6, e5, T3Length);

		var v = T3VolumeFactor;
		var c1 = -v * v * v;
		var c2 = 3m * v * v + 3m * v * v * v;
		var c3 = -6m * v * v - 3m * v - 3m * v * v * v;
		var c4 = 1m + 3m * v + v * v * v + 3m * v * v;
		var t3 = c1 * e6 + c2 * e5 + c3 * e4 + c4 * e3;

		switch (Mode)
		{
			case TriggerMode.Twist:
			{
				if (_prev2.HasValue && _prev1.HasValue)
				{
					if (t3 > _prev1 && _prev1 <= _prev2 && Position <= 0)
					{
						if (Position < 0)
							ClosePosition();
						BuyMarket(Volume);
					}
					else if (t3 < _prev1 && _prev1 >= _prev2 && Position >= 0)
					{
						if (Position > 0)
							ClosePosition();
						SellMarket(Volume);
					}
				}

				_prev2 = _prev1;
				_prev1 = t3;
				break;
			}

			case TriggerMode.Disposition:
			{
				if (_prev1.HasValue)
				{
					if (t3 > HighLevel && _prev1 <= HighLevel && Position <= 0)
					{
						if (Position < 0)
							ClosePosition();
						BuyMarket(Volume);
					}
					else if (t3 < LowLevel && _prev1 >= LowLevel && Position >= 0)
					{
						if (Position > 0)
							ClosePosition();
						SellMarket(Volume);
					}
					else if (t3 > LowLevel && Position < 0)
					{
						ClosePosition();
					}
				}

				_prev1 = t3;
				break;
			}
		}
	}

	private static decimal GetMax(IEnumerable<decimal> values, int start, int count)
	{
		var i = 0;
		var max = decimal.MinValue;
		foreach (var v in values)
		{
			if (i >= start && i < start + count && v > max)
				max = v;
			i++;
		}
		return max;
	}

	private static decimal GetMin(IEnumerable<decimal> values, int start, int count)
	{
		var i = 0;
		var min = decimal.MaxValue;
		foreach (var v in values)
		{
			if (i >= start && i < start + count && v < min)
				min = v;
			i++;
		}
		return min;
	}

	private static decimal UpdateEma(ref decimal? prev, decimal input, int length)
	{
		var alpha = 2m / (length + 1m);
		var value = prev is null ? input : alpha * input + (1m - alpha) * prev.Value;
		prev = value;
		return value;
	}

	/// <summary>
	/// Modes of RD Trend Trigger.
	/// </summary>
	public enum TriggerMode
	{
		/// <summary>
		/// Trade on oscillator direction change.
		/// </summary>
		Twist,

		/// <summary>
		/// Trade on level breakouts.
		/// </summary>
		Disposition
	}
}
