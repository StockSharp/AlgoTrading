namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Pending order strategy driven by the DeMarker oscillator.
/// Reproduces the core behaviour of the MetaTrader expert "DeMarker Pending 2" using the StockSharp high-level API.
/// </summary>
public class DeMarkerPending2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingActivatePoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<bool> _trailingOnCloseOnly;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _minDistancePoints;
	private readonly StrategyParam<bool> _useStopOrders;
	private readonly StrategyParam<bool> _pendingOnlyOne;
	private readonly StrategyParam<bool> _pendingClosePrevious;
	private readonly StrategyParam<decimal> _pendingIndentPoints;
	private readonly StrategyParam<decimal> _pendingMaxSpreadPoints;
	private readonly StrategyParam<bool> _useTimeControl;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<int> _demarkerPeriod;
	private readonly StrategyParam<decimal> _demarkerUpperLevel;
	private readonly StrategyParam<decimal> _demarkerLowerLevel;

	private DeMarker _deMarker = null!;
	private decimal? _previousDeMarker;
	private decimal? _longStopPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfit;
	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;
	private DateTime _lastBuySignalTime;
	private DateTime _lastSellSignalTime;
	private readonly List<Order> _pendingOrders = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="DeMarkerPending2Strategy"/> class.
	/// </summary>
	public DeMarkerPending2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Working Candles", "Primary timeframe used for signals", "Data");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Default volume for pending orders", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 150m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pts)", "Initial stop-loss distance measured in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 460m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pts)", "Initial take-profit distance measured in price steps", "Risk");

		_trailingActivatePoints = Param(nameof(TrailingActivatePoints), 70m)
		.SetNotNegative()
		.SetDisplay("Trailing Activate (pts)", "Profit in points required before trailing starts", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 250m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pts)", "Distance between price and trailing stop", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 50m)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pts)", "Additional gain required to move the trailing stop", "Risk");

		_trailingOnCloseOnly = Param(nameof(TrailingOnCloseOnly), false)
		.SetDisplay("Trail On Close", "If true the trailing stop updates only on finished candles", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 5)
		.SetRange(0, 100)
		.SetDisplay("Max Positions", "Maximum number of simultaneous positions and pending orders", "Trading");

		_minDistancePoints = Param(nameof(MinDistancePoints), 150m)
		.SetNotNegative()
		.SetDisplay("Min Distance (pts)", "Minimum distance between new entries and the current position", "Trading");

		_useStopOrders = Param(nameof(UseStopOrders), true)
		.SetDisplay("Use Stop Orders", "Place stop orders when true, limit orders otherwise", "Trading");

		_pendingOnlyOne = Param(nameof(PendingOnlyOne), false)
		.SetDisplay("Single Pending", "Allow only one active pending order", "Trading");

		_pendingClosePrevious = Param(nameof(PendingClosePrevious), false)
		.SetDisplay("Replace Pendings", "Cancel previous pending orders before queuing a new one", "Trading");

		_pendingIndentPoints = Param(nameof(PendingIndentPoints), 5m)
		.SetNotNegative()
		.SetDisplay("Pending Offset (pts)", "Indent for new pending orders in price steps", "Trading");

		_pendingMaxSpreadPoints = Param(nameof(PendingMaxSpreadPoints), 12m)
		.SetNotNegative()
		.SetDisplay("Max Spread (pts)", "Maximum spread allowed when placing pending orders", "Filters");

		_useTimeControl = Param(nameof(UseTimeControl), false)
		.SetDisplay("Use Session Filter", "Restrict order placement to the configured session", "Session");

		_startHour = Param(nameof(StartHour), 10)
		.SetRange(0, 23)
		.SetDisplay("Start Hour", "Session start hour", "Session");

		_startMinute = Param(nameof(StartMinute), 1)
		.SetRange(0, 59)
		.SetDisplay("Start Minute", "Session start minute", "Session");

		_endHour = Param(nameof(EndHour), 15)
		.SetRange(0, 23)
		.SetDisplay("End Hour", "Session end hour", "Session");

		_endMinute = Param(nameof(EndMinute), 2)
		.SetRange(0, 59)
		.SetDisplay("End Minute", "Session end minute", "Session");

		_demarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("DeMarker Period", "Averaging period for the DeMarker indicator", "Indicator");

		_demarkerUpperLevel = Param(nameof(DeMarkerUpperLevel), 0.7m)
		.SetDisplay("Upper Level", "DeMarker threshold that triggers sell setups", "Indicator");

		_demarkerLowerLevel = Param(nameof(DeMarkerLowerLevel), 0.3m)
		.SetDisplay("Lower Level", "DeMarker threshold that triggers buy setups", "Indicator");
	}

	/// <summary>
	/// Working candle type for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Default volume submitted with pending orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Initial take-profit distance in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Profit required before trailing activates.
	/// </summary>
	public decimal TrailingActivatePoints
	{
		get => _trailingActivatePoints.Value;
		set => _trailingActivatePoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance measured in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Additional profit required before moving the trailing stop.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// If true, the trailing stop is updated only on closed candles.
	/// </summary>
	public bool TrailingOnCloseOnly
	{
		get => _trailingOnCloseOnly.Value;
		set => _trailingOnCloseOnly.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open positions and pending orders.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Minimum distance between the current position and a new pending order.
	/// </summary>
	public decimal MinDistancePoints
	{
		get => _minDistancePoints.Value;
		set => _minDistancePoints.Value = value;
	}

	/// <summary>
	/// If true, the strategy places stop orders; otherwise limit orders are used.
	/// </summary>
	public bool UseStopOrders
	{
		get => _useStopOrders.Value;
		set => _useStopOrders.Value = value;
	}

	/// <summary>
	/// Whether only a single pending order can stay active.
	/// </summary>
	public bool PendingOnlyOne
	{
		get => _pendingOnlyOne.Value;
		set => _pendingOnlyOne.Value = value;
	}

	/// <summary>
	/// If true, every new signal cancels previous pending orders before placing the fresh one.
	/// </summary>
	public bool PendingClosePrevious
	{
		get => _pendingClosePrevious.Value;
		set => _pendingClosePrevious.Value = value;
	}

	/// <summary>
	/// Offset applied to the current market price when computing pending order prices.
	/// </summary>
	public decimal PendingIndentPoints
	{
		get => _pendingIndentPoints.Value;
		set => _pendingIndentPoints.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in points when issuing pending orders.
	/// </summary>
	public decimal PendingMaxSpreadPoints
	{
		get => _pendingMaxSpreadPoints.Value;
		set => _pendingMaxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Enables or disables the trading session filter.
	/// </summary>
	public bool UseTimeControl
	{
		get => _useTimeControl.Value;
		set => _useTimeControl.Value = value;
	}

	/// <summary>
	/// Start hour of the trading session.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Start minute of the trading session.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// End hour of the trading session.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// End minute of the trading session.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// DeMarker averaging period.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _demarkerPeriod.Value;
		set => _demarkerPeriod.Value = value;
	}

	/// <summary>
	/// Upper DeMarker level used for sell signals.
	/// </summary>
	public decimal DeMarkerUpperLevel
	{
		get => _demarkerUpperLevel.Value;
		set => _demarkerUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower DeMarker level used for buy signals.
	/// </summary>
	public decimal DeMarkerLowerLevel
	{
		get => _demarkerLowerLevel.Value;
		set => _demarkerLowerLevel.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousDeMarker = null;
		_longStopPrice = null;
		_longTakeProfit = null;
		_shortStopPrice = null;
		_shortTakeProfit = null;
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_bestBidPrice = null;
		_bestAskPrice = null;
		_lastBuySignalTime = default;
		_lastSellSignalTime = default;
		_pendingOrders.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_deMarker = new DeMarker
		{
			Length = Math.Max(1, DeMarkerPeriod)
		};

		SubscribeLevel1()
		.Bind(OnLevel1)
		.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_deMarker, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _deMarker);
			DrawOwnTrades(area);
		}
	}

	private void OnLevel1(Level1ChangeMessage message)
	{
		_bestBidPrice = message.TryGetDecimal(Level1Fields.BestBidPrice) ?? _bestBidPrice;
		_bestAskPrice = message.TryGetDecimal(Level1Fields.BestAskPrice) ?? _bestAskPrice;
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarker)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!TrailingOnCloseOnly)
		UpdateTrailing(candle);

		RemoveInactivePendingOrders();

		if (!IsWithinSession(candle.OpenTime))
		{
			_previousDeMarker = deMarker;
			return;
		}

		if (TrailingOnCloseOnly)
		UpdateTrailing(candle);

		ManageOpenPosition(candle);

		if (Position != 0m)
		{
			_previousDeMarker = deMarker;
			return;
		}

		if (OrderVolume <= 0m)
		{
			_previousDeMarker = deMarker;
			return;
		}

		if (!TryGetSpread(out var spreadPoints))
		spreadPoints = 0m;

		if (PendingMaxSpreadPoints > 0m && spreadPoints > PendingMaxSpreadPoints)
		{
			LogInfo($"Spread {spreadPoints:0.###} exceeds the maximum {PendingMaxSpreadPoints:0.###} points. Pending order skipped.");
			_previousDeMarker = deMarker;
			return;
		}

		if (_previousDeMarker is decimal prevValue)
		{
			if (prevValue >= DeMarkerLowerLevel && deMarker < DeMarkerLowerLevel)
			TryPlacePending(Sides.Buy, candle);
			else if (prevValue <= DeMarkerUpperLevel && deMarker > DeMarkerUpperLevel)
			TryPlacePending(Sides.Sell, candle);
		}

		_previousDeMarker = deMarker;
	}

	private void TryPlacePending(Sides side, ICandleMessage candle)
	{
		var openTime = candle.OpenTime.UtcDateTime;

		if (side == Sides.Buy && _lastBuySignalTime == openTime)
		return;

		if (side == Sides.Sell && _lastSellSignalTime == openTime)
		return;

		if (PendingOnlyOne && HasActivePendingOrder())
		return;

		if (MaxPositions > 0 && CountActiveSlots() >= MaxPositions)
		return;

		if (PendingClosePrevious)
		CancelPendingOrders();

		var step = GetPointSize();
		var indent = PendingIndentPoints * step;

		var bid = _bestBidPrice ?? candle.ClosePrice;
		var ask = _bestAskPrice ?? candle.ClosePrice;

		if (bid <= 0m)
		bid = candle.ClosePrice;

		if (ask <= 0m)
		ask = candle.ClosePrice;

		var price = side == Sides.Buy ? ask : bid;

		if (UseStopOrders)
		price = side == Sides.Buy ? price + indent : price - indent;
		else
		price = side == Sides.Buy ? Math.Max(step, price - indent) : price + indent;

		if (side == Sides.Buy && price <= 0m)
		price = Math.Max(step, ask + indent);
		else if (side == Sides.Sell && price <= 0m)
		price = Math.Max(step, bid - indent);

		if (!CheckMinDistance(price))
		return;

		Order order;

		if (side == Sides.Buy)
		order = UseStopOrders ? BuyStop(OrderVolume, price) : BuyLimit(price, OrderVolume);
		else
		order = UseStopOrders ? SellStop(OrderVolume, price) : SellLimit(price, OrderVolume);

		if (order != null)
		{
			_pendingOrders.Add(order);

			if (side == Sides.Buy)
			_lastBuySignalTime = openTime;
			else
			_lastSellSignalTime = openTime;
		}
	}

	private bool CheckMinDistance(decimal pendingPrice)
	{
		if (MinDistancePoints <= 0m)
		return true;

		var step = GetPointSize();
		var minDistance = MinDistancePoints * step;

		if (Position > 0m)
		{
			var entry = PositionPrice;
			if (entry > 0m && Math.Abs(entry - pendingPrice) < minDistance)
			return false;
		}
		else if (Position < 0m)
		{
			var entry = PositionPrice;
			if (entry > 0m && Math.Abs(entry - pendingPrice) < minDistance)
			return false;
		}

		return true;
	}

	private int CountActiveSlots()
	{
		var count = 0;

		if (Math.Abs(Position) > 0m)
		count++;

		for (var i = 0; i < _pendingOrders.Count; i++)
		{
			var order = _pendingOrders[i];
			if (order.State == OrderStates.Active)
			count++;
		}

		return count;
	}

	private bool HasActivePendingOrder()
	{
		for (var i = 0; i < _pendingOrders.Count; i++)
		{
			if (_pendingOrders[i].State == OrderStates.Active)
			return true;
		}

		return false;
	}

	private void CancelPendingOrders()
	{
		for (var i = 0; i < _pendingOrders.Count; i++)
		{
			var order = _pendingOrders[i];
			if (order.State == OrderStates.Active)
			CancelOrder(order);
		}
	}

	private void RemoveInactivePendingOrders()
	{
		for (var i = _pendingOrders.Count - 1; i >= 0; i--)
		{
			var order = _pendingOrders[i];
			if (order.State == OrderStates.Done || order.State == OrderStates.Failed || order.State == OrderStates.Cancelled)
			_pendingOrders.RemoveAt(i);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longEntryPrice <= 0m)
			_longEntryPrice = PositionPrice > 0m ? PositionPrice : candle.ClosePrice;

			var step = GetPointSize();
			if (_longStopPrice == null && StopLossPoints > 0m)
			_longStopPrice = _longEntryPrice - StopLossPoints * step;

			if (_longTakeProfit == null && TakeProfitPoints > 0m)
			_longTakeProfit = _longEntryPrice + TakeProfitPoints * step;

			if (_longTakeProfit is decimal tp && candle.HighPrice >= tp)
			{
				SellMarket(Position);
				return;
			}

			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position < 0m)
		{
			if (_shortEntryPrice >= 0m)
			_shortEntryPrice = PositionPrice > 0m ? PositionPrice : candle.ClosePrice;

			var step = GetPointSize();
			if (_shortStopPrice == null && StopLossPoints > 0m)
			_shortStopPrice = _shortEntryPrice + StopLossPoints * step;

			if (_shortTakeProfit == null && TakeProfitPoints > 0m)
			_shortTakeProfit = _shortEntryPrice - TakeProfitPoints * step;

			if (_shortTakeProfit is decimal tp && candle.LowPrice <= tp)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
		}
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		var step = GetPointSize();

		if (Position > 0m && TrailingStopPoints > 0m)
		{
			if (_longEntryPrice <= 0m)
			_longEntryPrice = PositionPrice > 0m ? PositionPrice : candle.ClosePrice;

			var activate = TrailingActivatePoints * step;
			var distance = TrailingStopPoints * step;
			var stepDistance = TrailingStepPoints * step;

			if (candle.ClosePrice - _longEntryPrice >= activate)
			{
				var candidate = candle.ClosePrice - distance;

				if (_longStopPrice is not decimal currentStop || candidate > currentStop + stepDistance)
				_longStopPrice = candidate;
			}
		}
		else if (Position < 0m && TrailingStopPoints > 0m)
		{
			if (_shortEntryPrice >= 0m)
			_shortEntryPrice = PositionPrice > 0m ? PositionPrice : candle.ClosePrice;

			var activate = TrailingActivatePoints * step;
			var distance = TrailingStopPoints * step;
			var stepDistance = TrailingStepPoints * step;

			if (_shortEntryPrice - candle.ClosePrice >= activate)
			{
				var candidate = candle.ClosePrice + distance;

				if (_shortStopPrice is not decimal currentStop || candidate < currentStop - stepDistance)
				_shortStopPrice = candidate;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.Security != Security)
		return;

		if (order.State == OrderStates.Done || order.State == OrderStates.Failed || order.State == OrderStates.Cancelled)
		{
			for (var i = _pendingOrders.Count - 1; i >= 0; i--)
			{
				if (_pendingOrders[i] == order)
				_pendingOrders.RemoveAt(i);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order.Security != Security)
		return;

		if (Position == 0m)
		{
			_longEntryPrice = 0m;
			_shortEntryPrice = 0m;
			_longStopPrice = null;
			_shortStopPrice = null;
			_longTakeProfit = null;
			_shortTakeProfit = null;
			return;
		}

		if (Position > 0m)
		{
			_longEntryPrice = PositionPrice > 0m ? PositionPrice : trade.Trade.Price;
			_shortEntryPrice = 0m;
			_shortStopPrice = null;
			_shortTakeProfit = null;
		}
		else if (Position < 0m)
		{
			_shortEntryPrice = PositionPrice > 0m ? PositionPrice : trade.Trade.Price;
			_longEntryPrice = 0m;
			_longStopPrice = null;
			_longTakeProfit = null;
		}
	}

	private decimal GetPointSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 1m;
		return step;
	}

	private bool TryGetSpread(out decimal spreadPoints)
	{
		spreadPoints = 0m;

		if (_bestBidPrice is not decimal bid || _bestAskPrice is not decimal ask)
		return false;

		if (bid <= 0m || ask <= 0m || ask <= bid)
		return false;

		var step = Security?.PriceStep ?? 0m;
		if (step > 0m)
		spreadPoints = (ask - bid) / step;
		else
		spreadPoints = ask - bid;

		return true;
	}

	private bool IsWithinSession(DateTimeOffset time)
	{
		if (!UseTimeControl)
		return true;

		var start = new TimeSpan(StartHour, StartMinute, 0);
		var end = new TimeSpan(EndHour, EndMinute, 0);
		var current = time.LocalDateTime.TimeOfDay;

		if (end < start)
		return current >= start || current <= end;

		return current >= start && current <= end;
	}
}

