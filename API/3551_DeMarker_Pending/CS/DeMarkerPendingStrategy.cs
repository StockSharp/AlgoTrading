using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MetaTrader expert "DeMarker Pending 2.5".
/// Places pending orders after DeMarker crosses configurable levels.
/// Pending orders can be configured as stop or limit with optional expiration.
/// </summary>
public class DeMarkerPendingStrategy : Strategy
{
	/// <summary>
	/// Type of pending order created after a signal.
	/// </summary>
	public enum PendingMode
	{
		/// <summary>
		/// Place stop orders beyond the current market price.
		/// </summary>
		Stop,

		/// <summary>
		/// Place limit orders inside the current market price.
		/// </summary>
		Limit
	}

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _pendingIndentPoints;
	private readonly StrategyParam<int> _pendingExpirationMinutes;
	private readonly StrategyParam<PendingMode> _pendingMode;
	private readonly StrategyParam<bool> _singlePendingOnly;
	private readonly StrategyParam<bool> _replacePreviousPending;
	private readonly StrategyParam<int> _demarkerPeriod;
	private readonly StrategyParam<decimal> _demarkerUpper;
	private readonly StrategyParam<decimal> _demarkerLower;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useTimeWindow;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;

	private DeMarker _deMarker = null!;
	private decimal _priceStep;
	private Order _pendingBuy;
	private Order _pendingSell;
	private readonly Dictionary<long, DateTimeOffset> _pendingExpirations = new();
	private decimal? _previousDeMarker;
	private DateTimeOffset? _lastBuySignalTime;
	private DateTimeOffset? _lastSellSignalTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="DeMarkerPendingStrategy"/> class.
	/// </summary>
	public DeMarkerPendingStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume expressed in lots.", "Trading")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 150m)
		.SetDisplay("Stop Loss (points)", "Distance from entry price to stop loss in points.", "Risk")
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 460m)
		.SetDisplay("Take Profit (points)", "Distance from entry price to take profit in points.", "Risk")
		.SetCanOptimize(true);

		_pendingIndentPoints = Param(nameof(PendingIndentPoints), 5m)
		.SetDisplay("Pending Indent", "Offset in points between market price and pending order.", "Trading")
		.SetCanOptimize(true);

		_pendingExpirationMinutes = Param(nameof(PendingExpirationMinutes), 600)
		.SetDisplay("Pending Expiration", "Lifetime of pending orders in minutes (0 disables expiration).", "Trading")
		.SetCanOptimize(true);

		_pendingMode = Param(nameof(Mode), PendingMode.Stop)
		.SetDisplay("Pending Mode", "Choose stop or limit pending orders.", "Trading");

		_singlePendingOnly = Param(nameof(SinglePendingOnly), false)
		.SetDisplay("Single Pending", "Allow only one active pending order at a time.", "Trading");

		_replacePreviousPending = Param(nameof(ReplacePreviousPending), true)
		.SetDisplay("Replace Pending", "Remove active pendings before placing a new one.", "Trading");

		_demarkerPeriod = Param(nameof(DemarkerPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("DeMarker Period", "Averaging period for DeMarker indicator.", "Indicator")
		.SetCanOptimize(true);

		_demarkerUpper = Param(nameof(DemarkerUpperLevel), 0.7m)
		.SetDisplay("Upper Level", "DeMarker value that triggers sell setup.", "Indicator")
		.SetCanOptimize(true);

		_demarkerLower = Param(nameof(DemarkerLowerLevel), 0.3m)
		.SetDisplay("Lower Level", "DeMarker value that triggers buy setup.", "Indicator")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for signal evaluation.", "General");

		_useTimeWindow = Param(nameof(UseTimeWindow), false)
		.SetDisplay("Use Time Window", "Filter trades by intraday time window.", "Schedule");

		_startTime = Param(nameof(StartTime), new TimeSpan(10, 1, 0))
		.SetDisplay("Start Time", "Trading window start time (HH:mm).", "Schedule");

		_endTime = Param(nameof(EndTime), new TimeSpan(15, 2, 0))
		.SetDisplay("End Time", "Trading window end time (HH:mm).", "Schedule");
	}

	/// <summary>
	/// Order volume expressed in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Distance from entry price to stop loss expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Distance from entry price to take profit expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Offset in points between market price and pending order.
	/// </summary>
	public decimal PendingIndentPoints
	{
		get => _pendingIndentPoints.Value;
		set => _pendingIndentPoints.Value = value;
	}

	/// <summary>
	/// Lifetime of pending orders in minutes.
	/// </summary>
	public int PendingExpirationMinutes
	{
		get => _pendingExpirationMinutes.Value;
		set => _pendingExpirationMinutes.Value = value;
	}

	/// <summary>
	/// Pending order mode (stop or limit).
	/// </summary>
	public PendingMode Mode
	{
		get => _pendingMode.Value;
		set => _pendingMode.Value = value;
	}

	/// <summary>
	/// Allow only one pending order at a time.
	/// </summary>
	public bool SinglePendingOnly
	{
		get => _singlePendingOnly.Value;
		set => _singlePendingOnly.Value = value;
	}

	/// <summary>
	/// Remove existing pending orders before creating a new one.
	/// </summary>
	public bool ReplacePreviousPending
	{
		get => _replacePreviousPending.Value;
		set => _replacePreviousPending.Value = value;
	}

	/// <summary>
	/// DeMarker indicator period.
	/// </summary>
	public int DemarkerPeriod
	{
		get => _demarkerPeriod.Value;
		set => _demarkerPeriod.Value = value;
	}

	/// <summary>
	/// Upper DeMarker threshold that triggers sell setups.
	/// </summary>
	public decimal DemarkerUpperLevel
	{
		get => _demarkerUpper.Value;
		set => _demarkerUpper.Value = value;
	}

	/// <summary>
	/// Lower DeMarker threshold that triggers buy setups.
	/// </summary>
	public decimal DemarkerLowerLevel
	{
		get => _demarkerLower.Value;
		set => _demarkerLower.Value = value;
	}

	/// <summary>
	/// Candle type used for signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable intraday time filtering.
	/// </summary>
	public bool UseTimeWindow
	{
		get => _useTimeWindow.Value;
		set => _useTimeWindow.Value = value;
	}

	/// <summary>
	/// Start time of the intraday window.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End time of the intraday window.
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
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

		_priceStep = Security?.PriceStep ?? 0m;
		_deMarker = new DeMarker { Length = DemarkerPeriod };

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

	private void ProcessCandle(ICandleMessage candle, decimal deMarkerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		EnsurePendingReferences();
		CheckPendingExpiration(candle.CloseTime);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (UseTimeWindow && !IsWithinTimeWindow(candle.OpenTime))
			return;

		_previousDeMarker ??= deMarkerValue;

		if (Volume <= 0m)
			return;

		if (_priceStep <= 0m)
			_priceStep = Security?.PriceStep ?? 0m;

		if (deMarkerValue <= DemarkerLowerLevel && (_previousDeMarker ?? 1m) > DemarkerLowerLevel)
			{
			TryCreatePending(Sides.Buy, candle.ClosePrice, candle.OpenTime);
		}
		else if (deMarkerValue >= DemarkerUpperLevel && (_previousDeMarker ?? 0m) < DemarkerUpperLevel)
		{
			TryCreatePending(Sides.Sell, candle.ClosePrice, candle.OpenTime);
		}

		_previousDeMarker = deMarkerValue;
	}

	private void TryCreatePending(Sides direction, decimal referencePrice, DateTimeOffset signalTime)
	{
		if (ReplacePreviousPending)
			CancelPendingOrders();

		if (SinglePendingOnly && HasActivePending())
			return;

		if (direction == Sides.Buy)
			{
			if (_lastBuySignalTime == signalTime)
				return;

			_lastBuySignalTime = signalTime;
		}
		else
		{
			if (_lastSellSignalTime == signalTime)
				return;

			_lastSellSignalTime = signalTime;
		}

		var indent = PendingIndentPoints * _priceStep;
		if (indent <= 0m)
			indent = _priceStep > 0m ? _priceStep : 0m;

		if (indent <= 0m)
			return;

		decimal price;
		if (direction == Sides.Buy)
			price = Mode == PendingMode.Stop ? referencePrice + indent : referencePrice - indent;
		else
			price = Mode == PendingMode.Stop ? referencePrice - indent : referencePrice + indent;

		price = RoundPrice(price);
		if (price <= 0m)
			return;

		var stopDistance = StopLossPoints * _priceStep;
		var takeDistance = TakeProfitPoints * _priceStep;

		decimal? stopLoss = null;
		decimal? takeProfit = null;

		if (stopDistance > 0m)
			stopLoss = direction == Sides.Buy ? price - stopDistance : price + stopDistance;

		if (takeDistance > 0m)
			takeProfit = direction == Sides.Buy ? price + takeDistance : price - takeDistance;

		Order order = direction == Sides.Buy
		? (Mode == PendingMode.Stop
		? BuyStop(Volume, price, stopLoss: stopLoss, takeProfit: takeProfit)
		: BuyLimit(Volume, price, stopLoss: stopLoss, takeProfit: takeProfit))
		: (Mode == PendingMode.Stop
		? SellStop(Volume, price, stopLoss: stopLoss, takeProfit: takeProfit)
		: SellLimit(Volume, price, stopLoss: stopLoss, takeProfit: takeProfit));

		if (order == null)
			return;

		if (direction == Sides.Buy)
			_pendingBuy = order;
		else
			_pendingSell = order;

		if (PendingExpirationMinutes > 0)
			{
			_pendingExpirations[order.TransactionId] = signalTime + TimeSpan.FromMinutes(PendingExpirationMinutes);
		}
	}

	private void CancelPendingOrders()
	{
		if (_pendingBuy != null)
			{
			CancelOrder(_pendingBuy);
			_pendingExpirations.Remove(_pendingBuy.TransactionId);
			_pendingBuy = null;
		}

		if (_pendingSell != null)
			{
			CancelOrder(_pendingSell);
			_pendingExpirations.Remove(_pendingSell.TransactionId);
			_pendingSell = null;
		}
	}

	private bool HasActivePending()
	{
		var buyActive = _pendingBuy != null && _pendingBuy.State.IsActive();
		var sellActive = _pendingSell != null && _pendingSell.State.IsActive();
		return buyActive || sellActive;
	}

	private void EnsurePendingReferences()
	{
		if (_pendingBuy != null && !_pendingBuy.State.IsActive())
			{
			_pendingExpirations.Remove(_pendingBuy.TransactionId);
			_pendingBuy = null;
		}

		if (_pendingSell != null && !_pendingSell.State.IsActive())
			{
			_pendingExpirations.Remove(_pendingSell.TransactionId);
			_pendingSell = null;
		}
	}

	private void CheckPendingExpiration(DateTimeOffset time)
	{
		if (PendingExpirationMinutes <= 0)
			return;

		if (_pendingBuy != null &&
			_pendingExpirations.TryGetValue(_pendingBuy.TransactionId, out var buyExpiry) &&
			time >= buyExpiry &&
		_pendingBuy.State.IsActive())
		{
			CancelOrder(_pendingBuy);
			_pendingExpirations.Remove(_pendingBuy.TransactionId);
			_pendingBuy = null;
		}

		if (_pendingSell != null &&
			_pendingExpirations.TryGetValue(_pendingSell.TransactionId, out var sellExpiry) &&
			time >= sellExpiry &&
		_pendingSell.State.IsActive())
		{
			CancelOrder(_pendingSell);
			_pendingExpirations.Remove(_pendingSell.TransactionId);
			_pendingSell = null;
		}
	}

	private bool IsWithinTimeWindow(DateTimeOffset time)
	{
		var start = StartTime;
		var end = EndTime;
		var t = time.TimeOfDay;

		if (start <= end)
			return t >= start && t <= end;

		return t >= start || t <= end;
	}

	private decimal RoundPrice(decimal price)
	{
		if (_priceStep <= 0m)
			return price;

		return Math.Round(price / _priceStep, MidpointRounding.AwayFromZero) * _priceStep;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == null || order.Security != Security)
			return;

		if (_pendingBuy != null && order == _pendingBuy && !_pendingBuy.State.IsActive())
			{
			_pendingExpirations.Remove(_pendingBuy.TransactionId);
			_pendingBuy = null;
		}

		if (_pendingSell != null && order == _pendingSell && !_pendingSell.State.IsActive())
			{
			_pendingExpirations.Remove(_pendingSell.TransactionId);
			_pendingSell = null;
		}
	}
}
