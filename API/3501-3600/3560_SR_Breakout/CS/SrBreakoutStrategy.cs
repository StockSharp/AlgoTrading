using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Support and resistance breakout strategy using Donchian channels.
/// Buys when price breaks above resistance (upper band), sells when breaks below support (lower band).
/// </summary>
public class SrBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _highHistory = new();
	private readonly Queue<decimal> _lowHistory = new();

	public int LookbackLength
	{
		get => _lookbackLength.Value;
		set => _lookbackLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SrBreakoutStrategy()
	{
		_lookbackLength = Param(nameof(LookbackLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of candles for Donchian channel", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highHistory.Clear();
		_lowHistory.Clear();

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

		if (_highHistory.Count < LookbackLength)
		{
			EnqueueCandle(candle);
			return;
		}

		var highs = _highHistory.ToArray();
		var lows = _lowHistory.ToArray();
		var upper = GetMax(highs);
		var lower = GetMin(lows);
		var close = candle.ClosePrice;
		var range = upper - lower;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;
		var breakoutPadding = range * 0.05m;

		// Break above resistance
		if (close > upper + breakoutPadding)
		{
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
		}
		// Break below support
		else if (close < lower - breakoutPadding)
		{
			if (Position >= 0)
				SellMarket(Position > 0 ? Math.Abs(Position) + volume : volume);
		}

		EnqueueCandle(candle);
	}

	private void EnqueueCandle(ICandleMessage candle)
	{
		_highHistory.Enqueue(candle.HighPrice);
		_lowHistory.Enqueue(candle.LowPrice);

		if (_highHistory.Count > LookbackLength)
		{
			_highHistory.Dequeue();
			_lowHistory.Dequeue();
		}
	}

	private static decimal GetMax(IEnumerable<decimal> values)
	{
		var max = decimal.MinValue;

		foreach (var value in values)
		{
			if (value > max)
				max = value;
		}

		return max;
	}

	private static decimal GetMin(IEnumerable<decimal> values)
	{
		var min = decimal.MaxValue;

		foreach (var value in values)
		{
			if (value < min)
				min = value;
		}

		return min;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_highHistory.Clear();
		_lowHistory.Clear();

		base.OnReseted();
	}
}
