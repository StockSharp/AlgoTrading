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
/// Port of the SailSystemEA hedging scalper. The strategy keeps a virtual long/short pair
/// and manages orders based on spread, time windows and configurable virtual protection levels.
/// </summary>
public class SailSystemEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _useVirtualLevels;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _putTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _modifyDelayTicks;
	private readonly StrategyParam<decimal> _stepModifyPips;
	private readonly StrategyParam<decimal> _replaceDistancePips;
	private readonly StrategyParam<decimal> _safeMultiplier;
	private readonly StrategyParam<bool> _usePendingOrders;
	private readonly StrategyParam<decimal> _pendingDistancePips;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _stopTime;
	private readonly StrategyParam<ExistingOrdersAction> _manageExistingOrders;
	private readonly StrategyParam<bool> _autoLotSize;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _manualLotSize;
	private readonly StrategyParam<decimal> _acceptedStopLevelPips;
	private readonly StrategyParam<int> _slippagePoints;
	private readonly StrategyParam<int> _ordersId;
	private readonly StrategyParam<decimal> _maxSpreadPips;
	private readonly StrategyParam<SpreadUsageMode> _spreadUsageMode;
	private readonly StrategyParam<SpreadAction> _highSpreadAction;
	private readonly StrategyParam<decimal> _increaseMultiplier;
	private readonly StrategyParam<decimal> _commissionPips;
	private readonly StrategyParam<bool> _countAverageSpread;
	private readonly StrategyParam<int> _spreadAveragingPeriod;
	private readonly StrategyParam<bool> _closeOnHighSpread;
	private readonly StrategyParam<decimal> _volumeTolerance;

	private readonly Dictionary<Order, PendingOrderInfo> _pendingOrders = new();

	private PositionState _longPosition;
	private PositionState _shortPosition;

	private decimal _pipValue;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _stepModifyOffset;
	private decimal _replaceDistanceOffset;
	private decimal _pendingDistanceOffset;
	private decimal _commissionOffset;
	private decimal _currentEntryMultiplier = 1m;

	private decimal _averageSpread;
	private int _averageSpreadSamples;

	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _hasBestBid;
	private bool _hasBestAsk;

	private int _longModifyCounter;
	private int _shortModifyCounter;

	private bool _stopLevelTooHigh;

	/// <summary>
	/// Spread usage mode taken from the original MQL enumeration.
	/// </summary>
	public enum SpreadUsageMode
	{
		UseAverage,
		UseCurrent
	}

	/// <summary>
	/// Action to take when spread is above the configured limit.
	/// </summary>
	public enum SpreadAction
	{
		IncreaseLevels,
		CloseOrders
	}

	/// <summary>
	/// Behaviour applied when trading is paused by the time filter.
	/// </summary>
	public enum ExistingOrdersAction
	{
		KeepAll,
		DeletePending,
		CloseMarket,
		DeletePendingAndCloseMarket
	}

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public SailSystemEaStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetDisplay("Order Volume", "Default trade volume when automatic sizing is disabled", "Risk");

		_useVirtualLevels = Param(nameof(UseVirtualLevels), true)
		.SetDisplay("Use Virtual Levels", "Mimic the virtual stop/target handling from SailSystemEA", "Risk");

		_stopLossPips = Param(nameof(OrdersStopLoss), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Distance in pips for stop-loss placement", "Risk");

		_putTakeProfit = Param(nameof(PutTakeProfit), false)
		.SetDisplay("Use Take-Profit", "Enable the fixed take-profit distance", "Risk");

		_takeProfitPips = Param(nameof(OrdersTakeProfit), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Distance in pips for take-profit when enabled", "Risk");

		_modifyDelayTicks = Param(nameof(DelayModifyOrders), 4)
		.SetGreaterThanZero()
		.SetDisplay("Modify Delay", "Number of quote updates before virtual levels are refreshed", "Execution");

		_stepModifyPips = Param(nameof(StepModifyOrders), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Step Modify (pips)", "Minimal improvement before stops/targets are shifted", "Execution");

		_replaceDistancePips = Param(nameof(PipsReplaceOrders), 0m)
		.SetDisplay("Replace Distance (pips)", "Distance threshold that forces re-entry when orders are far from the market", "Execution");

		_safeMultiplier = Param(nameof(SafeMultiplier), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Safe Multiplier", "Multiplier used when re-arming the hedge after large displacements", "Execution");

		_usePendingOrders = Param(nameof(UsePendingOrders), false)
		.SetDisplay("Use Pending Orders", "Place limit orders instead of immediate market orders", "Execution");

		_pendingDistancePips = Param(nameof(DistancePending), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Pending Distance (pips)", "Initial distance for pending hedge orders", "Execution");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
		.SetDisplay("Use Time Filter", "Limit trading to the selected session", "Session");

		_startTime = Param(nameof(TimeStartTrade), TimeSpan.Zero)
		.SetDisplay("Start Time", "Session start in exchange time", "Session");

		_stopTime = Param(nameof(TimeStopTrade), TimeSpan.Zero)
		.SetDisplay("Stop Time", "Session end in exchange time", "Session");

		_manageExistingOrders = Param(nameof(ManageExistingOrders), ExistingOrdersAction.DeletePendingAndCloseMarket)
		.SetDisplay("Manage Existing", "Action applied to open exposure when the session closes", "Session");

		_autoLotSize = Param(nameof(AutoLotSize), false)
		.SetDisplay("Auto Lot Size", "Size positions based on equity and risk factor", "Risk");

		_riskFactor = Param(nameof(RiskFactor), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Factor", "Percentage of equity allocated when sizing automatically", "Risk");

		_manualLotSize = Param(nameof(ManualLotSize), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Manual Lot", "Fixed order volume when auto sizing is disabled", "Risk");

		_acceptedStopLevelPips = Param(nameof(AcceptStopLevel), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Accept Stop Level", "Minimal broker stop level allowed", "Execution");

		_slippagePoints = Param(nameof(Slippage), 1)
		.SetDisplay("Slippage", "Maximum slippage in price steps for market orders", "Execution");

		_ordersId = Param(nameof(OrdersId), 0)
		.SetDisplay("Order Id", "Magic number equivalent for generated orders", "Execution");

		_maxSpreadPips = Param(nameof(MaxSpread), 1m)
		.SetDisplay("Max Spread (pips)", "Spread ceiling including commissions", "Filters");

		_spreadUsageMode = Param(nameof(TypeOfSpreadUse), SpreadUsageMode.UseAverage)
		.SetDisplay("Spread Mode", "Use average or current spread for filtering", "Filters");

		_highSpreadAction = Param(nameof(HighSpreadAction), SpreadAction.IncreaseLevels)
		.SetDisplay("High Spread Action", "Behaviour when spread exceeds the limit", "Filters");

		_increaseMultiplier = Param(nameof(MultiplierIncrease), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Level Multiplier", "Multiplier applied to entry distances when spread is high", "Filters");

		_commissionPips = Param(nameof(CommissionInPip), 0m)
		.SetDisplay("Commission (pips)", "Commission expressed in pips for spread filtering", "Filters");

		_countAverageSpread = Param(nameof(CountAvgSpread), false)
		.SetDisplay("Average Spread", "Accumulate a rolling spread average", "Filters");

		_spreadAveragingPeriod = Param(nameof(TimesForAverage), 30)
		.SetGreaterThanZero()
		.SetDisplay("Average Period", "Number of samples used for the rolling spread average", "Filters");

		_closeOnHighSpread = Param(nameof(CloseOnHighSpread), false)
		.SetDisplay("Close On High Spread", "Flatten positions when the spread filter is violated", "Filters");

		_volumeTolerance = Param(nameof(VolumeTolerance), 0.0000001m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Volume Tolerance", "Tolerance used when comparing remaining order volumes", "Execution");
	}

	/// <summary>
	/// Default order volume when auto sizing is disabled.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Toggle for the virtual stop/target logic.
	/// </summary>
	public bool UseVirtualLevels
	{
		get => _useVirtualLevels.Value;
		set => _useVirtualLevels.Value = value;
	}

	/// <summary>
	/// Distance in pips used for stop-loss placement.
	/// </summary>
	public decimal OrdersStopLoss
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Enable or disable the take-profit target.
	/// </summary>
	public bool PutTakeProfit
	{
		get => _putTakeProfit.Value;
		set => _putTakeProfit.Value = value;
	}

	/// <summary>
	/// Distance in pips for the take-profit target.
	/// </summary>
	public decimal OrdersTakeProfit
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Number of quote updates required before stops are updated.
	/// </summary>
	public int DelayModifyOrders
	{
		get => _modifyDelayTicks.Value;
		set => _modifyDelayTicks.Value = value;
	}

	/// <summary>
	/// Minimal price improvement before the stop is re-armed.
	/// </summary>
	public decimal StepModifyOrders
	{
		get => _stepModifyPips.Value;
		set => _stepModifyPips.Value = value;
	}

	/// <summary>
	/// Distance threshold that forces new entries when the market drifts away.
	/// </summary>
	public decimal PipsReplaceOrders
	{
		get => _replaceDistancePips.Value;
		set => _replaceDistancePips.Value = value;
	}

	/// <summary>
	/// Pending order distance expressed in pips.
	/// </summary>
	public decimal DistancePending
	{
		get => _pendingDistancePips.Value;
		set => _pendingDistancePips.Value = value;
	}

	/// <summary>
	/// Rolling average window size for the spread filter.
	/// </summary>
	public int TimesForAverage
	{
		get => _spreadAveragingPeriod.Value;
		set => _spreadAveragingPeriod.Value = value;
	}

	public decimal VolumeTolerance
	{
		get => _volumeTolerance.Value;
		set => _volumeTolerance.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pendingOrders.Clear();
		_longPosition = null;
		_shortPosition = null;
		_averageSpread = 0m;
		_averageSpreadSamples = 0;
		_bestBid = 0m;
		_bestAsk = 0m;
		_hasBestBid = false;
		_hasBestAsk = false;
		_longModifyCounter = 0;
		_shortModifyCounter = 0;
		_currentEntryMultiplier = 1m;
		_stopLevelTooHigh = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;
		StartProtection();

		_pipValue = CalculatePipValue();
		RecalculateOffsets();

		ValidateStopLevel();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private void ValidateStopLevel()
	{
		var security = Security;
		if (security == null)
		throw new InvalidOperationException("Security is not assigned.");

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		throw new InvalidOperationException("Price step is not specified for the security.");

		var minimalDistance = security.StopPriceStep ?? priceStep;
		var accepted = _acceptedStopLevelPips.Value * _pipValue;

		if (accepted < minimalDistance)
		{
			_stopLevelTooHigh = true;
			this.LogWarning("Stop level {0} is below broker minimum {1}. Strategy will stay idle.", accepted, minimalDistance);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (_stopLevelTooHigh)
		{
			return;
		}

		UpdateBestQuotes(message);

		if (!_hasBestBid || !_hasBestAsk)
		return;

		RecalculateOffsets();

		var isTradingTime = CheckTradingWindow(message.ServerTime.UtcDateTime.TimeOfDay);
		var spread = _bestAsk - _bestBid;

		UpdateSpreadAverage(spread);

		var spreadToUse = _spreadUsageMode.Value == SpreadUsageMode.UseAverage && _countAverageSpread.Value
		? _averageSpread
		: spread;

		var commission = _commissionOffset;
		var spreadLimit = _maxSpreadPips.Value <= 0m
		? decimal.MaxValue
		: _maxSpreadPips.Value * _pipValue;

		var spreadAllowed = spreadToUse + commission <= spreadLimit;

		if (!spreadAllowed)
		{
			HandleHighSpread();

			if (_closeOnHighSpread.Value)
			FlattenPositions(CloseReason.SpreadLimit);

			return;
		}

		_currentEntryMultiplier = 1m;

		if (!isTradingTime)
		{
			HandleOutOfSession();
			return;
		}

		ProcessActivePositions();
		EnsureEntryOrders();
		UpdateReplaceLogic();
	}

	private void ProcessActivePositions()
	{
		if (_longPosition is { IsActive: true })
		{
			ManageLong(_longPosition);
		}

		if (_shortPosition is { IsActive: true })
		{
			ManageShort(_shortPosition);
		}
	}

	private void EnsureEntryOrders()
	{
		if (!_usePendingOrders.Value)
		{
			TryOpenMarketPair();
			return;
		}

		TryPlacePending(Sides.Buy);
		TryPlacePending(Sides.Sell);
	}

	private void TryOpenMarketPair()
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		return;

		if (!(_longPosition?.IsActive ?? false) && !HasPendingEntry(Sides.Buy))
		{
			var longPosition = new PositionState { Side = Sides.Buy };
			var order = CreateMarketOrder(Sides.Buy, volume, "SailSystemEA:LongEntry");
			RegisterEntryOrder(order, longPosition);
		}

		if (!(_shortPosition?.IsActive ?? false) && !HasPendingEntry(Sides.Sell))
		{
			var shortPosition = new PositionState { Side = Sides.Sell };
			var order = CreateMarketOrder(Sides.Sell, volume, "SailSystemEA:ShortEntry");
			RegisterEntryOrder(order, shortPosition);
		}
	}

	private void TryPlacePending(Sides side)
	{
		if (!_hasBestBid || !_hasBestAsk)
		return;

		if (HasPendingEntry(side))
		return;

		var position = side == Sides.Buy ? _longPosition : _shortPosition;
		if (position is { IsActive: true })
		return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		return;

		var order = new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Volume = volume,
			Side = side,
			Type = OrderTypes.Limit,
			Price = CalculatePendingPrice(side),
			Comment = BuildComment(side == Sides.Buy ? "SailSystemEA:LongPending" : "SailSystemEA:ShortPending"),
			Slippage = _slippagePoints.Value
		};

		var state = new PositionState { Side = side };
		RegisterEntryOrder(order, state);
	}

	private decimal CalculatePendingPrice(Sides side)
	{
		var distance = _pendingDistanceOffset * _currentEntryMultiplier;
		if (distance <= 0m)
		distance = _pipValue;

		return side == Sides.Buy
		? Math.Max(_bestBid - distance, 0m)
		: _bestAsk + distance;
	}

	private bool HasPendingEntry(Sides side)
	{
		return _pendingOrders.Any(pair => pair.Value.IsEntry && pair.Value.Position.Side == side);
	}

	private void ManageLong(PositionState position)
	{
		var price = _bestBid;
		if (price <= 0m)
		return;

		_longModifyCounter++;

		if (UseVirtualLevels)
		{
			UpdateVirtualLevels(position, price, ref _longModifyCounter);
		}

		if (position.StopPrice is decimal stop && price <= stop)
		{
			ClosePosition(position, CloseReason.StopLoss);
			return;
		}

		if (position.TakePrice is decimal take && price >= take)
		{
			ClosePosition(position, CloseReason.TakeProfit);
		}
	}

	private void ManageShort(PositionState position)
	{
		var price = _bestAsk;
		if (price <= 0m)
		return;

		_shortModifyCounter++;

		if (UseVirtualLevels)
		{
			UpdateVirtualLevels(position, price, ref _shortModifyCounter);
		}

		if (position.StopPrice is decimal stop && price >= stop)
		{
			ClosePosition(position, CloseReason.StopLoss);
			return;
		}

		if (position.TakePrice is decimal take && price <= take)
		{
			ClosePosition(position, CloseReason.TakeProfit);
		}
	}

	private void UpdateVirtualLevels(PositionState position, decimal price, ref int counter)
	{
		if (counter < _modifyDelayTicks.Value)
		return;

		counter = 0;

		if (position.Side == Sides.Buy)
		{
			var newStop = price - _stopLossOffset;
			var currentStop = position.StopPrice ?? (position.EntryPrice - _stopLossOffset);
			if (newStop - currentStop >= _stepModifyOffset)
			{
				position.StopPrice = newStop;
			}

			if (position.TakePrice is decimal take && PutTakeProfit)
			{
				var newTake = price + _takeProfitOffset;
				if (newTake - take >= _stepModifyOffset)
				position.TakePrice = newTake;
			}
		}
		else
		{
			var newStop = price + _stopLossOffset;
			var currentStop = position.StopPrice ?? (position.EntryPrice + _stopLossOffset);
			if (currentStop - newStop >= _stepModifyOffset)
			{
				position.StopPrice = newStop;
			}

			if (position.TakePrice is decimal take && PutTakeProfit)
			{
				var newTake = price - _takeProfitOffset;
				if (take - newTake >= _stepModifyOffset)
				position.TakePrice = newTake;
			}
		}
	}

	private void HandleHighSpread()
	{
		switch (_highSpreadAction.Value)
		{
		case SpreadAction.IncreaseLevels:
			_currentEntryMultiplier = _increaseMultiplier.Value > 1m ? _increaseMultiplier.Value : 1m;
			break;

		case SpreadAction.CloseOrders:
			CancelPendingEntries();
			break;
		}
	}

	private void HandleOutOfSession()
	{
		switch (_manageExistingOrders.Value)
		{
		case ExistingOrdersAction.KeepAll:
			return;

		case ExistingOrdersAction.DeletePending:
			CancelPendingEntries();
			break;

		case ExistingOrdersAction.CloseMarket:
			FlattenPositions(CloseReason.SessionEnd);
			break;

		case ExistingOrdersAction.DeletePendingAndCloseMarket:
			CancelPendingEntries();
			FlattenPositions(CloseReason.SessionEnd);
			break;
		}
	}

	private void CancelPendingEntries()
	{
		foreach (var order in _pendingOrders.Where(p => p.Value.IsEntry).Select(p => p.Key).ToList())
		{
			CancelOrder(order);
			_pendingOrders.Remove(order);
		}
	}

	private void FlattenPositions(CloseReason reason)
	{
		if (_longPosition is { IsActive: true })
		{
			ClosePosition(_longPosition, reason);
		}

		if (_shortPosition is { IsActive: true })
		{
			ClosePosition(_shortPosition, reason);
		}
	}

	private void ClosePosition(PositionState position, CloseReason reason)
	{
		if (position.IsClosing)
		return;

		var volume = NormalizeVolume(position.Volume);
		if (volume <= 0m)
		{
			ReleasePosition(position);
			return;
		}

		var exitSide = position.Side == Sides.Buy ? Sides.Sell : Sides.Buy;
		var comment = reason switch
		{
			CloseReason.StopLoss => "SailSystemEA:StopLoss",
			CloseReason.TakeProfit => "SailSystemEA:TakeProfit",
			CloseReason.SessionEnd => "SailSystemEA:SessionExit",
			CloseReason.SpreadLimit => "SailSystemEA:SpreadExit",
			_ => "SailSystemEA:Exit"
		};

		var order = CreateMarketOrder(exitSide, volume, comment);
		position.IsClosing = true;
		RegisterExitOrder(order, position, reason);
	}

	private void RegisterEntryOrder(Order order, PositionState position)
	{
		_pendingOrders[order] = new PendingOrderInfo
		{
			Position = position,
			IsEntry = true,
			RemainingVolume = order.Volume,
			CloseReason = CloseReason.None
		};

		RegisterOrder(order);
	}

	private void RegisterExitOrder(Order order, PositionState position, CloseReason reason)
	{
		_pendingOrders[order] = new PendingOrderInfo
		{
			Position = position,
			IsEntry = false,
			RemainingVolume = order.Volume,
			CloseReason = reason
		};

		RegisterOrder(order);
	}

	private Order CreateMarketOrder(Sides side, decimal volume, string comment)
	{
		return new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Volume = volume,
			Side = side,
			Type = OrderTypes.Market,
			Comment = BuildComment(comment),
			Slippage = _slippagePoints.Value
		};
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
		return;

		if (!_pendingOrders.TryGetValue(trade.Order, out var info))
		return;

		var tradeVolume = trade.Trade.Volume;
		var tradePrice = trade.Trade.Price;

		info.RemainingVolume -= tradeVolume;
		info.FilledVolume += tradeVolume;
		info.WeightedPrice += tradePrice * tradeVolume;

		if (info.RemainingVolume > VolumeTolerance)
		return;

		_pendingOrders.Remove(trade.Order);

		if (info.FilledVolume <= 0m)
		return;

		var averagePrice = info.WeightedPrice / info.FilledVolume;
		var position = info.Position;

		if (info.IsEntry)
		{
			position.Volume = info.FilledVolume;
			position.EntryPrice = averagePrice;
			position.IsActive = true;
			position.IsClosing = false;
			position.HighestPrice = averagePrice;
			position.LowestPrice = averagePrice;
			position.StopPrice = averagePrice + (position.Side == Sides.Sell ? _stopLossOffset : -_stopLossOffset);
			position.TakePrice = PutTakeProfit
			? averagePrice + (position.Side == Sides.Buy ? _takeProfitOffset : -_takeProfitOffset)
			: null;

			if (position.Side == Sides.Buy)
			{
				_longPosition = position;
				_longModifyCounter = 0;
			}
			else
			{
				_shortPosition = position;
				_shortModifyCounter = 0;
			}

			this.LogInfo("{0} entry filled at {1} for {2}", position.Side, averagePrice, info.FilledVolume);

			CancelOppositePending(position.Side);
		}
		else
		{
			this.LogInfo("{0} exit filled at {1} for {2} ({3})", position.Side, averagePrice, info.FilledVolume, info.CloseReason);
			ReleasePosition(position);
		}
	}

	private void CancelOppositePending(Sides side)
	{
		var opposite = side == Sides.Buy ? Sides.Sell : Sides.Buy;
		foreach (var order in _pendingOrders.Where(p => p.Value.IsEntry && p.Value.Position.Side == opposite).Select(p => p.Key).ToList())
		{
			CancelOrder(order);
			_pendingOrders.Remove(order);
		}
	}

	private void ReleasePosition(PositionState position)
	{
		position.IsActive = false;
		position.IsClosing = false;
		position.Volume = 0m;
		position.StopPrice = null;
		position.TakePrice = null;

		if (position.Side == Sides.Buy)
		{
			_longPosition = null;
		}
		else
		{
			_shortPosition = null;
		}
	}

	private void UpdateBestQuotes(Level1ChangeMessage message)
	{
		if (message.BestBidPrice is decimal bid)
		{
			_bestBid = bid;
			_hasBestBid = true;
		}

		if (message.BestAskPrice is decimal ask)
		{
			_bestAsk = ask;
			_hasBestAsk = true;
		}
	}

	private void UpdateSpreadAverage(decimal spread)
	{
		if (!_countAverageSpread.Value)
		return;

		_averageSpreadSamples++;

		var length = Math.Min(Math.Max(1, _spreadAveragingPeriod.Value), 100);
		if (_averageSpreadSamples <= length)
		{
			_averageSpread += (spread - _averageSpread) / _averageSpreadSamples;
		}
		else
		{
			_averageSpread += (spread - _averageSpread) / length;
		}
	}

	private bool CheckTradingWindow(TimeSpan currentTime)
	{
		if (!_useTimeFilter.Value)
		return true;

		var start = _startTime.Value;
		var stop = _stopTime.Value;

		if (start == stop)
		return true;

		return start <= stop
		? currentTime >= start && currentTime < stop
		: currentTime >= start || currentTime < stop;
	}

	private decimal CalculateOrderVolume()
	{
		if (!_autoLotSize.Value)
		return NormalizeVolume(_manualLotSize.Value);

		var portfolio = Portfolio;
		var security = Security;
		if (portfolio == null || security == null)
		return NormalizeVolume(_manualLotSize.Value);

		var equity = portfolio.CurrentValue;
		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		return NormalizeVolume(_manualLotSize.Value);

		var volumeStep = security.VolumeStep ?? 1m;
		var valuePerLot = step * volumeStep;
		if (valuePerLot <= 0m)
		return NormalizeVolume(_manualLotSize.Value);

		var riskAmount = equity * (_riskFactor.Value / 100m);
		var lots = riskAmount / valuePerLot;
		return NormalizeVolume(lots);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security ?? throw new InvalidOperationException("Security is not set.");

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Round(volume / step);
			volume = steps * step;
		}

		var maxVolume = security.MaxVolume;
		if (maxVolume is decimal max && max > 0m && volume > max)
		volume = max;

		return volume;
	}

	private decimal CalculatePipValue()
	{
		var security = Security ?? throw new InvalidOperationException("Security is not set.");
		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		throw new InvalidOperationException("Price step is not specified for the security.");

		var decimals = security.Decimals;
		var multiplier = decimals == 3 || decimals == 5 ? 10m : 1m;
		return step * multiplier;
	}

	private string BuildComment(string baseComment)
	{
		var id = _ordersId.Value;
		return id > 0 ? $"{baseComment}#{id}" : baseComment;
	}

	private void RecalculateOffsets()
	{
		_stopLossOffset = OrdersStopLoss * _pipValue;
		_takeProfitOffset = OrdersTakeProfit * _pipValue;
		_stepModifyOffset = StepModifyOrders * _pipValue;
		_replaceDistanceOffset = PipsReplaceOrders * _pipValue;
		_pendingDistanceOffset = DistancePending * _pipValue;
		_commissionOffset = _commissionPips.Value * _pipValue;
	}

	private void UpdateReplaceLogic()
	{
		if (_replaceDistanceOffset <= 0m)
		return;

		var longPending = _pendingOrders.Where(p => p.Value.IsEntry && p.Value.Position.Side == Sides.Buy).Select(p => p.Key).FirstOrDefault();
		if (longPending != null && Math.Abs(longPending.Price - _bestBid) > _replaceDistanceOffset * _safeMultiplier.Value)
		{
			CancelOrder(longPending);
			_pendingOrders.Remove(longPending);
		}

		var shortPending = _pendingOrders.Where(p => p.Value.IsEntry && p.Value.Position.Side == Sides.Sell).Select(p => p.Key).FirstOrDefault();
		if (shortPending != null && Math.Abs(shortPending.Price - _bestAsk) > _replaceDistanceOffset * _safeMultiplier.Value)
		{
			CancelOrder(shortPending);
			_pendingOrders.Remove(shortPending);
		}
	}

	private void RecalculateOffsetsAndReplace()
	{
		RecalculateOffsets();
		UpdateReplaceLogic();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.State.IsFinished())
		{
			RecalculateOffsetsAndReplace();
		}
	}

	private sealed class PositionState
	{
		public required Sides Side { get; init; }
		public decimal Volume { get; set; }
		public decimal EntryPrice { get; set; }
		public decimal? StopPrice { get; set; }
		public decimal? TakePrice { get; set; }
		public decimal HighestPrice { get; set; }
		public decimal LowestPrice { get; set; }
		public bool IsActive { get; set; }
		public bool IsClosing { get; set; }
	}

	private sealed class PendingOrderInfo
	{
		public required PositionState Position { get; init; }
		public bool IsEntry { get; init; }
		public decimal RemainingVolume { get; set; }
		public decimal FilledVolume { get; set; }
		public decimal WeightedPrice { get; set; }
		public CloseReason CloseReason { get; init; }
	}

	private enum CloseReason
	{
		None,
		StopLoss,
		TakeProfit,
		SessionEnd,
		SpreadLimit
	}
}

