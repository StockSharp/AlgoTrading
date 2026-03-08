using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using manually calculated channel based trailing stops with optional "noose" adjustment.
/// </summary>
public class ChannelTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _trailPeriod;
	private readonly StrategyParam<decimal> _trailStop;
	private readonly StrategyParam<bool> _useNooseTrailing;
	private readonly StrategyParam<bool> _useChannelTrailing;
	private readonly StrategyParam<bool> _deletePendingOrders;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();
	private decimal _longStop;
	private decimal _shortStop;
	private decimal? _takeProfitPrice;
	private int _cooldownRemaining;

	/// <summary>
	/// Period to calculate channel boundaries.
	/// </summary>
	public int TrailPeriod
	{
		get => _trailPeriod.Value;
		set => _trailPeriod.Value = value;
	}

	/// <summary>
	/// Offset added to channel boundaries.
	/// </summary>
	public decimal TrailStop
	{
		get => _trailStop.Value;
		set => _trailStop.Value = value;
	}

	/// <summary>
	/// Enable symmetrical "noose" trailing.
	/// </summary>
	public bool UseNooseTrailing
	{
		get => _useNooseTrailing.Value;
		set => _useNooseTrailing.Value = value;
	}

	/// <summary>
	/// Enable trailing stop based on channel levels.
	/// </summary>
	public bool UseChannelTrailing
	{
		get => _useChannelTrailing.Value;
		set => _useChannelTrailing.Value = value;
	}

	/// <summary>
	/// Delete pending orders after trade execution.
	/// </summary>
	public bool DeletePendingOrders
	{
		get => _deletePendingOrders.Value;
		set => _deletePendingOrders.Value = value;
	}

	/// <summary>
	/// Type of candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of completed candles to wait after a position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChannelTrailingStopStrategy"/> class.
	/// </summary>
	public ChannelTrailingStopStrategy()
	{
		_trailPeriod = Param(nameof(TrailPeriod), 10)
			.SetDisplay("Channel Period", "Lookback for channel calculation", "Parameters")
			.SetOptimize(5, 50, 5);

		_trailStop = Param(nameof(TrailStop), 100m)
			.SetDisplay("Trail Stop", "Offset from channel boundaries", "Parameters");

		_useNooseTrailing = Param(nameof(UseNooseTrailing), true)
			.SetDisplay("Use Noose Trailing", "Mirror stop relative to take profit", "Parameters");

		_useChannelTrailing = Param(nameof(UseChannelTrailing), true)
			.SetDisplay("Use Channel Trailing", "Adjust stop to channel levels", "Parameters");

		_deletePendingOrders = Param(nameof(DeletePendingOrders), true)
			.SetDisplay("Delete Pending Orders", "Cancel pending orders after fill", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");

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
		_highs.Clear();
		_lows.Clear();
		_longStop = 0m;
		_shortStop = 0m;
		_takeProfitPrice = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
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

		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);

		while (_highs.Count > TrailPeriod)
			_highs.Dequeue();

		while (_lows.Count > TrailPeriod)
			_lows.Dequeue();

		if (_highs.Count < TrailPeriod || _lows.Count < TrailPeriod)
			return;

		var upper = GetHighest();
		var lower = GetLowest();
		var range = upper - lower;
		if (range <= 0m)
			return;

		var threshold = range * 0.05m;
		if (_cooldownRemaining == 0)
		{
			if (candle.ClosePrice >= upper - threshold && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();

				BuyMarket();
				_longStop = candle.ClosePrice - TrailStop;
				_takeProfitPrice = candle.ClosePrice + TrailStop;
				_cooldownRemaining = CooldownBars;
			}
			else if (candle.ClosePrice <= lower + threshold && Position >= 0)
			{
				if (Position > 0)
					SellMarket();

				SellMarket();
				_shortStop = candle.ClosePrice + TrailStop;
				_takeProfitPrice = candle.ClosePrice - TrailStop;
				_cooldownRemaining = CooldownBars;
			}
		}

		if (UseChannelTrailing)
		{
			if (Position > 0)
			{
				var level = lower - TrailStop;
				if (level > _longStop)
					_longStop = level;
			}
			else if (Position < 0)
			{
				var level = upper + TrailStop;
				if (_shortStop == 0m || level < _shortStop)
					_shortStop = level;
			}
		}

		if (UseNooseTrailing && _takeProfitPrice is decimal takeProfitPrice)
		{
			if (Position > 0)
			{
				var noose = candle.ClosePrice - (takeProfitPrice - candle.ClosePrice);
				if (noose > _longStop)
					_longStop = noose;
			}
			else if (Position < 0)
			{
				var noose = candle.ClosePrice + (candle.ClosePrice - takeProfitPrice);
				if (_shortStop == 0m || noose < _shortStop)
					_shortStop = noose;
			}
		}

		if (Position > 0 && _longStop > 0m && candle.LowPrice <= _longStop)
		{
			SellMarket();
			_longStop = 0m;
			_takeProfitPrice = null;
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && _shortStop > 0m && candle.HighPrice >= _shortStop)
		{
			BuyMarket();
			_shortStop = 0m;
			_takeProfitPrice = null;
			_cooldownRemaining = CooldownBars;
		}
	}

	private decimal GetHighest()
	{
		var highest = decimal.MinValue;
		foreach (var value in _highs)
		{
			if (value > highest)
				highest = value;
		}

		return highest;
	}

	private decimal GetLowest()
	{
		var lowest = decimal.MaxValue;
		foreach (var value in _lows)
		{
			if (value < lowest)
				lowest = value;
		}

		return lowest;
	}
}
