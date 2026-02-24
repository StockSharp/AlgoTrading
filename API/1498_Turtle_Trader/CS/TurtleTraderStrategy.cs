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
/// Simplified Turtle Trader strategy.
/// Uses Donchian channel breakout: buy on new high, sell on new low.
/// Exits on opposite channel level.
/// </summary>
public class TurtleTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _entryLength;
	private readonly StrategyParam<int> _exitLength;
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal _entryPrice;

	public int EntryLength { get => _entryLength.Value; set => _entryLength.Value = value; }
	public int ExitLength { get => _exitLength.Value; set => _exitLength.Value = value; }
	public decimal StopPct { get => _stopPct.Value; set => _stopPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TurtleTraderStrategy()
	{
		_entryLength = Param(nameof(EntryLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Entry Length", "Donchian breakout length", "General");

		_exitLength = Param(nameof(ExitLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Exit Length", "Donchian exit length", "General");

		_stopPct = Param(nameof(StopPct), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_highs.Clear();
		_lows.Clear();
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = EntryLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > EntryLength + 1)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < EntryLength)
			return;

		// Calculate entry Donchian channel (exclude current bar)
		var entryHigh = decimal.MinValue;
		var entryLow = decimal.MaxValue;
		for (var i = 0; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > entryHigh) entryHigh = _highs[i];
			if (_lows[i] < entryLow) entryLow = _lows[i];
		}

		// Calculate exit channel (shorter period)
		var exitLen = Math.Min(ExitLength, _highs.Count - 1);
		var exitHigh = decimal.MinValue;
		var exitLow = decimal.MaxValue;
		for (var i = _highs.Count - 1 - exitLen; i < _highs.Count - 1; i++)
		{
			if (i < 0) continue;
			if (_highs[i] > exitHigh) exitHigh = _highs[i];
			if (_lows[i] < exitLow) exitLow = _lows[i];
		}

		// Exit checks first
		if (Position > 0)
		{
			var stop = _entryPrice * (1m - StopPct / 100m);
			if (candle.LowPrice <= exitLow || candle.LowPrice <= stop)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			var stop = _entryPrice * (1m + StopPct / 100m);
			if (candle.HighPrice >= exitHigh || candle.HighPrice >= stop)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry: Donchian breakout
		if (candle.HighPrice >= entryHigh && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (candle.LowPrice <= entryLow && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
	}
}
