using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double ZigZag alignment strategy. Uses fast and slow swing detectors
/// based on highest/lowest price lookbacks to find aligned pivot points and trade breakouts.
/// </summary>
public class DoubleZigZagStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	private int _fastDirection;
	private int _slowDirection;
	private decimal _lastFastPivotHigh;
	private decimal _lastFastPivotLow;

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DoubleZigZagStrategy()
	{
		_fastLength = Param(nameof(FastLength), 8)
			.SetDisplay("Fast Length", "Lookback for the fast swing detector", "Indicators");
		_slowLength = Param(nameof(SlowLength), 30)
			.SetDisplay("Slow Length", "Lookback for the slow confirmation swing", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to analyze", "General");
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
		_highs.Clear();
		_lows.Clear();
		_fastDirection = 0;
		_slowDirection = 0;
		_lastFastPivotHigh = 0;
		_lastFastPivotLow = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highs.Clear();
		_lows.Clear();
		_fastDirection = 0;
		_slowDirection = 0;
		_lastFastPivotHigh = 0;
		_lastFastPivotLow = 0;

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

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		var maxBuf = Math.Max(FastLength, SlowLength) + 1;
		if (_highs.Count > maxBuf)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < SlowLength)
			return;

		// Calculate fast and slow highest/lowest
		var fastHighest = GetMax(_highs, FastLength);
		var fastLowest = GetMin(_lows, FastLength);
		var slowHighest = GetMax(_highs, SlowLength);
		var slowLowest = GetMin(_lows, SlowLength);

		var prevFastDir = _fastDirection;
		var prevSlowDir = _slowDirection;

		// Detect fast swing pivot
		if (_fastDirection <= 0 && candle.HighPrice >= fastHighest)
		{
			_fastDirection = 1;
			_lastFastPivotHigh = candle.HighPrice;
		}
		else if (_fastDirection >= 0 && candle.LowPrice <= fastLowest)
		{
			_fastDirection = -1;
			_lastFastPivotLow = candle.LowPrice;
		}

		// Detect slow swing pivot
		if (_slowDirection <= 0 && candle.HighPrice >= slowHighest)
			_slowDirection = 1;
		else if (_slowDirection >= 0 && candle.LowPrice <= slowLowest)
			_slowDirection = -1;

		// When both fast and slow pivot in the same direction, generate signal
		var fastFlippedUp = prevFastDir <= 0 && _fastDirection > 0;
		var fastFlippedDown = prevFastDir >= 0 && _fastDirection < 0;
		var slowFlippedUp = prevSlowDir <= 0 && _slowDirection > 0;
		var slowFlippedDown = prevSlowDir >= 0 && _slowDirection < 0;

		if (fastFlippedUp && (slowFlippedUp || _slowDirection > 0))
		{
			if (Position <= 0)
				BuyMarket();
		}
		else if (fastFlippedDown && (slowFlippedDown || _slowDirection < 0))
		{
			if (Position >= 0)
				SellMarket();
		}
	}

	private static decimal GetMax(List<decimal> data, int length)
	{
		var max = decimal.MinValue;
		var start = Math.Max(0, data.Count - length);
		for (var i = start; i < data.Count; i++)
		{
			if (data[i] > max)
				max = data[i];
		}
		return max;
	}

	private static decimal GetMin(List<decimal> data, int length)
	{
		var min = decimal.MaxValue;
		var start = Math.Max(0, data.Count - length);
		for (var i = start; i < data.Count; i++)
		{
			if (data[i] < min)
				min = data[i];
		}
		return min;
	}
}
