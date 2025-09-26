using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy converted from the RUBBERBANDS_2 MetaTrader expert advisor.
/// Maintains separate long and short ladders and adds positions when price moves by a configurable step.
/// Closes all exposure once floating profit or loss hits the configured session target.
/// </summary>
public class RubberBandsGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _gridStepPoints;
	private readonly StrategyParam<bool> _quiesceMode;
	private readonly StrategyParam<bool> _triggerImmediateEntries;
	private readonly StrategyParam<bool> _stopNow;
	private readonly StrategyParam<bool> _closeNow;
	private readonly StrategyParam<bool> _useSessionTakeProfit;
	private readonly StrategyParam<decimal> _sessionTakeProfitPerLot;
	private readonly StrategyParam<bool> _useSessionStopLoss;
	private readonly StrategyParam<decimal> _sessionStopLossPerLot;
	private readonly StrategyParam<bool> _useInitialValues;
	private readonly StrategyParam<decimal> _initialMax;
	private readonly StrategyParam<decimal> _initialMin;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal _upperExtreme;
	private decimal _lowerExtreme;
	private bool _extremesInitialized;
	private bool _closeAllRequested;
	private bool _stopLogged;
	private bool _quiesceLogged;
	private bool _pendingInitialBuy;
	private bool _pendingInitialSell;
	private DateTimeOffset? _lastMinuteTrigger;

	private decimal _longVolume;
	private decimal _longAveragePrice;
	private int _longTrades;

	private decimal _shortVolume;
	private decimal _shortAveragePrice;
	private int _shortTrades;

	private Order _activeBuyOrder;
	private Order _activeSellOrder;
	private Order _closeBuyOrder;
	private Order _closeSellOrder;

	/// <summary>
	/// Volume sent with each market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open trades (sum of long and short legs).
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Distance in points that price must move before adding another grid trade.
	/// </summary>
	public int GridStepPoints
	{
		get => _gridStepPoints.Value;
		set => _gridStepPoints.Value = value;
	}

	/// <summary>
	/// If true the strategy stays idle when there are no open trades.
	/// </summary>
	public bool QuiesceMode
	{
		get => _quiesceMode.Value;
		set => _quiesceMode.Value = value;
	}

	/// <summary>
	/// Opens the initial buy and sell orders as soon as the strategy becomes ready.
	/// </summary>
	public bool TriggerImmediateEntries
	{
		get => _triggerImmediateEntries.Value;
		set => _triggerImmediateEntries.Value = value;
	}

	/// <summary>
	/// When enabled the strategy ignores new entry signals.
	/// </summary>
	public bool StopNow
	{
		get => _stopNow.Value;
		set => _stopNow.Value = value;
	}

	/// <summary>
	/// If true all exposure is closed as soon as the strategy starts.
	/// </summary>
	public bool CloseNow
	{
		get => _closeNow.Value;
		set => _closeNow.Value = value;
	}

	/// <summary>
	/// Enables session take-profit management.
	/// </summary>
	public bool UseSessionTakeProfit
	{
		get => _useSessionTakeProfit.Value;
		set => _useSessionTakeProfit.Value = value;
	}

	/// <summary>
	/// Floating profit target per one lot.
	/// </summary>
	public decimal SessionTakeProfitPerLot
	{
		get => _sessionTakeProfitPerLot.Value;
		set => _sessionTakeProfitPerLot.Value = value;
	}

	/// <summary>
	/// Enables session stop-loss management.
	/// </summary>
	public bool UseSessionStopLoss
	{
		get => _useSessionStopLoss.Value;
		set => _useSessionStopLoss.Value = value;
	}

	/// <summary>
	/// Floating loss threshold per one lot.
	/// </summary>
	public decimal SessionStopLossPerLot
	{
		get => _sessionStopLossPerLot.Value;
		set => _sessionStopLossPerLot.Value = value;
	}

	/// <summary>
	/// Reuses the provided extremes instead of initializing them from the first quote.
	/// </summary>
	public bool UseInitialValues
	{
		get => _useInitialValues.Value;
		set => _useInitialValues.Value = value;
	}

	/// <summary>
	/// Initial upper extreme used on restart.
	/// </summary>
	public decimal InitialMax
	{
		get => _initialMax.Value;
		set => _initialMax.Value = value;
	}

	/// <summary>
	/// Initial lower extreme used on restart.
	/// </summary>
	public decimal InitialMin
	{
		get => _initialMin.Value;
		set => _initialMin.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RubberBandsGridStrategy"/> class.
	/// </summary>
	public RubberBandsGridStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume used for every market order", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.2m, 0.01m);

		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum simultaneously open trades", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_gridStepPoints = Param(nameof(GridStepPoints), 50)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step (points)", "Distance between consecutive grid entries", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_quiesceMode = Param(nameof(QuiesceMode), false)
			.SetDisplay("Quiesce", "Stay idle when no trades are open", "Execution");

		_triggerImmediateEntries = Param(nameof(TriggerImmediateEntries), false)
			.SetDisplay("Immediate Entries", "Open both directions as soon as the strategy is ready", "Execution");

		_stopNow = Param(nameof(StopNow), false)
			.SetDisplay("Stop Now", "Disable automated entries without closing positions", "Execution");

		_closeNow = Param(nameof(CloseNow), false)
			.SetDisplay("Close Now", "Close all exposure on start", "Execution");

		_useSessionTakeProfit = Param(nameof(UseSessionTakeProfit), true)
			.SetDisplay("Use Session TP", "Enable floating profit target", "Risk");

		_sessionTakeProfitPerLot = Param(nameof(SessionTakeProfitPerLot), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Session TP", "Floating profit target per one lot", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 2000m, 100m);

		_useSessionStopLoss = Param(nameof(UseSessionStopLoss), false)
			.SetDisplay("Use Session SL", "Enable floating loss protection", "Risk");

		_sessionStopLossPerLot = Param(nameof(SessionStopLossPerLot), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Session SL", "Floating loss threshold per one lot", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 1000m, 50m);

		_useInitialValues = Param(nameof(UseInitialValues), false)
			.SetDisplay("Use Initial Extremes", "Load stored extremes on restart", "Recovery");

		_initialMax = Param(nameof(InitialMax), 0m)
			.SetDisplay("Initial Max", "Stored upper extreme", "Recovery");

		_initialMin = Param(nameof(InitialMin), 0m)
			.SetDisplay("Initial Min", "Stored lower extreme", "Recovery");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastBid = null;
		_lastAsk = null;
		_upperExtreme = 0m;
		_lowerExtreme = 0m;
		_extremesInitialized = false;
		_closeAllRequested = false;
		_stopLogged = false;
		_quiesceLogged = false;
		_pendingInitialBuy = false;
		_pendingInitialSell = false;
		_lastMinuteTrigger = null;

		_longVolume = 0m;
		_longAveragePrice = 0m;
		_longTrades = 0;

		_shortVolume = 0m;
		_shortAveragePrice = 0m;
		_shortTrades = 0;

		_activeBuyOrder = null;
		_activeSellOrder = null;
		_closeBuyOrder = null;
		_closeSellOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_pendingInitialBuy = TriggerImmediateEntries;
		_pendingInitialSell = TriggerImmediateEntries;
		_closeAllRequested = CloseNow;

		if (UseInitialValues)
		{
			_upperExtreme = InitialMax;
			_lowerExtreme = InitialMin;
			_extremesInitialized = true;
		}

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		_lastBid = Convert.ToDecimal(bidValue);

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		_lastAsk = Convert.ToDecimal(askValue);

		if (StopNow)
		{
			if (!_stopLogged)
			{
				LogInfo("Trading paused because StopNow parameter is enabled.");
				_stopLogged = true;
			}

			return;
		}

		_stopLogged = false;

		if (_lastAsk is not decimal ask || ask <= 0m)
		return;

		var bid = _lastBid ?? ask;

		if (!_extremesInitialized)
		{
			_upperExtreme = ask;
			_lowerExtreme = ask;
			_extremesInitialized = true;
		}

		if (_closeAllRequested)
		{
			TryCloseAll();
			return;
		}

		var floatingProfit = CalculateFloatingProfit(bid, ask);

		if (UseSessionTakeProfit && OrderVolume > 0m && floatingProfit >= SessionTakeProfitPerLot * OrderVolume)
		{
			LogInfo($"Session take-profit reached: {floatingProfit:F2}");
			_closeAllRequested = true;
			TryCloseAll();
			return;
		}

		if (UseSessionStopLoss && OrderVolume > 0m && floatingProfit <= -SessionStopLossPerLot * OrderVolume)
		{
			LogInfo($"Session stop-loss reached: {floatingProfit:F2}");
			_closeAllRequested = true;
			TryCloseAll();
			return;
		}

		var totalTrades = GetTotalTrades();

		if (QuiesceMode && totalTrades == 0)
		{
			if (!_quiesceLogged)
			{
				LogInfo("Strategy is quiesced with no open trades.");
				_quiesceLogged = true;
			}

			return;
		}

		_quiesceLogged = false;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (totalTrades == 0)
		{
			var time = level1.ServerTime;
			if (time.Second == 0)
			{
				var minute = new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0, time.Offset);
				if (_lastMinuteTrigger != minute)
				{
					_pendingInitialBuy = true;
					_pendingInitialSell = true;
					_lastMinuteTrigger = minute;
				}
			}
		}

		if (_pendingInitialBuy)
		{
			if (TrySendBuy(OrderVolume))
			_pendingInitialBuy = false;
		}

		if (_pendingInitialSell)
		{
			if (TrySendSell(OrderVolume))
			_pendingInitialSell = false;
		}

		var gridOffset = GetGridOffset();
		if (gridOffset <= 0m)
		return;

		totalTrades = GetTotalTrades();
		if (totalTrades >= MaxTrades)
		return;

		if (ask >= _upperExtreme + gridOffset)
		{
			_upperExtreme = ask;
			if (TrySendSell(OrderVolume))
			return;
		}

		if (ask <= _lowerExtreme - gridOffset)
		{
			_lowerExtreme = ask;
			TrySendBuy(OrderVolume);
		}
	}

	private decimal CalculateFloatingProfit(decimal bid, decimal ask)
	{
		var profit = 0m;

		if (_longVolume > 0m)
		profit += PriceToMoney(bid - _longAveragePrice, _longVolume);

		if (_shortVolume > 0m)
		profit += PriceToMoney(_shortAveragePrice - ask, _shortVolume);

		return profit;
	}

	private decimal PriceToMoney(decimal priceDiff, decimal volume)
	{
		if (priceDiff == 0m || volume <= 0m)
		return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
		return priceDiff * volume;

		return priceDiff / priceStep * stepPrice * volume;
	}

	private decimal GetGridOffset()
	{
		var priceStep = Security?.PriceStep ?? 0m;

		if (GridStepPoints <= 0)
		return priceStep > 0m ? priceStep : 0m;

		if (priceStep <= 0m)
		return GridStepPoints;

		return GridStepPoints * priceStep;
	}

	private bool TrySendBuy(decimal volume)
	{
		if (volume <= 0m)
		return false;

		if (_activeBuyOrder != null && _activeBuyOrder.State is OrderStates.Pending or OrderStates.Active)
		return false;

		_activeBuyOrder = BuyMarket(volume);
		return true;
	}

	private bool TrySendSell(decimal volume)
	{
		if (volume <= 0m)
		return false;

		if (_activeSellOrder != null && _activeSellOrder.State is OrderStates.Pending or OrderStates.Active)
		return false;

		_activeSellOrder = SellMarket(volume);
		return true;
	}

	private void TryCloseAll()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var hasLong = _longVolume > 0m;
		var hasShort = _shortVolume > 0m;

		if (!hasLong && !hasShort)
		{
			_closeAllRequested = false;
			if (_lastAsk is decimal ask)
			ResetExtremes(ask);
			return;
		}

		if (hasLong)
		{
			if (_closeSellOrder == null || _closeSellOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
			_closeSellOrder = SellMarket(_longVolume);
		}

		if (hasShort)
		{
			if (_closeBuyOrder == null || _closeBuyOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
			_closeBuyOrder = BuyMarket(_shortVolume);
		}
	}

	private void ResetExtremes(decimal ask)
	{
		_upperExtreme = ask;
		_lowerExtreme = ask;
	}

	private int GetTotalTrades()
	{
		return _longTrades + _shortTrades;
	}

	private int CountTrades(decimal volume)
	{
		var size = OrderVolume;
		if (size <= 0m)
		return 0;

		var count = (int)Math.Round(volume / size, MidpointRounding.AwayFromZero);
		return Math.Max(0, count);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade.Security != Security)
		return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			var closing = Math.Min(_shortVolume, volume);
			if (closing > 0m)
			{
				_shortVolume -= closing;
				_shortTrades = Math.Max(0, _shortTrades - CountTrades(closing));
				volume -= closing;

				if (_shortVolume <= 0m)
				{
					_shortVolume = 0m;
					_shortAveragePrice = 0m;
				}
			}

			if (volume > 0m)
			{
				var newVolume = _longVolume + volume;
				_longAveragePrice = newVolume == 0m ? 0m : (_longAveragePrice * _longVolume + price * volume) / newVolume;
				_longVolume = newVolume;
				_longTrades += CountTrades(volume);
				_lowerExtreme = Math.Min(_lowerExtreme, price);
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			var closing = Math.Min(_longVolume, volume);
			if (closing > 0m)
			{
				_longVolume -= closing;
				_longTrades = Math.Max(0, _longTrades - CountTrades(closing));
				volume -= closing;

				if (_longVolume <= 0m)
				{
					_longVolume = 0m;
					_longAveragePrice = 0m;
				}
			}

			if (volume > 0m)
			{
				var newVolume = _shortVolume + volume;
				_shortAveragePrice = newVolume == 0m ? 0m : (_shortAveragePrice * _shortVolume + price * volume) / newVolume;
				_shortVolume = newVolume;
				_shortTrades += CountTrades(volume);
				_upperExtreme = Math.Max(_upperExtreme, price);
			}
		}

		if (_longVolume == 0m && _shortVolume == 0m && _closeAllRequested)
		{
			_closeAllRequested = false;
			if (_lastAsk is decimal ask)
			ResetExtremes(ask);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.Security != Security)
		return;

		if (order == _activeBuyOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
		_activeBuyOrder = null;

		if (order == _activeSellOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
		_activeSellOrder = null;

		if (order == _closeBuyOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
		_closeBuyOrder = null;

		if (order == _closeSellOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
		_closeSellOrder = null;
	}
}

