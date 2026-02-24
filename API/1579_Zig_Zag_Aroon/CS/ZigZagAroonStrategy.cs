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
/// ZigZag + Aroon strategy.
/// Uses manual ZigZag pivot detection and Aroon crossover for entry signals.
/// </summary>
public class ZigZagAroonStrategy : Strategy
{
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<int> _aroonLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	private decimal _lastZigzagHigh;
	private decimal _lastZigzagLow;
	private int _direction;
	private decimal _prevAroonUp;
	private decimal _prevAroonDown;

	public int ZigZagDepth { get => _zigZagDepth.Value; set => _zigZagDepth.Value = value; }
	public int AroonLength { get => _aroonLength.Value; set => _aroonLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZigZagAroonStrategy()
	{
		_zigZagDepth = Param(nameof(ZigZagDepth), 5)
			.SetGreaterThanZero()
			.SetDisplay("ZigZag Depth", "Pivot search depth", "ZigZag");

		_aroonLength = Param(nameof(AroonLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Aroon Period", "Aroon indicator period", "Aroon");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_highs.Clear();
		_lows.Clear();
		_lastZigzagHigh = 0;
		_lastZigzagLow = 0;
		_direction = 0;
		_prevAroonUp = 0;
		_prevAroonDown = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 2 };

		_highs.Clear();
		_lows.Clear();
		_lastZigzagHigh = 0;
		_lastZigzagLow = 0;
		_direction = 0;
		_prevAroonUp = 0;
		_prevAroonDown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _dummy)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		var maxLen = Math.Max(ZigZagDepth, AroonLength) + 2;
		if (_highs.Count > maxLen * 2)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < ZigZagDepth)
			return;

		// Manual highest/lowest over ZigZagDepth
		var recentHighs = _highs.Skip(_highs.Count - ZigZagDepth);
		var recentLows = _lows.Skip(_lows.Count - ZigZagDepth);
		var highest = recentHighs.Max();
		var lowest = recentLows.Min();

		// ZigZag direction
		if (candle.HighPrice >= highest && _direction != 1)
		{
			_lastZigzagHigh = candle.HighPrice;
			_direction = 1;
		}
		else if (candle.LowPrice <= lowest && _direction != -1)
		{
			_lastZigzagLow = candle.LowPrice;
			_direction = -1;
		}

		if (_highs.Count < AroonLength + 1)
			return;

		// Manual Aroon calculation
		var aroonHighs = _highs.Skip(_highs.Count - AroonLength - 1).ToList();
		var aroonLows = _lows.Skip(_lows.Count - AroonLength - 1).ToList();

		var highestIdx = 0;
		var lowestIdx = 0;
		for (int i = 1; i < aroonHighs.Count; i++)
		{
			if (aroonHighs[i] >= aroonHighs[highestIdx]) highestIdx = i;
			if (aroonLows[i] <= aroonLows[lowestIdx]) lowestIdx = i;
		}

		var aroonUp = 100m * highestIdx / AroonLength;
		var aroonDown = 100m * lowestIdx / AroonLength;

		// Aroon crossover
		var crossUp = _prevAroonUp <= _prevAroonDown && aroonUp > aroonDown;
		var crossDown = _prevAroonDown <= _prevAroonUp && aroonDown > aroonUp;

		if (crossUp && _direction == 1 && Position <= 0)
			BuyMarket();
		else if (crossDown && _direction == -1 && Position >= 0)
			SellMarket();

		_prevAroonUp = aroonUp;
		_prevAroonDown = aroonDown;
	}
}
