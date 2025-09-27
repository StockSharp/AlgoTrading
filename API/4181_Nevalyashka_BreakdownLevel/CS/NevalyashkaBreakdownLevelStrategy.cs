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
/// Range breakout strategy converted from the Nevalyashka BreakdownLevel expert advisor.
/// Builds an opening range between the configured start and end times and trades breakouts with optional martingale recovery.
/// </summary>
public class NevalyashkaBreakdownLevelStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _rangeStart;
	private readonly StrategyParam<TimeSpan> _rangeEnd;
	private readonly StrategyParam<TimeSpan> _ordersCloseTime;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<bool> _useBreakeven;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentSessionDate;
	private bool _sessionInitialized;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private bool _rangeReady;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private bool _breakevenActivated;
	private decimal _lastOrderVolume;
	private bool _reversePending;
	private Sides? _reverseDirection;
	private decimal _reverseDistance;
	private DateTime? _blockedTradeDate;

	/// <summary>
	/// Opening range start time (inclusive).
	/// </summary>
	public TimeSpan RangeStart
	{
		get => _rangeStart.Value;
		set => _rangeStart.Value = value;
	}

	/// <summary>
	/// Opening range end time (inclusive).
	/// </summary>
	public TimeSpan RangeEnd
	{
		get => _rangeEnd.Value;
		set => _rangeEnd.Value = value;
	}

	/// <summary>
	/// Time to flatten all positions. New entries are blocked afterwards when this time is after the range end.
	/// </summary>
	public TimeSpan OrdersCloseTime
	{
		get => _ordersCloseTime.Value;
		set => _ordersCloseTime.Value = value;
	}

	/// <summary>
	/// Base order volume used for the initial breakout trades.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the next trade after a stop-loss to recover the previous loss (martingale step).
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Enables moving the stop to break-even once the trade moves halfway to the take-profit level.
	/// </summary>
	public bool UseBreakeven
	{
		get => _useBreakeven.Value;
		set => _useBreakeven.Value = value;
	}

	/// <summary>
	/// Candle type used for range construction and trading decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="NevalyashkaBreakdownLevelStrategy"/> with default parameters.
	/// </summary>
	public NevalyashkaBreakdownLevelStrategy()
	{
		_rangeStart = Param(nameof(RangeStart), new TimeSpan(4, 0, 0))
		.SetDisplay("Range Start", "Start time of the reference range", "Schedule");

		_rangeEnd = Param(nameof(RangeEnd), new TimeSpan(9, 0, 0))
		.SetDisplay("Range End", "End time of the reference range", "Schedule");

		_ordersCloseTime = Param(nameof(OrdersCloseTime), new TimeSpan(23, 30, 0))
		.SetDisplay("Orders Close Time", "Time of day to flatten all positions", "Risk");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Lot size used for breakout entries", "Trading")
		.SetCanOptimize(true);

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Martingale Multiplier", "Multiplier applied after a stop-loss", "Risk")
		.SetCanOptimize(true);

		_useBreakeven = Param(nameof(UseBreakeven), true)
		.SetDisplay("Use Breakeven", "Move the stop to break-even once the trade is in profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to subscribe", "General");

		_lastOrderVolume = 0m;
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

		_sessionInitialized = false;
		_rangeHigh = null;
		_rangeLow = null;
		_rangeReady = false;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakevenActivated = false;
		_reversePending = false;
		_reverseDirection = null;
		_reverseDistance = 0m;
		_lastOrderVolume = OrderVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lastOrderVolume = OrderVolume;

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

		var localTime = candle.OpenTime.ToLocalTime();
		var sessionDate = localTime.Date;
		var time = localTime.TimeOfDay;

		if (!_sessionInitialized || sessionDate != _currentSessionDate)
		ResetSession(sessionDate);

		if (OrdersCloseTime > RangeEnd && time >= OrdersCloseTime)
		{
			HandleOrdersCloseTime(sessionDate);
			return;
		}

		if (time >= RangeStart && time <= RangeEnd)
		UpdateRange(candle);

		if (time >= RangeEnd && _rangeHigh.HasValue && _rangeLow.HasValue)
		_rangeReady = true;

		if (Position != 0)
		{
			ManageActivePosition(candle, sessionDate);
			return;
		}

		if (_reversePending && _reverseDirection.HasValue)
		{
			if (TryOpenReverseTrade(candle))
			return;
		}

		if (!_rangeReady)
		return;

		if (_blockedTradeDate.HasValue && _blockedTradeDate.Value.Date == sessionDate)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (OrdersCloseTime > RangeEnd && time >= OrdersCloseTime)
		return;

		TryBreakoutEntry(candle);
	}

	private void ResetSession(DateTime date)
	{
		_sessionInitialized = true;
		_currentSessionDate = date;
		_rangeHigh = null;
		_rangeLow = null;
		_rangeReady = false;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakevenActivated = false;
		_reversePending = false;
		_reverseDirection = null;
		_reverseDistance = 0m;
	}

	private void HandleOrdersCloseTime(DateTime sessionDate)
	{
		if (Position != 0)
		{
			ClosePosition();
			LogInfo($"Forced exit at {OrdersCloseTime} to flatten all positions.");
		}

		_blockedTradeDate = sessionDate;
		_reversePending = false;
		_reverseDirection = null;
		_reverseDistance = 0m;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private void UpdateRange(ICandleMessage candle)
	{
		_rangeHigh = _rangeHigh.HasValue ? Math.Max(_rangeHigh.Value, candle.HighPrice) : candle.HighPrice;
		_rangeLow = _rangeLow.HasValue ? Math.Min(_rangeLow.Value, candle.LowPrice) : candle.LowPrice;
	}

	private void ManageActivePosition(ICandleMessage candle, DateTime sessionDate)
	{
		if (_entryPrice is null || _stopPrice is null || _takeProfitPrice is null)
		return;

		var direction = Position > 0 ? Sides.Buy : Sides.Sell;

		if (direction == Sides.Buy)
		{
			if (candle.LowPrice <= _stopPrice.Value)
			{
				ClosePosition();
				HandleStop(direction, _stopPrice.Value, candle);
				return;
			}

			if (candle.HighPrice >= _takeProfitPrice.Value)
			{
				ClosePosition();
				HandleTakeProfit(sessionDate, _takeProfitPrice.Value, direction);
				return;
			}

			TryActivateBreakeven(candle.HighPrice, direction);
		}
		else
		{
			if (candle.HighPrice >= _stopPrice.Value)
			{
				ClosePosition();
				HandleStop(direction, _stopPrice.Value, candle);
				return;
			}

			if (candle.LowPrice <= _takeProfitPrice.Value)
			{
				ClosePosition();
				HandleTakeProfit(sessionDate, _takeProfitPrice.Value, direction);
				return;
			}

			TryActivateBreakeven(candle.LowPrice, direction);
		}
	}

	private void TryActivateBreakeven(decimal extremePrice, Sides direction)
	{
		if (!UseBreakeven || !_entryPrice.HasValue || !_takeProfitPrice.HasValue || _breakevenActivated)
		return;

		var entry = _entryPrice.Value;
		var target = _takeProfitPrice.Value;
		var halfDistance = Math.Abs(target - entry) / 2m;

		if (halfDistance <= 0m)
		return;

		if (direction == Sides.Buy)
		{
			if (extremePrice >= entry + halfDistance)
			{
				_stopPrice = Math.Max(_stopPrice ?? entry, entry);
				_breakevenActivated = true;
				LogInfo("Breakeven enabled for long position.");
			}
		}
		else
		{
			if (extremePrice <= entry - halfDistance)
			{
				_stopPrice = Math.Min(_stopPrice ?? entry, entry);
				_breakevenActivated = true;
				LogInfo("Breakeven enabled for short position.");
			}
		}
	}

	private void HandleStop(Sides direction, decimal exitPrice, ICandleMessage candle)
	{
		if (_entryPrice is null)
		{
			ResetTradeState();
			return;
		}

		var entry = _entryPrice.Value;
		var lossDistance = Math.Abs(entry - exitPrice);

		if (lossDistance <= 0m)
		{
			ResetTradeState();
			return;
		}

		var nextDistance = MartingaleMultiplier > 0m ? lossDistance / MartingaleMultiplier : lossDistance;
		_lastOrderVolume = _lastOrderVolume * MartingaleMultiplier;

		_reverseDirection = direction == Sides.Buy ? Sides.Sell : Sides.Buy;
		_reverseDistance = nextDistance;
		_reversePending = true;

		LogInfo($"Stop-loss hit at {exitPrice:F5}. Preparing {_reverseDirection} martingale trade with volume {_lastOrderVolume} and distance {_reverseDistance:F5}.");

		ResetTradeState();

		TryOpenReverseTrade(candle);
	}

	private void HandleTakeProfit(DateTime sessionDate, decimal exitPrice, Sides direction)
	{
		LogInfo($"Take-profit hit at {exitPrice:F5} for {direction} position. Trading blocked for the rest of the day.");

		_blockedTradeDate = sessionDate;
		_lastOrderVolume = OrderVolume;
		ResetTradeState();
		_reversePending = false;
		_reverseDirection = null;
		_reverseDistance = 0m;
	}

	private bool TryOpenReverseTrade(ICandleMessage candle)
	{
		if (!_reversePending || !_reverseDirection.HasValue || _reverseDistance <= 0m)
		return false;

		if (!IsFormedAndOnlineAndAllowTrading())
		return false;

		var direction = _reverseDirection.Value;
		var volume = _lastOrderVolume;

		if (volume <= 0m)
		{
			_reversePending = false;
			_reverseDirection = null;
			return false;
		}

		decimal actualVolume;
		if (direction == Sides.Buy)
		{
			actualVolume = volume + Math.Max(0m, -Position);
			BuyMarket(actualVolume);
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - _reverseDistance;
			_takeProfitPrice = _entryPrice + _reverseDistance;
		}
		else
		{
			actualVolume = volume + Math.Max(0m, Position);
			SellMarket(actualVolume);
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + _reverseDistance;
			_takeProfitPrice = _entryPrice - _reverseDistance;
		}

		_breakevenActivated = false;
		_reversePending = false;

		LogInfo($"Opened {direction} martingale trade at {_entryPrice:F5} with stop {_stopPrice:F5} and target {_takeProfitPrice:F5}.");
		_reverseDirection = null;

		return true;
	}

	private void TryBreakoutEntry(ICandleMessage candle)
	{
		if (_rangeHigh is null || _rangeLow is null)
		return;

		var high = _rangeHigh.Value;
		var low = _rangeLow.Value;
		var rangeWidth = high - low;

		if (rangeWidth <= 0m)
		return;

		var close = candle.ClosePrice;

		if (close > high && Position <= 0)
		{
			var volume = OrderVolume + Math.Max(0m, -Position);
			if (volume <= 0m)
			return;

			BuyMarket(volume);
			_entryPrice = close;
			_stopPrice = low;
			_takeProfitPrice = close + rangeWidth;
			_breakevenActivated = false;
			_lastOrderVolume = OrderVolume;
			LogInfo($"Breakout long entry at {close:F5} above range high {high:F5}.");
		}
		else if (close < low && Position >= 0)
		{
			var volume = OrderVolume + Math.Max(0m, Position);
			if (volume <= 0m)
			return;

			SellMarket(volume);
			_entryPrice = close;
			_stopPrice = high;
			_takeProfitPrice = close - rangeWidth;
			_breakevenActivated = false;
			_lastOrderVolume = OrderVolume;
			LogInfo($"Breakout short entry at {close:F5} below range low {low:F5}.");
		}
	}

	private void ResetTradeState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakevenActivated = false;
	}
}
