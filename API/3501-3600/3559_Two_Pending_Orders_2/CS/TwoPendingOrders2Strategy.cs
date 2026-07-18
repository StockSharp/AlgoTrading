using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Two pending orders 2" MetaTrader expert.
/// Places symmetric breakout levels around recent range and enters on breakout.
/// Uses high/low of N bars as breakout boundaries.
/// </summary>
public class TwoPendingOrders2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookback;

	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	public TwoPendingOrders2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for breakout detection", "General");

		_lookback = Param(nameof(Lookback), 10)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of bars for high/low range", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highs.Clear();
		_lows.Clear();

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

		if (_highs.Count < Lookback)
		{
			EnqueueCandle(candle);
			return;
		}

		decimal highest = decimal.MinValue;
		decimal lowest = decimal.MaxValue;
		var highs = _highs.ToArray();
		var lows = _lows.ToArray();
		foreach (var h in highs)
			if (h > highest) highest = h;
		foreach (var l in lows)
			if (l < lowest) lowest = l;

		var close = candle.ClosePrice;
		var volume = Volume;
		if (volume <= 0)
			volume = 1;
		var range = highest - lowest;
		var breakoutPadding = range * 0.05m;

		// Breakout above range
		if (close > highest + breakoutPadding)
		{
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
		}
		// Breakout below range
		else if (close < lowest - breakoutPadding)
		{
			if (Position >= 0)
				SellMarket(Position > 0 ? Math.Abs(Position) + volume : volume);
		}

		EnqueueCandle(candle);
	}

	private void EnqueueCandle(ICandleMessage candle)
	{
		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);

		if (_highs.Count > Lookback)
		{
			_highs.Dequeue();
			_lows.Dequeue();
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_highs.Clear();
		_lows.Clear();

		base.OnReseted();
	}
}
