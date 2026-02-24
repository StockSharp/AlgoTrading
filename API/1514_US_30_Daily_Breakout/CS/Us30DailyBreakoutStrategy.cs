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
/// Daily breakout strategy using rolling high/low from previous session.
/// Buys on breakout above previous session high, sells on breakdown below previous session low.
/// Uses percent-based TP/SL.
/// </summary>
public class Us30DailyBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _tpPct;
	private readonly StrategyParam<decimal> _slPct;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal _entryPrice;
	private bool _breakoutTraded;
	private bool _breakdownTraded;
	private int _barsSinceReset;

	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public decimal TpPct { get => _tpPct.Value; set => _tpPct.Value = value; }
	public decimal SlPct { get => _slPct.Value; set => _slPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Us30DailyBreakoutStrategy()
	{
		_lookback = Param(nameof(Lookback), 48)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Bars for high/low calculation", "General");

		_tpPct = Param(nameof(TpPct), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");

		_slPct = Param(nameof(SlPct), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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
		_breakoutTraded = false;
		_breakdownTraded = false;
		_barsSinceReset = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 2 };

		_highs.Clear();
		_lows.Clear();
		_entryPrice = 0;
		_breakoutTraded = false;
		_breakdownTraded = false;
		_barsSinceReset = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		_barsSinceReset++;

		while (_highs.Count > Lookback + 1)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count <= Lookback)
			return;

		// Previous session high/low (excluding current bar)
		decimal prevHigh = decimal.MinValue;
		decimal prevLow = decimal.MaxValue;
		for (int i = 0; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > prevHigh) prevHigh = _highs[i];
			if (_lows[i] < prevLow) prevLow = _lows[i];
		}

		// Reset breakout flags periodically (every lookback bars)
		if (_barsSinceReset >= Lookback)
		{
			_breakoutTraded = false;
			_breakdownTraded = false;
			_barsSinceReset = 0;
		}

		// TP/SL management
		if (Position > 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice >= _entryPrice * (1m + TpPct / 100m) ||
				candle.ClosePrice <= _entryPrice * (1m - SlPct / 100m))
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice <= _entryPrice * (1m - TpPct / 100m) ||
				candle.ClosePrice >= _entryPrice * (1m + SlPct / 100m))
			{
				BuyMarket();
				_entryPrice = 0;
				return;
			}
		}

		// Entry signals
		if (Position == 0)
		{
			if (!_breakoutTraded && candle.ClosePrice > prevHigh)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_breakoutTraded = true;
			}
			else if (!_breakdownTraded && candle.ClosePrice < prevLow)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_breakdownTraded = true;
			}
		}
	}
}
