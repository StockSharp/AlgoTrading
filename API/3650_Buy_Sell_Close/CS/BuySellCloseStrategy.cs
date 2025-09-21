using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manual trading helper that mirrors the @Buy_Sell_Close MetaTrader panel.
/// Provides toggles for opening and closing positions plus a one-click stop-loss / take-profit updater.
/// </summary>
public class BuySellCloseStrategy : Strategy
{
	private readonly StrategyParam<bool> _autoMoneyManagement;
	private readonly StrategyParam<decimal> _riskPerTenThousand;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _requestBuy;
	private readonly StrategyParam<bool> _requestSell;
	private readonly StrategyParam<bool> _closeBuys;
	private readonly StrategyParam<bool> _closeSells;
	private readonly StrategyParam<bool> _closeAll;
	private readonly StrategyParam<bool> _modifyProtection;
	private readonly StrategyParam<bool> _snapshotRequest;

	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;
	private decimal _floatingBuyProfit;
	private decimal _floatingSellProfit;
	private decimal? _lastTradePrice;
	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;

	private Order? _longStopOrder;
	private Order? _longTakeOrder;
	private Order? _shortStopOrder;
	private Order? _shortTakeOrder;

	private bool _isRunning;

	/// <summary>
	/// Initializes a new instance of the <see cref="BuySellCloseStrategy"/> class.
	/// </summary>
	public BuySellCloseStrategy()
	{
		_autoMoneyManagement = Param(nameof(AutoMoneyManagement), true)
			.SetDisplay("Automatic Money Management", "Recalculate order size from balance and risk setting", "Risk");

		_riskPerTenThousand = Param(nameof(RiskPerTenThousand), 0.2m)
			.SetNotNegative()
			.SetDisplay("Risk (1/10000)", "Risk allocation expressed in ten-thousandth fractions of the balance", "Risk");

		_fixedVolume = Param(nameof(FixedVolume), 0.01m)
			.SetNotNegative()
			.SetDisplay("Fixed Volume", "Manual volume used when automatic money management is disabled", "Execution");

		_stopLossPoints = Param(nameof(StopLossPoints), 250m)
			.SetNotNegative()
			.SetDisplay("Stop-Loss Points", "Distance in MetaTrader points used for protective stops", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Take-Profit Points", "Distance in MetaTrader points used for protective targets", "Risk");

		_requestBuy = Param(nameof(OpenBuy), false)
			.SetDisplay("Open Buy", "Set to true to send a market buy order", "Controls");

		_requestSell = Param(nameof(OpenSell), false)
			.SetDisplay("Open Sell", "Set to true to send a market sell order", "Controls");

		_closeBuys = Param(nameof(CloseBuyPositions), false)
			.SetDisplay("Close Buys", "Close all open long positions", "Controls");

		_closeSells = Param(nameof(CloseSellPositions), false)
			.SetDisplay("Close Sells", "Close all open short positions", "Controls");

		_closeAll = Param(nameof(CloseAllPositions), false)
			.SetDisplay("Close Everything", "Close both long and short exposure", "Controls");

		_modifyProtection = Param(nameof(ModifyStops), false)
			.SetDisplay("Apply Stops", "Update stop-loss and take-profit orders for existing positions", "Controls");

		_snapshotRequest = Param(nameof(RefreshSnapshot), false)
			.SetDisplay("Refresh Snapshot", "Log current account, exposure and floating PnL", "Controls");
	}

	/// <summary>
	/// Enables risk based position sizing.
	/// </summary>
	public bool AutoMoneyManagement
	{
		get => _autoMoneyManagement.Value;
		set => _autoMoneyManagement.Value = value;
	}

	/// <summary>
	/// Risk allocation expressed as parts per ten thousand of the account balance.
	/// </summary>
	public decimal RiskPerTenThousand
	{
		get => _riskPerTenThousand.Value;
		set => _riskPerTenThousand.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Fixed order volume used when automatic sizing is disabled.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Stop-loss distance in MetaTrader points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Take-profit distance in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Submit a market buy order.
	/// </summary>
	public bool OpenBuy
	{
		get => _requestBuy.Value;
		set
		{
			if (_requestBuy.Value == value)
				return;

			_requestBuy.Value = value;

			if (value)
				ProcessBuyRequest();
		}
	}

	/// <summary>
	/// Submit a market sell order.
	/// </summary>
	public bool OpenSell
	{
		get => _requestSell.Value;
		set
		{
			if (_requestSell.Value == value)
				return;

			_requestSell.Value = value;

			if (value)
				ProcessSellRequest();
		}
	}

	/// <summary>
	/// Close all open buy positions.
	/// </summary>
	public bool CloseBuyPositions
	{
		get => _closeBuys.Value;
		set
		{
			if (_closeBuys.Value == value)
				return;

			_closeBuys.Value = value;

			if (value)
				ProcessCloseBuys();
		}
	}

	/// <summary>
	/// Close all open sell positions.
	/// </summary>
	public bool CloseSellPositions
	{
		get => _closeSells.Value;
		set
		{
			if (_closeSells.Value == value)
				return;

			_closeSells.Value = value;

			if (value)
				ProcessCloseSells();
		}
	}

	/// <summary>
	/// Close both buy and sell exposure.
	/// </summary>
	public bool CloseAllPositions
	{
		get => _closeAll.Value;
		set
		{
			if (_closeAll.Value == value)
				return;

			_closeAll.Value = value;

			if (value)
				ProcessCloseAll();
		}
	}

	/// <summary>
	/// Apply stop-loss and take-profit orders to open positions.
	/// </summary>
	public bool ModifyStops
	{
		get => _modifyProtection.Value;
		set
		{
			if (_modifyProtection.Value == value)
				return;

			_modifyProtection.Value = value;

			if (value)
				ProcessModifyStops();
		}
	}

	/// <summary>
	/// Request a log entry with detailed account statistics.
	/// </summary>
	public bool RefreshSnapshot
	{
		get => _snapshotRequest.Value;
		set
		{
			if (_snapshotRequest.Value == value)
				return;

			_snapshotRequest.Value = value;

			if (value)
				ProcessSnapshotRequest();
		}
	}

	/// <summary>
	/// Floating profit coming from long exposure.
	/// </summary>
	public decimal FloatingBuyProfit => _floatingBuyProfit;

	/// <summary>
	/// Floating profit coming from short exposure.
	/// </summary>
	public decimal FloatingSellProfit => _floatingSellProfit;

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);
		yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longVolume = 0m;
		_shortVolume = 0m;
		_longAveragePrice = 0m;
		_shortAveragePrice = 0m;
		_floatingBuyProfit = 0m;
		_floatingSellProfit = 0m;
		_lastTradePrice = null;
		_bestBidPrice = null;
		_bestAskPrice = null;
		_isRunning = false;

		CancelProtectionOrders();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_isRunning = true;

		StartProtection();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		SubscribeTrades()
			.Bind(ProcessTrade)
			.Start();

		LogAccountSnapshot("Strategy started");
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_isRunning = false;
		CancelProtectionOrders();
		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade == null)
			return;

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
			return;

		var price = trade.Trade.Price;
		_lastTradePrice = price;

		if (trade.Order.Side == Sides.Buy)
		{
			var remaining = volume;

			if (_shortVolume > 0m)
			{
				var closing = Math.Min(remaining, _shortVolume);
				_shortVolume -= closing;
				remaining -= closing;

				if (_shortVolume <= 0m)
				{
					_shortVolume = 0m;
					_shortAveragePrice = 0m;
					CancelOrder(ref _shortStopOrder);
					CancelOrder(ref _shortTakeOrder);
				}
			}

			if (remaining > 0m)
			{
				var newVolume = _longVolume + remaining;
				_longAveragePrice = newVolume > 0m
					? (_longAveragePrice * _longVolume + price * remaining) / newVolume
					: 0m;
				_longVolume = newVolume;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			var remaining = volume;

			if (_longVolume > 0m)
			{
				var closing = Math.Min(remaining, _longVolume);
				_longVolume -= closing;
				remaining -= closing;

				if (_longVolume <= 0m)
				{
					_longVolume = 0m;
					_longAveragePrice = 0m;
					CancelOrder(ref _longStopOrder);
					CancelOrder(ref _longTakeOrder);
				}
			}

			if (remaining > 0m)
			{
				var newVolume = _shortVolume + remaining;
				_shortAveragePrice = newVolume > 0m
					? (_shortAveragePrice * _shortVolume + price * remaining) / newVolume
					: 0m;
				_shortVolume = newVolume;
			}
		}

		UpdateFloatingProfit();
	}

	/// <summary>
	/// Update the latest trade price so the floating PnL stays accurate.
	/// </summary>
	private void ProcessTrade(ExecutionMessage trade)
	{
		if (trade.TradePrice is not decimal price)
			return;

		_lastTradePrice = price;
		UpdateFloatingProfit();
	}

	/// <summary>
	/// Capture best bid/ask quotes for price estimation.
	/// </summary>
	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid)
			_bestBidPrice = bid;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask)
			_bestAskPrice = ask;

		UpdateFloatingProfit();
	}

	private void ProcessBuyRequest()
	{
		try
		{
			if (!EnsureReady("open a buy position"))
				return;

			var volume = CalculateOrderVolume();
			if (volume <= 0m)
			{
				LogWarning("Buy request ignored because calculated volume is zero.");
				return;
			}

			BuyMarket(volume);
		}
		finally
		{
			_requestBuy.Value = false;
		}
	}

	private void ProcessSellRequest()
	{
		try
		{
			if (!EnsureReady("open a sell position"))
				return;

			var volume = CalculateOrderVolume();
			if (volume <= 0m)
			{
				LogWarning("Sell request ignored because calculated volume is zero.");
				return;
			}

			SellMarket(volume);
		}
		finally
		{
			_requestSell.Value = false;
		}
	}

	private void ProcessCloseBuys()
	{
		try
		{
			if (!EnsureReady("close buy positions"))
				return;

			var volume = _longVolume;
			if (volume > 0m)
				SellMarket(volume);

			CancelOrder(ref _longStopOrder);
			CancelOrder(ref _longTakeOrder);
		}
		finally
		{
			_closeBuys.Value = false;
		}
	}

	private void ProcessCloseSells()
	{
		try
		{
			if (!EnsureReady("close sell positions"))
				return;

			var volume = _shortVolume;
			if (volume > 0m)
				BuyMarket(volume);

			CancelOrder(ref _shortStopOrder);
			CancelOrder(ref _shortTakeOrder);
		}
		finally
		{
			_closeSells.Value = false;
		}
	}

	private void ProcessCloseAll()
	{
		try
		{
			if (!EnsureReady("close all positions"))
				return;

			if (_longVolume > 0m)
				SellMarket(_longVolume);

			if (_shortVolume > 0m)
				BuyMarket(_shortVolume);

			CancelProtectionOrders();
		}
		finally
		{
			_closeAll.Value = false;
		}
	}

	private void ProcessModifyStops()
	{
		try
		{
			if (!EnsureReady("update stops"))
				return;

			ApplyProtectionOrders();
		}
		finally
		{
			_modifyProtection.Value = false;
		}
	}

	private void ProcessSnapshotRequest()
	{
		try
		{
			LogAccountSnapshot("Snapshot requested");
		}
		finally
		{
			_snapshotRequest.Value = false;
		}
	}

	private decimal CalculateOrderVolume()
	{
		var baseVolume = FixedVolume > 0m ? FixedVolume : (Volume > 0m ? Volume : 1m);
		baseVolume = NormalizeVolume(baseVolume);

		if (!AutoMoneyManagement)
			return baseVolume;

		var riskFraction = RiskPerTenThousand / 10000m;
		if (riskFraction <= 0m)
			return baseVolume;

		var balance = GetPortfolioValue();
		if (balance <= 0m)
			return baseVolume;

		var stepPrice = Security?.StepPrice;
		if (stepPrice == null || stepPrice.Value <= 0m)
			return baseVolume;

		var stopPoints = StopLossPoints;
		if (stopPoints <= 0m)
			stopPoints = 1m;

		var riskAmount = balance * riskFraction;
		var lossPerVolume = stopPoints * stepPrice.Value;

		if (riskAmount <= 0m || lossPerVolume <= 0m)
			return baseVolume;

		var calculated = riskAmount / lossPerVolume;
		return NormalizeVolume(calculated);
	}

	private void ApplyProtectionOrders()
	{
		CancelProtectionOrders();

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 1m;

		if (_longVolume > 0m && _longAveragePrice > 0m)
		{
			var stopDistance = StopLossPoints * priceStep;
			var takeDistance = TakeProfitPoints * priceStep;

			if (stopDistance > 0m)
			{
				var stopPrice = Math.Max(priceStep, _longAveragePrice - stopDistance);
				_longStopOrder = SellStop(_longVolume, stopPrice);
			}

			if (takeDistance > 0m)
			{
				var takePrice = _longAveragePrice + takeDistance;
				_longTakeOrder = SellLimit(_longVolume, takePrice);
			}
		}

		if (_shortVolume > 0m && _shortAveragePrice > 0m)
		{
			var stopDistance = StopLossPoints * priceStep;
			var takeDistance = TakeProfitPoints * priceStep;

			if (stopDistance > 0m)
			{
				var stopPrice = _shortAveragePrice + stopDistance;
				_shortStopOrder = BuyStop(_shortVolume, stopPrice);
			}

			if (takeDistance > 0m)
			{
				var takePrice = Math.Max(priceStep, _shortAveragePrice - takeDistance);
				_shortTakeOrder = BuyLimit(_shortVolume, takePrice);
			}
		}

		LogInfo("Protective orders updated.");
	}

	private void CancelProtectionOrders()
	{
		CancelOrder(ref _longStopOrder);
		CancelOrder(ref _longTakeOrder);
		CancelOrder(ref _shortStopOrder);
		CancelOrder(ref _shortTakeOrder);
	}

	private void CancelOrder(ref Order? order)
	{
		if (order != null && order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	private void UpdateFloatingProfit()
	{
		var marketPrice = GetMarketPrice();
		if (marketPrice == null)
			return;

		var stepPrice = Security?.StepPrice ?? 0m;
		var priceStep = Security?.PriceStep ?? 0m;
		if (stepPrice <= 0m || priceStep <= 0m)
		{
			// Fallback to plain price difference if contract meta is unavailable.
			stepPrice = 1m;
			priceStep = 1m;
		}

		var moneyPerPoint = stepPrice / priceStep;

		if (_longVolume > 0m)
			_floatingBuyProfit = (marketPrice.Value - _longAveragePrice) * moneyPerPoint * _longVolume;
		else
			_floatingBuyProfit = 0m;

		if (_shortVolume > 0m)
			_floatingSellProfit = (_shortAveragePrice - marketPrice.Value) * moneyPerPoint * _shortVolume;
		else
			_floatingSellProfit = 0m;
	}

	private decimal? GetMarketPrice()
	{
		if (_lastTradePrice != null)
			return _lastTradePrice;

		if (_bestBidPrice != null && _bestAskPrice != null)
			return (_bestBidPrice.Value + _bestAskPrice.Value) / 2m;

		return _bestBidPrice ?? _bestAskPrice;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep;
		if (step != null && step.Value > 0m)
			volume = Math.Round(volume / step.Value, MidpointRounding.AwayFromZero) * step.Value;

		var minVolume = Security?.VolumeMin;
		if (minVolume != null && minVolume.Value > 0m && volume < minVolume.Value)
			volume = minVolume.Value;

		var maxVolume = Security?.VolumeMax;
		if (maxVolume != null && maxVolume.Value > 0m && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal GetPortfolioValue()
	{
		var current = Portfolio?.CurrentValue;
		if (current != null && current.Value > 0m)
			return current.Value;

		var begin = Portfolio?.BeginValue;
		if (begin != null && begin.Value > 0m)
			return begin.Value;

		return 0m;
	}

	private bool EnsureReady(string action)
	{
		if (!_isRunning || ProcessState != ProcessStates.Started)
		{
			LogWarning($"Cannot {action} because the strategy is not running.");
			return false;
		}

		if (Portfolio == null)
		{
			LogWarning($"Cannot {action} because portfolio is not assigned.");
			return false;
		}

		if (Security == null)
		{
			LogWarning($"Cannot {action} because security is not assigned.");
			return false;
		}

		return true;
	}

	private void LogAccountSnapshot(string reason)
	{
		var balance = GetPortfolioValue();
		var equity = Portfolio?.CurrentValue ?? balance;
		var totalProfit = _floatingBuyProfit + _floatingSellProfit;

		LogInfo($"{reason}: Balance={balance:0.##}, Equity={equity:0.##}, LongVolume={_longVolume:0.####}, " +
			$"LongProfit={_floatingBuyProfit:0.##}, ShortVolume={_shortVolume:0.####}, ShortProfit={_floatingSellProfit:0.##}, " +
			$"TotalProfit={totalProfit:0.##}");
	}
}
