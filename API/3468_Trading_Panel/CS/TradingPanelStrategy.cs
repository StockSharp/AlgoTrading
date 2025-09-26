using System;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manual trading helper converted from the MQL Trading Panel expert advisor.
/// Provides Buy/Sell entry methods with configurable volume and protective distances.
/// </summary>
public class TradingPanelStrategy : Strategy
{
	private readonly StrategyParam<int> _tradeCount;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _volumePerTrade;
	private readonly StrategyParam<Security> _targetSecurity;

	private Order _longStopOrder;
	private Order _longTargetOrder;
	private Order _shortStopOrder;
	private Order _shortTargetOrder;

	/// <summary>
	/// Number of market orders sent whenever a panel action is executed.
	/// </summary>
	public int TradeCount
	{
		get => _tradeCount.Value;
		set => _tradeCount.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Volume used for each individual market order.
	/// </summary>
	public decimal VolumePerTrade
	{
		get => _volumePerTrade.Value;
		set => _volumePerTrade.Value = value;
	}

	/// <summary>
	/// Optional override for the traded security.
	/// When null the strategy uses <see cref="Strategy.Security"/>.
	/// </summary>
	public Security TargetSecurity
	{
		get => _targetSecurity.Value;
		set => _targetSecurity.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults matching the MQL panel.
	/// </summary>
	public TradingPanelStrategy()
	{
		_tradeCount = Param(nameof(TradeCount), 1)
			.SetDisplay("Trades", "Number of market orders per action", "General")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 2m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
			.SetDisplay("Take Profit (pips)", "Protective target distance", "Risk")
			.SetCanOptimize(true);

		_volumePerTrade = Param(nameof(VolumePerTrade), 0.01m)
			.SetDisplay("Volume", "Volume for each submitted order", "Execution")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_targetSecurity = Param<Security>(nameof(TargetSecurity))
			.SetDisplay("Panel Security", "Security used for panel actions", "Execution");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Enable built-in position protection to guard against stale open positions after restarts.
		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time)
	{
		base.OnStopped(time);

		// Cancel any leftover protective orders when the strategy stops.
		CancelLongProtection();
		CancelShortProtection();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longStopOrder = null;
		_longTargetOrder = null;
		_shortStopOrder = null;
		_shortTargetOrder = null;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		// Drop references once a protective order is no longer active.
		if (_longStopOrder != null && ReferenceEquals(order, _longStopOrder) && !order.State.IsActive())
			_longStopOrder = null;

		if (_longTargetOrder != null && ReferenceEquals(order, _longTargetOrder) && !order.State.IsActive())
			_longTargetOrder = null;

		if (_shortStopOrder != null && ReferenceEquals(order, _shortStopOrder) && !order.State.IsActive())
			_shortStopOrder = null;

		if (_shortTargetOrder != null && ReferenceEquals(order, _shortTargetOrder) && !order.State.IsActive())
			_shortTargetOrder = null;
	}

	/// <summary>
	/// Submit buy orders according to the configured panel settings.
	/// </summary>
	public void PlaceBuyOrders()
	{
		PlaceOrders(Sides.Buy);
	}

	/// <summary>
	/// Submit sell orders according to the configured panel settings.
	/// </summary>
	public void PlaceSellOrders()
	{
		PlaceOrders(Sides.Sell);
	}

	private void PlaceOrders(Sides side)
	{
		var security = ResolveSecurity();
		if (security == null)
		{
			LogError("Security is not set. Assign Strategy.Security or TargetSecurity before trading.");
			return;
		}

		var pipSize = ResolvePipSize(security);
		if (pipSize <= 0m)
		{
			LogError($"Unable to resolve pip size for {security.Id}.");
			return;
		}

		var referencePrice = GetReferencePrice(security, side);
		if (referencePrice <= 0m)
		{
			LogError($"Reference price is unavailable for {security.Id}. Wait for market data updates.");
			return;
		}

		var desiredVolume = Math.Max(VolumePerTrade, 0m) * Math.Max(TradeCount, 0);
		if (desiredVolume <= 0m)
		{
			LogError("Panel volume or trade count must be positive.");
			return;
		}

		var normalizedVolume = AlignVolume(security, desiredVolume);
		if (normalizedVolume <= 0m)
		{
			LogError("Calculated volume is zero after applying security limits.");
			return;
		}

		var executionVolume = normalizedVolume;
		var position = Position;
		if (side == Sides.Buy && position < 0m)
			executionVolume += Math.Abs(position);
		else if (side == Sides.Sell && position > 0m)
			executionVolume += Math.Abs(position);

		if (executionVolume <= 0m)
		{
			LogInfo("No orders are required because the position already matches the desired direction.");
			return;
		}

		if (side == Sides.Buy)
			CancelShortProtection();
		else
			CancelLongProtection();

		if (side == Sides.Buy)
		{
			BuyMarket(executionVolume, security);
			LogInfo($"Submitted buy market order for {executionVolume} at ~{referencePrice:F5}.");

			ApplyProtection(side, security, normalizedVolume, referencePrice, pipSize);
		}
		else
		{
			SellMarket(executionVolume, security);
			LogInfo($"Submitted sell market order for {executionVolume} at ~{referencePrice:F5}.");

			ApplyProtection(side, security, normalizedVolume, referencePrice, pipSize);
		}
	}

	private void ApplyProtection(Sides side, Security security, decimal volume, decimal referencePrice, decimal pipSize)
	{
		var stopPrice = CalculatePrice(security, referencePrice, pipSize, StopLossPips, side, isTakeProfit: false);
		var takePrice = CalculatePrice(security, referencePrice, pipSize, TakeProfitPips, side, isTakeProfit: true);

		if (side == Sides.Buy)
		{
			if (stopPrice > 0m)
				_longStopOrder = SellStop(volume, stopPrice, security);

			if (takePrice > 0m)
				_longTargetOrder = SellLimit(volume, takePrice, security);
		}
		else
		{
			if (stopPrice > 0m)
				_shortStopOrder = BuyStop(volume, stopPrice, security);

			if (takePrice > 0m)
				_shortTargetOrder = BuyLimit(volume, takePrice, security);
		}
	}

	private void CancelLongProtection()
	{
		CancelOrderSafe(ref _longStopOrder);
		CancelOrderSafe(ref _longTargetOrder);
	}

	private void CancelShortProtection()
	{
		CancelOrderSafe(ref _shortStopOrder);
		CancelOrderSafe(ref _shortTargetOrder);
	}

	private void CancelOrderSafe(ref Order order)
	{
		if (order != null && order.State.IsActive())
			CancelOrder(order);

		order = null;
	}

	private Security ResolveSecurity()
	{
		return TargetSecurity ?? Security;
	}

	private static decimal AlignVolume(Security security, decimal volume)
	{
		var min = security.MinVolume ?? 0m;
		var max = security.MaxVolume ?? decimal.MaxValue;
		var step = security.VolumeStep ?? 0m;

		if (max > 0m && volume > max)
			volume = max;

		if (step > 0m)
		{
			var offset = min > 0m ? min : 0m;
			var steps = Math.Floor((volume - offset) / step);
			volume = offset + steps * step;
		}

		if (min > 0m && volume < min)
			volume = min;

		return Math.Max(volume, 0m);
	}

	private static decimal ResolvePipSize(Security security)
	{
		var step = security.PriceStep ?? 0.0001m;
		var decimals = security.Decimals;
		if (decimals >= 3)
			return step * 10m;

		return step > 0m ? step : 0.0001m;
	}

	private static decimal GetReferencePrice(Security security, Sides side)
	{
		decimal price;
		if (side == Sides.Buy)
			price = security.BestAsk?.Price ?? security.LastTrade?.Price ?? 0m;
		else
			price = security.BestBid?.Price ?? security.LastTrade?.Price ?? 0m;

		return price > 0m ? security.ShrinkPrice(price) : 0m;
	}

	private static decimal CalculatePrice(Security security, decimal referencePrice, decimal pipSize, decimal distance, Sides side, bool isTakeProfit)
	{
		if (referencePrice <= 0m || pipSize <= 0m || distance <= 0m)
			return 0m;

		var offset = pipSize * distance;
		decimal raw;

		if (side == Sides.Buy)
			raw = isTakeProfit ? referencePrice + offset : referencePrice - offset;
		else
			raw = isTakeProfit ? referencePrice - offset : referencePrice + offset;

		if (raw <= 0m)
			return 0m;

		return security.ShrinkPrice(raw);
	}
}
