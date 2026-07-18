using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on simple bar trend reversals.
/// </summary>
public class SimpleBarsStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<bool> _useClose;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly Queue<decimal> _lows = new();
	private readonly Queue<decimal> _highs = new();
	private decimal _prevMinLow;
	private decimal _prevMaxHigh;
	private int _prevTrend;
	private int? _pendingSignal;
	private bool _isInitialized;
	private int _cooldownRemaining;

	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	public bool UseClose
	{
		get => _useClose.Value;
		set => _useClose.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public SimpleBarsStrategy()
	{
		_period = Param(nameof(Period), 6)
			.SetDisplay("Period", "Number of bars for trend check", "General")
			.SetGreaterThanZero();

		_useClose = Param(nameof(UseClose), true)
			.SetDisplay("Use Close", "Use close price instead of extremes", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cooldownBars = Param(nameof(CooldownBars), 4)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_lows.Clear();
		_highs.Clear();
		_prevMinLow = 0m;
		_prevMaxHigh = 0m;
		_prevTrend = 0;
		_pendingSignal = null;
		_isInitialized = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (_pendingSignal is int pending && _cooldownRemaining == 0)
		{
			if (pending == 1 && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();

				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (pending == -1 && Position >= 0)
			{
				if (Position > 0)
					SellMarket();

				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}

		_pendingSignal = null;
		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);
		while (_highs.Count > Period)
			_highs.Dequeue();
		while (_lows.Count > Period)
			_lows.Dequeue();

		if (_highs.Count < Period || _lows.Count < Period)
			return;

		var minLow = GetLowest();
		var maxHigh = GetHighest();
		var buyPrice = UseClose ? candle.ClosePrice : candle.LowPrice;
		var sellPrice = UseClose ? candle.ClosePrice : candle.HighPrice;
		var trend = 0;
		if (!_isInitialized)
		{
			trend = candle.ClosePrice > candle.OpenPrice ? 1 : -1;
			_isInitialized = true;
		}
		else if (_prevTrend >= 0)
		{
			trend = buyPrice > _prevMinLow ? 1 : -1;
		}
		else
		{
			trend = sellPrice < _prevMaxHigh ? -1 : 1;
		}

		_pendingSignal = trend;
		_prevTrend = trend;
		_prevMinLow = minLow;
		_prevMaxHigh = maxHigh;
	}

	private decimal GetLowest()
	{
		var lowest = decimal.MaxValue;
		foreach (var low in _lows)
		{
			if (low < lowest)
				lowest = low;
		}

		return lowest;
	}

	private decimal GetHighest()
	{
		var highest = decimal.MinValue;
		foreach (var high in _highs)
		{
			if (high > highest)
				highest = high;
		}

		return highest;
	}
}
