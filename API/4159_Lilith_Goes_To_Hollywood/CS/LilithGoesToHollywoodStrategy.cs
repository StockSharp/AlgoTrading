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
/// Hedging grid strategy converted from the "Lilith goes to Hollywood" MetaTrader expert.
/// It alternates between automated Parabolic SAR triggers and semi-manual pending order management.
/// </summary>
public class LilithGoesToHollywoodStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarAcceleration;
	private readonly StrategyParam<decimal> _sarMaxAcceleration;

	private readonly StrategyParam<bool> _automated;
	private readonly StrategyParam<decimal> _priceUp;
	private readonly StrategyParam<decimal> _priceDown;
	private readonly StrategyParam<int> _anchorSteps;
	private readonly StrategyParam<decimal> _manualVolume;
	private readonly StrategyParam<decimal> _xFactor;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _longVolume;
	private decimal _longAveragePrice;
	private decimal _shortVolume;
	private decimal _shortAveragePrice;
	private decimal _pendingBuyVolume;
	private decimal _pendingSellVolume;
	private decimal _focusPrice;
	private decimal _targetProfit;
	private decimal _currentProfit;

	private Order _manualBuyStopOrder;
	private Order _manualBuyLimitOrder;
	private Order _manualSellStopOrder;
	private Order _manualSellLimitOrder;
	private Order _coreBuyStopOrder;
	private Order _coreSellStopOrder;

	public LilithGoesToHollywoodStrategy()
	{
		_sarAcceleration = Param(nameof(SarAcceleration), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Acceleration", "Base acceleration used by the Parabolic SAR trigger.", "Indicators");

		_sarMaxAcceleration = Param(nameof(SarMaxAcceleration), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max Acceleration", "Maximum acceleration used by the Parabolic SAR trigger.", "Indicators");

		_automated = Param(nameof(Automated), true)
			.SetDisplay("Automated mode", "When enabled the strategy reacts to the Parabolic SAR signal and opens market positions.", "General");

		_priceUp = Param(nameof(PriceUp), 1.37001m)
			.SetDisplay("Manual buy price", "Price level used for manual buy stop/limit orders.", "Manual mode");

		_priceDown = Param(nameof(PriceDown), 1.36501m)
			.SetDisplay("Manual sell price", "Price level used for manual sell stop/limit orders.", "Manual mode");

		_anchorSteps = Param(nameof(AnchorSteps), 250)
			.SetGreaterThanZero()
			.SetDisplay("Anchor distance (steps)", "Distance expressed in price steps used to project recovery orders around the focus price.", "Risk");

		_manualVolume = Param(nameof(ManualVolume), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Manual volume", "Lot size used when the automated sizing rule is not applicable.", "Risk");

		_xFactor = Param(nameof(XFactor), 1.8m)
			.SetGreaterThanZero()
			.SetDisplay("Volume multiplier", "Coefficient used to oversize recovery orders compared to the opposing exposure.", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Drawdown lock percent", "Maximum floating loss expressed as a percentage of the account balance before hedging is enforced.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Time-frame used to drive indicator updates and management logic.", "General");
	}

	/// <summary>
	/// Enables or disables the automated SAR driven entries.
	/// </summary>
	public bool Automated
	{
		get => _automated.Value;
		set => _automated.Value = value;
	}

	/// <summary>
	/// Manual entry price for buy orders.
	/// </summary>
	public decimal PriceUp
	{
		get => _priceUp.Value;
		set => _priceUp.Value = value;
	}

	/// <summary>
	/// Manual entry price for sell orders.
	/// </summary>
	public decimal PriceDown
	{
		get => _priceDown.Value;
		set => _priceDown.Value = value;
	}

	/// <summary>
	/// Distance in price steps applied around the focus price when arming recovery orders.
	/// </summary>
	public int AnchorSteps
	{
		get => _anchorSteps.Value;
		set => _anchorSteps.Value = value;
	}

	/// <summary>
	/// Default lot size used in manual mode or as a fallback when the dynamic sizing rule yields zero.
	/// </summary>
	public decimal ManualVolume
	{
		get => _manualVolume.Value;
		set => _manualVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the opposing exposure when preparing recovery orders.
	/// </summary>
	public decimal XFactor
	{
		get => _xFactor.Value;
		set => _xFactor.Value = value;
	}

	/// <summary>
	/// Percentage of the account value allowed as floating loss before hedging orders are deployed.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Candle type used to run indicator updates and management decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Acceleration factor applied to the Parabolic SAR indicator.
	/// </summary>
	public decimal SarAcceleration
	{
		get => _sarAcceleration.Value;
		set => _sarAcceleration.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor used by the Parabolic SAR indicator.
	/// </summary>
	public decimal SarMaxAcceleration
	{
		get => _sarMaxAcceleration.Value;
		set => _sarMaxAcceleration.Value = value;
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

		_longVolume = 0m;
		_longAveragePrice = 0m;
		_shortVolume = 0m;
		_shortAveragePrice = 0m;
		_pendingBuyVolume = 0m;
		_pendingSellVolume = 0m;
		_focusPrice = 0m;
		_targetProfit = 0m;
		_currentProfit = 0m;

		_manualBuyStopOrder = null;
		_manualBuyLimitOrder = null;
		_manualSellStopOrder = null;
		_manualSellLimitOrder = null;
		_coreBuyStopOrder = null;
		_coreSellStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = AdjustVolume(ManualVolume);

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarAcceleration,
			AccelerationMax = SarMaxAcceleration
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTargetProfit();
		UpdateOrderStatistics();
		UpdateProfit(candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_longVolume == 0m && _shortVolume == 0m)
		{
			HandleEntry(candle, sarValue);
		}
		else
		{
			HandleManagement(candle.ClosePrice);
		}
	}

	private void HandleEntry(ICandleMessage candle, decimal sarValue)
	{
		var currentPrice = candle.ClosePrice;

		if (Automated)
		{
			var volume = GetAutomatedVolume();
			if (volume <= 0m)
				return;

			// Automated mode mirrors the MT4 logic: open a buy when price is above SAR and a sell otherwise.
			if (currentPrice >= sarValue)
			{
				BuyMarket(volume);
				LogInfo($"Opening long position at {currentPrice} against SAR value {sarValue}.");
			}
			else if (currentPrice <= sarValue)
			{
				SellMarket(volume);
				LogInfo($"Opening short position at {currentPrice} against SAR value {sarValue}.");
			}
		}
		else
		{
			// Manual mode arms pending orders around the configured price levels.
			EnsureManualPendingOrders(currentPrice);
		}
	}

	private void HandleManagement(decimal currentPrice)
	{
		if (_targetProfit > 0m && _currentProfit >= _targetProfit)
		{
			LogInfo($"Profit target reached: {_currentProfit:F2} >= {_targetProfit:F2}.");
			CloseAllPositionsAndOrders();
			return;
		}

		var accountValue = GetPortfolioValue();
		if (accountValue > 0m)
		{
			var maxLoss = -accountValue * RiskPercent / 100m;
			if (_currentProfit <= maxLoss)
			{
				DeployEmergencyHedge();
			}
		}

		var anchorOffset = GetAnchorOffset();
		decimal upPrice;
		decimal downPrice;

		if (Automated && anchorOffset > 0m && _focusPrice > 0m)
		{
			upPrice = AlignPrice(_focusPrice + anchorOffset);
			downPrice = AlignPrice(Math.Max(0m, _focusPrice - anchorOffset));
		}
		else
		{
			upPrice = AlignPrice(PriceUp);
			downPrice = AlignPrice(PriceDown);
		}

		EnsureRecoveryOrders(upPrice, downPrice);
	}

	private void EnsureManualPendingOrders(decimal referencePrice)
	{
		var volume = AdjustVolume(ManualVolume);
		if (volume <= 0m)
			return;

		var buyPrice = AlignPrice(PriceUp);
		var sellPrice = AlignPrice(PriceDown);

		// Mimic the MT4 logic by submitting a single pending order per side when none exist.
		if (_pendingBuyVolume <= 0m)
		{
			if (referencePrice < buyPrice)
				SubmitBuyStop(ref _manualBuyStopOrder, buyPrice, volume);
			else if (referencePrice > buyPrice)
				SubmitBuyLimit(ref _manualBuyLimitOrder, buyPrice, volume);
		}

		if (_pendingSellVolume <= 0m)
		{
			if (referencePrice > sellPrice)
				SubmitSellStop(ref _manualSellStopOrder, sellPrice, volume);
			else if (referencePrice < sellPrice)
				SubmitSellLimit(ref _manualSellLimitOrder, sellPrice, volume);
		}
	}

	private void EnsureRecoveryOrders(decimal upPrice, decimal downPrice)
	{
		if (_shortVolume > _longVolume + _pendingBuyVolume)
		{
			var desiredVolume = _shortVolume * XFactor - _longVolume;
			SubmitBuyStop(ref _coreBuyStopOrder, upPrice, desiredVolume);
		}
		else
		{
			CancelOrder(ref _coreBuyStopOrder);
		}

		if (_longVolume > _shortVolume + _pendingSellVolume)
		{
			var desiredVolume = _longVolume * XFactor - _shortVolume;
			SubmitSellStop(ref _coreSellStopOrder, downPrice, desiredVolume);
		}
		else
		{
			CancelOrder(ref _coreSellStopOrder);
		}
	}

	private void DeployEmergencyHedge()
	{
		var excessShort = _shortVolume - (_longVolume + _pendingBuyVolume);
		if (excessShort > 0m)
		{
			var volume = AdjustVolume(excessShort);
			if (volume > 0m)
				BuyMarket(volume);
		}

		var excessLong = _longVolume - (_shortVolume + _pendingSellVolume);
		if (excessLong > 0m)
		{
			var volume = AdjustVolume(excessLong);
			if (volume > 0m)
				SellMarket(volume);
		}
	}

	private void CloseAllPositionsAndOrders()
	{
		CancelOrder(ref _manualBuyStopOrder);
		CancelOrder(ref _manualBuyLimitOrder);
		CancelOrder(ref _manualSellStopOrder);
		CancelOrder(ref _manualSellLimitOrder);
		CancelOrder(ref _coreBuyStopOrder);
		CancelOrder(ref _coreSellStopOrder);

		if (_longVolume > 0m)
			SellMarket(_longVolume);

		if (_shortVolume > 0m)
			BuyMarket(_shortVolume);
	}

	private void SubmitBuyStop(ref Order orderField, decimal price, decimal volume)
	{
		EnsureOrder(ref orderField, price, volume, (v, p) => BuyStop(v, p));
	}

	private void SubmitBuyLimit(ref Order orderField, decimal price, decimal volume)
	{
		EnsureOrder(ref orderField, price, volume, (v, p) => BuyLimit(v, p));
	}

	private void SubmitSellStop(ref Order orderField, decimal price, decimal volume)
	{
		EnsureOrder(ref orderField, price, volume, (v, p) => SellStop(v, p));
	}

	private void SubmitSellLimit(ref Order orderField, decimal price, decimal volume)
	{
		EnsureOrder(ref orderField, price, volume, (v, p) => SellLimit(v, p));
	}

	private void EnsureOrder(ref Order orderField, decimal price, decimal volume, Func<decimal, decimal, Order> factory)
	{
		price = AlignPrice(price);
		volume = AdjustVolume(volume);

		if (price <= 0m || volume <= 0m)
		{
			CancelOrder(ref orderField);
			return;
		}

		var existing = orderField;
		if (existing != null && existing.State is OrderStates.Pending or OrderStates.Active)
		{
			if (ArePricesEqual(existing.Price, price) && AreVolumesEqual(existing.Balance, volume))
				return;

			CancelOrder(existing);
		}

		orderField = factory(volume, price);
	}

	private void CancelOrder(ref Order orderField)
	{
		var order = orderField;
		if (order == null)
			return;

		if (order.State is OrderStates.Pending or OrderStates.Active)
			CancelOrder(order);

		orderField = null;
	}

	private decimal GetAutomatedVolume()
	{
		var volume = ManualVolume;
		var accountValue = GetPortfolioValue();
		if (accountValue > 0m)
		{
			var dynamicVolume = Math.Round(accountValue / 1000m, MidpointRounding.AwayFromZero) / 100m;
			if (dynamicVolume > 0m)
				volume = dynamicVolume;
		}

		return AdjustVolume(volume);
	}

	private void UpdateTargetProfit()
	{
		var accountValue = GetPortfolioValue();
		_targetProfit = accountValue > 0m ? accountValue / 1000m : 0m;
	}

	private void UpdateOrderStatistics()
	{
		_pendingBuyVolume = 0m;
		_pendingSellVolume = 0m;

		foreach (var order in ActiveOrders)
		{
			if (order.Security != Security)
				continue;

			var balance = order.Balance;
			if (balance <= 0m)
				continue;

			if (order.Direction == Sides.Buy)
				_pendingBuyVolume += balance;
			else if (order.Direction == Sides.Sell)
				_pendingSellVolume += balance;
		}
	}

	private void UpdateProfit(decimal currentPrice)
	{
		var longPnL = _longVolume <= 0m ? 0m : _longVolume * (currentPrice - _longAveragePrice);
		var shortPnL = _shortVolume <= 0m ? 0m : _shortVolume * (_shortAveragePrice - currentPrice);
		_currentProfit = longPnL + shortPnL;
	}

	private decimal GetPortfolioValue()
	{
		return Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
	}

	private decimal GetAnchorOffset()
	{
		var step = GetPriceStep();
		return AnchorSteps > 0 ? AnchorSteps * step : 0m;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}

	private decimal GetVolumeStep()
	{
		var step = Security?.VolumeStep ?? 0m;
		return step > 0m ? step : 0.00000001m;
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal AlignPrice(decimal price)
	{
		var step = GetPriceStep();
		return step > 0m ? Math.Round(price / step, MidpointRounding.AwayFromZero) * step : price;
	}

	private bool ArePricesEqual(decimal left, decimal right)
	{
		var step = GetPriceStep();
		var tolerance = step > 0m ? step / 2m : 0.0000001m;
		return Math.Abs(left - right) <= tolerance;
	}

	private bool AreVolumesEqual(decimal left, decimal right)
	{
		var step = GetVolumeStep();
		var tolerance = step > 0m ? step / 2m : 0.0000001m;
		return Math.Abs(left - right) <= tolerance;
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
				_focusPrice = price - GetAnchorOffset();
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			var closing = Math.Min(_longVolume, volume);
			if (closing > 0m)
			{
				_longVolume -= closing;
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
				_focusPrice = price + GetAnchorOffset();
			}
		}

		if (_longVolume == 0m && _shortVolume == 0m)
			_focusPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order.Security != Security)
			return;

		if (order == _manualBuyStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_manualBuyStopOrder = null;

		if (order == _manualBuyLimitOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_manualBuyLimitOrder = null;

		if (order == _manualSellStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_manualSellStopOrder = null;

		if (order == _manualSellLimitOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_manualSellLimitOrder = null;

		if (order == _coreBuyStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_coreBuyStopOrder = null;

		if (order == _coreSellStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_coreSellStopOrder = null;
	}
}
