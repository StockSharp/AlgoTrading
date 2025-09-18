using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Universal trailing strategy inspired by the "Universal 1.64" expert advisor.
/// Manages pending orders, trailing stops, timed entries, and global profit monitoring.
/// </summary>
public class UniversalTrailingManagerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<bool> _waitClose;
	private readonly StrategyParam<bool> _allowBuyStop;
	private readonly StrategyParam<bool> _allowSellLimit;
	private readonly StrategyParam<bool> _allowSellStop;
	private readonly StrategyParam<bool> _allowBuyLimit;
	private readonly StrategyParam<int> _maxMarketPositions;
	private readonly StrategyParam<decimal> _marketTakeProfitPoints;
	private readonly StrategyParam<decimal> _marketStopLossPoints;
	private readonly StrategyParam<decimal> _marketTrailingStopPoints;
	private readonly StrategyParam<decimal> _marketTrailingStepPoints;
	private readonly StrategyParam<bool> _waitForProfit;
	private readonly StrategyParam<decimal> _stopOrderOffsetPoints;
	private readonly StrategyParam<decimal> _stopOrderTakeProfitPoints;
	private readonly StrategyParam<decimal> _stopOrderStopLossPoints;
	private readonly StrategyParam<decimal> _stopOrderTrailingStopPoints;
	private readonly StrategyParam<decimal> _stopOrderTrailingStepPoints;
	private readonly StrategyParam<decimal> _limitOrderOffsetPoints;
	private readonly StrategyParam<decimal> _limitOrderTakeProfitPoints;
	private readonly StrategyParam<decimal> _limitOrderStopLossPoints;
	private readonly StrategyParam<decimal> _limitOrderTrailingStopPoints;
	private readonly StrategyParam<decimal> _limitOrderTrailingStepPoints;
	private readonly StrategyParam<bool> _useTime;
	private readonly StrategyParam<int> _timeHour;
	private readonly StrategyParam<int> _timeMinute;
	private readonly StrategyParam<bool> _timeBuy;
	private readonly StrategyParam<bool> _timeSell;
	private readonly StrategyParam<bool> _timeBuyStop;
	private readonly StrategyParam<bool> _timeSellLimit;
	private readonly StrategyParam<bool> _timeSellStop;
	private readonly StrategyParam<bool> _timeBuyLimit;
	private readonly StrategyParam<decimal> _scalpProfitPoints;
	private readonly StrategyParam<bool> _useGlobalLevels;
	private readonly StrategyParam<decimal> _globalTakeProfitPercent;
	private readonly StrategyParam<decimal> _globalStopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private Order _buyLimitOrder;
	private Order _sellLimitOrder;
	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _marketStopOrder;
	private Order _marketTakeProfitOrder;
	private decimal? _pendingBuyLimitPrice;
	private decimal? _pendingSellLimitPrice;
	private decimal? _pendingBuyStopPrice;
	private decimal? _pendingSellStopPrice;
	private decimal? _pendingStopPrice;
	private decimal? _pendingTakeProfitPrice;
	private Sides? _pendingStopSide;
	private Sides? _pendingTakeProfitSide;
	private decimal _pendingStopVolume;
	private decimal _pendingTakeProfitVolume;
	private decimal? _marketStopPrice;
	private decimal? _marketTakeProfitPrice;
	private decimal? _overrideStopDistance;
	private decimal? _overrideTakeDistance;
	private bool _timeBuySignal;
	private bool _timeSellSignal;
	private bool _timeBuyStopSignal;
	private bool _timeSellLimitSignal;
	private bool _timeSellStopSignal;
	private bool _timeBuyLimitSignal;
	private DateTimeOffset? _lastBuyEntryCandle;
	private DateTimeOffset? _lastSellEntryCandle;
	private decimal _priceStep;
	private decimal _minStopDistance;
	private decimal _initialBalance;
	private bool _takeProfitNotified;
	private bool _stopLossNotified;

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Wait for positions to close before placing new orders.
	/// </summary>
	public bool WaitClose
	{
		get => _waitClose.Value;
		set => _waitClose.Value = value;
	}

	/// <summary>
	/// Allow buy stop pending orders.
	/// </summary>
	public bool AllowBuyStop
	{
		get => _allowBuyStop.Value;
		set => _allowBuyStop.Value = value;
	}

	/// <summary>
	/// Allow sell limit pending orders.
	/// </summary>
	public bool AllowSellLimit
	{
		get => _allowSellLimit.Value;
		set => _allowSellLimit.Value = value;
	}

	/// <summary>
	/// Allow sell stop pending orders.
	/// </summary>
	public bool AllowSellStop
	{
		get => _allowSellStop.Value;
		set => _allowSellStop.Value = value;
	}

	/// <summary>
	/// Allow buy limit pending orders.
	/// </summary>
	public bool AllowBuyLimit
	{
		get => _allowBuyLimit.Value;
		set => _allowBuyLimit.Value = value;
	}

	/// <summary>
	/// Maximum number of market positions per direction.
	/// </summary>
	public int MaxMarketPositions
	{
		get => _maxMarketPositions.Value;
		set => _maxMarketPositions.Value = value;
	}

	/// <summary>
	/// Take profit distance for market positions (in points).
	/// </summary>
	public decimal MarketTakeProfitPoints
	{
		get => _marketTakeProfitPoints.Value;
		set => _marketTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance for market positions (in points).
	/// </summary>
	public decimal MarketStopLossPoints
	{
		get => _marketStopLossPoints.Value;
		set => _marketStopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing distance for market positions (in points).
	/// </summary>
	public decimal MarketTrailingStopPoints
	{
		get => _marketTrailingStopPoints.Value;
		set => _marketTrailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing step for market positions (in points).
	/// </summary>
	public decimal MarketTrailingStepPoints
	{
		get => _marketTrailingStepPoints.Value;
		set => _marketTrailingStepPoints.Value = value;
	}

	/// <summary>
	/// Require profit before enabling trailing for market positions.
	/// </summary>
	public bool WaitForProfit
	{
		get => _waitForProfit.Value;
		set => _waitForProfit.Value = value;
	}

	/// <summary>
	/// Offset for stop orders (in points).
	/// </summary>
	public decimal StopOrderOffsetPoints
	{
		get => _stopOrderOffsetPoints.Value;
		set => _stopOrderOffsetPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance for stop orders (in points).
	/// </summary>
	public decimal StopOrderTakeProfitPoints
	{
		get => _stopOrderTakeProfitPoints.Value;
		set => _stopOrderTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance for stop orders (in points).
	/// </summary>
	public decimal StopOrderStopLossPoints
	{
		get => _stopOrderStopLossPoints.Value;
		set => _stopOrderStopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing distance for stop orders (in points).
	/// </summary>
	public decimal StopOrderTrailingStopPoints
	{
		get => _stopOrderTrailingStopPoints.Value;
		set => _stopOrderTrailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing step for stop orders (in points).
	/// </summary>
	public decimal StopOrderTrailingStepPoints
	{
		get => _stopOrderTrailingStepPoints.Value;
		set => _stopOrderTrailingStepPoints.Value = value;
	}

	/// <summary>
	/// Offset for limit orders (in points).
	/// </summary>
	public decimal LimitOrderOffsetPoints
	{
		get => _limitOrderOffsetPoints.Value;
		set => _limitOrderOffsetPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance for limit orders (in points).
	/// </summary>
	public decimal LimitOrderTakeProfitPoints
	{
		get => _limitOrderTakeProfitPoints.Value;
		set => _limitOrderTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance for limit orders (in points).
	/// </summary>
	public decimal LimitOrderStopLossPoints
	{
		get => _limitOrderStopLossPoints.Value;
		set => _limitOrderStopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing distance for limit orders (in points).
	/// </summary>
	public decimal LimitOrderTrailingStopPoints
	{
		get => _limitOrderTrailingStopPoints.Value;
		set => _limitOrderTrailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing step for limit orders (in points).
	/// </summary>
	public decimal LimitOrderTrailingStepPoints
	{
		get => _limitOrderTrailingStepPoints.Value;
		set => _limitOrderTrailingStepPoints.Value = value;
	}

	/// <summary>
	/// Enable time-based actions.
	/// </summary>
	public bool UseTime
	{
		get => _useTime.Value;
		set => _useTime.Value = value;
	}

	/// <summary>
	/// Hour for scheduled actions (terminal time).
	/// </summary>
	public int TimeHour
	{
		get => _timeHour.Value;
		set => _timeHour.Value = value;
	}

	/// <summary>
	/// Minute for scheduled actions (terminal time).
	/// </summary>
	public int TimeMinute
	{
		get => _timeMinute.Value;
		set => _timeMinute.Value = value;
	}

	/// <summary>
	/// Open market buy position at the scheduled time.
	/// </summary>
	public bool TimeBuy
	{
		get => _timeBuy.Value;
		set => _timeBuy.Value = value;
	}

	/// <summary>
	/// Open market sell position at the scheduled time.
	/// </summary>
	public bool TimeSell
	{
		get => _timeSell.Value;
		set => _timeSell.Value = value;
	}

	/// <summary>
	/// Place buy stop order at the scheduled time.
	/// </summary>
	public bool TimeBuyStop
	{
		get => _timeBuyStop.Value;
		set => _timeBuyStop.Value = value;
	}

	/// <summary>
	/// Place sell limit order at the scheduled time.
	/// </summary>
	public bool TimeSellLimit
	{
		get => _timeSellLimit.Value;
		set => _timeSellLimit.Value = value;
	}

	/// <summary>
	/// Place sell stop order at the scheduled time.
	/// </summary>
	public bool TimeSellStop
	{
		get => _timeSellStop.Value;
		set => _timeSellStop.Value = value;
	}

	/// <summary>
	/// Place buy limit order at the scheduled time.
	/// </summary>
	public bool TimeBuyLimit
	{
		get => _timeBuyLimit.Value;
		set => _timeBuyLimit.Value = value;
	}

	/// <summary>
	/// Scalping profit target (in points) for early exits.
	/// </summary>
	public decimal ScalpProfitPoints
	{
		get => _scalpProfitPoints.Value;
		set => _scalpProfitPoints.Value = value;
	}

	/// <summary>
	/// Monitor global profit and loss levels.
	/// </summary>
	public bool UseGlobalLevels
	{
		get => _useGlobalLevels.Value;
		set => _useGlobalLevels.Value = value;
	}

	/// <summary>
	/// Percentage increase for global profit alert.
	/// </summary>
	public decimal GlobalTakeProfitPercent
	{
		get => _globalTakeProfitPercent.Value;
		set => _globalTakeProfitPercent.Value = value;
	}

	/// <summary>
	/// Percentage decrease for global stop alert.
	/// </summary>
	public decimal GlobalStopLossPercent
	{
		get => _globalStopLossPercent.Value;
		set => _globalStopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type used for periodic processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults matching the original expert advisor.
	/// </summary>
	public UniversalTrailingManagerStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume in lots", "General");

		_waitClose = Param(nameof(WaitClose), true)
			.SetDisplay("Wait Close", "Wait for positions before new orders", "General");

		_allowBuyStop = Param(nameof(AllowBuyStop), true)
			.SetDisplay("Allow Buy Stop", "Enable buy stop orders", "Pending Orders");

		_allowSellLimit = Param(nameof(AllowSellLimit), false)
			.SetDisplay("Allow Sell Limit", "Enable sell limit orders", "Pending Orders");

		_allowSellStop = Param(nameof(AllowSellStop), true)
			.SetDisplay("Allow Sell Stop", "Enable sell stop orders", "Pending Orders");

		_allowBuyLimit = Param(nameof(AllowBuyLimit), false)
			.SetDisplay("Allow Buy Limit", "Enable buy limit orders", "Pending Orders");

		_maxMarketPositions = Param(nameof(MaxMarketPositions), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum open positions per side", "Market");

		_marketTakeProfitPoints = Param(nameof(MarketTakeProfitPoints), 200m)
			.SetDisplay("Market TP", "Take profit distance for market trades", "Market");

		_marketStopLossPoints = Param(nameof(MarketStopLossPoints), 100m)
			.SetDisplay("Market SL", "Stop loss distance for market trades", "Market");

		_marketTrailingStopPoints = Param(nameof(MarketTrailingStopPoints), 100m)
			.SetDisplay("Market Trail", "Trailing distance for market trades", "Market");

		_marketTrailingStepPoints = Param(nameof(MarketTrailingStepPoints), 10m)
			.SetDisplay("Market Trail Step", "Trailing step for market trades", "Market");

		_waitForProfit = Param(nameof(WaitForProfit), true)
			.SetDisplay("Wait For Profit", "Start trailing after reaching profit", "Market");

		_stopOrderOffsetPoints = Param(nameof(StopOrderOffsetPoints), 50m)
			.SetDisplay("Stop Order Offset", "Distance to place stop orders", "Pending Orders");

		_stopOrderTakeProfitPoints = Param(nameof(StopOrderTakeProfitPoints), 200m)
			.SetDisplay("Stop Order TP", "Take profit for stop orders", "Pending Orders");

		_stopOrderStopLossPoints = Param(nameof(StopOrderStopLossPoints), 100m)
			.SetDisplay("Stop Order SL", "Stop loss for stop orders", "Pending Orders");

		_stopOrderTrailingStopPoints = Param(nameof(StopOrderTrailingStopPoints), 0m)
			.SetDisplay("Stop Order Trail", "Trailing distance for stop orders", "Pending Orders");

		_stopOrderTrailingStepPoints = Param(nameof(StopOrderTrailingStepPoints), 3m)
			.SetDisplay("Stop Order Trail Step", "Trailing step for stop orders", "Pending Orders");

		_limitOrderOffsetPoints = Param(nameof(LimitOrderOffsetPoints), 50m)
			.SetDisplay("Limit Order Offset", "Distance to place limit orders", "Pending Orders");

		_limitOrderTakeProfitPoints = Param(nameof(LimitOrderTakeProfitPoints), 200m)
			.SetDisplay("Limit Order TP", "Take profit for limit orders", "Pending Orders");

		_limitOrderStopLossPoints = Param(nameof(LimitOrderStopLossPoints), 100m)
			.SetDisplay("Limit Order SL", "Stop loss for limit orders", "Pending Orders");

		_limitOrderTrailingStopPoints = Param(nameof(LimitOrderTrailingStopPoints), 0m)
			.SetDisplay("Limit Order Trail", "Trailing distance for limit orders", "Pending Orders");

		_limitOrderTrailingStepPoints = Param(nameof(LimitOrderTrailingStepPoints), 3m)
			.SetDisplay("Limit Order Trail Step", "Trailing step for limit orders", "Pending Orders");

		_useTime = Param(nameof(UseTime), true)
			.SetDisplay("Use Time", "Enable scheduled actions", "Time");

		_timeHour = Param(nameof(TimeHour), 23)
			.SetDisplay("Hour", "Hour for scheduled actions", "Time")
			.SetRange(0, 23);

		_timeMinute = Param(nameof(TimeMinute), 59)
			.SetDisplay("Minute", "Minute for scheduled actions", "Time")
			.SetRange(0, 59);

		_timeBuy = Param(nameof(TimeBuy), false)
			.SetDisplay("Time Buy", "Open buy at scheduled time", "Time");

		_timeSell = Param(nameof(TimeSell), false)
			.SetDisplay("Time Sell", "Open sell at scheduled time", "Time");

		_timeBuyStop = Param(nameof(TimeBuyStop), true)
			.SetDisplay("Time Buy Stop", "Place buy stop at scheduled time", "Time");

		_timeSellLimit = Param(nameof(TimeSellLimit), false)
			.SetDisplay("Time Sell Limit", "Place sell limit at scheduled time", "Time");

		_timeSellStop = Param(nameof(TimeSellStop), true)
			.SetDisplay("Time Sell Stop", "Place sell stop at scheduled time", "Time");

		_timeBuyLimit = Param(nameof(TimeBuyLimit), false)
			.SetDisplay("Time Buy Limit", "Place buy limit at scheduled time", "Time");

		_scalpProfitPoints = Param(nameof(ScalpProfitPoints), 0m)
			.SetDisplay("Scalp Profit", "Close trades after profit distance", "Market");

		_useGlobalLevels = Param(nameof(UseGlobalLevels), true)
			.SetDisplay("Use Global Levels", "Monitor account level changes", "Global");

		_globalTakeProfitPercent = Param(nameof(GlobalTakeProfitPercent), 2m)
			.SetDisplay("Global Take Profit", "Percent increase for alert", "Global");

		_globalStopLossPercent = Param(nameof(GlobalStopLossPercent), 2m)
			.SetDisplay("Global Stop Loss", "Percent decrease for alert", "Global");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Processing candle type", "General");
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

		_buyLimitOrder = null;
		_sellLimitOrder = null;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_marketStopOrder = null;
		_marketTakeProfitOrder = null;
		_pendingBuyLimitPrice = null;
		_pendingSellLimitPrice = null;
		_pendingBuyStopPrice = null;
		_pendingSellStopPrice = null;
		_pendingStopPrice = null;
		_pendingTakeProfitPrice = null;
		_pendingStopSide = null;
		_pendingTakeProfitSide = null;
		_pendingStopVolume = 0m;
		_pendingTakeProfitVolume = 0m;
		_marketStopPrice = null;
		_marketTakeProfitPrice = null;
		_overrideStopDistance = null;
		_overrideTakeDistance = null;
		_timeBuySignal = false;
		_timeSellSignal = false;
		_timeBuyStopSignal = false;
		_timeSellLimitSignal = false;
		_timeSellStopSignal = false;
		_timeBuyLimitSignal = false;
		_lastBuyEntryCandle = null;
		_lastSellEntryCandle = null;
		_priceStep = 0m;
		_minStopDistance = 0m;
		_initialBalance = 0m;
		_takeProfitNotified = false;
		_stopLossNotified = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		_minStopDistance = _priceStep;
		_initialBalance = Portfolio?.CurrentValue ?? 0m;
		_takeProfitNotified = false;
		_stopLossNotified = false;
		Volume = TradeVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Volume != TradeVolume)
			Volume = TradeVolume;

		UpdateOrderReferences();
		ResetTimeSignals();
		UpdateTimeSignals(candle);

		if (TradeVolume > 0m)
		{
			HandleTimedEntries(candle);
			PlacePendingOrders(candle);
		}

		ApplyScalping(candle);
		UpdateMarketProtection(candle);
		UpdateGlobalLevels();
	}

	private void UpdateOrderReferences()
	{
		if (_buyLimitOrder != null && _buyLimitOrder.State != OrderStates.Active)
			_buyLimitOrder = null;

		if (_sellLimitOrder != null && _sellLimitOrder.State != OrderStates.Active)
			_sellLimitOrder = null;

		if (_buyStopOrder != null && _buyStopOrder.State != OrderStates.Active)
			_buyStopOrder = null;

		if (_sellStopOrder != null && _sellStopOrder.State != OrderStates.Active)
			_sellStopOrder = null;

		if (_marketStopOrder != null && _marketStopOrder.State != OrderStates.Active)
		{
			_marketStopOrder = null;
			_marketStopPrice = null;
		}

		if (_marketTakeProfitOrder != null && _marketTakeProfitOrder.State != OrderStates.Active)
		{
			_marketTakeProfitOrder = null;
			_marketTakeProfitPrice = null;
		}

		TryPlacePendingRef(ref _buyLimitOrder, ref _pendingBuyLimitPrice, price => BuyLimit(price));
		TryPlacePendingRef(ref _sellLimitOrder, ref _pendingSellLimitPrice, price => SellLimit(price));
		TryPlacePendingRef(ref _buyStopOrder, ref _pendingBuyStopPrice, price => BuyStop(price));
		TryPlacePendingRef(ref _sellStopOrder, ref _pendingSellStopPrice, price => SellStop(price));

		if (_pendingStopPrice.HasValue && _marketStopOrder == null && _pendingStopSide.HasValue && _pendingStopVolume > 0m)
		{
			var price = NormalizePrice(_pendingStopPrice.Value);
			_marketStopOrder = _pendingStopSide == Sides.Sell
				? SellStop(_pendingStopVolume, price)
				: BuyStop(_pendingStopVolume, price);
			_marketStopPrice = price;
			_pendingStopPrice = null;
			_pendingStopSide = null;
			_pendingStopVolume = 0m;
		}

		if (_pendingTakeProfitPrice.HasValue && _marketTakeProfitOrder == null && _pendingTakeProfitSide.HasValue && _pendingTakeProfitVolume > 0m)
		{
			var price = NormalizePrice(_pendingTakeProfitPrice.Value);
			_marketTakeProfitOrder = _pendingTakeProfitSide == Sides.Sell
				? SellLimit(_pendingTakeProfitVolume, price)
				: BuyLimit(_pendingTakeProfitVolume, price);
			_marketTakeProfitPrice = price;
			_pendingTakeProfitPrice = null;
			_pendingTakeProfitSide = null;
			_pendingTakeProfitVolume = 0m;
		}
	}

	private void TryPlacePendingRef(ref Order target, ref decimal? pendingPrice, Func<decimal, Order> placer)
	{
		if (pendingPrice.HasValue && target == null)
		{
			var price = NormalizePrice(pendingPrice.Value);
			if (price > 0m)
				target = placer(price);
			pendingPrice = null;
		}
	}

	private void ResetTimeSignals()
	{
		_timeBuySignal = false;
		_timeSellSignal = false;
		_timeBuyStopSignal = false;
		_timeSellLimitSignal = false;
		_timeSellStopSignal = false;
		_timeBuyLimitSignal = false;
	}

	private void UpdateTimeSignals(ICandleMessage candle)
	{
		if (!UseTime)
			return;

		var time = candle.CloseTime;
		if (time.Hour == TimeHour && time.Minute == TimeMinute)
		{
			_timeBuySignal = TimeBuy;
			_timeSellSignal = TimeSell;
			_timeBuyStopSignal = TimeBuyStop;
			_timeSellLimitSignal = TimeSellLimit;
			_timeSellStopSignal = TimeSellStop;
			_timeBuyLimitSignal = TimeBuyLimit;
		}
	}

	private void HandleTimedEntries(ICandleMessage candle)
	{
		if (!UseTime)
			return;

		var openTime = candle.OpenTime;

		if (_timeBuySignal && CanOpen(true) && openTime != _lastBuyEntryCandle)
		{
			var volume = TradeVolume + (Position < 0m ? Math.Abs(Position) : 0m);
			if (volume > 0m)
			{
				BuyMarket(volume);
				_lastBuyEntryCandle = openTime;
			}
		}

		if (_timeSellSignal && CanOpen(false) && openTime != _lastSellEntryCandle)
		{
			var volume = TradeVolume + (Position > 0m ? Math.Abs(Position) : 0m);
			if (volume > 0m)
			{
				SellMarket(volume);
				_lastSellEntryCandle = openTime;
			}
		}
	}

	private void PlacePendingOrders(ICandleMessage candle)
	{
		var closePrice = candle.ClosePrice;
		var canOpenLong = CanOpen(true);
		var canOpenShort = CanOpen(false);

		if (_buyLimitOrder == null && canOpenLong && ShouldPlaceLimit(true))
		{
			var price = NormalizePrice(closePrice - PointsToPrice(LimitOrderOffsetPoints));
			if (price > 0m)
				_buyLimitOrder = BuyLimit(price);
		}
		else if (_buyLimitOrder != null && _buyLimitOrder.State == OrderStates.Active && LimitOrderTrailingStopPoints > 0m && LimitOrderTrailingStepPoints > 0m)
		{
			var trigger = PointsToPrice(LimitOrderTrailingStopPoints + LimitOrderTrailingStepPoints);
			if (closePrice > _buyLimitOrder.Price + trigger)
			{
				_pendingBuyLimitPrice = closePrice - PointsToPrice(LimitOrderTrailingStopPoints);
				CancelOrder(_buyLimitOrder);
			}
		}

		if (_sellLimitOrder == null && canOpenShort && ShouldPlaceLimit(false))
		{
			var price = NormalizePrice(closePrice + PointsToPrice(LimitOrderOffsetPoints));
			if (price > 0m)
				_sellLimitOrder = SellLimit(price);
		}
		else if (_sellLimitOrder != null && _sellLimitOrder.State == OrderStates.Active && LimitOrderTrailingStopPoints > 0m && LimitOrderTrailingStepPoints > 0m)
		{
			var trigger = PointsToPrice(LimitOrderTrailingStopPoints + LimitOrderTrailingStepPoints);
			if (closePrice < _sellLimitOrder.Price - trigger)
			{
				_pendingSellLimitPrice = closePrice + PointsToPrice(LimitOrderTrailingStopPoints);
				CancelOrder(_sellLimitOrder);
			}
		}

		if (_buyStopOrder == null && canOpenLong && ShouldPlaceStop(true))
		{
			var price = NormalizePrice(closePrice + PointsToPrice(StopOrderOffsetPoints));
			if (price > 0m)
				_buyStopOrder = BuyStop(price);
		}
		else if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active && StopOrderTrailingStopPoints > 0m && StopOrderTrailingStepPoints > 0m)
		{
			var trigger = PointsToPrice(StopOrderTrailingStopPoints + StopOrderTrailingStepPoints);
			if (closePrice < _buyStopOrder.Price - trigger)
			{
				_pendingBuyStopPrice = closePrice + PointsToPrice(StopOrderTrailingStopPoints);
				CancelOrder(_buyStopOrder);
			}
		}

		if (_sellStopOrder == null && canOpenShort && ShouldPlaceStop(false))
		{
			var price = NormalizePrice(closePrice - PointsToPrice(StopOrderOffsetPoints));
			if (price > 0m)
				_sellStopOrder = SellStop(price);
		}
		else if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active && StopOrderTrailingStopPoints > 0m && StopOrderTrailingStepPoints > 0m)
		{
			var trigger = PointsToPrice(StopOrderTrailingStopPoints + StopOrderTrailingStepPoints);
			if (closePrice > _sellStopOrder.Price + trigger)
			{
				_pendingSellStopPrice = closePrice - PointsToPrice(StopOrderTrailingStopPoints);
				CancelOrder(_sellStopOrder);
			}
		}
	}

	private bool ShouldPlaceLimit(bool isBuy)
	{
		if (PointsToPrice(LimitOrderOffsetPoints) < _minStopDistance)
			return false;

		return isBuy
			? (AllowBuyLimit || _timeBuyLimitSignal)
			: (AllowSellLimit || _timeSellLimitSignal);
	}

	private bool ShouldPlaceStop(bool isBuy)
	{
		if (PointsToPrice(StopOrderOffsetPoints) < _minStopDistance)
			return false;

		return isBuy
			? (AllowBuyStop || _timeBuyStopSignal)
			: (AllowSellStop || _timeSellStopSignal);
	}

	private void ApplyScalping(ICandleMessage candle)
	{
		if (ScalpProfitPoints <= 0m || Position == 0m || PositionPrice <= 0m)
			return;

		var target = PointsToPrice(ScalpProfitPoints);
		if (target <= 0m)
			return;

		if (Position > 0m && candle.ClosePrice >= PositionPrice + target)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0m && candle.ClosePrice <= PositionPrice - target)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	private void UpdateMarketProtection(ICandleMessage candle)
	{
		if (Position == 0m)
		{
			CancelAndResetProtection();
			return;
		}

		var volume = Math.Abs(Position);
		var entryPrice = PositionPrice;
		if (entryPrice <= 0m || volume <= 0m)
			return;

		var closePrice = candle.ClosePrice;
		var stopDistance = _overrideStopDistance ?? PointsToPrice(MarketStopLossPoints);
		var takeDistance = _overrideTakeDistance ?? PointsToPrice(MarketTakeProfitPoints);
		var trailingDistance = PointsToPrice(MarketTrailingStopPoints);
		var trailingStep = PointsToPrice(MarketTrailingStepPoints);

		decimal? desiredStop;
		decimal? desiredTake;
		Sides closeSide;

		if (Position > 0m)
		{
			closeSide = Sides.Sell;
			desiredTake = takeDistance > 0m ? entryPrice + takeDistance : (decimal?)null;
			desiredStop = stopDistance > 0m ? entryPrice - stopDistance : (decimal?)null;

			if (trailingDistance > 0m)
			{
				var candidate = closePrice - trailingDistance;
				var allowMove = !WaitForProfit || closePrice - entryPrice >= trailingDistance;
				if (allowMove)
				{
					if (!_marketStopPrice.HasValue || candidate - _marketStopPrice.Value >= (trailingStep > 0m ? trailingStep : _priceStep))
						desiredStop = candidate;
					else if (_marketStopPrice.HasValue)
						desiredStop = _marketStopPrice;
				}
			}
		}
		else
		{
			closeSide = Sides.Buy;
			desiredTake = takeDistance > 0m ? entryPrice - takeDistance : (decimal?)null;
			desiredStop = stopDistance > 0m ? entryPrice + stopDistance : (decimal?)null;

			if (trailingDistance > 0m)
			{
				var candidate = closePrice + trailingDistance;
				var allowMove = !WaitForProfit || entryPrice - closePrice >= trailingDistance;
				if (allowMove)
				{
					if (!_marketStopPrice.HasValue || _marketStopPrice.Value - candidate >= (trailingStep > 0m ? trailingStep : _priceStep))
						desiredStop = candidate;
					else if (_marketStopPrice.HasValue)
						desiredStop = _marketStopPrice;
				}
			}
		}

		UpdateProtectiveOrder(closeSide, volume, desiredStop, desiredTake);
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade?.Order == null)
			return;

		if (trade.Order.State == OrderStates.Active || trade.Order.Balance > 0)
			return;

		if (trade.Order == _buyLimitOrder || trade.Order == _sellLimitOrder)
		{
			SetOverrideDistances(LimitOrderStopLossPoints, LimitOrderTakeProfitPoints);
		}
		else if (trade.Order == _buyStopOrder || trade.Order == _sellStopOrder)
		{
			SetOverrideDistances(StopOrderStopLossPoints, StopOrderTakeProfitPoints);
		}
		else
		{
			SetOverrideDistances(MarketStopLossPoints, MarketTakeProfitPoints);
		}
	}

	private void CancelAndResetProtection()
	{
		if (_marketStopOrder != null && _marketStopOrder.State == OrderStates.Active)
			CancelOrder(_marketStopOrder);

		if (_marketTakeProfitOrder != null && _marketTakeProfitOrder.State == OrderStates.Active)
			CancelOrder(_marketTakeProfitOrder);

		_marketStopOrder = null;
		_marketTakeProfitOrder = null;
		_pendingStopPrice = null;
		_pendingTakeProfitPrice = null;
		_pendingStopSide = null;
		_pendingTakeProfitSide = null;
		_pendingStopVolume = 0m;
		_pendingTakeProfitVolume = 0m;
		_marketStopPrice = null;
		_marketTakeProfitPrice = null;
		_overrideStopDistance = null;
		_overrideTakeDistance = null;
	}

	private void UpdateProtectiveOrder(Sides closeSide, decimal volume, decimal? stopPrice, decimal? takePrice)
	{
		if (stopPrice.HasValue)
		{
			var normalized = NormalizePrice(stopPrice.Value);
			if (_marketStopOrder == null)
			{
				_marketStopOrder = closeSide == Sides.Sell
					? SellStop(volume, normalized)
					: BuyStop(volume, normalized);
				_marketStopPrice = normalized;
			}
			else if (_marketStopOrder.State == OrderStates.Active)
			{
				var needsUpdate = Math.Abs(_marketStopOrder.Price - normalized) >= _priceStep || _marketStopOrder.Volume != volume;
				if (needsUpdate)
				{
					_pendingStopPrice = normalized;
					_pendingStopSide = closeSide;
					_pendingStopVolume = volume;
					CancelOrder(_marketStopOrder);
				}
			}
		}
		else if (_marketStopOrder != null && _marketStopOrder.State == OrderStates.Active)
		{
			CancelOrder(_marketStopOrder);
		}

		if (takePrice.HasValue)
		{
			var normalized = NormalizePrice(takePrice.Value);
			if (_marketTakeProfitOrder == null)
			{
				_marketTakeProfitOrder = closeSide == Sides.Sell
					? SellLimit(volume, normalized)
					: BuyLimit(volume, normalized);
				_marketTakeProfitPrice = normalized;
			}
			else if (_marketTakeProfitOrder.State == OrderStates.Active)
			{
				var needsUpdate = Math.Abs(_marketTakeProfitOrder.Price - normalized) >= _priceStep || _marketTakeProfitOrder.Volume != volume;
				if (needsUpdate)
				{
					_pendingTakeProfitPrice = normalized;
					_pendingTakeProfitSide = closeSide;
					_pendingTakeProfitVolume = volume;
					CancelOrder(_marketTakeProfitOrder);
				}
			}
		}
		else if (_marketTakeProfitOrder != null && _marketTakeProfitOrder.State == OrderStates.Active)
		{
			CancelOrder(_marketTakeProfitOrder);
		}
	}

	private void SetOverrideDistances(decimal stopPoints, decimal takePoints)
	{
		var stop = PointsToPrice(stopPoints);
		var take = PointsToPrice(takePoints);
		_overrideStopDistance = stop > 0m ? stop : (decimal?)null;
		_overrideTakeDistance = take > 0m ? take : (decimal?)null;
	}

	private void UpdateGlobalLevels()
	{
		if (!UseGlobalLevels)
			return;

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
			return;

		if (_initialBalance <= 0m)
			_initialBalance = equity;

		var targetProfit = _initialBalance * (1m + GlobalTakeProfitPercent / 100m);
		var targetLoss = _initialBalance * (1m - GlobalStopLossPercent / 100m);

		if (!_takeProfitNotified && GlobalTakeProfitPercent > 0m && equity >= targetProfit)
		{
			LogInfo($"Equity increased by {GlobalTakeProfitPercent}% (current {equity}).");
			_takeProfitNotified = true;
		}

		if (!_stopLossNotified && GlobalStopLossPercent > 0m && equity <= targetLoss)
		{
			LogInfo($"Equity decreased by {GlobalStopLossPercent}% (current {equity}).");
			_stopLossNotified = true;
		}
	}

	private bool CanOpen(bool isLong)
	{
		if (!WaitClose)
			return true;

		var max = MaxMarketPositions;
		if (max <= 0)
			return true;

		return GetOpenCount(isLong) < max;
	}

	private int GetOpenCount(bool isLong)
	{
		if (TradeVolume <= 0m)
			return 0;

		var pos = Position;
		if (isLong)
		{
			if (pos <= 0m)
				return 0;

			return (int)decimal.Round(pos / TradeVolume, MidpointRounding.AwayFromZero);
		}

		if (pos >= 0m)
			return 0;

		return (int)decimal.Round(Math.Abs(pos) / TradeVolume, MidpointRounding.AwayFromZero);
	}

	private decimal PointsToPrice(decimal points)
	{
		return points * _priceStep;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceStep <= 0m)
			return price;

		return Math.Round(price / _priceStep, MidpointRounding.AwayFromZero) * _priceStep;
	}
}
