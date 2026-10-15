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
/// Exp Hull Trend strategy based on Hull moving average cross.
/// Opens long when fast hull crosses above smoothed hull and short on opposite.
/// </summary>
public class ExpHullTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _minSpreadPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;
	private int _cooldownRemaining;

	// Manual WMA for final smoothing
	private readonly List<decimal> _finalBuffer = new();
	private int _finalLength;

	/// <summary>
	/// Base period for Hull moving average.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Minimum normalized spread between the fast and slow lines required for a valid signal.
	/// </summary>
	public decimal MinSpreadPercent
	{
		get => _minSpreadPercent.Value;
		set => _minSpreadPercent.Value = value;
	}

	/// <summary>
	/// Number of completed candles to wait after a position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Type of candles for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpHullTrendStrategy"/>.
	/// </summary>
	public ExpHullTrendStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetDisplay("Hull Length", "Base period for Hull calculation", "Indicator");

		_minSpreadPercent = Param(nameof(MinSpreadPercent), 0.0015m)
			.SetDisplay("Min Spread %", "Minimum normalized spread between Hull lines", "Signal");

		_cooldownBars = Param(nameof(CooldownBars), 12)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for processing", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_finalLength = Math.Max(1, (int)Math.Sqrt(Length));

		var wmaHalf = new WeightedMovingAverage { Length = Math.Max(1, Length / 2) };
		var wmaFull = new WeightedMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wmaHalf, wmaFull, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = 0m;
		_prevSlow = 0m;
		_initialized = false;
		_cooldownRemaining = 0;
		_finalBuffer.Clear();
		_finalLength = 0;
	}

	private decimal CalcWma(decimal newVal)
	{
		_finalBuffer.Add(newVal);
		if (_finalBuffer.Count > _finalLength)
			_finalBuffer.RemoveAt(0);

		if (_finalBuffer.Count < _finalLength)
			return newVal;

		decimal sumWeight = 0;
		decimal sumVal = 0;
		for (int i = 0; i < _finalBuffer.Count; i++)
		{
			var w = i + 1;
			sumVal += _finalBuffer[i] * w;
			sumWeight += w;
		}
		return sumVal / sumWeight;
	}

	private void ProcessCandle(ICandleMessage candle, decimal halfValue, decimal fullValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fast = 2m * halfValue - fullValue; // intermediate Hull value
		var slow = CalcWma(fast); // smoothed Hull

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;
		var spread = Math.Abs(fast - slow) / Math.Max(Math.Abs(slow), 1m);

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (crossUp && spread >= MinSpreadPercent && _cooldownRemaining == 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}
		else if (crossDown && spread >= MinSpreadPercent && _cooldownRemaining == 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_cooldownRemaining = CooldownBars;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
