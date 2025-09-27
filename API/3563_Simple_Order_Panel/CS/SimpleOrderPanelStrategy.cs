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
/// Port of the MetaTrader panel "SimpleOrderPanel" that offers manual execution controls with risk-based sizing.
/// </summary>
public class SimpleOrderPanelStrategy : Strategy
{
	public enum RiskModes
	{
		FixedVolume,
		BalancePercent,
	}

	public enum StopTakeModes
	{
		PriceLevels,
		PointOffsets,
	}

	private readonly StrategyParam<RiskModes> _riskCalculationMode;
	private readonly StrategyParam<decimal> _riskValue;
	private readonly StrategyParam<StopTakeModes> _stopTakeMode;
	private readonly StrategyParam<decimal> _stopLossValue;
	private readonly StrategyParam<decimal> _takeProfitValue;
	private readonly StrategyParam<bool> _buyMarketRequest;
	private readonly StrategyParam<bool> _sellMarketRequest;
	private readonly StrategyParam<bool> _breakEvenRequest;
	private readonly StrategyParam<bool> _modifyStopRequest;
	private readonly StrategyParam<bool> _modifyTakeRequest;
	private readonly StrategyParam<bool> _closeAllRequest;
	private readonly StrategyParam<bool> _closeBuyRequest;
	private readonly StrategyParam<bool> _closeSellRequest;
	private readonly StrategyParam<bool> _partialCloseRequest;
	private readonly StrategyParam<decimal> _partialVolume;
	private readonly StrategyParam<bool> _placeBuyPendingRequest;
	private readonly StrategyParam<bool> _placeSellPendingRequest;
	private readonly StrategyParam<decimal> _pendingPrice;
	private readonly StrategyParam<bool> _cancelPendingRequest;

	private decimal? _pendingBuyPrice;
	private decimal? _pendingSellPrice;
	private decimal _pendingBuyVolume;
	private decimal _pendingSellVolume;
	private bool _pendingBuyIsStop;
	private bool _pendingSellIsStop;

	private decimal _bestBid;
	private decimal _bestAsk;
	private decimal _lastTradePrice;

	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal _pointSize;
	private decimal _stepPrice;

	private bool _previousBuyRequest;
	private bool _previousSellRequest;
	private bool _previousCloseAllRequest;
	private bool _previousCloseBuyRequest;
	private bool _previousCloseSellRequest;
	private bool _previousBreakEvenRequest;
	private bool _previousModifyStopRequest;
	private bool _previousModifyTakeRequest;
	private bool _previousPartialCloseRequest;
	private bool _previousPlaceBuyPendingRequest;
	private bool _previousPlaceSellPendingRequest;
	private bool _previousCancelPendingRequest;

	/// <summary>
	/// Initializes a new instance of the <see cref="SimpleOrderPanelStrategy"/> class.
	/// </summary>
	public SimpleOrderPanelStrategy()
	{
		_riskCalculationMode = Param(nameof(RiskCalculation), RiskModes.FixedVolume)
		.SetDisplay("Risk Mode", "Choose between fixed lots or balance percentage sizing.", "Risk")
		.SetCanOptimize(false);

		_riskValue = Param(nameof(RiskValue), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Value", "Lot size or balance percent depending on the selected mode.", "Risk")
		.SetCanOptimize(false);

		_stopTakeMode = Param(nameof(StopTakeCalculation), StopTakeModes.PointOffsets)
		.SetDisplay("Stop/Take Mode", "Interpret stop-loss and take-profit values as absolute prices or MetaTrader points.", "Risk")
		.SetCanOptimize(false);

		_stopLossValue = Param(nameof(StopLossValue), 300m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Absolute price or MetaTrader points depending on the stop/take mode.", "Risk")
		.SetCanOptimize(false);

		_takeProfitValue = Param(nameof(TakeProfitValue), 300m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Absolute price or MetaTrader points depending on the stop/take mode.", "Risk")
		.SetCanOptimize(false);

		_buyMarketRequest = Param(nameof(BuyMarketRequest), false)
		.SetDisplay("Buy", "Set to true to send a market buy order using the configured risk.", "Controls")
		.SetCanOptimize(false);

		_sellMarketRequest = Param(nameof(SellMarketRequest), false)
		.SetDisplay("Sell", "Set to true to send a market sell order using the configured risk.", "Controls")
		.SetCanOptimize(false);

		_breakEvenRequest = Param(nameof(BreakEvenRequest), false)
		.SetDisplay("Break-even", "Set to true to move the protective stop to the average entry price.", "Controls")
		.SetCanOptimize(false);

		_modifyStopRequest = Param(nameof(ModifyStopRequest), false)
		.SetDisplay("Apply Stop", "Reapply the stop-loss configuration to the open position.", "Controls")
		.SetCanOptimize(false);

		_modifyTakeRequest = Param(nameof(ModifyTakeRequest), false)
		.SetDisplay("Apply Take", "Reapply the take-profit configuration to the open position.", "Controls")
		.SetCanOptimize(false);

		_closeAllRequest = Param(nameof(CloseAllRequest), false)
		.SetDisplay("Close All", "Close every open position managed by the strategy.", "Controls")
		.SetCanOptimize(false);

		_closeBuyRequest = Param(nameof(CloseBuyRequest), false)
		.SetDisplay("Close Buys", "Close the long position if it exists.", "Controls")
		.SetCanOptimize(false);

		_closeSellRequest = Param(nameof(CloseSellRequest), false)
		.SetDisplay("Close Sells", "Close the short position if it exists.", "Controls")
		.SetCanOptimize(false);

		_partialCloseRequest = Param(nameof(PartialCloseRequest), false)
		.SetDisplay("Close Partial", "Close a portion of the current position using the partial volume parameter.", "Controls")
		.SetCanOptimize(false);

		_partialVolume = Param(nameof(PartialVolume), 0.05m)
		.SetGreaterThanZero()
		.SetDisplay("Partial Volume", "Volume used when closing a portion of the position.", "Risk")
		.SetCanOptimize(false);

		_placeBuyPendingRequest = Param(nameof(PlaceBuyPendingRequest), false)
		.SetDisplay("Arm Buy Pending", "Reserve a pending buy entry at the configured price.", "Pending")
		.SetCanOptimize(false);

		_placeSellPendingRequest = Param(nameof(PlaceSellPendingRequest), false)
		.SetDisplay("Arm Sell Pending", "Reserve a pending sell entry at the configured price.", "Pending")
		.SetCanOptimize(false);

		_pendingPrice = Param(nameof(PendingPrice), 0m)
		.SetNotNegative()
		.SetDisplay("Pending Price", "Trigger price used for pending entries.", "Pending")
		.SetCanOptimize(false);

		_cancelPendingRequest = Param(nameof(CancelPendingRequest), false)
		.SetDisplay("Cancel Pending", "Remove armed pending entries.", "Pending")
		.SetCanOptimize(false);

		_pointSize = 0m;
		_stepPrice = 0m;
	}

	/// <summary>
	/// Determines whether the strategy sizes trades by fixed volume or balance percentage.
	/// </summary>
	public RiskModes RiskCalculation
	{
		get => _riskCalculationMode.Value;
		set => _riskCalculationMode.Value = value;
	}

	/// <summary>
	/// Risk amount expressed either in lots or as a percentage of the account balance.
	/// </summary>
	public decimal RiskValue
	{
		get => _riskValue.Value;
		set => _riskValue.Value = value;
	}

	/// <summary>
	/// Defines how stop-loss and take-profit inputs are interpreted.
	/// </summary>
	public StopTakeModes StopTakeCalculation
	{
		get => _stopTakeMode.Value;
		set => _stopTakeMode.Value = value;
	}

	/// <summary>
	/// Stop-loss number entered by the user.
	/// </summary>
	public decimal StopLossValue
	{
		get => _stopLossValue.Value;
		set => _stopLossValue.Value = value;
	}

	/// <summary>
	/// Take-profit number entered by the user.
	/// </summary>
	public decimal TakeProfitValue
	{
		get => _takeProfitValue.Value;
		set => _takeProfitValue.Value = value;
	}

	/// <summary>
	/// Manual toggle that fires a market buy request.
	/// </summary>
	public bool BuyMarketRequest
	{
		get => _buyMarketRequest.Value;
		set => _buyMarketRequest.Value = value;
	}

	/// <summary>
	/// Manual toggle that fires a market sell request.
	/// </summary>
	public bool SellMarketRequest
	{
		get => _sellMarketRequest.Value;
		set => _sellMarketRequest.Value = value;
	}

	/// <summary>
	/// Manual toggle that moves the stop-loss to the entry price.
	/// </summary>
	public bool BreakEvenRequest
	{
		get => _breakEvenRequest.Value;
		set => _breakEvenRequest.Value = value;
	}

	/// <summary>
	/// Manual toggle that reapplies the stop-loss rule.
	/// </summary>
	public bool ModifyStopRequest
	{
		get => _modifyStopRequest.Value;
		set => _modifyStopRequest.Value = value;
	}

	/// <summary>
	/// Manual toggle that reapplies the take-profit rule.
	/// </summary>
	public bool ModifyTakeRequest
	{
		get => _modifyTakeRequest.Value;
		set => _modifyTakeRequest.Value = value;
	}

	/// <summary>
	/// Manual toggle that closes every open position.
	/// </summary>
	public bool CloseAllRequest
	{
		get => _closeAllRequest.Value;
		set => _closeAllRequest.Value = value;
	}

	/// <summary>
	/// Manual toggle that closes the long position.
	/// </summary>
	public bool CloseBuyRequest
	{
		get => _closeBuyRequest.Value;
		set => _closeBuyRequest.Value = value;
	}

	/// <summary>
	/// Manual toggle that closes the short position.
	/// </summary>
	public bool CloseSellRequest
	{
		get => _closeSellRequest.Value;
		set => _closeSellRequest.Value = value;
	}

	/// <summary>
	/// Manual toggle that closes part of the open position.
	/// </summary>
	public bool PartialCloseRequest
	{
		get => _partialCloseRequest.Value;
		set => _partialCloseRequest.Value = value;
	}

	/// <summary>
	/// Volume used when closing a portion of the position.
	/// </summary>
	public decimal PartialVolume
	{
		get => _partialVolume.Value;
		set => _partialVolume.Value = value;
	}

	/// <summary>
	/// Manual toggle that arms a pending buy entry.
	/// </summary>
	public bool PlaceBuyPendingRequest
	{
		get => _placeBuyPendingRequest.Value;
		set => _placeBuyPendingRequest.Value = value;
	}

	/// <summary>
	/// Manual toggle that arms a pending sell entry.
	/// </summary>
	public bool PlaceSellPendingRequest
	{
		get => _placeSellPendingRequest.Value;
		set => _placeSellPendingRequest.Value = value;
	}

	/// <summary>
	/// Price threshold for pending orders.
	/// </summary>
	public decimal PendingPrice
	{
		get => _pendingPrice.Value;
		set => _pendingPrice.Value = value;
	}

	/// <summary>
	/// Manual toggle that cancels every pending request.
	/// </summary>
	public bool CancelPendingRequest
	{
		get => _cancelPendingRequest.Value;
		set => _cancelPendingRequest.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pendingBuyPrice = null;
		_pendingSellPrice = null;
		_pendingBuyVolume = 0m;
		_pendingSellVolume = 0m;
		_pendingBuyIsStop = false;
		_pendingSellIsStop = false;

		_bestBid = 0m;
		_bestAsk = 0m;
		_lastTradePrice = 0m;

		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_pointSize = 0m;
		_stepPrice = 0m;

		_previousBuyRequest = false;
		_previousSellRequest = false;
		_previousCloseAllRequest = false;
		_previousCloseBuyRequest = false;
		_previousCloseSellRequest = false;
		_previousBreakEvenRequest = false;
		_previousModifyStopRequest = false;
		_previousModifyTakeRequest = false;
		_previousPartialCloseRequest = false;
		_previousPlaceBuyPendingRequest = false;
		_previousPlaceSellPendingRequest = false;
		_previousCancelPendingRequest = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = CalculatePointSize();
		_stepPrice = Security?.StepPrice ?? 0m;

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		SubscribeTicks()
		.Bind(ProcessTrade)
		.Start();

		Timer.Start(TimeSpan.FromMilliseconds(200), ProcessManualControls);
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		Timer.Stop();

		base.OnStop();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_entryPrice = null;
			_stopLossPrice = null;
			_takeProfitPrice = null;
			_previousBuyRequest = false;
			_previousSellRequest = false;
			_previousBreakEvenRequest = false;
			_previousModifyStopRequest = false;
			_previousModifyTakeRequest = false;
			_previousPartialCloseRequest = false;
			return;
		}

		if ((Position > 0m && delta > 0m) || (Position < 0m && delta < 0m))
		{
			InitializePositionState();
		}
	}

	private void ProcessManualControls()
	{
		HandleMarketRequests();
		HandleCloseRequests();
		HandleProtectiveRequests();
		HandlePendingRequests();
	}

	private void HandleMarketRequests()
	{
		var buyRequest = BuyMarketRequest;
		if (buyRequest && !_previousBuyRequest)
		{
			ExecuteMarketOrder(Sides.Buy);
			BuyMarketRequest = false;
		}
		_previousBuyRequest = buyRequest;

		var sellRequest = SellMarketRequest;
		if (sellRequest && !_previousSellRequest)
		{
			ExecuteMarketOrder(Sides.Sell);
			SellMarketRequest = false;
		}
		_previousSellRequest = sellRequest;
	}

	private void HandleCloseRequests()
	{
		var closeAll = CloseAllRequest;
		if (closeAll && !_previousCloseAllRequest)
		{
			ClosePosition();
			CloseAllRequest = false;
		}
		_previousCloseAllRequest = closeAll;

		var closeBuy = CloseBuyRequest;
		if (closeBuy && !_previousCloseBuyRequest)
		{
			if (Position > 0m)
			{
				ClosePosition();
			}
			CloseBuyRequest = false;
		}
		_previousCloseBuyRequest = closeBuy;

		var closeSell = CloseSellRequest;
		if (closeSell && !_previousCloseSellRequest)
		{
			if (Position < 0m)
			{
				ClosePosition();
			}
			CloseSellRequest = false;
		}
		_previousCloseSellRequest = closeSell;

		var partialClose = PartialCloseRequest;
		if (partialClose && !_previousPartialCloseRequest)
		{
			ExecutePartialClose();
			PartialCloseRequest = false;
		}
		_previousPartialCloseRequest = partialClose;
	}

	private void HandleProtectiveRequests()
	{
		var breakEven = BreakEvenRequest;
		if (breakEven && !_previousBreakEvenRequest)
		{
			ApplyBreakEven();
			BreakEvenRequest = false;
		}
		_previousBreakEvenRequest = breakEven;

		var modifyStop = ModifyStopRequest;
		if (modifyStop && !_previousModifyStopRequest)
		{
			ApplyStopLoss();
			ModifyStopRequest = false;
		}
		_previousModifyStopRequest = modifyStop;

		var modifyTake = ModifyTakeRequest;
		if (modifyTake && !_previousModifyTakeRequest)
		{
			ApplyTakeProfit();
			ModifyTakeRequest = false;
		}
		_previousModifyTakeRequest = modifyTake;
	}

	private void HandlePendingRequests()
	{
		var placeBuyPending = PlaceBuyPendingRequest;
		if (placeBuyPending && !_previousPlaceBuyPendingRequest)
		{
			ArmPendingOrder(Sides.Buy);
			PlaceBuyPendingRequest = false;
		}
		_previousPlaceBuyPendingRequest = placeBuyPending;

		var placeSellPending = PlaceSellPendingRequest;
		if (placeSellPending && !_previousPlaceSellPendingRequest)
		{
			ArmPendingOrder(Sides.Sell);
			PlaceSellPendingRequest = false;
		}
		_previousPlaceSellPendingRequest = placeSellPending;

		var cancelPending = CancelPendingRequest;
		if (cancelPending && !_previousCancelPendingRequest)
		{
			CancelPendingOrders();
			CancelPendingRequest = false;
		}
		_previousCancelPendingRequest = cancelPending;
	}

	private void ExecuteMarketOrder(Sides direction)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0m && Math.Sign(Position) == (direction == Sides.Buy ? 1 : -1))
		{
			LogInfo("Position already opened in the requested direction.");
			return;
		}

		var entryPrice = GetEntryPrice(direction);
		var volume = CalculateVolume(direction, entryPrice);

		if (volume <= 0m)
		{
			LogWarning("Calculated volume is not tradable. Check risk settings and stop-loss distance.");
			return;
		}

		if (direction == Sides.Buy)
		BuyMarket(volume);
		else
		SellMarket(volume);
	}

	private void ExecutePartialClose()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var position = Position;

		if (position == 0m)
		{
			LogInfo("No position to close partially.");
			return;
		}

		var volume = AdjustVolume(PartialVolume);

		if (volume <= 0m)
		{
			LogWarning("Partial close volume is not valid.");
			return;
		}

		volume = Math.Min(Math.Abs(position), volume);

		if (position > 0m)
		{
			SellMarket(volume);
		}
		else
		{
			BuyMarket(volume);
		}
	}

	private void ApplyBreakEven()
	{
		if (_entryPrice is not decimal entry)
		{
			LogInfo("Cannot apply break-even without an entry price.");
			return;
		}

		_stopLossPrice = entry;
		LogInfo($"Stop-loss moved to break-even at {entry:F5}.");
	}

	private void ApplyStopLoss()
	{
		if (_entryPrice is not decimal entry)
		return;

		_stopLossPrice = CalculateStopPrice(Position > 0m ? Sides.Buy : Sides.Sell, entry);

		if (_stopLossPrice is decimal price && price > 0m)
		LogInfo($"Stop-loss updated to {price:F5}.");
	}

	private void ApplyTakeProfit()
	{
		if (_entryPrice is not decimal entry)
		return;

		_takeProfitPrice = CalculateTakePrice(Position > 0m ? Sides.Buy : Sides.Sell, entry);

		if (_takeProfitPrice is decimal price && price > 0m)
		LogInfo($"Take-profit updated to {price:F5}.");
	}

	private void ArmPendingOrder(Sides direction)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var price = PendingPrice;

		if (price <= 0m)
		{
			LogWarning("Pending price must be greater than zero.");
			return;
		}

		var referencePrice = direction == Sides.Buy ? (_bestAsk > 0m ? _bestAsk : _lastTradePrice) : (_bestBid > 0m ? _bestBid : _lastTradePrice);

		if (referencePrice <= 0m)
		{
			LogWarning("No market data to arm pending order.");
			return;
		}

		var volume = CalculateVolume(direction, price);

		if (volume <= 0m)
		{
			LogWarning("Calculated pending volume is not tradable.");
			return;
		}

		if (direction == Sides.Buy)
		{
			_pendingBuyPrice = price;
			_pendingBuyVolume = volume;
			_pendingBuyIsStop = price > referencePrice;
			LogInfo($"Armed pending buy {( _pendingBuyIsStop ? "stop" : "limit" )} at {price:F5} with volume {volume}.");
		}
		else
		{
			_pendingSellPrice = price;
			_pendingSellVolume = volume;
			_pendingSellIsStop = price < referencePrice;
			LogInfo($"Armed pending sell {( _pendingSellIsStop ? "stop" : "limit" )} at {price:F5} with volume {volume}.");
		}
	}

	private void CancelPendingOrders()
	{
		if (_pendingBuyPrice is decimal buyPrice)
		{
			LogInfo($"Cancelled pending buy at {buyPrice:F5}.");
		}

		if (_pendingSellPrice is decimal sellPrice)
		{
			LogInfo($"Cancelled pending sell at {sellPrice:F5}.");
		}

		_pendingBuyPrice = null;
		_pendingSellPrice = null;
		_pendingBuyVolume = 0m;
		_pendingSellVolume = 0m;
		_pendingBuyIsStop = false;
		_pendingSellIsStop = false;
	}

	private void InitializePositionState()
	{
		var direction = Position > 0m ? Sides.Buy : Sides.Sell;

		var entry = PositionPrice;
		if (entry <= 0m)
		entry = _lastTradePrice;

		if (entry <= 0m)
		{
			LogWarning("Unable to determine entry price for protective orders.");
			return;
		}

		_entryPrice = entry;
		_stopLossPrice = CalculateStopPrice(direction, entry);
		_takeProfitPrice = CalculateTakePrice(direction, entry);

		_pendingBuyPrice = null;
		_pendingSellPrice = null;
		_pendingBuyVolume = 0m;
		_pendingSellVolume = 0m;
		_pendingBuyIsStop = false;
		_pendingSellIsStop = false;
	}

	private decimal CalculateVolume(Sides direction, decimal entryPrice)
	{
		var volume = RiskCalculation == RiskModes.FixedVolume
		? RiskValue
		: CalculateRiskBasedVolume(direction, entryPrice);

		return AdjustVolume(volume);
	}

	private decimal CalculateRiskBasedVolume(Sides direction, decimal entryPrice)
	{
		if (RiskValue <= 0m)
		return 0m;

		var portfolio = Portfolio;
		var balance = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;

		if (balance <= 0m)
		return 0m;

		var stopDistance = CalculateStopDistance(direction, entryPrice);

		if (stopDistance <= 0m)
		return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = _stepPrice;

		if (priceStep <= 0m || stepPrice <= 0m)
		return 0m;

		var steps = stopDistance / priceStep;

		if (steps <= 0m)
		return 0m;

		var riskAmount = balance * RiskValue / 100m;
		var valuePerLot = steps * stepPrice;

		if (valuePerLot <= 0m)
		return 0m;

		return riskAmount / valuePerLot;
	}

	private decimal CalculateStopDistance(Sides direction, decimal entryPrice)
	{
		if (StopLossValue <= 0m)
		return 0m;

		if (StopTakeCalculation == StopTakeModes.PriceLevels)
		{
			return Math.Abs(entryPrice - StopLossValue);
		}

		var size = _pointSize;

		if (size <= 0m)
		{
			size = CalculatePointSize();
			_pointSize = size;
		}

		return StopLossValue * size;
	}

	private decimal? CalculateStopPrice(Sides direction, decimal entryPrice)
	{
		if (StopLossValue <= 0m)
		return null;

		if (StopTakeCalculation == StopTakeModes.PriceLevels)
		return StopLossValue;

		var distance = StopLossValue * (_pointSize > 0m ? _pointSize : CalculatePointSize());

		if (distance <= 0m)
		return null;

		return direction == Sides.Buy ? entryPrice - distance : entryPrice + distance;
	}

	private decimal? CalculateTakePrice(Sides direction, decimal entryPrice)
	{
		if (TakeProfitValue <= 0m)
		return null;

		if (StopTakeCalculation == StopTakeModes.PriceLevels)
		return TakeProfitValue;

		var distance = TakeProfitValue * (_pointSize > 0m ? _pointSize : CalculatePointSize());

		if (distance <= 0m)
		return null;

		return direction == Sides.Buy ? entryPrice + distance : entryPrice - distance;
	}

	private decimal GetEntryPrice(Sides direction)
	{
		if (direction == Sides.Buy)
		{
			if (_bestAsk > 0m)
			return _bestAsk;
		}
		else
		{
			if (_bestBid > 0m)
			return _bestBid;
		}

		return _lastTradePrice;
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;

		if (security == null)
		return volume;

		var step = security.VolumeStep;

		if (step > 0m)
		{
			volume = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;
		}

		var minVolume = security.MinVolume;
		var maxVolume = security.MaxVolume;

		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		if (maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
		{
			_bestBid = bidPrice;
		}

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
		{
			_bestAsk = askPrice;
		}

		CheckPendingOrders();
			CheckProtectiveLevels();
	}

		private void ProcessTrade(ITickTradeMessage trade)
		{
			var tradePrice = trade.Price;

			_lastTradePrice = tradePrice;
			CheckProtectiveLevels();
	}

	private void CheckPendingOrders()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_pendingBuyPrice is decimal buyPrice && _pendingBuyVolume > 0m && _bestAsk > 0m)
		{
			var conditionMet = _pendingBuyIsStop ? _bestAsk >= buyPrice : _bestAsk <= buyPrice;

			if (conditionMet)
			{
				LogInfo($"Pending buy filled at {buyPrice:F5}.");
				BuyMarket(_pendingBuyVolume);
				_pendingBuyPrice = null;
				_pendingBuyVolume = 0m;
				_pendingBuyIsStop = false;
			}
		}

		if (_pendingSellPrice is decimal sellPrice && _pendingSellVolume > 0m && _bestBid > 0m)
		{
			var conditionMet = _pendingSellIsStop ? _bestBid <= sellPrice : _bestBid >= sellPrice;

			if (conditionMet)
			{
				LogInfo($"Pending sell filled at {sellPrice:F5}.");
				SellMarket(_pendingSellVolume);
				_pendingSellPrice = null;
				_pendingSellVolume = 0m;
				_pendingSellIsStop = false;
			}
		}
	}

	private void CheckProtectiveLevels()
	{
		if (Position == 0m)
		return;

		if (_entryPrice is not decimal entry)
		return;

		var direction = Position > 0m ? Sides.Buy : Sides.Sell;
		var currentPrice = direction == Sides.Buy ? (_bestBid > 0m ? _bestBid : _lastTradePrice) : (_bestAsk > 0m ? _bestAsk : _lastTradePrice);

		if (currentPrice <= 0m)
		return;

		if (_stopLossPrice is decimal stop)
		{
			var stopHit = direction == Sides.Buy ? currentPrice <= stop : currentPrice >= stop;

			if (stopHit)
			{
				LogInfo("Stop-loss condition met. Closing position.");
				ClosePosition();
				return;
			}
		}

		if (_takeProfitPrice is decimal take)
		{
			var takeHit = direction == Sides.Buy ? currentPrice >= take : currentPrice <= take;

			if (takeHit)
			{
				LogInfo("Take-profit condition met. Closing position.");
				ClosePosition();
			}
		}
	}

	private decimal CalculatePointSize()
	{
		var security = Security;

		if (security?.PriceStep is decimal step && step > 0m)
		return step;

		var decimals = security?.Decimals ?? 0;

		if (decimals > 0)
		{
			var value = Math.Pow(10d, -decimals);
			return (decimal)value;
		}

		return 0.0001m;
	}
}

