namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trading strategy based on a Schaff-style cycle built from fast and slow RVI averages.
/// </summary>
public class ColorSchaffRviTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastRviLength;
	private readonly StrategyParam<int> _slowRviLength;
	private readonly StrategyParam<int> _cycleLength;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _recentCandles = [];
	private readonly Queue<decimal> _fastWindow = [];
	private readonly Queue<decimal> _slowWindow = [];
	private readonly List<decimal> _macd = [];
	private readonly List<decimal> _st = [];
	private decimal _fastSum;
	private decimal _slowSum;
	private bool _stReady;
	private bool _stcReady;
	private decimal _prevSt;
	private decimal _prevStc;
	private int _cooldownRemaining;

	/// <summary>
	/// Fast RVI smoothing length.
	/// </summary>
	public int FastRviLength
	{
		get => _fastRviLength.Value;
		set => _fastRviLength.Value = value;
	}

	/// <summary>
	/// Slow RVI smoothing length.
	/// </summary>
	public int SlowRviLength
	{
		get => _slowRviLength.Value;
		set => _slowRviLength.Value = value;
	}

	/// <summary>
	/// Cycle length for stochastic calculations.
	/// </summary>
	public int CycleLength
	{
		get => _cycleLength.Value;
		set => _cycleLength.Value = value;
	}

	/// <summary>
	/// Upper threshold for the cycle.
	/// </summary>
	public int HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for the cycle.
	/// </summary>
	public int LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Bars to wait between reversals.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorSchaffRviTrendCycleStrategy"/>.
	/// </summary>
	public ColorSchaffRviTrendCycleStrategy()
	{
		_fastRviLength = Param(nameof(FastRviLength), 23)
			.SetGreaterThanZero()
			.SetDisplay("Fast RVI Length", "Smoothing length for fast RVI", "General");

		_slowRviLength = Param(nameof(SlowRviLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow RVI Length", "Smoothing length for slow RVI", "General");

		_cycleLength = Param(nameof(CycleLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Cycle", "Length of the stochastic cycle", "General");

		_highLevel = Param(nameof(HighLevel), 60)
			.SetDisplay("High Level", "Upper threshold", "General");

		_lowLevel = Param(nameof(LowLevel), -60)
			.SetDisplay("Low Level", "Lower threshold", "General");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between reversals", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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

		_recentCandles.Clear();
		_fastWindow.Clear();
		_slowWindow.Clear();
		_macd.Clear();
		_st.Clear();
		_fastSum = 0m;
		_slowSum = 0m;
		_stReady = false;
		_stcReady = false;
		_prevSt = 0m;
		_prevStc = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_recentCandles.Clear();
		_fastWindow.Clear();
		_slowWindow.Clear();
		_macd.Clear();
		_st.Clear();
		_fastSum = 0m;
		_slowSum = 0m;
		_stReady = false;
		_stcReady = false;
		_prevSt = 0m;
		_prevStc = 0m;
		_cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		_recentCandles.Add(candle);
		if (_recentCandles.Count > 4)
			_recentCandles.RemoveAt(0);

		if (_recentCandles.Count < 4)
			return;

		var rawRvi = CalculateRawRvi();
		UpdateWindow(_fastWindow, ref _fastSum, rawRvi, FastRviLength);
		UpdateWindow(_slowWindow, ref _slowSum, rawRvi, SlowRviLength);

		if (_fastWindow.Count < FastRviLength || _slowWindow.Count < SlowRviLength)
			return;

		var fast = _fastSum / _fastWindow.Count;
		var slow = _slowSum / _slowWindow.Count;
		var macd = fast - slow;
		AddValue(_macd, macd, CycleLength);
		if (_macd.Count < CycleLength)
			return;

		GetMinMax(_macd, out var minMacd, out var maxMacd);
		var st = maxMacd == minMacd ? _prevSt : (macd - minMacd) / (maxMacd - minMacd) * 100m;
		if (_stReady)
			st = 0.5m * (st - _prevSt) + _prevSt;
		else
			_stReady = true;

		_prevSt = st;
		AddValue(_st, st, CycleLength);

		GetMinMax(_st, out var minSt, out var maxSt);
		var previousStc = _prevStc;
		var stc = maxSt == minSt ? previousStc : (st - minSt) / (maxSt - minSt) * 200m - 100m;
		if (_stcReady)
			stc = 0.5m * (stc - previousStc) + previousStc;
		else
			_stcReady = true;

		_prevStc = stc;
		var delta = stc - previousStc;
		var longEntry = previousStc <= HighLevel && stc > HighLevel && delta > 0m;
		var shortEntry = previousStc >= LowLevel && stc < LowLevel && delta < 0m;
		var longExit = Position > 0 && stc < 0m;
		var shortExit = Position < 0 && stc > 0m;

		if (longExit)
		{
			SellMarket(Position);
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (shortExit)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && longEntry && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && shortEntry && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_cooldownRemaining = SignalCooldownBars;
		}
	}

	private decimal CalculateRawRvi()
	{
		var c0 = _recentCandles[0];
		var c1 = _recentCandles[1];
		var c2 = _recentCandles[2];
		var c3 = _recentCandles[3];
		var valueUp = ((c0.ClosePrice - c0.OpenPrice) +
			2m * (c1.ClosePrice - c1.OpenPrice) +
			2m * (c2.ClosePrice - c2.OpenPrice) +
			(c3.ClosePrice - c3.OpenPrice)) / 6m;
		var valueDn = ((c0.HighPrice - c0.LowPrice) +
			2m * (c1.HighPrice - c1.LowPrice) +
			2m * (c2.HighPrice - c2.LowPrice) +
			(c3.HighPrice - c3.LowPrice)) / 6m;
		return valueDn == 0m ? valueUp : valueUp / valueDn;
	}

	private static void UpdateWindow(Queue<decimal> window, ref decimal sum, decimal value, int length)
	{
		window.Enqueue(value);
		sum += value;

		while (window.Count > length)
			sum -= window.Dequeue();
	}

	private static void AddValue(List<decimal> values, decimal value, int limit)
	{
		values.Add(value);
		if (values.Count > limit)
			values.RemoveAt(0);
	}

	private static void GetMinMax(List<decimal> values, out decimal min, out decimal max)
	{
		min = values[0];
		max = values[0];

		for (var i = 1; i < values.Count; i++)
		{
			var value = values[i];
			if (value < min)
				min = value;
			if (value > max)
				max = value;
		}
	}
}
