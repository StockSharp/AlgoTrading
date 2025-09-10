using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Opening Range Breakout Strategy.
/// Tracks the high and low of the opening range and trades breakouts with risk management.
/// </summary>
public class OpeningRangeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rangeMinutes;
	private readonly StrategyParam<decimal> _rewardRisk;
	private readonly StrategyParam<decimal> _entryBuffer;
	private readonly StrategyParam<TimeSpan> _sessionStart;

	private decimal? _orHigh;
	private decimal? _orLow;
	private DateTimeOffset _sessionStartTime;
	private DateTimeOffset _sessionEndTime;
	private bool _rangeReady;
	private decimal _longEntry;
	private decimal _shortEntry;
	private decimal _stopLong;
	private decimal _stopShort;
	private decimal _longTp;
	private decimal _shortTp;
	private decimal _stopPrice;
	private decimal _targetPrice;

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Duration of the opening range in minutes.
	/// </summary>
	public int RangeMinutes
	{
		get => _rangeMinutes.Value;
		set => _rangeMinutes.Value = value;
	}

	/// <summary>
	/// Reward to risk ratio for target calculation.
	/// </summary>
	public decimal RewardRisk
	{
		get => _rewardRisk.Value;
		set => _rewardRisk.Value = value;
	}

	/// <summary>
	/// Entry buffer in price units.
	/// </summary>
	public decimal EntryBuffer
	{
		get => _entryBuffer.Value;
		set => _entryBuffer.Value = value;
	}

	/// <summary>
	/// Session start time (UTC).
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public OpeningRangeBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_rangeMinutes = Param(nameof(RangeMinutes), 15)
		.SetGreaterThanZero()
		.SetDisplay("Range Minutes", "Opening range duration", "General")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);

		_rewardRisk = Param(nameof(RewardRisk), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Reward/Risk", "Reward to risk ratio", "General")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);

		_entryBuffer = Param(nameof(EntryBuffer), 0.0001m)
		.SetDisplay("Entry Buffer", "Entry buffer", "General")
		.SetCanOptimize(true)
		.SetOptimize(0m, 0.001m, 0.0001m);

		_sessionStart = Param(nameof(SessionStart), TimeSpan.FromHours(8))
		.SetDisplay("Session Start", "Session start time (UTC)", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sessionStartTime = GetNextSessionStart(time);
		_sessionEndTime = _sessionStartTime.AddMinutes(RangeMinutes);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private DateTimeOffset GetNextSessionStart(DateTimeOffset time)
	{
		var start = new DateTimeOffset(time.Date + SessionStart, time.Offset);
		return time <= start ? start : start.AddDays(1);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var openTime = candle.OpenTime;

		// Reset for new day
		if (openTime.Date > _sessionStartTime.Date)
		{
			_orHigh = null;
			_orLow = null;
			_rangeReady = false;
			_sessionStartTime = GetNextSessionStart(openTime);
			_sessionEndTime = _sessionStartTime.AddMinutes(RangeMinutes);
		}

		// Collect opening range
		if (openTime >= _sessionStartTime && openTime < _sessionEndTime)
		{
			_orHigh = _orHigh.HasValue ? Math.Max(_orHigh.Value, candle.HighPrice) : candle.HighPrice;
			_orLow = _orLow.HasValue ? Math.Min(_orLow.Value, candle.LowPrice) : candle.LowPrice;
			return;
		}

		// Prepare breakout levels after range ends
		if (!_rangeReady && openTime >= _sessionEndTime && _orHigh.HasValue && _orLow.HasValue)
		{
			_rangeReady = true;
			_longEntry = _orHigh.Value + EntryBuffer;
			_shortEntry = _orLow.Value - EntryBuffer;
			_stopLong = _orLow.Value - EntryBuffer;
			_stopShort = _orHigh.Value + EntryBuffer;
			_longTp = _longEntry + (_longEntry - _stopLong) * RewardRisk;
			_shortTp = _shortEntry - (_stopShort - _shortEntry) * RewardRisk;
		}

		if (!_rangeReady)
		return;

		// Entry logic
		if (Position <= 0 && candle.ClosePrice > _longEntry)
		{
			RegisterOrder(CreateOrder(Sides.Buy, _longEntry, Volume));
			_stopPrice = _stopLong;
			_targetPrice = _longTp;
		}
		else if (Position >= 0 && candle.ClosePrice < _shortEntry)
		{
			RegisterOrder(CreateOrder(Sides.Sell, _shortEntry, Volume));
			_stopPrice = _stopShort;
			_targetPrice = _shortTp;
		}

		// Exit logic
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice)
			RegisterOrder(CreateOrder(Sides.Sell, _stopPrice, Math.Abs(Position)));
			else if (candle.HighPrice >= _targetPrice)
			RegisterOrder(CreateOrder(Sides.Sell, _targetPrice, Math.Abs(Position)));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice)
			RegisterOrder(CreateOrder(Sides.Buy, _stopPrice, Math.Abs(Position)));
			else if (candle.LowPrice <= _targetPrice)
			RegisterOrder(CreateOrder(Sides.Buy, _targetPrice, Math.Abs(Position)));
		}
	}
}
