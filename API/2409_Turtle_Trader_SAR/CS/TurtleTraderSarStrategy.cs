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
/// Turtle Trader strategy with Donchian breakout entries and ATR-based stops.
/// </summary>
public class TurtleTraderSarStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _closes = new();
	private decimal _stopPrice;

	public int ShortPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }
	public decimal StopMultiplier { get => _stopMultiplier.Value; set => _stopMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TurtleTraderSarStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Short Period", "Donchian breakout period", "General");

		_stopMultiplier = Param(nameof(StopMultiplier), 2m)
			.SetDisplay("Stop Multiplier", "ATR multiplier for stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

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
		_closes.Clear();
		_stopPrice = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highs.Clear();
		_lows.Clear();
		_closes.Clear();
		_stopPrice = 0m;

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		_closes.Add(candle.ClosePrice);

		if (_highs.Count < ShortPeriod + 1)
			return;

		// Trim to keep memory bounded
		while (_highs.Count > ShortPeriod + 10)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
			_closes.RemoveAt(0);
		}

		// Compute Donchian channel (excluding current candle)
		var len = _highs.Count;
		decimal highest = 0, lowest = decimal.MaxValue;
		for (int i = len - 1 - ShortPeriod; i < len - 1; i++)
		{
			if (_highs[i] > highest) highest = _highs[i];
			if (_lows[i] < lowest) lowest = _lows[i];
		}

		// Simple ATR approximation: average of (high-low) over last 20 candles
		var atrPeriod = Math.Min(20, len);
		decimal sumRange = 0;
		for (int i = len - atrPeriod; i < len; i++)
			sumRange += _highs[i] - _lows[i];
		var atr = sumRange / atrPeriod;

		var price = candle.ClosePrice;

		// Manage existing position
		if (Position > 0 && _stopPrice > 0 && price <= _stopPrice)
		{
			SellMarket();
			return;
		}
		else if (Position < 0 && _stopPrice > 0 && price >= _stopPrice)
		{
			BuyMarket();
			return;
		}

		if (Position != 0)
			return;

		// Breakout entry
		if (price > highest && atr > 0)
		{
			_stopPrice = price - StopMultiplier * atr;
			BuyMarket();
		}
		else if (price < lowest && atr > 0)
		{
			_stopPrice = price + StopMultiplier * atr;
			SellMarket();
		}
	}
}
