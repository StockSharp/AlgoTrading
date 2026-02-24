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
/// Turtle Trading breakout strategy with Donchian channel and trailing stop.
/// Enters on channel breakout, exits on shorter channel break or trailing stop.
/// </summary>
public class TurtleTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _entryLength;
	private readonly StrategyParam<int> _exitLength;
	private readonly StrategyParam<decimal> _stopMultiple;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal _prevEntryHigh;
	private decimal _prevEntryLow;
	private decimal _prevClose;
	private decimal _trailingStop;
	private decimal _entryPrice;

	public int EntryLength { get => _entryLength.Value; set => _entryLength.Value = value; }
	public int ExitLength { get => _exitLength.Value; set => _exitLength.Value = value; }
	public decimal StopMultiple { get => _stopMultiple.Value; set => _stopMultiple.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TurtleTradingStrategy()
	{
		_entryLength = Param(nameof(EntryLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Entry Length", "Entry channel length", "General");

		_exitLength = Param(nameof(ExitLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Exit Length", "Exit channel length", "General");

		_stopMultiple = Param(nameof(StopMultiple), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Multiple", "StdDev multiple for stop", "Risk");

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
		_prevEntryHigh = 0;
		_prevEntryLow = 0;
		_prevClose = 0;
		_trailingStop = 0;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stdDev = new StandardDeviation { Length = EntryLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		var maxLen = Math.Max(EntryLength, ExitLength) + 1;
		while (_highs.Count > maxLen)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count <= EntryLength)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		// Calculate entry channel (excluding current)
		var entryHigh = decimal.MinValue;
		var entryLow = decimal.MaxValue;
		var start = _highs.Count - 1 - EntryLength;
		for (var i = start; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > entryHigh) entryHigh = _highs[i];
			if (_lows[i] < entryLow) entryLow = _lows[i];
		}

		// Calculate exit channel
		var exitLen = Math.Min(ExitLength, _highs.Count - 1);
		var exitHigh = decimal.MinValue;
		var exitLow = decimal.MaxValue;
		for (var i = _highs.Count - 1 - exitLen; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > exitHigh) exitHigh = _highs[i];
			if (_lows[i] < exitLow) exitLow = _lows[i];
		}

		// Trailing stop management
		if (Position > 0 && stdVal > 0)
		{
			var newStop = candle.ClosePrice - StopMultiple * stdVal;
			if (_trailingStop == 0) _trailingStop = newStop;
			else _trailingStop = Math.Max(_trailingStop, newStop);
		}
		else if (Position < 0 && stdVal > 0)
		{
			var newStop = candle.ClosePrice + StopMultiple * stdVal;
			if (_trailingStop == 0) _trailingStop = newStop;
			else _trailingStop = Math.Min(_trailingStop, newStop);
		}

		// Exits
		if (Position > 0)
		{
			if (candle.ClosePrice < exitLow || candle.ClosePrice < _trailingStop)
			{
				SellMarket();
				_trailingStop = 0;
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice > exitHigh || candle.ClosePrice > _trailingStop)
			{
				BuyMarket();
				_trailingStop = 0;
				_entryPrice = 0;
			}
		}

		// Entries: Donchian breakout with crossover
		if (_prevEntryHigh > 0 && _prevClose > 0)
		{
			var longBreakout = _prevClose <= _prevEntryHigh && candle.ClosePrice > entryHigh;
			var shortBreakout = _prevClose >= _prevEntryLow && candle.ClosePrice < entryLow;

			if (longBreakout && Position <= 0)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_trailingStop = candle.ClosePrice - StopMultiple * stdVal;
			}
			else if (shortBreakout && Position >= 0)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_trailingStop = candle.ClosePrice + StopMultiple * stdVal;
			}
		}

		_prevEntryHigh = entryHigh;
		_prevEntryLow = entryLow;
		_prevClose = candle.ClosePrice;
	}
}
