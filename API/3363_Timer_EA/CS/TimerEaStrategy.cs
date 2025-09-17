using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that schedules order placement and liquidation at predefined timestamps.
/// Supports market, stop and limit entries with optional trailing and break-even logic.
/// </summary>
public class TimerEaStrategy : Strategy
{
	/// <summary>
	/// Entry order type used at the scheduled moment.
	/// </summary>
	public enum TimerOrderMode
	{
		/// <summary>Place market orders.</summary>
		Market,
		/// <summary>Place stop pending orders.</summary>
		PendingStop,
		/// <summary>Place limit pending orders.</summary>
		PendingLimit
	}

	/// <summary>
	/// Position sizing logic that mimics the original MQL configuration.
	/// </summary>
	public enum LotSizingMode
	{
		/// <summary>Use fixed volume defined by the user.</summary>
		ManualLot,
		/// <summary>Risk based position size derived from portfolio balance.</summary>
		BalanceRisk
	}

	private readonly StrategyParam<TimerOrderMode> _orderMode;
	private readonly StrategyParam<bool> _openBuy;
	private readonly StrategyParam<bool> _openSell;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenSteps;
	private readonly StrategyParam<decimal> _pendingDistanceSteps;
	private readonly StrategyParam<int> _expirationMinutes;
	private readonly StrategyParam<bool> _cancelPendingOnClose;
	private readonly StrategyParam<LotSizingMode> _lotSizing;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _manualVolume;
	private readonly StrategyParam<DateTimeOffset> _openTime;
	private readonly StrategyParam<DateTimeOffset> _closeTime;
	private readonly StrategyParam<DataType> _candleType;

	private bool _entryTriggered;
	private bool _closeTriggered;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private Order? _buyPendingOrder;
	private Order? _sellPendingOrder;
	private DateTimeOffset? _buyExpiry;
	private DateTimeOffset? _sellExpiry;

	/// <summary>
	/// Initializes a new instance of the <see cref="TimerEaStrategy"/> class.
	/// </summary>
	public TimerEaStrategy()
	{
		_orderMode = Param(nameof(OrderMode), TimerOrderMode.Market)
			.SetDisplay("Order Mode", "Type of order submitted at the scheduled moment", "Entries");

		_openBuy = Param(nameof(OpenBuy), false)
			.SetDisplay("Open Buy", "Enable long entries at the scheduled time", "Entries");

		_openSell = Param(nameof(OpenSell), false)
			.SetDisplay("Open Sell", "Enable short entries at the scheduled time", "Entries");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 10m)
			.SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk");

		_stopLossSteps = Param(nameof(StopLossSteps), 10m)
			.SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing", "Enable trailing stop adjustments", "Risk");

		_trailingStep = Param(nameof(TrailingStep), 1m)
			.SetDisplay("Trailing Step", "Additional step before the stop is trailed", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), false)
			.SetDisplay("Break Even", "Allow trailing logic to respect break-even threshold", "Risk");

		_breakEvenSteps = Param(nameof(BreakEvenSteps), 10m)
			.SetDisplay("Break Even Steps", "Extra profit distance required before trailing", "Risk");

		_pendingDistanceSteps = Param(nameof(PendingDistanceSteps), 10m)
			.SetDisplay("Pending Distance", "Distance from market price for pending entries", "Entries");

		_expirationMinutes = Param(nameof(ExpirationMinutes), 60)
			.SetDisplay("Expiration (min)", "Lifetime for pending orders (0 keeps them active)", "Entries");

		_cancelPendingOnClose = Param(nameof(CancelPendingOnClose), true)
			.SetDisplay("Cancel Pending", "Cancel pending orders when the close time is reached", "Exits");

		_lotSizing = Param(nameof(LotSizing), LotSizingMode.ManualLot)
			.SetDisplay("Lot Sizing", "How to derive order volume", "Money Management");

		_riskFactor = Param(nameof(RiskFactor), 1m)
			.SetDisplay("Risk Factor", "Multiplier for balance based position sizing", "Money Management");

		_manualVolume = Param(nameof(ManualVolume), 1m)
			.SetDisplay("Manual Volume", "Fixed volume used for entries", "Money Management");

		_openTime = Param(nameof(OpenTime), DateTimeOffset.MinValue)
			.SetDisplay("Open Time", "Timestamp that triggers order placement", "Schedule");

		_closeTime = Param(nameof(CloseTime), DateTimeOffset.MaxValue)
			.SetDisplay("Close Time", "Timestamp that forces position exit", "Schedule");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used to evaluate the schedule", "Schedule");

		Volume = 1m;
	}

	/// <summary>
	/// Selected order placement mode.
	/// </summary>
	public TimerOrderMode OrderMode
	{
		get => _orderMode.Value;
		set => _orderMode.Value = value;
	}

	/// <summary>
	/// Flag that enables long entries at the scheduled time.
	/// </summary>
	public bool OpenBuy
	{
		get => _openBuy.Value;
		set => _openBuy.Value = value;
	}

	/// <summary>
	/// Flag that enables short entries at the scheduled time.
	/// </summary>
	public bool OpenSell
	{
		get => _openSell.Value;
		set => _openSell.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Enables trailing stop adjustments.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Additional distance that must be covered before moving the trailing stop.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Enables break-even protection during trailing.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Extra profit requirement before trailing resumes.
	/// </summary>
	public decimal BreakEvenSteps
	{
		get => _breakEvenSteps.Value;
		set => _breakEvenSteps.Value = value;
	}

	/// <summary>
	/// Distance used when placing pending orders.
	/// </summary>
	public decimal PendingDistanceSteps
	{
		get => _pendingDistanceSteps.Value;
		set => _pendingDistanceSteps.Value = value;
	}

	/// <summary>
	/// Expiration in minutes for pending orders.
	/// </summary>
	public int ExpirationMinutes
	{
		get => _expirationMinutes.Value;
		set => _expirationMinutes.Value = value;
	}

	/// <summary>
	/// Cancels pending orders once the scheduled close time is reached.
	/// </summary>
	public bool CancelPendingOnClose
	{
		get => _cancelPendingOnClose.Value;
		set => _cancelPendingOnClose.Value = value;
	}

	/// <summary>
	/// Position sizing logic selector.
	/// </summary>
	public LotSizingMode LotSizing
	{
		get => _lotSizing.Value;
		set => _lotSizing.Value = value;
	}

	/// <summary>
	/// Multiplier used when risk based sizing is selected.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Fixed volume submitted when <see cref="LotSizingMode.ManualLot"/> is active.
	/// </summary>
	public decimal ManualVolume
	{
		get => _manualVolume.Value;
		set => _manualVolume.Value = value;
	}

	/// <summary>
	/// Timestamp that triggers order placement.
	/// </summary>
	public DateTimeOffset OpenTime
	{
		get => _openTime.Value;
		set => _openTime.Value = value;
	}

	/// <summary>
	/// Timestamp that triggers position liquidation.
	/// </summary>
	public DateTimeOffset CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}

	/// <summary>
	/// Candle data type used for the schedule.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_entryTriggered = false;
		_closeTriggered = false;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_buyPendingOrder = null;
		_sellPendingOrder = null;
		_buyExpiry = null;
		_sellExpiry = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		CancelExpiredPending(candle.CloseTime);

		var openTrigger = TruncateToMinute(OpenTime);
		var closeTrigger = TruncateToMinute(CloseTime);

		if (!_entryTriggered && candle.CloseTime >= openTrigger && candle.CloseTime < openTrigger + TimeSpan.FromMinutes(1))
		{
			_entryTriggered = SubmitEntries(candle);
		}

		ManageProtection(candle);

		if (!_closeTriggered && candle.CloseTime >= closeTrigger && candle.CloseTime < closeTrigger + TimeSpan.FromMinutes(1))
		{
			CloseScheduled();
		}
	}

	private bool SubmitEntries(ICandleMessage candle)
	{
		var volume = GetOrderVolume();
		if (volume <= 0m)
			return false;

		var submitted = false;
		var distance = GetOffset(PendingDistanceSteps);
		var expiration = ExpirationMinutes > 0
			? candle.CloseTime + TimeSpan.FromMinutes(ExpirationMinutes)
			: (DateTimeOffset?)null;

		switch (OrderMode)
		{
			case TimerOrderMode.Market:
			{
				if (OpenBuy)
				{
					BuyMarket(volume);
					submitted = true;
				}

				if (OpenSell)
				{
					SellMarket(volume);
					submitted = true;
				}

				break;
			}

			case TimerOrderMode.PendingStop:
			{
				if (OpenBuy && _buyPendingOrder == null)
				{
					var price = NormalizePrice(candle.ClosePrice + distance);
					var order = BuyStop(volume, price);
					if (order != null)
					{
						_buyPendingOrder = order;
						_buyExpiry = expiration;
						submitted = true;
					}
				}

				if (OpenSell && _sellPendingOrder == null)
				{
					var price = NormalizePrice(candle.ClosePrice - distance);
					var order = SellStop(volume, price);
					if (order != null)
					{
						_sellPendingOrder = order;
						_sellExpiry = expiration;
						submitted = true;
					}
				}

				break;
			}

			case TimerOrderMode.PendingLimit:
			{
				if (OpenBuy && _buyPendingOrder == null)
				{
					var price = NormalizePrice(candle.ClosePrice - distance);
					var order = BuyLimit(volume, price);
					if (order != null)
					{
						_buyPendingOrder = order;
						_buyExpiry = expiration;
						submitted = true;
					}
				}

				if (OpenSell && _sellPendingOrder == null)
				{
					var price = NormalizePrice(candle.ClosePrice + distance);
					var order = SellLimit(volume, price);
					if (order != null)
					{
						_sellPendingOrder = order;
						_sellExpiry = expiration;
						submitted = true;
					}
				}

				break;
			}
		}

		return submitted;
	}

	private void ManageProtection(ICandleMessage candle)
	{
		if (_entryPrice == null)
			return;

		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				return;
			}

			if (_takeProfitPrice is decimal target && candle.HighPrice >= target)
			{
				SellMarket(Position);
				return;
			}

			if (UseTrailingStop && StopLossSteps > 0m && _stopPrice is decimal currentStop)
			{
				var distance = candle.ClosePrice - currentStop;
				var required = GetOffset(StopLossSteps + TrailingStep);
				if (distance > required)
				{
					var profit = candle.ClosePrice - _entryPrice.Value;
					var minProfit = GetOffset(StopLossSteps + BreakEvenSteps);
					if (!UseBreakEven || profit >= minProfit)
					{
						var newStop = NormalizePrice(candle.ClosePrice - GetOffset(StopLossSteps));
						if (newStop > currentStop)
							_stopPrice = newStop;
					}
				}
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				return;
			}

			if (_takeProfitPrice is decimal target && candle.LowPrice <= target)
			{
				BuyMarket(-Position);
				return;
			}

			if (UseTrailingStop && StopLossSteps > 0m && _stopPrice is decimal currentStop)
			{
				var distance = currentStop - candle.ClosePrice;
				var required = GetOffset(StopLossSteps + TrailingStep);
				if (distance > required)
				{
					var profit = _entryPrice.Value - candle.ClosePrice;
					var minProfit = GetOffset(StopLossSteps + BreakEvenSteps);
					if (!UseBreakEven || profit >= minProfit)
					{
						var newStop = NormalizePrice(candle.ClosePrice + GetOffset(StopLossSteps));
						if (newStop < currentStop)
							_stopPrice = newStop;
					}
				}
			}
		}
	}

	private void CloseScheduled()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);

		if (CancelPendingOnClose)
			CancelPending();

		_closeTriggered = true;
	}

	private void CancelExpiredPending(DateTimeOffset currentTime)
	{
		if (_buyPendingOrder?.State == OrderStates.Active && _buyExpiry is DateTimeOffset buyExpiry && currentTime >= buyExpiry)
			CancelOrder(_buyPendingOrder);

		if (_sellPendingOrder?.State == OrderStates.Active && _sellExpiry is DateTimeOffset sellExpiry && currentTime >= sellExpiry)
			CancelOrder(_sellPendingOrder);
	}

	private void CancelPending()
	{
		if (_buyPendingOrder?.State == OrderStates.Active)
			CancelOrder(_buyPendingOrder);

		if (_sellPendingOrder?.State == OrderStates.Active)
			CancelOrder(_sellPendingOrder);
	}

	private decimal GetOrderVolume()
	{
		return LotSizing switch
		{
			LotSizingMode.ManualLot => Math.Max(0m, ManualVolume),
			LotSizingMode.BalanceRisk => CalculateRiskVolume(),
			_ => ManualVolume
		};
	}

	private decimal CalculateRiskVolume()
	{
		var portfolio = Portfolio;
		var security = Security;

		if (portfolio == null || security == null)
			return Math.Max(0m, ManualVolume);

		var balance = portfolio.CurrentValue;
		var lotSize = security.LotSize > 0m ? security.LotSize : 1m;
		var volume = (balance / lotSize) * RiskFactor;
		return Math.Max(0m, volume);
	}

	private decimal GetOffset(decimal steps)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		return steps * step;
	}

	private decimal NormalizePrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	private static DateTimeOffset TruncateToMinute(DateTimeOffset value)
	{
		return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Offset);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position == 0)
			return;

		if (Position > 0 && trade.Order?.Direction == Sides.Buy)
		{
			_entryPrice = PositionPrice ?? trade.Trade.Price;
			SetupProtection(true);
		}
		else if (Position < 0 && trade.Order?.Direction == Sides.Sell)
		{
			_entryPrice = PositionPrice ?? trade.Trade.Price;
			SetupProtection(false);
		}
	}

	private void SetupProtection(bool isLong)
	{
		if (_entryPrice is not decimal entry)
			return;

		var stopDistance = StopLossSteps > 0m ? GetOffset(StopLossSteps) : 0m;
		var takeDistance = TakeProfitSteps > 0m ? GetOffset(TakeProfitSteps) : 0m;

		if (isLong)
		{
			_stopPrice = stopDistance > 0m ? NormalizePrice(entry - stopDistance) : null;
			_takeProfitPrice = takeDistance > 0m ? NormalizePrice(entry + takeDistance) : null;
		}
		else
		{
			_stopPrice = stopDistance > 0m ? NormalizePrice(entry + stopDistance) : null;
			_takeProfitPrice = takeDistance > 0m ? NormalizePrice(entry - takeDistance) : null;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			_entryPrice = null;
			_stopPrice = null;
			_takeProfitPrice = null;
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == _buyPendingOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_buyPendingOrder = null;
			_buyExpiry = null;
		}

		if (order == _sellPendingOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_sellPendingOrder = null;
			_sellExpiry = null;
		}
	}
}
