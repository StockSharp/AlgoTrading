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
/// Bill Williams WiseMan-1 breakout strategy converted from the MQL version.
/// Generates entries when a candle breaks away from the Alligator lines with confirmation from prior highs or lows.
/// Signals can be inverted to trade in a counter-trend manner.
/// </summary>
public class BwWiseman1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _retrograde;
	private readonly StrategyParam<int> _back;
	private readonly StrategyParam<int> _jawLength;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethLength;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsLength;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _enableCloseLong;
	private readonly StrategyParam<bool> _enableCloseShort;

	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;

	private readonly Queue<decimal> _jawShiftBuffer = new();
	private readonly Queue<decimal> _teethShiftBuffer = new();
	private readonly Queue<decimal> _lipsShiftBuffer = new();
	private readonly Queue<decimal> _recentHighs = new();
	private readonly Queue<decimal> _recentLows = new();
	private readonly Queue<(bool buy, bool sell)> _signalQueue = new();

	/// <summary>
	/// Candle type used for generating signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trade in counter-trend mode (swap buy and sell signals).
	/// </summary>
	public bool Retrograde
	{
		get => _retrograde.Value;
		set => _retrograde.Value = value;
	}

	/// <summary>
	/// Number of previous bars used to confirm the breakout condition.
	/// </summary>
	public int Back
	{
		get => _back.Value;
		set => _back.Value = value;
	}

	/// <summary>
	/// Alligator jaw SMMA length.
	/// </summary>
	public int JawLength
	{
		get => _jawLength.Value;
		set => _jawLength.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the jaw line.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Alligator teeth SMMA length.
	/// </summary>
	public int TeethLength
	{
		get => _teethLength.Value;
		set => _teethLength.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the teeth line.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Alligator lips SMMA length.
	/// </summary>
	public int LipsLength
	{
		get => _lipsLength.Value;
		set => _lipsLength.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the lips line.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Delay in bars before a signal becomes active.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Allow closing existing long positions on opposite signals.
	/// </summary>
	public bool EnableCloseLong
	{
		get => _enableCloseLong.Value;
		set => _enableCloseLong.Value = value;
	}

	/// <summary>
	/// Allow closing existing short positions on opposite signals.
	/// </summary>
	public bool EnableCloseShort
	{
		get => _enableCloseShort.Value;
		set => _enableCloseShort.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BwWiseman1Strategy"/>.
	/// </summary>
	public BwWiseman1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signal generation", "General");

		_retrograde = Param(nameof(Retrograde), true)
			.SetDisplay("Counter-Trend Mode", "Swap buy and sell signals", "Signals");

		_back = Param(nameof(Back), 2)
			.SetNotNegative()
			.SetDisplay("Breakout Depth", "Number of previous bars checked for highs or lows", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0, 5, 1);

		_jawLength = Param(nameof(JawLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Length", "Smoothed moving average length for the jaw", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(8, 21, 1);

		_jawShift = Param(nameof(JawShift), 8)
			.SetNotNegative()
			.SetDisplay("Jaw Shift", "Forward displacement of the jaw line", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(0, 10, 1);

		_teethLength = Param(nameof(TeethLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Length", "Smoothed moving average length for the teeth", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_teethShift = Param(nameof(TeethShift), 5)
			.SetNotNegative()
			.SetDisplay("Teeth Shift", "Forward displacement of the teeth line", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(0, 8, 1);

		_lipsLength = Param(nameof(LipsLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Length", "Smoothed moving average length for the lips", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_lipsShift = Param(nameof(LipsShift), 3)
			.SetNotNegative()
			.SetDisplay("Lips Shift", "Forward displacement of the lips line", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(0, 6, 1);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal Bar", "Number of completed bars to wait before acting", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0, 3, 1);

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow opening long positions", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow opening short positions", "Trading");

		_enableCloseLong = Param(nameof(EnableCloseLong), true)
			.SetDisplay("Close Long", "Allow closing long positions on opposite signals", "Trading");

		_enableCloseShort = Param(nameof(EnableCloseShort), true)
			.SetDisplay("Close Short", "Allow closing short positions on opposite signals", "Trading");
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

		_jawShiftBuffer.Clear();
		_teethShiftBuffer.Clear();
		_lipsShiftBuffer.Clear();
		_recentHighs.Clear();
		_recentLows.Clear();
		_signalQueue.Clear();
		_jaw = null;
		_teeth = null;
		_lips = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_jawShiftBuffer.Clear();
		_teethShiftBuffer.Clear();
		_lipsShiftBuffer.Clear();
		_recentHighs.Clear();
		_recentLows.Clear();
		_signalQueue.Clear();

		_jaw = new SmoothedMovingAverage { Length = JawLength };
		_teeth = new SmoothedMovingAverage { Length = TeethLength };
		_lips = new SmoothedMovingAverage { Length = LipsLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle);
		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var jawValue = _jaw.Process(median);
		var teethValue = _teeth.Process(median);
		var lipsValue = _lips.Process(median);

		var buySignal = false;
		var sellSignal = false;

		if (jawValue.IsFinal && teethValue.IsFinal && lipsValue.IsFinal)
		{
			var jawLine = ShiftValue(_jawShiftBuffer, jawValue.ToDecimal(), JawShift);
			var teethLine = ShiftValue(_teethShiftBuffer, teethValue.ToDecimal(), TeethShift);
			var lipsLine = ShiftValue(_lipsShiftBuffer, lipsValue.ToDecimal(), LipsShift);

			if (jawLine is decimal jaw && teethLine is decimal teeth && lipsLine is decimal lips)
			{
				var higherHigh = HasHigherHigh(candle.HighPrice);
				var lowerLow = HasLowerLow(candle.LowPrice);

				if (lowerLow &&
					candle.HighPrice < lips && candle.HighPrice < teeth && candle.HighPrice < jaw &&
					candle.ClosePrice > median)
				{
					buySignal = true;
				}

				if (higherHigh &&
					candle.LowPrice > lips && candle.LowPrice > teeth && candle.LowPrice > jaw &&
					candle.ClosePrice < median)
				{
					sellSignal = true;
				}
			}
		}

		if (Retrograde)
		{
			(buySignal, sellSignal) = (sellSignal, buySignal);
		}

		_signalQueue.Enqueue((buySignal, sellSignal));

		if (_signalQueue.Count > SignalBar)
		{
			var (buy, sell) = _signalQueue.Dequeue();

			if (buy)
			{
				if (EnableCloseShort && Position < 0m)
				{
					BuyMarket(Math.Abs(Position));
				}

				if (EnableLong && Position == 0m)
				{
					BuyMarket(Volume);
				}
			}

			if (sell)
			{
				if (EnableCloseLong && Position > 0m)
				{
					SellMarket(Position);
				}

				if (EnableShort && Position == 0m)
				{
					SellMarket(Volume);
				}
			}
		}

		UpdateHistory(candle.HighPrice, candle.LowPrice);
	}

	private static decimal? ShiftValue(Queue<decimal> queue, decimal newValue, int shift)
	{
		queue.Enqueue(newValue);

		if (queue.Count <= shift)
			return null;

		if (queue.Count > shift + 1)
			queue.Dequeue();

		return queue.Peek();
	}

	private bool HasHigherHigh(decimal value)
	{
		if (Back <= 0)
			return true;

		if (_recentHighs.Count < Back)
			return false;

		foreach (var high in _recentHighs)
		{
			if (value <= high)
				return false;
		}

		return true;
	}

	private bool HasLowerLow(decimal value)
	{
		if (Back <= 0)
			return true;

		if (_recentLows.Count < Back)
			return false;

		foreach (var low in _recentLows)
		{
			if (value >= low)
				return false;
		}

		return true;
	}

	private void UpdateHistory(decimal high, decimal low)
	{
		if (Back <= 0)
			return;

		_recentHighs.Enqueue(high);
		if (_recentHighs.Count > Back)
			_recentHighs.Dequeue();

		_recentLows.Enqueue(low);
		if (_recentLows.Count > Back)
			_recentLows.Dequeue();
	}
}