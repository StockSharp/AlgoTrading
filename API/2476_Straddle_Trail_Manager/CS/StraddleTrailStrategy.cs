using System;
using System.Globalization;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that places a straddle of stop orders ahead of a scheduled event and trails the triggered position.
/// </summary>
public class StraddleTrailStrategy : Strategy
{
	private readonly StrategyParam<bool> _shutdownNow;
	private readonly StrategyParam<ShutdownOption> _shutdownMode;
	private readonly StrategyParam<decimal> _distanceFromPrice;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailPips;
	private readonly StrategyParam<bool> _trailAfterBreakeven;
	private readonly StrategyParam<decimal> _breakevenLockPips;
	private readonly StrategyParam<decimal> _breakevenTriggerPips;
	private readonly StrategyParam<int> _eventHour;
	private readonly StrategyParam<int> _eventMinute;
	private readonly StrategyParam<int> _preEventEntryMinutes;
	private readonly StrategyParam<int> _stopAdjustMinutes;
	private readonly StrategyParam<bool> _removeOppositeOrder;
	private readonly StrategyParam<bool> _adjustPendingOrders;
	private readonly StrategyParam<bool> _placeStraddleImmediately;
	private readonly StrategyParam<DataType> _candleType;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;

	private decimal? _bestBid;
	private decimal? _bestAsk;

	private decimal _pipSize;
	private DateTime? _lastAdjustmentMinute;
	private DateTime? _lastPlacementDate;

	private decimal? _stopLevel;
	private decimal? _takeLevel;
	private bool _breakevenActivated;
	private bool? _isLongPosition;

	public bool ShutdownNow { get => _shutdownNow.Value; set => _shutdownNow.Value = value; }
	public ShutdownOption ShutdownMode { get => _shutdownMode.Value; set => _shutdownMode.Value = value; }
	public decimal DistanceFromPrice { get => _distanceFromPrice.Value; set => _distanceFromPrice.Value = value; }
	public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public decimal TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public decimal TrailPips { get => _trailPips.Value; set => _trailPips.Value = value; }
	public bool TrailAfterBreakeven { get => _trailAfterBreakeven.Value; set => _trailAfterBreakeven.Value = value; }
	public decimal BreakevenLockPips { get => _breakevenLockPips.Value; set => _breakevenLockPips.Value = value; }
	public decimal BreakevenTriggerPips { get => _breakevenTriggerPips.Value; set => _breakevenTriggerPips.Value = value; }
	public int EventHour { get => _eventHour.Value; set => _eventHour.Value = value; }
	public int EventMinute { get => _eventMinute.Value; set => _eventMinute.Value = value; }
	public int PreEventEntryMinutes { get => _preEventEntryMinutes.Value; set => _preEventEntryMinutes.Value = value; }
	public int StopAdjustMinutes { get => _stopAdjustMinutes.Value; set => _stopAdjustMinutes.Value = value; }
	public bool RemoveOppositeOrder { get => _removeOppositeOrder.Value; set => _removeOppositeOrder.Value = value; }
	public bool AdjustPendingOrders { get => _adjustPendingOrders.Value; set => _adjustPendingOrders.Value = value; }
	public bool PlaceStraddleImmediately { get => _placeStraddleImmediately.Value; set => _placeStraddleImmediately.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StraddleTrailStrategy()
	{
		Volume = 1m;

		_shutdownNow = Param(nameof(ShutdownNow), false)
		.SetDisplay("Shutdown", "Force close/cancel", "Risk");

		_shutdownMode = Param(nameof(ShutdownMode), ShutdownOption.All)
		.SetDisplay("Shutdown Mode", "What to cancel", "Risk");

		_distanceFromPrice = Param(nameof(DistanceFromPrice), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Distance (pips)", "Pending order offset", "Orders");

		_stopLossPips = Param(nameof(StopLossPips), 30m)
		.SetDisplay("Stop Loss (pips)", "Initial stop distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 60m)
		.SetDisplay("Take Profit (pips)", "Initial target distance", "Risk");

		_trailPips = Param(nameof(TrailPips), 15m)
		.SetDisplay("Trail (pips)", "Trailing stop distance", "Risk");

		_trailAfterBreakeven = Param(nameof(TrailAfterBreakeven), true)
		.SetDisplay("Trail After BE", "Start trailing only after breakeven", "Risk");

		_breakevenLockPips = Param(nameof(BreakevenLockPips), 1m)
		.SetDisplay("BE Lock (pips)", "Locked profit after breakeven", "Risk");

		_breakevenTriggerPips = Param(nameof(BreakevenTriggerPips), 5m)
		.SetDisplay("BE Trigger (pips)", "Move stop to BE after this profit", "Risk");

		_eventHour = Param(nameof(EventHour), 12)
		.SetDisplay("Event Hour", "Scheduled event hour", "Schedule");

		_eventMinute = Param(nameof(EventMinute), 30)
		.SetDisplay("Event Minute", "Scheduled event minute", "Schedule");

		_preEventEntryMinutes = Param(nameof(PreEventEntryMinutes), 30)
		.SetDisplay("Entry Minutes", "Minutes before event to place straddle", "Schedule");

		_stopAdjustMinutes = Param(nameof(StopAdjustMinutes), 2)
		.SetDisplay("Adjust Stop Minutes", "Minutes before event to stop adjusting", "Schedule");

		_removeOppositeOrder = Param(nameof(RemoveOppositeOrder), true)
		.SetDisplay("Remove Opposite", "Cancel opposite pending order after fill", "Orders");

		_adjustPendingOrders = Param(nameof(AdjustPendingOrders), false)
		.SetDisplay("Adjust Pending", "Recenter pending orders before event", "Orders");

		_placeStraddleImmediately = Param(nameof(PlaceStraddleImmediately), false)
		.SetDisplay("Immediate Straddle", "Place straddle without waiting for schedule", "Orders");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle subscription used for timing", "General");
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

		_buyStopOrder = null;
		_sellStopOrder = null;
		_bestBid = null;
		_bestAsk = null;
		_pipSize = 0m;
		_lastAdjustmentMinute = null;
		_lastPlacementDate = null;
		_stopLevel = null;
		_takeLevel = null;
		_breakevenActivated = false;
		_isLongPosition = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		Volume = Math.Max(Volume, 1m);

		var candleSub = SubscribeCandles(CandleType);
		candleSub.Bind(ProcessCandle).Start();

		SubscribeOrderBook()
		.Bind(depth =>
		{
			_bestBid = depth.GetBestBid()?.Price ?? _bestBid;
			_bestAsk = depth.GetBestAsk()?.Price ?? _bestAsk;
		})
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Volume <= 0)
		return;

		Volume = Math.Max(Volume, 1m);

		_bestBid ??= candle.ClosePrice;
		_bestAsk ??= candle.ClosePrice;

		UpdateOrderReferences();

		var now = candle.CloseTime;
		var minuteStamp = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Offset);

		if (ShutdownNow)
		{
			if (PerformShutdown())
			{
				ShutdownNow = false;
				return;
			}
		}

		if (PlaceStraddleImmediately)
		{
			TryPlaceStraddle(now);
		}
		else
		{
			HandleScheduledStraddle(now);
		}

		if (AdjustPendingOrders && !PlaceStraddleImmediately)
		{
			AdjustStraddleIfNeeded(now, minuteStamp);
		}

		HandleTrailing(now);
	}

	private void HandleScheduledStraddle(DateTimeOffset now)
	{
		if (!IsEventEnabled())
		return;

		var eventTime = new TimeSpan(EventHour, EventMinute, 0);
		var startWindow = eventTime - TimeSpan.FromMinutes(Math.Max(0, PreEventEntryMinutes));

		if (startWindow < TimeSpan.Zero)
		startWindow = TimeSpan.Zero;

		var current = now.TimeOfDay;

		if (current < startWindow || current > eventTime)
		{
			if (current > eventTime)
			{
				_lastPlacementDate = null;
			}

			return;
		}

		TryPlaceStraddle(now);
	}

	private void TryPlaceStraddle(DateTimeOffset time)
	{
		if (Position != 0)
		return;

		if (HasActiveStraddle())
		return;

		if (_lastPlacementDate == time.Date)
		return;

		if (_bestBid is null || _bestAsk is null)
		return;

		var distance = DistanceFromPrice * _pipSize;
		if (distance <= 0)
		return;

		var ask = _bestAsk.Value;
		var bid = _bestBid.Value;

		var buyPrice = AlignPrice(ask + distance, true);
		var sellPrice = AlignPrice(bid - distance, false);

		_buyStopOrder = BuyStop(Volume, buyPrice);
		_sellStopOrder = SellStop(Volume, sellPrice);

		_lastPlacementDate = time.Date;
	}

	private void AdjustStraddleIfNeeded(DateTimeOffset now, DateTime minuteStamp)
	{
		if (!HasActiveStraddle())
		return;

		if (ShouldStopAdjusting(now))
		return;

		if (_lastAdjustmentMinute == minuteStamp)
		return;

		_lastAdjustmentMinute = minuteStamp;

		CancelActiveStraddle();
		_lastPlacementDate = null;
		TryPlaceStraddle(now);
	}

	private bool ShouldStopAdjusting(DateTimeOffset time)
	{
		if (!IsEventEnabled())
		return true;

		var eventTime = new TimeSpan(EventHour, EventMinute, 0);
		var stopMinutes = Math.Max(1, StopAdjustMinutes);
		var stopWindowStart = eventTime - TimeSpan.FromMinutes(stopMinutes);
		if (stopWindowStart < TimeSpan.Zero)
		stopWindowStart = TimeSpan.Zero;

		var current = time.TimeOfDay;
		return current >= stopWindowStart && current <= eventTime;
	}

	private void HandleTrailing(DateTimeOffset time)
	{
		if (Position == 0)
		{
			_stopLevel = null;
			_takeLevel = null;
			_breakevenActivated = false;
			_isLongPosition = null;
			return;
		}

		var price = Position > 0 ? _bestBid ?? Security.LastPrice : _bestAsk ?? Security.LastPrice;
		if (price == null)
		return;

		var entry = PositionPrice;

		if (_isLongPosition is null || (_isLongPosition == true && Position < 0) || (_isLongPosition == false && Position > 0))
		{
			InitializeProtection();
		}

		var isLong = Position > 0;
		_isLongPosition = isLong;

		if (!entry.HasValue)
		return;

		if (_takeLevel.HasValue)
		{
			if (isLong && price.Value >= _takeLevel)
			{
				ClosePosition();
				return;
			}

			if (!isLong && price.Value <= _takeLevel)
			{
				ClosePosition();
				return;
			}
		}

		if (BreakevenTriggerPips > 0)
		{
			var trigger = BreakevenTriggerPips * _pipSize;
			var lockOffset = BreakevenLockPips * _pipSize;

			if (isLong)
			{
				if (!_breakevenActivated && price.Value >= entry.Value + trigger)
				{
					_stopLevel = entry.Value + lockOffset;
					_breakevenActivated = true;
				}
			}
			else
			{
				if (!_breakevenActivated && price.Value <= entry.Value - trigger)
				{
					_stopLevel = entry.Value - lockOffset;
					_breakevenActivated = true;
				}
			}
		}

		if (TrailPips > 0)
		{
			var trailOffset = TrailPips * _pipSize;

			if (!TrailAfterBreakeven || _breakevenActivated || BreakevenTriggerPips <= 0)
			{
				if (isLong)
				{
					var newStop = price.Value - trailOffset;
					if (!_stopLevel.HasValue || newStop > _stopLevel)
					_stopLevel = newStop;
				}
				else
				{
					var newStop = price.Value + trailOffset;
					if (!_stopLevel.HasValue || newStop < _stopLevel)
					_stopLevel = newStop;
				}
			}
			else
			{
				var trigger = BreakevenTriggerPips * _pipSize;

				if (isLong && price.Value >= entry.Value + trigger)
				{
					var newStop = price.Value - trailOffset;
					if (!_stopLevel.HasValue || newStop > _stopLevel)
					_stopLevel = newStop;
				}
				else if (!isLong && price.Value <= entry.Value - trigger)
				{
					var newStop = price.Value + trailOffset;
					if (!_stopLevel.HasValue || newStop < _stopLevel)
					_stopLevel = newStop;
				}
			}
		}

		if (_stopLevel.HasValue)
		{
			if (isLong && price.Value <= _stopLevel)
			{
				ClosePosition();
			}
			else if (!isLong && price.Value >= _stopLevel)
			{
				ClosePosition();
			}
		}
	}

	private void InitializeProtection()
	{
		if (Position == 0)
		return;

		_stopLevel = null;
		_takeLevel = null;
		_breakevenActivated = false;

		var entryPrice = PositionPrice;
		if (!entryPrice.HasValue)
		return;

		if (StopLossPips > 0)
		{
			var slOffset = StopLossPips * _pipSize;
			_stopLevel = Position > 0 ? entryPrice.Value - slOffset : entryPrice.Value + slOffset;
		}

		if (TakeProfitPips > 0)
		{
			var tpOffset = TakeProfitPips * _pipSize;
			_takeLevel = Position > 0 ? entryPrice.Value + tpOffset : entryPrice.Value - tpOffset;
		}
	}

	private void CancelActiveStraddle()
	{
		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
		CancelOrder(_buyStopOrder);
		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
		CancelOrder(_sellStopOrder);

		_buyStopOrder = null;
		_sellStopOrder = null;
	}

	private bool HasActiveStraddle()
	{
		return (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active) ||
		(_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active);
	}

	private bool PerformShutdown()
	{
		var hasActivity = false;

		if ((ShutdownMode == ShutdownOption.All || ShutdownMode == ShutdownOption.LongPositions) && Position > 0)
		{
			ClosePosition();
			hasActivity = true;
		}

		if ((ShutdownMode == ShutdownOption.All || ShutdownMode == ShutdownOption.ShortPositions) && Position < 0)
		{
			ClosePosition();
			hasActivity = true;
		}

		if (ShutdownMode == ShutdownOption.All || ShutdownMode == ShutdownOption.PendingLong)
		{
			if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
			{
				CancelOrder(_buyStopOrder);
				hasActivity = true;
			}
		}

		if (ShutdownMode == ShutdownOption.All || ShutdownMode == ShutdownOption.PendingShort)
		{
			if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
			{
				CancelOrder(_sellStopOrder);
				hasActivity = true;
			}
		}

		return hasActivity;
	}

	private bool IsEventEnabled()
	{
		return EventHour > 0 && EventMinute > 0;
	}

	private void UpdateOrderReferences()
	{
		if (_buyStopOrder != null && _buyStopOrder.State.IsFinal())
		_buyStopOrder = null;

		if (_sellStopOrder != null && _sellStopOrder.State.IsFinal())
		_sellStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			CancelOppositePending();
			_lastPlacementDate = null;
			_stopLevel = null;
			_takeLevel = null;
			_breakevenActivated = false;
			_isLongPosition = null;
			return;
		}

		InitializeProtection();

		if (RemoveOppositeOrder)
		{
			CancelOppositePending();
		}
	}

	private void CancelOppositePending()
	{
		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active && Position < 0)
		{
			CancelOrder(_buyStopOrder);
			_buyStopOrder = null;
		}

		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active && Position > 0)
		{
			CancelOrder(_sellStopOrder);
			_sellStopOrder = null;
		}

		if (Position == 0)
		{
			_buyStopOrder = null;
			_sellStopOrder = null;
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;

		var decimals = CountDecimals(step);
		var multiplier = decimals is 3 or 5 ? 10m : 1m;

		return step * multiplier;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);
		var text = value.ToString(CultureInfo.InvariantCulture);
		var index = text.IndexOf('.');
		return index >= 0 ? text.Length - index - 1 : 0;
	}

	private decimal AlignPrice(decimal price, bool roundUp)
	{
		var step = Security?.PriceStep ?? 0.0001m;
		if (step <= 0)
		{
			return price;
		}

		var ratio = price / step;
		var rounded = roundUp ? Math.Ceiling(ratio) : Math.Floor(ratio);
		return rounded * step;
	}

	public enum ShutdownOption
	{
		All,
		LongPositions,
		ShortPositions,
		PendingLong,
		PendingShort
	}
}

internal static class OrderStateExtensions
{
	public static bool IsFinal(this OrderStates state)
	{
		return state switch
		{
			OrderStates.Done => true,
			OrderStates.Failed => true,
			OrderStates.Canceled => true,
			_ => false
		};
	}
}
