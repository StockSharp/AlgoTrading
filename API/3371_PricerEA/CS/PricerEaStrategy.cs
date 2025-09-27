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
/// Places pending orders at manually configured price levels and manages their protective stops.
/// Reproduces the behaviour of the PricerEA expert with optional trailing stop and break-even logic.
/// </summary>
public class PricerEaStrategy : Strategy
{
	/// <summary>
	/// Volume selection mode.
	/// </summary>
	public enum LotSizingMode
	{
		/// <summary>
		/// Always use the manual volume parameter.
		/// </summary>
		Manual,

		/// <summary>
		/// Derive order volume from portfolio balance and the risk factor.
		/// </summary>
		Automatic
	}

	private readonly StrategyParam<decimal> _buyStopPrice;
	private readonly StrategyParam<decimal> _sellStopPrice;
	private readonly StrategyParam<decimal> _buyLimitPrice;
	private readonly StrategyParam<decimal> _sellLimitPrice;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _enableTrailingStop;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPoints;
	private readonly StrategyParam<int> _pendingExpiryMinutes;
	private readonly StrategyParam<LotSizingMode> _lotSizingMode;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _manualVolume;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _buyLimitOrder;
	private Order _sellLimitOrder;
	private Order _stopOrder;
	private Order _takeProfitOrder;

	private bool _stopProtectsLong;
	private decimal _stopVolume;
	private decimal _takeVolume;

	private DateTimeOffset? _buyStopExpiry;
	private DateTimeOffset? _sellStopExpiry;
	private DateTimeOffset? _buyLimitExpiry;
	private DateTimeOffset? _sellLimitExpiry;

	private decimal _bestBid;
	private decimal _bestAsk;

	/// <summary>
	/// Initializes strategy parameters that map the original expert inputs.
	/// </summary>
	public PricerEaStrategy()
	{
		_buyStopPrice = Param(nameof(BuyStopPrice), 0m)
			.SetDisplay("Buy Stop Price", "Absolute price for the buy stop entry (0 disables the order)", "Pending Orders");

		_sellStopPrice = Param(nameof(SellStopPrice), 0m)
			.SetDisplay("Sell Stop Price", "Absolute price for the sell stop entry (0 disables the order)", "Pending Orders");

		_buyLimitPrice = Param(nameof(BuyLimitPrice), 0m)
			.SetDisplay("Buy Limit Price", "Absolute price for the buy limit entry (0 disables the order)", "Pending Orders");

		_sellLimitPrice = Param(nameof(SellLimitPrice), 0m)
			.SetDisplay("Sell Limit Price", "Absolute price for the sell limit entry (0 disables the order)", "Pending Orders");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10m)
			.SetDisplay("Take Profit (points)", "Distance to take profit expressed in price points", "Protection");

		_stopLossPoints = Param(nameof(StopLossPoints), 10m)
			.SetDisplay("Stop Loss (points)", "Distance to stop loss expressed in price points", "Protection");

		_enableTrailingStop = Param(nameof(EnableTrailingStop), false)
			.SetDisplay("Enable Trailing", "Move the stop loss with the market once it advances", "Protection");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 1m)
			.SetDisplay("Trailing Step (points)", "Minimal distance required to move the trailing stop", "Protection");

		_enableBreakEven = Param(nameof(EnableBreakEven), false)
			.SetDisplay("Enable Break-Even", "Move the stop loss beyond the entry after sufficient profit", "Protection");

		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 10m)
			.SetDisplay("Break-Even Trigger (points)", "Extra profit required before the stop locks in gains", "Protection");

		_pendingExpiryMinutes = Param(nameof(PendingExpiryMinutes), 60)
			.SetDisplay("Pending Expiry (minutes)", "Lifetime of the pending orders (0 keeps them alive)", "Pending Orders");

		_lotSizingMode = Param(nameof(VolumeMode), LotSizingMode.Manual)
			.SetDisplay("Lot Sizing Mode", "Choose between manual and automatic volume", "Risk");

		_riskFactor = Param(nameof(RiskFactor), 1m)
			.SetDisplay("Risk Factor", "Multiplier applied when automatic sizing is enabled", "Risk")
			.SetCanOptimize(true);

		_manualVolume = Param(nameof(ManualVolume), 0.01m)
			.SetDisplay("Manual Volume", "Fixed order volume when manual sizing is active", "Risk")
			.SetGreaterThanZero();
	}

	/// <summary>
	/// Price for the buy stop order. Zero disables the order.
	/// </summary>
	public decimal BuyStopPrice
	{
		get => _buyStopPrice.Value;
		set => _buyStopPrice.Value = value;
	}

	/// <summary>
	/// Price for the sell stop order. Zero disables the order.
	/// </summary>
	public decimal SellStopPrice
	{
		get => _sellStopPrice.Value;
		set => _sellStopPrice.Value = value;
	}

	/// <summary>
	/// Price for the buy limit order. Zero disables the order.
	/// </summary>
	public decimal BuyLimitPrice
	{
		get => _buyLimitPrice.Value;
		set => _buyLimitPrice.Value = value;
	}

	/// <summary>
	/// Price for the sell limit order. Zero disables the order.
	/// </summary>
	public decimal SellLimitPrice
	{
		get => _sellLimitPrice.Value;
		set => _sellLimitPrice.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enables the trailing stop behaviour.
	/// </summary>
	public bool EnableTrailingStop
	{
		get => _enableTrailingStop.Value;
		set => _enableTrailingStop.Value = value;
	}

	/// <summary>
	/// Minimal price improvement required before moving the trailing stop.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Enables the break-even logic.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Additional profit (in points) required before break-even activates.
	/// </summary>
	public decimal BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	/// <summary>
	/// Lifetime of pending orders in minutes. Zero keeps them until filled or manually cancelled.
	/// </summary>
	public int PendingExpiryMinutes
	{
		get => _pendingExpiryMinutes.Value;
		set => _pendingExpiryMinutes.Value = value;
	}

	/// <summary>
	/// Volume selection mode.
	/// </summary>
	public LotSizingMode VolumeMode
	{
		get => _lotSizingMode.Value;
		set => _lotSizingMode.Value = value;
	}

	/// <summary>
	/// Risk multiplier used when automatic sizing is selected.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Manual volume used when <see cref="VolumeMode"/> equals <see cref="LotSizingMode.Manual"/>.
	/// </summary>
	public decimal ManualVolume
	{
		get => _manualVolume.Value;
		set => _manualVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		PlaceInitialPendingOrders(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		CancelOrderSafe(ref _buyStopOrder);
		CancelOrderSafe(ref _sellStopOrder);
		CancelOrderSafe(ref _buyLimitOrder);
		CancelOrderSafe(ref _sellLimitOrder);
		CancelProtectionOrders();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyStopOrder = null;
		_sellStopOrder = null;
		_buyLimitOrder = null;
		_sellLimitOrder = null;
		_stopOrder = null;
		_takeProfitOrder = null;

		_stopProtectsLong = false;
		_stopVolume = 0m;
		_takeVolume = 0m;

		_buyStopExpiry = null;
		_sellStopExpiry = null;
		_buyLimitExpiry = null;
		_sellLimitExpiry = null;

		_bestBid = 0m;
		_bestAsk = 0m;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelProtectionOrders();
			return;
		}

		EnsureProtectionOrders();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.Security != Security)
		{
			return;
		}

		if (order == _stopOrder && order.State != OrderStates.Active)
		{
			_stopOrder = null;
			_stopVolume = 0m;
		}
		else if (order == _takeProfitOrder && order.State != OrderStates.Active)
		{
			_takeProfitOrder = null;
			_takeVolume = 0m;
		}
		else if (order == _buyStopOrder && order.State != OrderStates.Active)
		{
			_buyStopOrder = null;
			_buyStopExpiry = null;
		}
		else if (order == _sellStopOrder && order.State != OrderStates.Active)
		{
			_sellStopOrder = null;
			_sellStopExpiry = null;
		}
		else if (order == _buyLimitOrder && order.State != OrderStates.Active)
		{
			_buyLimitOrder = null;
			_buyLimitExpiry = null;
		}
		else if (order == _sellLimitOrder && order.State != OrderStates.Active)
		{
			_sellLimitOrder = null;
			_sellLimitExpiry = null;
		}
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		{
			_bestBid = (decimal)bid;
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		{
			_bestAsk = (decimal)ask;
		}

		var now = message.ServerTime != default ? message.ServerTime : CurrentTime;

		if (now != default)
		{
			UpdatePendingExpiries(now);
		}

		ManageTrailingAndBreakEven();
	}

	private void PlaceInitialPendingOrders(DateTimeOffset time)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		{
			return;
		}

		if (BuyStopPrice > 0m && _buyStopOrder == null)
		{
			_buyStopOrder = BuyStop(volume, BuyStopPrice);
			_buyStopExpiry = GetExpiryTime(time);
		}

		if (SellStopPrice > 0m && _sellStopOrder == null)
		{
			_sellStopOrder = SellStop(volume, SellStopPrice);
			_sellStopExpiry = GetExpiryTime(time);
		}

		if (BuyLimitPrice > 0m && _buyLimitOrder == null)
		{
			_buyLimitOrder = BuyLimit(volume, BuyLimitPrice);
			_buyLimitExpiry = GetExpiryTime(time);
		}

		if (SellLimitPrice > 0m && _sellLimitOrder == null)
		{
			_sellLimitOrder = SellLimit(volume, SellLimitPrice);
			_sellLimitExpiry = GetExpiryTime(time);
		}
	}

	private void UpdatePendingExpiries(DateTimeOffset currentTime)
	{
		if (PendingExpiryMinutes <= 0)
		{
			return;
		}

		TryExpireOrder(ref _buyStopOrder, ref _buyStopExpiry, currentTime);
		TryExpireOrder(ref _sellStopOrder, ref _sellStopExpiry, currentTime);
		TryExpireOrder(ref _buyLimitOrder, ref _buyLimitExpiry, currentTime);
		TryExpireOrder(ref _sellLimitOrder, ref _sellLimitExpiry, currentTime);
	}

	private void ManageTrailingAndBreakEven()
	{
		if (_stopOrder == null || StopLossPoints <= 0m)
		{
			return;
		}

		var priceStep = GetPointSize();
		var stopDistance = StopLossPoints * priceStep;
		var trailingStep = TrailingStepPoints * priceStep;
		var breakEvenOffset = BreakEvenTriggerPoints * priceStep;

		if (Position > 0m && _stopProtectsLong)
		{
			var referencePrice = _bestBid;
			if (referencePrice <= 0m || PositionPrice <= 0m)
			{
				return;
			}

			if (EnableBreakEven && breakEvenOffset > 0m && _stopOrder.Price < PositionPrice)
			{
				if (referencePrice - PositionPrice >= breakEvenOffset + stopDistance)
				{
					var newStop = referencePrice - stopDistance;
					UpdateStopOrder(newStop, Math.Abs(Position), true);
				}
			}

			if (EnableTrailingStop && trailingStep > 0m)
			{
				var desired = referencePrice - stopDistance;
				if (desired > _stopOrder.Price + trailingStep)
				{
					UpdateStopOrder(desired, Math.Abs(Position), true);
				}
			}
		}
		else if (Position < 0m && !_stopProtectsLong)
		{
			var referencePrice = _bestAsk;
			if (referencePrice <= 0m || PositionPrice <= 0m)
			{
				return;
			}

			if (EnableBreakEven && breakEvenOffset > 0m && _stopOrder.Price > PositionPrice)
			{
				if (PositionPrice - referencePrice >= breakEvenOffset + stopDistance)
				{
					var newStop = referencePrice + stopDistance;
					UpdateStopOrder(newStop, Math.Abs(Position), false);
				}
			}

			if (EnableTrailingStop && trailingStep > 0m)
			{
				var desired = referencePrice + stopDistance;
				if (desired < _stopOrder.Price - trailingStep)
				{
					UpdateStopOrder(desired, Math.Abs(Position), false);
				}
			}
		}
	}

	private void EnsureProtectionOrders()
	{
		if (Position > 0m)
		{
			EnsureLongProtection();
		}
		else if (Position < 0m)
		{
			EnsureShortProtection();
		}
	}

	private void EnsureLongProtection()
	{
		var volume = Math.Abs(Position);
		var entry = PositionPrice;
		if (entry <= 0m)
		{
			return;
		}

		if (StopLossPoints > 0m)
		{
			var stopPrice = entry - StopLossPoints * GetPointSize();
			if (_stopOrder == null || !_stopProtectsLong)
			{
				UpdateStopOrder(stopPrice, volume, true);
			}
			else if (_stopVolume != volume && _stopOrder != null)
			{
				UpdateStopOrder(_stopOrder.Price, volume, true);
			}
		}
		else
		{
			CancelStopOrder();
		}

		if (TakeProfitPoints > 0m)
		{
			var takePrice = entry + TakeProfitPoints * GetPointSize();
			if (_takeProfitOrder == null)
			{
				_takeProfitOrder = SellLimit(volume, takePrice);
				_takeVolume = volume;
			}
			else if (_takeVolume != volume && _takeProfitOrder != null)
			{
				var price = _takeProfitOrder.Price;
				CancelOrder(_takeProfitOrder);
				_takeProfitOrder = SellLimit(volume, price);
				_takeVolume = volume;
			}
		}
		else
		{
			CancelTakeProfitOrder();
		}
	}

	private void EnsureShortProtection()
	{
		var volume = Math.Abs(Position);
		var entry = PositionPrice;
		if (entry <= 0m)
		{
			return;
		}

		if (StopLossPoints > 0m)
		{
			var stopPrice = entry + StopLossPoints * GetPointSize();
			if (_stopOrder == null || _stopProtectsLong)
			{
				UpdateStopOrder(stopPrice, volume, false);
			}
			else if (_stopVolume != volume && _stopOrder != null)
			{
				UpdateStopOrder(_stopOrder.Price, volume, false);
			}
		}
		else
		{
			CancelStopOrder();
		}

		if (TakeProfitPoints > 0m)
		{
			var takePrice = entry - TakeProfitPoints * GetPointSize();
			if (_takeProfitOrder == null)
			{
				_takeProfitOrder = BuyLimit(volume, takePrice);
				_takeVolume = volume;
			}
			else if (_takeVolume != volume && _takeProfitOrder != null)
			{
				var price = _takeProfitOrder.Price;
				CancelOrder(_takeProfitOrder);
				_takeProfitOrder = BuyLimit(volume, price);
				_takeVolume = volume;
			}
		}
		else
		{
			CancelTakeProfitOrder();
		}
	}

	private void UpdateStopOrder(decimal price, decimal volume, bool protectsLong)
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		{
			CancelOrder(_stopOrder);
		}

		_stopOrder = protectsLong
		? SellStop(volume, price)
		: BuyStop(volume, price);

		_stopProtectsLong = protectsLong;
		_stopVolume = volume;
	}

	private void CancelProtectionOrders()
	{
		CancelStopOrder();
		CancelTakeProfitOrder();
	}

	private void CancelStopOrder()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		{
			CancelOrder(_stopOrder);
		}

		_stopOrder = null;
		_stopVolume = 0m;
	}

	private void CancelTakeProfitOrder()
	{
		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
		{
			CancelOrder(_takeProfitOrder);
		}

		_takeProfitOrder = null;
		_takeVolume = 0m;
	}

	private void CancelOrderSafe(ref Order order)
	{
		if (order != null && order.State == OrderStates.Active)
		{
			CancelOrder(order);
		}

		order = null;
	}

	private void TryExpireOrder(ref Order order, ref DateTimeOffset? expiry, DateTimeOffset now)
	{
		if (order == null || order.State != OrderStates.Active)
		{
			expiry = null;
			return;
		}

		if (expiry.HasValue && now >= expiry.Value)
		{
			CancelOrder(order);
			order = null;
			expiry = null;
		}
	}

	private DateTimeOffset? GetExpiryTime(DateTimeOffset reference)
	{
		if (PendingExpiryMinutes <= 0)
		{
			return null;
		}

		return reference + TimeSpan.FromMinutes(PendingExpiryMinutes);
	}

	private decimal CalculateOrderVolume()
	{
		var volume = VolumeMode == LotSizingMode.Manual
			? ManualVolume
			: CalculateAutomaticVolume();

		if (volume <= 0m)
		{
			return 0m;
		}

		var step = Security.VolumeStep > 0m ? Security.VolumeStep.Value : 1m;
		var min = Security.MinVolume > 0m ? Security.MinVolume.Value : step;
		var max = Security.MaxVolume > 0m ? Security.MaxVolume.Value : decimal.MaxValue;

		volume = AlignToStep(volume, step);
		volume = Math.Min(Math.Max(volume, min), max);

		return volume;
	}

	private decimal CalculateAutomaticVolume()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
		{
			return 0m;
		}

		var balance = portfolio.CurrentValue != 0m ? portfolio.CurrentValue : portfolio.BeginValue;
		if (balance <= 0m)
		{
			return 0m;
		}

		var contractSize = Security.ContractMultiplier > 0m ? Security.ContractMultiplier.Value : 1m;
		var volume = (balance / contractSize) * RiskFactor;

		return volume;
	}

	private decimal AlignToStep(decimal value, decimal step)
	{
		if (step <= 0m)
		{
			return value;
		}

		var steps = Math.Round(value / step, MidpointRounding.AwayFromZero);
		return steps * step;
	}

	private decimal GetPointSize()
	{
		var priceStep = Security.PriceStep;
		return priceStep > 0m ? priceStep.Value : 1m;
	}
}

