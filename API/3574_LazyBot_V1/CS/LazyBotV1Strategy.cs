using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "LazyBot V1" MetaTrader expert.
/// Daily breakout strategy using previous N-bar high/low range.
/// Buys when price breaks above previous high, sells when below previous low.
/// </summary>
public class LazyBotV1Strategy : Strategy
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

	public LazyBotV1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for breakout detection", "General");

		_lookback = Param(nameof(Lookback), 20)
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

		// Build range from previous bars (not including current)
		if (_highs.Count >= Lookback)
		{
			decimal highest = decimal.MinValue;
			decimal lowest = decimal.MaxValue;
			foreach (var h in _highs)
				if (h > highest) highest = h;
			foreach (var l in _lows)
				if (l < lowest) lowest = l;

			var close = candle.ClosePrice;
			var volume = Volume;
			if (volume <= 0)
				volume = 1;

			if (close > highest)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));

				if (Position <= 0)
					BuyMarket(volume);
			}
			else if (close < lowest)
			{
				if (Position > 0)
					SellMarket(Position);

				if (Position >= 0)
					SellMarket(volume);
			}
		}

		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);

		if (_highs.Count > Lookback)
		{
			_highs.Dequeue();
			_lows.Dequeue();
		}
	}
}
