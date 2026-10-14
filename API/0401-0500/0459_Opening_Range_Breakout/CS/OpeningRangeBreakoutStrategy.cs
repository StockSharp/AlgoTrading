namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Opening Range Breakout Strategy.
/// Candles opening during the first RangeMinutes after SessionStart (UTC) form the opening range. Afterwards, a close above the
/// range high plus EntryBuffer buys and a close below the range low minus EntryBuffer sells, once per session. The stop sits
/// on the opposite side of the range and the target is RewardRisk times the risk away from the entry.
/// </summary>
public class OpeningRangeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _rangeMinutes;
	private readonly StrategyParam<decimal> _rewardRisk;
	private readonly StrategyParam<decimal> _entryBuffer;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _sessionDate;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private bool _tradedToday;
	private decimal? _stopPrice;
	private decimal? _targetPrice;

	/// <summary>
	/// Length of the opening range in minutes.
	/// </summary>
	public int RangeMinutes
	{
		get => _rangeMinutes.Value;
		set => _rangeMinutes.Value = value;
	}

	/// <summary>
	/// Target distance as a multiple of the risk.
	/// </summary>
	public decimal RewardRisk
	{
		get => _rewardRisk.Value;
		set => _rewardRisk.Value = value;
	}

	/// <summary>
	/// Price buffer beyond the range required for a breakout.
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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public OpeningRangeBreakoutStrategy()
	{
		_rangeMinutes = Param(nameof(RangeMinutes), 15)
			.SetGreaterThanZero()
			.SetDisplay("Range Minutes", "Length of the opening range in minutes", "Session");

		_rewardRisk = Param(nameof(RewardRisk), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Reward/Risk", "Target distance as a multiple of the risk", "Risk");

		_entryBuffer = Param(nameof(EntryBuffer), 0.0001m)
			.SetNotNegative()
			.SetDisplay("Entry Buffer", "Price buffer beyond the range required for a breakout", "Trading");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(8, 0, 0))
			.SetDisplay("Session Start", "Session start time (UTC)", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_sessionDate = null;
		_rangeHigh = null;
		_rangeLow = null;
		_tradedToday = false;
		_stopPrice = null;
		_targetPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

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

		var openTime = candle.OpenTime;
		var date = openTime.Date;

		if (_sessionDate != date)
		{
			_sessionDate = date;
			_rangeHigh = null;
			_rangeLow = null;
			_tradedToday = false;
		}

		var rangeStart = date + SessionStart;
		var rangeEnd = rangeStart + TimeSpan.FromMinutes(RangeMinutes);

		if (openTime >= rangeStart && openTime < rangeEnd)
		{
			_rangeHigh = _rangeHigh is decimal h ? Math.Max(h, candle.HighPrice) : candle.HighPrice;
			_rangeLow = _rangeLow is decimal l ? Math.Min(l, candle.LowPrice) : candle.LowPrice;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if ((_stopPrice is decimal stop && candle.LowPrice <= stop) || (_targetPrice is decimal target && candle.HighPrice >= target))
			{
				SellMarket(Position);
				_stopPrice = null;
				_targetPrice = null;
			}

			return;
		}

		if (Position < 0)
		{
			if ((_stopPrice is decimal stop && candle.HighPrice >= stop) || (_targetPrice is decimal target && candle.LowPrice <= target))
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_targetPrice = null;
			}

			return;
		}

		if (_tradedToday || openTime < rangeEnd || _rangeHigh is not decimal high || _rangeLow is not decimal low)
			return;

		var close = candle.ClosePrice;

		if (close > high + EntryBuffer)
		{
			BuyMarket(Volume);
			_stopPrice = low;
			_targetPrice = close + RewardRisk * (close - low);
			_tradedToday = true;
		}
		else if (close < low - EntryBuffer)
		{
			SellMarket(Volume);
			_stopPrice = high;
			_targetPrice = close - RewardRisk * (high - close);
			_tradedToday = true;
		}
	}
}
