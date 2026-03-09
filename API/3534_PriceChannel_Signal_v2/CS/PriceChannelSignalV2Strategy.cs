using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price Channel Signal v2 strategy that reacts to Donchian channel breakouts.
/// </summary>
public class PriceChannelSignalV2Strategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _highHistory = new();
	private readonly Queue<decimal> _lowHistory = new();
	private int _previousTrend;
	private decimal? _previousClose;

	/// <summary>
	/// Channel lookback length.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize a new instance of <see cref="PriceChannelSignalV2Strategy"/>.
	/// </summary>
	public PriceChannelSignalV2Strategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Donchian lookback used for Price Channel", "Price Channel");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for Price Channel", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousTrend = 0;
		_previousClose = null;
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

		if (_highHistory.Count < ChannelPeriod)
		{
			EnqueueCandle(candle);
			return;
		}

		var highs = _highHistory.ToArray();
		var lows = _lowHistory.ToArray();
		var channelHigh = GetMax(highs);
		var channelLow = GetMin(lows);
		var range = channelHigh - channelLow;
		if (range <= 0m)
		{
			_previousClose = candle.ClosePrice;
			EnqueueCandle(candle);
			return;
		}

		var mid = (channelHigh + channelLow) / 2m;

		// Update trend state based on channel breakout
		var trend = _previousTrend;
		if (candle.ClosePrice > channelHigh + range * 0.05m)
			trend = 1;
		else if (candle.ClosePrice < channelLow - range * 0.05m)
			trend = -1;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Trend reversal signals
		var changedPosition = false;

		if (trend > 0 && _previousTrend <= 0)
		{
			if (Position <= 0)
			{
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
				changedPosition = true;
			}
		}
		else if (trend < 0 && _previousTrend >= 0)
		{
			if (Position >= 0)
			{
				SellMarket(Position > 0 ? Math.Abs(Position) + volume : volume);
				changedPosition = true;
			}
		}

		// Exit on mid-line cross
		if (!changedPosition && Position > 0 && _previousClose is decimal pc1 && pc1 >= mid && candle.ClosePrice < mid)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (!changedPosition && Position < 0 && _previousClose is decimal pc2 && pc2 <= mid && candle.ClosePrice > mid)
		{
			BuyMarket(Math.Abs(Position));
		}

		_previousTrend = trend;
		_previousClose = candle.ClosePrice;
		EnqueueCandle(candle);
	}

	private void EnqueueCandle(ICandleMessage candle)
	{
		_highHistory.Enqueue(candle.HighPrice);
		_lowHistory.Enqueue(candle.LowPrice);

		while (_highHistory.Count > ChannelPeriod)
			_highHistory.Dequeue();

		while (_lowHistory.Count > ChannelPeriod)
			_lowHistory.Dequeue();
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
		_previousTrend = 0;
		_previousClose = null;
		_highHistory.Clear();
		_lowHistory.Clear();

		base.OnReseted();
	}
}
